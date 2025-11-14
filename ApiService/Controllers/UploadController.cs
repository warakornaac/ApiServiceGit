using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace ApiService.Controllers
{
    public class UploadController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        public UploadController()
        {
            _apiServerService = new ApiServerController();
        }

        [HttpPost]
        [Route("Upload/File")]
        public async Task<IHttpActionResult> UploadFile()
        {
            var httpRequest = HttpContext.Current.Request;
            var targetPath = httpRequest.Form["targetPath"];
            var fileName = httpRequest.Form["fileName"];

            try
            {
                if (httpRequest.Files.Count == 0)
                    return Content(HttpStatusCode.BadRequest, new { success = false, message = "No files uploaded" });

                //ตรวจสอบว่า path ปลายทางถูกส่งมาหรือไม่
                string basePath = !string.IsNullOrEmpty(targetPath)
                    ? targetPath
                    : HttpContext.Current.Server.MapPath("~/App_Data/uploads");

                //ตรวจสอบสิทธิ์การเขียนใน folder
                var checkResult = CheckFolderWritePermission(basePath);
                if (!checkResult.Success)
                {
                    return Content(HttpStatusCode.Forbidden, new
                    {
                        success = false,
                        message = checkResult.Message
                    });
                }

                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                var uploadedFiles = new List<object>();
                var imageExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".heic" };
                var docExt = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip" };

                foreach (string fileKey in httpRequest.Files)
                {
                    var postedFile = httpRequest.Files[fileKey];
                    if (postedFile == null || postedFile.ContentLength == 0) continue;

                    var ext = Path.GetExtension(postedFile.FileName).ToLowerInvariant();
                    string subFolder = imageExt.Contains(ext) ? "images"
                                      : docExt.Contains(ext) ? "docs" : "others";

                    var savePath = Path.Combine(basePath);
                    if (!Directory.Exists(savePath))
                        Directory.CreateDirectory(savePath);

                    //ตั้งชื่อไฟล์
                    string newFileName = !string.IsNullOrEmpty(fileName)
                        ? $"{fileName}{ext}"
                        : $"{Guid.NewGuid()}{ext}";

                    var fullPath = Path.Combine(savePath, newFileName);
                    //postedFile.SaveAs(fullPath);
                    try
                    {
                        postedFile.SaveAs(fullPath);
                    }
                    catch (Exception ex)
                    {
                        return Content(HttpStatusCode.InternalServerError, new
                        {
                            success = false,
                            message = $"Cannot save file to {fullPath}",
                            error = ex.Message
                        });
                    }

                    uploadedFiles.Add(new
                    {
                        original = postedFile.FileName,
                        savedAs = newFileName,
                        fullPath = fullPath
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Files uploaded successfully",
                    files = uploadedFiles
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// ตรวจสอบสิทธิ์การเขียนไฟล์ในโฟลเดอร์ (รองรับ UNC path)
        /// </summary>
        private PermissionResult CheckFolderWritePermission(string folderPath)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath))
                    return new PermissionResult { Success = false, Message = "Folder path is null or empty" };

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string testFile = Path.Combine(folderPath, $"permission_test_{Guid.NewGuid()}.tmp");

                using (var fs = File.Create(testFile))
                {
                    byte[] info = new System.Text.UTF8Encoding(true).GetBytes("test");
                    fs.Write(info, 0, info.Length);
                }

                File.Delete(testFile);
                return new PermissionResult { Success = true, Message = "Folder is writable" };
            }
            catch (UnauthorizedAccessException)
            {
                return new PermissionResult { Success = false, Message = $"Access denied: The API user does not have write permission for folder '{folderPath}'" };
            }
            catch (Exception ex)
            {
                return new PermissionResult { Success = false, Message = $"Cannot access folder: {ex.Message}" };
            }
        }
        private class PermissionResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
    }
}
