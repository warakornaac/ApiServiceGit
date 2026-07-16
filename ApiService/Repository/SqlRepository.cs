using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.DirectoryServices;
using System.Linq;
using System.Web;
using ApiService.Models;

namespace ApiService.Repository
{
    public class SqlRepository
    {
        private readonly string _conn = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;

        public List<ProductSearchVioDataResponse> Search(SearchSqlRequest request) {
            List<ProductSearchVioDataResponse> items =
                new List<ProductSearchVioDataResponse>();

            using (SqlConnection conn = new SqlConnection(_conn)) {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "P_Search_Product_Global",
                    conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue(
                    "@Keyword",
                    request.Keyword);

                using (SqlDataReader dr = cmd.ExecuteReader()) {
                    while (dr.Read()) {
                        items.Add(new ProductSearchVioDataResponse() {
                            stkcode = dr["stkcode"] == DBNull.Value ? "" : dr["stkcode"].ToString(),
                            stkcodeDescription = dr["stkcodeDescription"] == DBNull.Value ? "" : dr["stkcodeDescription"].ToString(),
                            brand = dr["brand"] == DBNull.Value ? "" : dr["brand"].ToString(),
                            makerName = dr["makerName"] == DBNull.Value ? "" : dr["makerName"].ToString(),
                            modelName = dr["modelName"] == DBNull.Value ? "" : dr["modelName"].ToString(),
                            qtyReady = dr["qtyReady"] == DBNull.Value ? "" : dr["qtyReady"].ToString(),
                            price = dr["price"] == DBNull.Value ? "" : dr["price"].ToString(),
                            productGroup = dr["productGroup"] == DBNull.Value ? "" : dr["productGroup"].ToString(),
                            productLine = dr["productLine"] == DBNull.Value ? "" : dr["productLine"].ToString(),
                            imagePath = dr["imagePath"] == DBNull.Value ? "" : dr["imagePath"].ToString()
                        });
                    }
                }
            }

            return items;
        }
    }
}