using ApiService.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.DirectoryServices;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using RouteAttribute = System.Web.Http.RouteAttribute;

namespace ApiService.Controllers
{
    public class AuthenEcatalogController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        public AuthenEcatalogController()
        {
            _apiServerService = new ApiServerController();
        }

        [HttpGet]
        [Route("Ecatalog/UserAuthen")]
        [ApiKeyAuthorize]
        public IHttpActionResult UserAuthen(string Username, string Password,
            string Latitude = "", string Longitude = "", string UserAgent = "")
        {
            string errorMessage = "Success";
            string authSource = "";

            string getStatus = "";
            string getUsername = "";
            string getUserType = "";
            string getEmail = "";
            string getSlmcode = "";
            string getCuscode = "";
            string getIsActive = "";

            string adFullname = "";
            string adDepartment = "";
            bool adVerified = false;

            var connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;

            if (string.IsNullOrWhiteSpace(Username))
                errorMessage = "Username not null";
            else if (string.IsNullOrWhiteSpace(Password))
                errorMessage = "Password not null";

            if (errorMessage == "Success")
            {
                // STEP 1: AD
                try
                {
                    string ldapPath = ConfigurationManager.AppSettings["LdapPath"]
                                      ?? "LDAP://ADSRV2016-01/dc=Automotive,dc=com";

                    DirectoryEntry dirEntry = new DirectoryEntry(ldapPath, Username, Password);
                    DirectorySearcher searcher = new DirectorySearcher(dirEntry)
                    {
                        Filter = "(SAMAccountName=" + Username + ")"
                    };

                    SearchResult adResult = searcher.FindOne();
                    if (adResult != null)
                    {
                        DirectoryEntry userEntry = adResult.GetDirectoryEntry();
                        adFullname = userEntry.Properties["Name"]?.Value?.ToString() ?? "";
                        adDepartment = userEntry.Properties["Department"]?.Value?.ToString() ?? "";
                        adVerified = true;
                        authSource = "AD";
                    }
                }
                catch
                {
                    adVerified = false;
                }

                // STEP 2: DB
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (SqlCommand command = new SqlCommand("P_Ecatalog_Authen", conn))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@inUsername", Username);
                            command.Parameters.AddWithValue("@inPassword", adVerified ? "" : Password);
                            command.Parameters.AddWithValue("@inSkipPasswordCheck", adVerified ? "Y" : "N");

                            using (SqlDataReader dr = command.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    getStatus = dr["Status"].ToString();
                                    getUsername = dr["Username"].ToString();
                                    getUserType = dr["UserType"].ToString();
                                    getEmail = dr["Email"].ToString();
                                    getSlmcode = dr["Slmcode"].ToString();
                                    getCuscode = dr["Cuscode"].ToString();
                                    getIsActive = dr["IsActive"].ToString();
                                }
                            }

                            if (adVerified)
                            {
                                if (!string.IsNullOrEmpty(getIsActive) && getIsActive != "Y")
                                    errorMessage = "บัญชีผู้ใช้ถูกระงับการใช้งาน";
                                else
                                    authSource = "AD";
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(getIsActive) || getIsActive != "Y" || getStatus != "Y")
                                    errorMessage = "Username หรือ Password ไม่ถูกต้อง";
                                else
                                    authSource = "DB";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }
            }

            DataRespond dataRes = new DataRespond();
            dataRes.statusCode = 200;
            dataRes.errorMessage = errorMessage;
            dataRes.result = new List<resultAuthen>();

            if (errorMessage == "Success")
            {
                dataRes.result.Add(new resultAuthen
                {
                    verify = "True",
                    username = getUsername,
                    email = getEmail,
                    slmcode = getSlmcode,
                    cuscode = getCuscode,
                    userType = Convert.ToInt32(string.IsNullOrEmpty(getUserType) ? "0" : getUserType),
                    isActive = !string.IsNullOrEmpty(getIsActive) ? getIsActive : "Y",
                    authSource = authSource
                });
            }

            // LOG
            var jsonLog = JsonConvert.SerializeObject(new
            {
                Username,
                Password = "******",
                AuthSource = authSource
            });

            string jsonReturn = JsonConvert.SerializeObject(dataRes);
            string lastId = _apiServerService.SaveApiResponse("UserAuthenEcatalog", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn);

            // LOGIN LOG
            try
            {
                string ip = HttpContext.Current?.Request?.UserHostAddress ?? "";
                string ua = string.IsNullOrEmpty(UserAgent)
                            ? Request.Headers.UserAgent?.ToString() ?? ""
                            : UserAgent;
                string browser = "";
                string os = "";

                var match = System.Text.RegularExpressions.Regex.Match(ua,
                     @"(Chrome|Firefox|Safari|Edge|OPR|Trident)[/\s]([\d.]+)");
                if (match.Success)
                    browser = (match.Groups[1].Value == "OPR" ? "Opera" : match.Groups[1].Value)
                              + "/" + match.Groups[2].Value;
                else
                    browser = ua; // fallback
                if (ua.Contains("Windows NT 10")) os = "Windows 10";
                else if (ua.Contains("Windows NT 6.3")) os = "Windows 8.1";
                else if (ua.Contains("Windows NT 6.1")) os = "Windows 7";
                else if (ua.Contains("Mac OS X")) os = "macOS";
                else if (ua.Contains("Android")) os = "Android";
                else if (ua.Contains("iPhone")) os = "iOS";
                else if (ua.Contains("Linux")) os = "Linux";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("P_Ecatalog_LoginLog", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@UsrID", Username);
                        cmd.Parameters.AddWithValue("@UserType", getUserType);
                        cmd.Parameters.AddWithValue("@OS", os);
                        cmd.Parameters.AddWithValue("@Browser", browser);
                        cmd.Parameters.AddWithValue("@IpAddress", ip);
                        cmd.Parameters.AddWithValue("@Latitude", Latitude);
                        cmd.Parameters.AddWithValue("@Longitude", Longitude);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception exLog)
            {
                return Json(new { error = exLog.Message, stack = exLog.StackTrace });
            }

            return Json(dataRes);
        }

        public class DataRespond
        {
            public int statusCode { get; set; }
            public string errorMessage { get; set; }
            public List<resultAuthen> result { get; set; }
        }

        public class resultAuthen
        {
            public string verify { get; set; }
            public string username { get; set; }
            public string email { get; set; }
            public string slmcode { get; set; }
            public string cuscode { get; set; }
            public int userType { get; set; }
            public string isActive { get; set; }
            public string authSource { get; set; }
        }
    }
}