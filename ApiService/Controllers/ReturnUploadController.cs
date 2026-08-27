using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Http;
using ApiService;

namespace ApiService.Controllers
{
    public class ReturnUploadController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        public ReturnUploadController()
        {
            _apiServerService = new ApiServerController();
        }

        // Helper: อ่าน Physical Path จาก Web.config
        private static string GetConfigPath(string key)
        {
            string path = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException(
                    $"AppSettings key '{key}' is missing or empty in Web.config");

            // ให้แน่ใจว่ามี trailing backslash เสมอ
            return path.TrimEnd('\\', '/') + @"\";
        }

        // POST Return/Image/Upload
        [HttpPost]
        [Route("Return/Image/Upload")]
        public IHttpActionResult UploadImage()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;
                var form = httpRequest.Form;
                var files = httpRequest.Files;

                if (files.Count == 0)
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "No files uploaded"
                    });

                var connSetting =
                    ConfigurationManager.ConnectionStrings["ClaimTest_ConnectionString"];

                if (connSetting == null)
                    return Content(HttpStatusCode.InternalServerError, new
                    {
                        success = false,
                        message = "ClaimTest_ConnectionString not found in Web.config"
                    });

                string imgFolder = GetConfigPath("ReturnImagePath");
                // ตรวจว่า UNC Path เข้าถึงได้จริง
                if (!Directory.Exists(imgFolder))
                    return Content(HttpStatusCode.InternalServerError, new
                    {
                        success = false,
                        message = $"Image folder not accessible: {imgFolder}"
                    });

                int countPass = 0;
                int countError = 0;
                string message = string.Empty;

                using (var connection = new SqlConnection(connSetting.ConnectionString))
                {
                    connection.Open();

                    for (int i = 0; i < files.Count; i++)
                    {
                        string inCim_NoSub = form["inCim_NoSub"];
                        string No = form["No"];
                        string uname = string.Empty;
                        bool allowSave = true;

                        // 1) เรียก SP เพื่อให้ DB สร้างชื่อไฟล์
                        using (var command = new SqlCommand("P_Save_PathImageRT_Sales", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandTimeout = 0;
                            command.Parameters.AddWithValue("@inim_name", "");
                            command.Parameters.AddWithValue("@inCim_NoSub", inCim_NoSub);
                            command.Parameters.AddWithValue("@inCim_No", No);
                            command.Parameters.AddWithValue("@inIm_No", "");

                            var output = new SqlParameter("@outimagename", SqlDbType.VarChar, 100)
                            {
                                Direction = ParameterDirection.Output
                            };
                            command.Parameters.Add(output);
                            command.ExecuteNonQuery();

                            uname = output.Value == null || output.Value == DBNull.Value
                                ? ""
                                : output.Value.ToString();

                            if (string.IsNullOrWhiteSpace(uname))
                            {
                                using (var findCmd = new SqlCommand(@"
                                    SELECT TOP 1 IMAGE_NAME
                                    FROM PathImage_RT
                                    WHERE STMP_ID = @STMP_ID
                                      AND STMP_ID_SUB = @STMP_ID_SUB
                                    ORDER BY IMAGE_ID DESC", connection))
                                {
                                    findCmd.Parameters.AddWithValue("@STMP_ID", No);
                                    findCmd.Parameters.AddWithValue("@STMP_ID_SUB", inCim_NoSub);

                                    uname = findCmd.ExecuteScalar()?.ToString();
                                }
                            }

                            if (string.IsNullOrWhiteSpace(uname))
                            {
                                throw new Exception("ไม่พบ IMAGE_NAME ใน PathImage_RT");
                            }
                        }

                        // 2) Save ไฟล์ลง UNC Path เดิม
                        var file = files[i];
                        string fullPath = Path.Combine(imgFolder, uname);

                        file.SaveAs(fullPath);

                        // 3) Resize ถ้ารูปเกิน 1 MB (Business Logic เดิม)
                        int byteCount = file.ContentLength;
                        if (file.ContentType != null && file.ContentType.Contains("image"))
                        {
                            if (byteCount > 1048576) // > 1 MB
                            {
                                try
                                {
                                    var img = new System.Web.Helpers.WebImage(fullPath);
                                    if (img.Width > 1000)
                                    {
                                        img.Resize(1024, 768);
                                        var fi = new FileInfo(fullPath);
                                        if (fi.Length > 1048576)
                                            img.Resize(800, 600);
                                        img.Save(fullPath, "png", true);
                                    }
                                }
                                catch (Exception resizeEx)
                                {
                                    allowSave = false;
                                    countError++;

                                    // ลบไฟล์ที่ save ไปแล้ว
                                    if (File.Exists(fullPath))
                                        File.Delete(fullPath);

                                    // Rollback DB
                                    using (var delCmd = new SqlCommand("P_Delete_PathImageRT", connection))
                                    {
                                        delCmd.CommandType = CommandType.StoredProcedure;
                                        delCmd.Parameters.AddWithValue("@imageName", uname);
                                        delCmd.ExecuteNonQuery();
                                    }

                                    message = resizeEx.Message;
                                    continue; // ข้ามไฟล์นี้
                                }
                            }
                        }

                        if (allowSave)
                        {
                            countPass++;
                            message = "true";
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    message,
                    alertmessage = $"{countPass} Files uploaded!\n{countError} Unable upload file!"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.ToString()
                });
            }
        }

        // POST Return/Image/LegacyUpload
        [HttpPost]
        [Route("Return/Image/LegacyUpload")]
        public IHttpActionResult Upload()
        {
            string fileName = string.Empty;
            string imgName = string.Empty;
            string message = string.Empty;

            try
            {
                var httpRequest = HttpContext.Current.Request;
                var files = httpRequest.Files;

                if (files == null || files.Count == 0)
                {
                    return Content(
                        HttpStatusCode.BadRequest,
                        new
                        {
                            success = false,
                            message = "No files uploaded"
                        });
                }

                string imgFolder = GetConfigPath("ReturnImagePath");

                if (!Directory.Exists(imgFolder))
                {
                    return Content(
                        HttpStatusCode.InternalServerError,
                        new
                        {
                            success = false,
                            message = $"Image folder not accessible: {imgFolder}"
                        });
                }

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];

                    if (file == null || file.ContentLength == 0)
                        continue;

                    // Logic เดิมของ Upload():
                    // เอาชื่อไฟล์เดิมมาต่อ .png
                    fileName = Path.GetFileName(file.FileName);
                    imgName = fileName + ".png";

                    string fullPath = Path.Combine(
                        imgFolder,
                        imgName
                    );

                    file.SaveAs(fullPath);

                    message = "true";
                }

                return Ok(new
                {
                    success = true,
                    fileName,
                    imgname = imgName,
                    message
                });
            }
            catch (Exception ex)
            {
                return Content(
                    HttpStatusCode.InternalServerError,
                    new
                    {
                        success = false,
                        message = ex.Message,
                        detail = ex.ToString()
                    });
            }
        }

        // POST Return/Image/SavePath
        [HttpPost]
        [Route("Return/Image/SavePath")]
        public IHttpActionResult SavePathImagetemp(
            string im_name,
            string Cim_No,
            string Im_No,
            string inCim_NoSub)
        {
            string message = string.Empty;

            try
            {
                var connSetting =
                    ConfigurationManager.ConnectionStrings["ClaimTest_ConnectionString"];

                if (connSetting == null)
                {
                    return Content(
                        HttpStatusCode.InternalServerError,
                        new
                        {
                            success = false,
                            message = "ClaimTest_ConnectionString not found in Web.config"
                        });
                }

                using (var connection =
                       new SqlConnection(connSetting.ConnectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("P_Save_PathImage_RT", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 60;

                        command.Parameters.AddWithValue("@inim_name", im_name ?? "");
                        command.Parameters.AddWithValue("@inCim_No", Cim_No ?? "");
                        command.Parameters.AddWithValue("@inCim_NoSub", inCim_NoSub ?? "");
                        command.Parameters.AddWithValue("@inIm_No", Im_No ?? "");

                        command.ExecuteNonQuery();

                        return Ok(new
                        {
                            success = true,
                            message = "true",
                            imgname = im_name
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Content(
                    HttpStatusCode.InternalServerError,
                    new
                    {
                        success = false,
                        message = ex.Message,
                        detail = ex.ToString()
                    });
            }
        }

        // POST Return/Image/Delete
        [HttpPost]
        [Route("Return/Image/Delete")]
        public IHttpActionResult DeleteImage(
            string comid, string clmnoup, string clmidimg, string absPath)
        {
            string message = string.Empty;
            var connectionString =
                ConfigurationManager.ConnectionStrings["ClaimTest_ConnectionString"].ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    using (var command = new SqlCommand("P_Delimage_RT", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@DOC", comid);
                        command.Parameters.AddWithValue("@SUBDOC", clmnoup);
                        command.Parameters.AddWithValue("@CLMIMAGENO", clmidimg);
                        command.ExecuteNonQuery();
                    }

                    message = "true";

                    // ลบไฟล์จาก UNC Path เดิม
                    string imgFolder = GetConfigPath("ReturnImagePath");
                    string filePath = Path.Combine(imgFolder, absPath);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch (Exception exc)
                {
                    return Content(HttpStatusCode.InternalServerError, new
                    {
                        success = false,
                        message = exc.Message,
                        detail = exc.ToString()
                    });
                }
            }

            return Ok(new { message });
        }

        // POST Return/Video/Upload
        [HttpPost]
        [Route("Return/Video/Upload")]
        public IHttpActionResult UploadVideo()
        {
            string fileName = string.Empty;
            string message = string.Empty;
            int size = 0;

            try
            {
                string videoFolder = GetConfigPath("ReturnVideoPath");
                if (!Directory.Exists(videoFolder))
                    return Content(HttpStatusCode.InternalServerError, new
                    {
                        success = false,
                        message = $"Video folder not accessible: {videoFolder}"
                    });

                var httpRequest = HttpContext.Current.Request;

                for (int i = 0; i < httpRequest.Files.Count; i++)
                {
                    var file = httpRequest.Files[i];
                    fileName = Path.GetFileName(file.FileName);
                    size = file.ContentLength / 1000;

                    string fullPath = Path.Combine(videoFolder, fileName + ".mp4");
                    file.SaveAs(fullPath);
                }

                message = "true";
            }
            catch (Exception exc)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = exc.Message,
                    detail = exc.ToString()
                });
            }

            return Ok(new { fileName, message, Size = size });
        }

        // POST Return/Video/Save
        [HttpPost]
        [Route("Return/Video/Save")]
        public IHttpActionResult Savefilevideo(
            [FromUri] string aj_CLM_NO_SUB,
            [FromUri] string aj_CLM_NO,
            [FromUri] string im_name,
            [FromUri] string Size,
            [FromUri] string Im_No)
        {
            string fileName = im_name;
            string message = string.Empty;

            try
            {
                var CS = ConfigurationManager
                    .ConnectionStrings["ClaimTest_ConnectionString"].ConnectionString;

                using (var con = new SqlConnection(CS))
                {
                    con.Open();

                    using (var cmd = new SqlCommand("spAddNewVideoFile_RT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@inCim_No", aj_CLM_NO);
                        cmd.Parameters.AddWithValue("@inCim_NoSub", aj_CLM_NO_SUB);
                        cmd.Parameters.AddWithValue("@Name", im_name);
                        cmd.Parameters.AddWithValue("@FileSize", Size);
                        cmd.Parameters.AddWithValue("@inImg_ID", Im_No);
                        cmd.Parameters.AddWithValue(
                            "FilePath",
                            "~/VideoFileUploadRT/" + im_name + ".mp4"
                        );

                        cmd.ExecuteNonQuery();
                    }
                }

                message = "true";
            }
            catch (Exception exc)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = exc.Message,
                    detail = exc.ToString()
                });
            }

            return Ok(new
            {
                fileName,
                message
            });
        }

        // POST Return/Video/Delete
        [HttpPost]
        [Route("Return/Video/Delete")]
        public IHttpActionResult DeleteVideo(
            string comid, string clmnoup, string clmidimg, string absPath, string Im_No)
        {
            string message = string.Empty;
            var connectionString =
                ConfigurationManager.ConnectionStrings["ClaimTest_ConnectionString"].ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    using (var command = new SqlCommand("P_Delvideo_RT", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@DOC", comid);
                        command.Parameters.AddWithValue("@SUBDOC", clmnoup);
                        command.Parameters.AddWithValue("@inImg_ID", Im_No);
                        command.ExecuteNonQuery();
                    }

                    message = "true";

                    // ลบไฟล์จาก UNC Path เดิม
                    string videoFolder = GetConfigPath("ReturnVideoPath");
                    string filePath = Path.Combine(videoFolder, absPath);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch (Exception exc)
                {
                    return Content(HttpStatusCode.InternalServerError, new
                    {
                        success = false,
                        message = exc.Message,
                        detail = exc.ToString()
                    });
                }
            }

            return Ok(new { message });
        }

        // GET ReturnUpload/Test
        [HttpGet]
        [Route("ReturnUpload/Test")]
        public IHttpActionResult Test()
        {
            // ทดสอบว่า config และ path ถูกต้อง
            try
            {
                string imgPath = GetConfigPath("ReturnImagePath");
                string videoPath = GetConfigPath("ReturnVideoPath");
                bool imgOk = Directory.Exists(imgPath);
                bool videoOk = Directory.Exists(videoPath);

                return Ok(new
                {
                    controller = "ReturnUploadController OK",
                    imagePath = imgPath,
                    imageExists = imgOk,
                    videoPath,
                    videoExists = videoOk
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // POST Return/Image/GetPath
        [HttpPost]
        [Route("Return/Image/GetPath")]
        public IHttpActionResult GetPathImageRT(
            [FromUri] string inCLM_ID,
            [FromUri] string CLM_NO)
        {
            try
            {
                var connSetting =
                    ConfigurationManager.ConnectionStrings["ClaimTest_ConnectionString"];

                if (connSetting == null)
                {
                    return Content(
                        HttpStatusCode.InternalServerError,
                        new
                        {
                            success = false,
                            message = "ClaimTest_ConnectionString not found in Web.config"
                        });
                }

                var getdata = new System.Collections.Generic.List<object>();

                using (var connection =
                       new SqlConnection(connSetting.ConnectionString))
                using (var command =
                       new SqlCommand("P_GetPathImage_RT", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 60;

                    command.Parameters.AddWithValue(
                        "@inCim_No",
                        inCLM_ID ?? "");

                    command.Parameters.AddWithValue(
                        "@inCim_NoSub",
                        CLM_NO ?? "");

                    connection.Open();

                    using (var dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            getdata.Add(new
                            {
                                val = new
                                {
                                    IMAGE_ID = dr["IMAGE_ID"].ToString(),
                                    REQ_NO = dr["STMP_ID"].ToString(),
                                    CLM_NO_SUB = dr["STMP_ID_SUB"].ToString(),
                                    IMAGE_NO = dr["IMAGE_NO"].ToString(),
                                    IMAGE_NAME = dr["IMAGE_NAME"].ToString(),

                                    // RT ใช้ path แบบเดิม
                                    PATH = Path.Combine(
                                        @"..\ImgUploadRT\",
                                        dr["IMAGE_NAME"].ToString())
                                }
                            });
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    Getdata = getdata
                });
            }
            catch (Exception ex)
            {
                return Content(
                    HttpStatusCode.InternalServerError,
                    new
                    {
                        success = false,
                        message = ex.Message,
                        detail = ex.ToString()
                    });
            }
        }

        [HttpGet]
        [Route("Return/Video/Get")]
        public IHttpActionResult GetfileVideoRT(
            [FromUri] string inCLM_ID,
            [FromUri] string CLM_NO,
            [FromUri] string Im_No)
        {
            try
            {
                var connSetting =
                    ConfigurationManager.ConnectionStrings["ClaimTest_ConnectionString"];

                if (connSetting == null)
                {
                    return Content(
                        HttpStatusCode.InternalServerError,
                        new
                        {
                            success = false,
                            message = "ClaimTest_ConnectionString not found in Web.config"
                        });
                }

                var videolist = new System.Collections.Generic.List<object>();

                using (var connection =
                       new SqlConnection(connSetting.ConnectionString))
                using (var command =
                       new SqlCommand("spGetAllVideoFile_RT", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 60;

                    command.Parameters.AddWithValue(
                        "@inCim_No",
                        inCLM_ID ?? "");

                    command.Parameters.AddWithValue(
                        "@inCim_NoSub",
                        CLM_NO ?? "");

                    command.Parameters.AddWithValue(
                        "@inImg_ID",
                        Im_No ?? "");

                    connection.Open();

                    using (var rdr = command.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            videolist.Add(new
                            {
                                ID = Convert.ToInt32(rdr["ID"]),
                                Name = rdr["Name"].ToString(),
                                FileSize = Convert.ToInt32(rdr["FileSize"]),

                                FilePath = Path.Combine(
                                    @"..\VideoFileUploadRT\",
                                    rdr["Name"].ToString())
                            });
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    videolist
                });
            }
            catch (Exception ex)
            {
                return Content(
                    HttpStatusCode.InternalServerError,
                    new
                    {
                        success = false,
                        message = ex.Message,
                        detail = ex.ToString()
                    });
            }
        }
    }
}