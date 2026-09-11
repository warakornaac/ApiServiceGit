using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ApiService.Services
{
    public class VerifyApiKey
    {
        /// <summary>
        /// Returns "Y" when the key is valid, otherwise the error text to surface to the
        /// caller. Behaviour is identical to the synchronous CheckApiKey it replaces.
        /// Async so the P_Verify_Key round trip does not block an ASP.NET thread-pool thread.
        /// (The synchronous twin was removed: the filter was its only caller.)
        /// </summary>
        public async Task<string> CheckApiKeyAsync(string username, string password, string apiKey) {
            var txtRespond = "Y";
            var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;
            try {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Verify_Key", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);
                    cmd.Parameters.AddWithValue("@ApiKey", apiKey);

                    SqlParameter p = new SqlParameter("@OutGenstatus", SqlDbType.NVarChar, 100);
                    p.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(p);

                    await conn.OpenAsync().ConfigureAwait(false);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                    var storedResult = cmd.Parameters["@OutGenstatus"].Value.ToString();
                    if (!string.IsNullOrEmpty(storedResult)) {
                        string[] fullResultStored = storedResult.Split('|');
                        var flagResult = fullResultStored[0];
                        var txtResult = fullResultStored[1];
                        if (flagResult != "ApiKey") {
                            txtRespond = txtResult;
                        }
                    }
                }
            }
            catch (Exception ex) {
                txtRespond = ex.ToString();
            }
            return txtRespond;
        }

    }
}