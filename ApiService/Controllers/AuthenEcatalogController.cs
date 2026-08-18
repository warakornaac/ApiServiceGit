using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using RouteAttribute = System.Web.Http.RouteAttribute;
using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using ApiService.Filters;
using System.Net;
using System.DirectoryServices;

namespace ApiService.Controllers
{
    public class AuthenEcatalogController : ApiController
    {
        // GET: AuthenEcatalog
        private readonly ApiServerController _apiServerService;

        public AuthenEcatalogController() {
            _apiServerService = new ApiServerController();
        }
        [HttpGet]
        [Route("Ecatalog/UserAuthen")]
        [ApiKeyAuthorize]
        public IHttpActionResult UserAuthen(string Username, string Password) {
            string errorMessage = "Success";
            string authSource = ""; // "AD" หรือ "DB"

            // ── ตัวแปรรับข้อมูลจาก DB ──
            string getStatus = "";
            string getUsername = "";
            string getUserType = "";
            string getEmail = "";
            string getSlmcode = "";
            string getCuscode = "";
            string getIsActive = "";

            // ── ตัวแปรรับข้อมูลจาก AD ──
            string adFullname = "";
            string adDepartment = "";
            bool   adVerified = false;

            var connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;

            if (string.IsNullOrWhiteSpace(Username)) {
                errorMessage = "Username not null";
            }

            else if (string.IsNullOrWhiteSpace(Password)) {
                errorMessage = "Password not null";
            }

            if (errorMessage == "Success") {
                // STEP 1 : ลอง Authenticate ผ่าน AD ก่อน
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
                    // AD ล้มเหลว (ผิด password หรือ user ไม่มีใน AD) → ให้ไป STEP 2
                    adVerified = false;
                }

                // STEP 2 : ดึงข้อมูล User จาก Database
                // - ถ้าเจอใน AD  → เช็คแค่ว่า username มีใน DB (ไม่เช็ค password)
                // - ถ้าไม่เจอ AD → เช็ค username + password ใน DB ตามปกติ

                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString)) {
                        conn.Open();
                        using (SqlCommand command = new SqlCommand("P_Ecatalog_Authen", conn)) {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@inUsername", Username);
                            // ถ้า AD verified แล้ว ไม่ต้องตรวจ password ใน DB
                            // ส่ง password จริงเฉพาะกรณี fallback to DB
                            command.Parameters.AddWithValue("@inPassword",
                                adVerified ? "" : Password);
                            // เพิ่ม parameter บอก Stored Proc ว่า skip password check หรือไม่
                            command.Parameters.AddWithValue("@inSkipPasswordCheck",
                                adVerified ? "Y" : "N");

                            using (SqlDataReader dr = command.ExecuteReader()) {
                                if (dr.Read()) {
                                    getStatus = dr["Status"].ToString();
                                    getUsername = dr["Username"].ToString();
                                    getUserType = dr["UserType"].ToString();
                                    getEmail = dr["Email"].ToString();
                                    getSlmcode = dr["Slmcode"].ToString();
                                    getCuscode = dr["Cuscode"].ToString();
                                    getIsActive = dr["IsActive"].ToString();
                                }
                            }

                            // ── ตัดสินผล ──
                            if (adVerified)
                            {
                                // AD pass แล้ว → เช็คแค่ว่า IsActive = Y ใน DB (ถ้ามีใน DB)
                                // ถ้าไม่มีใน DB เลย (getIsActive = "") ก็ให้ผ่าน (AD คือ source of truth)
                                if (!string.IsNullOrEmpty(getIsActive) && getIsActive != "Y")
                                {
                                    errorMessage = "บัญชีผู้ใช้ถูกระงับการใช้งาน";
                                }
                                else
                                {
                                    authSource = "AD";
                                }
                            }
                            else
                            {
                                // AD fail → ต้องผ่าน DB ทั้ง status และ isActive
                                if (string.IsNullOrEmpty(getIsActive)
                                    || getIsActive != "Y"
                                    || getStatus != "Y")
                                {
                                    errorMessage = "Username หรือ Password ไม่ถูกต้อง";
                                }
                                else
                                {
                                    authSource = "DB";
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    errorMessage = ex.Message;
                }
            }

            DataRespond dataRes = new DataRespond();

            dataRes.statusCode = 200;
            dataRes.errorMessage = errorMessage;
            dataRes.result = new List<resultAuthen>();

            if (errorMessage == "Success") {
                dataRes.result.Add(
                    new resultAuthen {
                        verify   = "True",
                        username = getUsername,
                        email    = getEmail,
                        slmcode  = getSlmcode,
                        cuscode  = getCuscode,
                        userType = Convert.ToInt32(
                                string.IsNullOrEmpty(getUserType)
                                ? "0"
                                : getUserType),
                        isActive = !string.IsNullOrEmpty(getIsActive) ? getIsActive : "Y",
                        authSource = authSource  // บอก client ว่า login ผ่านช่องทางไหน
                    }
                );
            }

            // LOG
            var jsonLog = JsonConvert.SerializeObject(
                    new {
                        Username,
                        Password = "******",
                        AuthSource = authSource
                    });

            string jsonReturn = JsonConvert.SerializeObject(dataRes);
            string lastId =
                _apiServerService.SaveApiResponse(
                    "UserAuthenEcatalog",
                    jsonLog,
                    "");

            _apiServerService.UpdateApiRespone(
                lastId,
                jsonReturn);

            return Json(dataRes);
        }
        //model
        public class DataRespond
        {
            public int statusCode { get; set; }
            public string errorMessage { get; set; }
            public List<resultAuthen> result { get; set; }
        }
        //array list result
        public class resultAuthen
        {
            public string verify { get; set; }
            public string username { get; set; }
            public string email { get; set; }
            public string slmcode { get; set; }
            public string cuscode { get; set; }
            public int userType { get; set; }
            public string isActive { get; set; }
            public string authSource { get; set; } // "AD" | "DB"
        }
    }
}