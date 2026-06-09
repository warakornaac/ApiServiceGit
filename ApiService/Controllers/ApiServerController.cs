using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;

namespace ApiService.Controllers
{

    public class ApiServerController
    {
        public string Env { get; set; }
        public string getEnv()
        {
            Env = ConfigurationManager.AppSettings["Environment"];
            return Env;
        }

        public string SaveApiResponse(string method, string inservice, string user)
        {
            if (string.IsNullOrEmpty(user))
            {
                user = CurrentUser + "_EnvTest";
            }

            string outReturn = "";
            var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("P_Save_Apiservice_log", conn))
                    {
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " Error_SAVE");
            }

            return outReturn;
        }
        public void UpdateApiRespone(string id, string respon)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("P_Update_Apiservice_log", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@response", respon);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " Error_UPDATE");
            }
        }

        public string CurrentUser
        {
            get
            {
                if (HttpContext.Current != null &&
                    HttpContext.Current.Items["UserLogin"] != null)
                {
                    return HttpContext.Current
                        .Items["UserLogin"]
                        .ToString();
                }

                return "SystemApiService";
            }
        }
    }
}
