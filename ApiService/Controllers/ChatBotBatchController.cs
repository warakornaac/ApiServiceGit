using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using ApiService.Filters;
using ApiService.Models;
using RouteAttribute = System.Web.Http.RouteAttribute;

namespace ApiService.Controllers
{
    /// <summary>
    /// Batched counterpart of ChatBotController.GetPriceStk.
    ///
    /// The single-part endpoint costs 4 database round trips per call (auth,
    /// the query, and two logging writes). Asking about 5 parts therefore cost
    /// 20. This endpoint answers the whole batch in one call, so it costs 4
    /// regardless of how many parts are requested.
    ///
    /// Deliberately a separate file and a separate action rather than a change
    /// to GetPriceStk: the single-part endpoint is being modified concurrently
    /// on another branch, and the two changes should not collide. Fold this
    /// into ChatBotController once that has merged, if you prefer it there.
    /// </summary>
    public class ChatBotBatchController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        /// <summary>Upper bound on one batch. A TVP will happily accept far more,
        /// but the per-part loop inside p_WH_ItmByBinHr_NoIdle is linear, so an
        /// unbounded batch is a denial-of-service on ourselves.</summary>
        private const int MaxBatchSize = 200;

        public ChatBotBatchController() {
            _apiServerService = new ApiServerController();
        }

        [HttpPost]
        [Route("price-stock/batch")]
        [ApiKeyAuthorize]
        public async Task<HttpResponseMessage> GetPriceStkBatch([FromBody] PriceStockBatchRequest request) {
            var jsonLog = JsonConvert.SerializeObject(request);

            if (request == null ||
                string.IsNullOrWhiteSpace(request.customer_code) ||
                request.part_nos == null ||
                request.part_nos.Count == 0) {
                return await FailAsync(HttpStatusCode.BadRequest, "Invalid or missing parameters in the request.", jsonLog).ConfigureAwait(false);
            }

            // Trim, drop blanks, de-duplicate. The TVP has a primary key on PartNo,
            // so duplicates would otherwise fail the insert on the SQL side.
            var parts = request.part_nos
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (parts.Count == 0) {
                return await FailAsync(HttpStatusCode.BadRequest, "part_nos contained no usable values.", jsonLog).ConfigureAwait(false);
            }

            if (parts.Count > MaxBatchSize) {
                return await FailAsync(HttpStatusCode.BadRequest,
                    "Too many part numbers in one batch. Maximum is " + MaxBatchSize + ".", jsonLog).ConfigureAwait(false);
            }

            var stk = new List<object>();

            try {
                var connectionString =
                    ConfigurationManager.ConnectionStrings["MobileOrder_ConnectionString"].ConnectionString;

                var partTable = new DataTable();
                partTable.Columns.Add("PartNo", typeof(string));
                foreach (var p in parts) {
                    partTable.Rows.Add(p);
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Search_PriceStock_ChatBot", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 60; // a batch legitimately takes longer than one part

                    cmd.Parameters.Add("@incuscod", SqlDbType.VarChar, 20).Value = request.customer_code;
                    cmd.Parameters.Add("@inpart_no", SqlDbType.VarChar, 20).Value = "";   // TVP wins in the proc
                    cmd.Parameters.Add("@inStk_flag", SqlDbType.Bit).Value = request.stock_flag ?? false;

                    SqlParameter tvp = cmd.Parameters.AddWithValue("@inPartNos", partTable);
                    tvp.SqlDbType = SqlDbType.Structured;
                    tvp.TypeName = "dbo.PartNoListTmp";

                    await conn.OpenAsync().ConfigureAwait(false);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false)) {
                        // Ordinals resolved once, not per row per column.
                        int oPeople = reader.GetOrdinal("PEOPLE");
                        int oStkcod = reader.GetOrdinal("STKCOD");
                        int oStkdes = reader.GetOrdinal("STKDES");
                        int oBrand = reader.GetOrdinal("Brand");
                        int oCompany = reader.GetOrdinal("company");
                        int oSalePrice = reader.GetOrdinal("SalePrice");
                        int oSpecial = reader.GetOrdinal("Special_Price");
                        int oLastSale = reader.GetOrdinal("LastSalesPrice");
                        int oTotbal = reader.GetOrdinal("TOTBAL");
                        int oEta = reader.GetOrdinal("Estimate_Date_Arrival");
                        int oMoq = reader.GetOrdinal("MOQ");
                        int oPack = reader.GetOrdinal("sales_packing_standard");
                        int oEop = reader.GetOrdinal("is_eop");
                        int oSubst = reader.GetOrdinal("subtitute_product");

                        while (await reader.ReadAsync().ConfigureAwait(false)) {
                            if (request.stock_flag == true) {
                                stk.Add(new StkPrice {
                                    customer_code = Str(reader, oPeople),
                                    part_no = Str(reader, oStkcod),
                                    product_name = Str(reader, oStkdes),
                                    brand = Str(reader, oBrand),
                                    company = Str(reader, oCompany),
                                    structure_price = Dec(reader, oSalePrice),
                                    special_price = Dec(reader, oSpecial),
                                    previous_price = Dec(reader, oLastSale),
                                    stock_quantity = Int(reader, oTotbal),
                                    estimated_arrival_date = Date(reader, oEta),
                                    moq = Int(reader, oMoq),
                                    sales_packing_standard = Int(reader, oPack),
                                    is_eop = Int(reader, oEop) == 1,
                                    subtitute_product = StrOrNull(reader, oSubst)
                                });
                            }
                            else {
                                stk.Add(new StkPrice_false {
                                    customer_code = Str(reader, oPeople),
                                    part_no = Str(reader, oStkcod),
                                    product_name = Str(reader, oStkdes),
                                    brand = Str(reader, oBrand),
                                    company = Str(reader, oCompany),
                                    structure_price = Dec(reader, oSalePrice),
                                    special_price = Dec(reader, oSpecial),
                                    previous_price = Dec(reader, oLastSale),
                                    estimated_arrival_date = Date(reader, oEta),
                                    moq = Int(reader, oMoq),
                                    sales_packing_standard = Int(reader, oPack),
                                    is_eop = Int(reader, oEop) == 1,
                                    subtitute_product = StrOrNull(reader, oSubst)
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException) {
                return await FailAsync(HttpStatusCode.InternalServerError, "Database error.", jsonLog).ConfigureAwait(false);
            }
            catch (Exception) {
                return await FailAsync(HttpStatusCode.InternalServerError, "Something went wrong on the server.", jsonLog).ConfigureAwait(false);
            }

            if (stk.Count == 0) {
                return await FailAsync(HttpStatusCode.NotFound, "No product found matching the provided Part No.", jsonLog).ConfigureAwait(false);
            }

            var resOk = new ApiResponse<List<object>> {
                Status = "OK",
                Message = "The request was successful and product information is returned.",
                Data = stk
            };

            // One log pair for the whole batch, not one per part.
            string logId = await _apiServerService.SaveApiResponseAsync("Chatbot/SearchPriceStockBatch", jsonLog, "").ConfigureAwait(false);
            await _apiServerService.UpdateApiResponeAsync(logId, JsonConvert.SerializeObject(resOk)).ConfigureAwait(false);

            return Request.CreateResponse(HttpStatusCode.OK, resOk);
        }

        private async Task<HttpResponseMessage> FailAsync(HttpStatusCode code, string message, string jsonLog) {
            var resFail = new ApiResponse<object> {
                Status = code.ToString(),
                Message = message,
                Data = null
            };
            string logId = await _apiServerService.SaveApiResponseAsync("Chatbot/SearchPriceStockBatch", jsonLog, "").ConfigureAwait(false);
            await _apiServerService.UpdateApiResponeAsync(logId, JsonConvert.SerializeObject(resFail)).ConfigureAwait(false);
            return Request.CreateResponse(code, resFail);
        }

        private static string Str(SqlDataReader r, int i) { return r.IsDBNull(i) ? "" : r.GetValue(i).ToString(); }
        private static string StrOrNull(SqlDataReader r, int i) { return r.IsDBNull(i) ? null : r.GetValue(i).ToString(); }
        private static decimal Dec(SqlDataReader r, int i) { return r.IsDBNull(i) ? 0 : Convert.ToDecimal(r.GetValue(i)); }
        private static int Int(SqlDataReader r, int i) { return r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i)); }
        private static DateTime? Date(SqlDataReader r, int i) { return r.IsDBNull(i) ? (DateTime?)null : Convert.ToDateTime(r.GetValue(i)); }

        public class PriceStockBatchRequest
        {
            public string customer_code { get; set; }
            public List<string> part_nos { get; set; }
            public bool? stock_flag { get; set; }
        }
    }
}