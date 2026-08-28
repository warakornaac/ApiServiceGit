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
    public class ClaimUploadController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        public ClaimUploadController()
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

        // POST Claim/Image/Upload
        [HttpPost]
        [Route("Claim/Image/Upload")]
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

                string imgFolder = GetConfigPath("ClaimImagePath");
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
                        using (var command = new SqlCommand("P_Save_PathImage", connection))
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

                            uname = output.Value?.ToString();
                            if (string.IsNullOrWhiteSpace(uname))
                                throw new Exception("P_Save_PathImage did not return @outimagename.");
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
                                    using (var delCmd = new SqlCommand("P_Delete_PathImage", connection))
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

        // POST Claim/Image/LegacyUpload
        [HttpPost]
        [Route("Claim/Image/LegacyUpload")]
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

                string imgFolder = GetConfigPath("ClaimImagePath");

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

        // POST Claim/Image/SavePath
        [HttpPost]
        [Route("Claim/Image/SavePath")]
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

                    using (var command =
                           new SqlCommand("P_Save_PathImage", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 60;

                        command.Parameters.AddWithValue(
                            "@inim_name",
                            im_name ?? "");

                        command.Parameters.AddWithValue(
                            "@inCim_No",
                            Cim_No ?? "");

                        command.Parameters.AddWithValue(
                            "@inCim_NoSub",
                            inCim_NoSub ?? "");

                        command.Parameters.AddWithValue(
                            "@inIm_No",
                            Im_No ?? "");

                        var output =
                            new SqlParameter(
                                "@outimagename",
                                SqlDbType.VarChar,
                                100);

                        output.Direction =
                            ParameterDirection.Output;

                        command.Parameters.Add(output);

                        command.ExecuteNonQuery();

                        string outputImageName =
                            output.Value == null ||
                            output.Value == DBNull.Value
                                ? ""
                                : output.Value.ToString();

                        message = "true";

                        return Ok(new
                        {
                            success = true,
                            message,
                            imgname = outputImageName
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

        // POST Claim/Image/Delete
        [HttpPost]
        [Route("Claim/Image/Delete")]
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

                    using (var command = new SqlCommand("P_Delimage", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@DOC", comid);
                        command.Parameters.AddWithValue("@SUBDOC", clmnoup);
                        command.Parameters.AddWithValue("@CLMIMAGENO", clmidimg);
                        command.ExecuteNonQuery();
                    }

                    message = "true";

                    // ลบไฟล์จาก UNC Path เดิม
                    string imgFolder = GetConfigPath("ClaimImagePath");
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

        // POST Claim/Video/Upload
        [HttpPost]
        [Route("Claim/Video/Upload")]
        public IHttpActionResult UploadVideo()
        {
            string fileName = string.Empty;
            string message = string.Empty;
            int size = 0;

            try
            {
                string videoFolder = GetConfigPath("ClaimVideoPath");
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

        // POST Claim/Video/Save
        [HttpPost]
        [Route("Claim/Video/Save")]
        public IHttpActionResult Savefilevideo(
        [FromUri] string aj_CLM_NO_SUB,
        [FromUri] string aj_CLM_NO,
        [FromUri] string im_name,
        [FromUri] string Size,
        [FromUri] string Im_No)
        {
            string fileName = string.Empty;
            string message = string.Empty;

            try
            {
                var CS = ConfigurationManager
                    .ConnectionStrings["ClaimTest_ConnectionString"].ConnectionString;

                using (var con = new SqlConnection(CS))
                {
                    con.Open();

                    using (var cmd = new SqlCommand("spAddNewVideoFile", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@inCim_No", aj_CLM_NO ?? "");
                        cmd.Parameters.AddWithValue("@inCim_NoSub", aj_CLM_NO_SUB ?? "");
                        cmd.Parameters.AddWithValue("@Name", im_name ?? "");
                        cmd.Parameters.AddWithValue("@FileSize", Size ?? "0");
                        cmd.Parameters.AddWithValue("@inImg_ID", Im_No ?? "");
                        cmd.Parameters.AddWithValue("FilePath",
                            "~/VideoFileUpload/" + im_name + ".mp4");
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

            return Ok(new { fileName, message });
        }

        // POST Claim/Video/Delete
        [HttpPost]
        [Route("Claim/Video/Delete")]
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

                    using (var command = new SqlCommand("P_Delvideo", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@DOC", comid);
                        command.Parameters.AddWithValue("@SUBDOC", clmnoup);
                        command.Parameters.AddWithValue("@inImg_ID", Im_No);
                        command.ExecuteNonQuery();
                    }

                    message = "true";

                    // ลบไฟล์จาก UNC Path เดิม
                    string videoFolder = GetConfigPath("ClaimVideoPath");
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

        // GET ClaimUpload/Test
        [HttpGet]
        [Route("ClaimUpload/Test")]
        public IHttpActionResult Test()
        {
            // ทดสอบว่า config และ path ถูกต้อง
            try
            {
                string imgPath = GetConfigPath("ClaimImagePath");
                string videoPath = GetConfigPath("ClaimVideoPath");
                bool imgOk = Directory.Exists(imgPath);
                bool videoOk = Directory.Exists(videoPath);

                return Ok(new
                {
                    controller = "ClaimUploadController OK",
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
    }
}