using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ApiService.Models;

namespace ApiService.DAL
{
    public class StockRepository
    {
        private readonly string _connectionString;

        public StockRepository(string connectionString) {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("MobileOrder_ConnectionString");

            _connectionString = connectionString;
        }

        /// <summary>
        /// Retrieves all stock items using the '%','%' wildcard parameters
        /// (i.e. all companies, all stock codes).
        /// </summary>
        public List<StockItem> GetAllStock() {
            var result = new List<StockItem>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.p_WH_ItmByBinHr_NoIdle", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120; // ~30,000 rows, allow enough time

                cmd.Parameters.Add(new SqlParameter("@inCompany", SqlDbType.VarChar, 10) { Value = "%" });
                cmd.Parameters.Add(new SqlParameter("@inSTKCOD", SqlDbType.VarChar, 50) { Value = "%" });

                conn.Open();

                using (var reader = cmd.ExecuteReader()) {
                    // Resolve column ordinals once, outside the loop:
                    // - faster than repeated name lookups per row
                    // - fails fast with a clear error if the SP's columns change again
                    int idxCompany = reader.GetOrdinal("Company");
                    int idxStkcod = reader.GetOrdinal("Stkcod");
                    int idxReadyQty = reader.GetOrdinal("ReadyQty");

                    while (reader.Read()) {
                        result.Add(new StockItem {
                            Company = reader.IsDBNull(idxCompany) ? null : reader.GetString(idxCompany),
                            ItemNo = reader.IsDBNull(idxStkcod) ? null : reader.GetString(idxStkcod),
                            ReadyQty = reader.IsDBNull(idxReadyQty) ? 0m : reader.GetDecimal(idxReadyQty)
                        });
                    }
                }
            }

            return result;
        }
    }
}