using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web;

namespace ApiService.Controllers
{

    public class ApiServerController
    {
        public string Env { get; set; }
        public string getEnv() {
            Env = ConfigurationManager.AppSettings["Environment"];
            return Env;
        }

        /// <summary>
        /// Async counterpart of SaveApiResponse, for endpoints on the async hot path.
        /// The sync version is kept because ~49 call sites still use it; keep the two
        /// bodies in step when the SP or its parameters change.
        /// </summary>
        public async Task<string> SaveApiResponseAsync(string method, string inservice, string user) {
            if (string.IsNullOrEmpty(user)) {
                user = CurrentUser + "_EnvTest";
            }

            string outReturn = "";
            var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;
            try {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Save_Apiservice_log", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@method", method);
                    cmd.Parameters.AddWithValue("@inservice", inservice);
                    cmd.Parameters.AddWithValue("@user", user);
                    SqlParameter p = new SqlParameter("@outGenstatus", SqlDbType.NVarChar, 100);
                    p.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(p);

                    await conn.OpenAsync().ConfigureAwait(false);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                    outReturn = cmd.Parameters["@outGenstatus"].Value.ToString();
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Trace.TraceError("SaveApiResponseAsync failed for " + method + ": " + ex);
            }

            return outReturn;
        }

        /// <summary>
        /// Async counterpart of UpdateApiRespone. Behaviour matches the sync original
        /// exactly, including calling the SP when id is empty.
        /// </summary>
        public async Task UpdateApiResponeAsync(string id, string respon) {
            var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;
            try {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Update_Apiservice_log", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@response", respon);

                    await conn.OpenAsync().ConfigureAwait(false);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Trace.TraceError("UpdateApiResponeAsync failed for id " + id + ": " + ex);
            }
        }

        /// <summary>
        /// Synchronous original, still used by ~49 call sites. Body untouched by this PR;
        /// keep in step with SaveApiResponseAsync above.
        /// </summary>
        public string SaveApiResponse(string method, string inservice, string user) {
            if (string.IsNullOrEmpty(user)) {
                user = CurrentUser + "_EnvTest";
            }

            string outReturn = "";
            var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;
            try {
                using (SqlConnection conn = new SqlConnection(connectionString)) {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("P_Save_Apiservice_log", conn)) {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@method", method);
                        cmd.Parameters.AddWithValue("@inservice", inservice);
                        cmd.Parameters.AddWithValue("@user", user);
                        SqlParameter p = new SqlParameter("@outGenstatus", SqlDbType.NVarChar, 100);
                        p.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(p);
                        cmd.ExecuteNonQuery();
                        outReturn = cmd.Parameters["@outGenstatus"].Value.ToString();
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString() + " Error_SAVE");
            }

            return outReturn;
        }
        public void UpdateApiRespone(string id, string respon) {
            var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;
            try {
                using (SqlConnection conn = new SqlConnection(connectionString)) {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("P_Update_Apiservice_log", conn)) {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@response", respon);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString() + " Error_UPDATE");
            }
        }

        public string CurrentUser {
            get {
                if (HttpContext.Current != null &&
                    HttpContext.Current.Items["UserLogin"] != null) {
                    return HttpContext.Current
                        .Items["UserLogin"]
                        .ToString();
                }

                return "SystemApiService";
            }
        }
    }
}