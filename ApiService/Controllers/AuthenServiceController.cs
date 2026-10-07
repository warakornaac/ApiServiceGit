using ApiService.Filters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;
using RouteAttribute = System.Web.Http.RouteAttribute;
namespace ApiService.Controllers
{
    public class AuthenServiceController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        public AuthenServiceController()
        {
            _apiServerService = new ApiServerController();
        }


        [HttpPost]
        [Route("Service/InsertLoginLog")]
        [ApiKeyAuthorize]
        public IHttpActionResult InsertLoginLog(
            string usrId,
            string usrTyp,
            string flag,
            string userHostAddress,
            string loginStatus,
            string failReason = null,
            string userAgent = "",
            string systemName = "")
        {
            string errorMessage = "Success";

            var validations = new[]
            {
        new KeyValuePair<string, string>(usrId, "usrId"),
        new KeyValuePair<string, string>(usrTyp, "usrTyp"),
        new KeyValuePair<string, string>(flag, "flag"),
        new KeyValuePair<string, string>(loginStatus, "loginStatus")
    };

            foreach (var item in validations)
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    return Json(new
                    {
                        statusCode = 400,
                        errorMessage = $"{item.Value} is required",
                        result = new { }
                    });
                }
            }

            try
            {
                string connectionString =
                    ConfigurationManager
                        .ConnectionStrings["APIDB_ConnectionString"]
                        .ConnectionString;

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmdLog = new SqlCommand("P_InsertUserAuthenLoginLog", conn))
                {
                    cmdLog.CommandType = CommandType.StoredProcedure;

                    cmdLog.Parameters.AddWithValue("@UsrID",
                        (object)usrId ?? DBNull.Value);

                    cmdLog.Parameters.AddWithValue("@UsrTyp",
                        (object)usrTyp ?? DBNull.Value);

                    cmdLog.Parameters.AddWithValue("@Flag",
                        (object)flag ?? DBNull.Value);

                    cmdLog.Parameters.AddWithValue("@IPAddress",
                        (object)userHostAddress ?? DBNull.Value);

                    if (!string.IsNullOrEmpty(userAgent) &&
                        userAgent.Length > 500)
                    {
                        userAgent = userAgent.Substring(0, 500);
                    }

                    cmdLog.Parameters.AddWithValue("@UserAgent",
                        string.IsNullOrEmpty(userAgent)
                            ? (object)DBNull.Value
                            : userAgent);

                    cmdLog.Parameters.AddWithValue("@LoginStatus",
                        string.IsNullOrEmpty(loginStatus)
                            ? "FAIL"
                            : loginStatus);

                    cmdLog.Parameters.AddWithValue("@FailReason",
                        string.IsNullOrEmpty(failReason)
                            ? (object)DBNull.Value
                            : failReason);

                    cmdLog.Parameters.AddWithValue("@SystemName",
                        string.IsNullOrEmpty(systemName)
                            ? (object)DBNull.Value
                            : systemName);
                    conn.Open();

                    cmdLog.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            var result = new
            {
                statusCode = errorMessage == "Success"
                    ? 200
                    : 500,

                errorMessage,

                result = new { }
            };

            return Json(result);
        }
    }
}