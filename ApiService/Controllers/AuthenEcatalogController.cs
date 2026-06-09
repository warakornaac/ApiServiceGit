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
            string getStatus= "";
            string getUsername = "";
            string getUserType = "";
            string getEmail = "";
            string getSlmcode = "";
            string getCuscode = "";
            string getIsActive = "";

            var connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;

            if (string.IsNullOrWhiteSpace(Username)) {
                errorMessage = "Username not null";
            }

            if (string.IsNullOrWhiteSpace(Password)) {
                errorMessage = "Password not null";
            }

            if (errorMessage == "Success") {
                try {
                    using (SqlConnection conn = new SqlConnection(connectionString)) {
                        conn.Open();
                        using (SqlCommand command = new SqlCommand("P_Ecatalog_Authen", conn)) {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@inUsername", Username);
                            command.Parameters.AddWithValue("@inPassword", Password);

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

                            if (string.IsNullOrEmpty(getIsActive) || getIsActive != "Y" || getStatus != "Y") {
                                errorMessage = "Username หรือ Password ไม่ถูกต้อง";
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
                        verify = "True",
                        username = getUsername,
                        email = getEmail,
                        slmcode = getSlmcode,
                        cuscode = getCuscode,
                        userType = Convert.ToInt32(
                                string.IsNullOrEmpty(getUserType)
                                ? "0"
                                : getUserType),
                        isActive = getIsActive
                    });
            }

            // LOG
            var jsonLog = JsonConvert.SerializeObject(
                    new {
                        Username,
                        Password = "******"
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
        }
    }
}