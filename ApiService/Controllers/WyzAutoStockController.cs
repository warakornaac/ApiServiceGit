using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using RouteAttribute = System.Web.Http.RouteAttribute;
using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using ApiService.Filters;
using System.Globalization;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;
using System.Runtime.Caching;
using System.Text;
using System.Net;
using System.Linq;

namespace ApiService.Controllers
{
    public class WyzAutoStockController : ApiController
    {
        private readonly ApiServerController _apiServerService;


        public WyzAutoStockController()
        {
            // สร้าง instance ของ IApiServerService แบบไหนก็ได้ หรือไม่ต้องสร้างก็ได้
            _apiServerService = new ApiServerController(); // หรือใช้วิธี dependency injection อื่น ๆ
        }
        //call get token
        private static readonly ObjectCache cache = MemoryCache.Default;
        private const string TokenCacheKey = "WyzAuto_AccessToken";
        [HttpPost]
        [Route("WyzAuto/RequestNewTokenAsync")]
        /// <summary>
        /// ขอ Token ใหม่จาก API
        /// </summary>
        private async Task<WyzTokenResponse> RequestNewTokenAsync()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string clientId = ConfigurationManager.AppSettings["WyzAuto_ClientId"];
            string clientSecret = ConfigurationManager.AppSettings["WyzAuto_ClientSecret"];
            string tokenUrl = ConfigurationManager.AppSettings["WyzAuto_TokenUrl"];

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var requestBody = new
                {
                    client_id = clientId,
                    client_secret = clientSecret
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(tokenUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine("Request JSON: " + json);
                Console.WriteLine("Response Status: " + response.StatusCode);
                Console.WriteLine("Response Body: " + responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Token Error: {response.StatusCode} - {responseBody}");
                }

                return JsonConvert.DeserializeObject<WyzTokenResponse>(responseBody);
            }
        }

        /// <summary>
        /// ดึง Token จาก Cache ถ้ายังไม่หมดอายุ
        /// ถ้าไม่มีหรือหมดอายุ ให้ขอใหม่แล้วเก็บ Cache
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            var cachedToken = cache.Get(TokenCacheKey) as string;

            if (!string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }
            var tokenResponse = await RequestNewTokenAsync();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
            {
                throw new Exception("ไม่สามารถรับ access token ได้");
            }
            //กัน token หมดอายุระหว่างใช้งาน เลยหักเวลาออก 60 วินาที
            int cacheMinutes = Math.Max((tokenResponse.expires_in - 60) / 60, 1);
            CacheItemPolicy policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(cacheMinutes)
            };
            cache.Set(TokenCacheKey, tokenResponse.access_token, policy);
            return tokenResponse.access_token;
        }
        [HttpPost]
        [Route("WyzAuto/GetAccessToken")]
        ///// <summary>
        ///// สร้าง HttpClient ที่มี Bearer Token ให้พร้อมใช้งาน
        ///// </summary>
        public async Task<HttpClient> CreateHttpClientWithTokenAsync()
        {
            string accessToken = await GetAccessTokenAsync();
            Console.WriteLine("TOKEN: " + accessToken);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return client;
        }
        [HttpPost]
        [Route("WyzAuto/UpdateProductStock")]
        [ApiKeyAuthorize]
        ///// <summary>
        ///// ตัวอย่างเรียก API Products
        ///// </summary>
        public async Task<string> sendUpdateProductTyreplus()
        {
            string url = "https://partner.platform.wyzauto.io/v1/partner/offers:import";
            // ดึงข้อมูลทั้งหมด
            var productList = await GetProductTyreplusData();
            Guid batchId = Guid.NewGuid(); //เพื่อดู log การรัน batch
            int batchSize = 100;
            int totalProduct = productList.Count; //รายการสินค้าที่จะส่งทั้งหมด
            int totalSuccess = 0;
            int totalFail = 0;
            string token = await GetAccessTokenAsync();
            var responseLogs = new List<string>();

            using (var client = await CreateHttpClientWithTokenAsync())
            {
                // แบ่ง batch ละ 100
                for (int i = 0; i < productList.Count; i += batchSize)
                {
                    var batch = productList
                        .Skip(i)
                        .Take(batchSize)
                        .ToList();
                    try
                    {
                        var requestBody = new
                        {
                            offers = batch.Select(x => new
                            {
                                product_id = x.ProductId,
                                //seller_sku_id = "0",
                                warehouse_id = x.WarehouseId,
                                base_price = x.BasePrice,
                                stock = x.Stock,
                                tax_included = false,
                                package_quantity = 1,
                                min_order_quantity = 1,
                                max_order_quantity = 50,
                                is_active = x.IsActive
                            }).ToArray()
                        };

                        var json = JsonConvert.SerializeObject(requestBody);
                        var content = new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json"
                        );

                        var response = await client.PostAsync(url, content);
                        var responseBody = await response.Content.ReadAsStringAsync();
                        //convert json des prepar save log
                        var apiResponse = JsonConvert.DeserializeObject<ProductTyreplusResponseModel>(responseBody);

                        bool isSuccess = response.IsSuccessStatusCode
                            && apiResponse != null
                            && apiResponse.Status == "ACCEPTED";
                        if (!isSuccess)
                        {
                            throw new Exception(
                                $"Batch {(i / batchSize) + 1} Error: {responseBody}"
                            );
                        }
                        // get jobId
                        string jobId = apiResponse?.JobId ?? "";
                        // เก็บ log response success, fail แล้ว
                        // Success
                        if (isSuccess)
                        {
                            foreach (var item in batch)
                            {
                                await SaveTyreplusUploadLog(
                                    batchId,
                                    jobId,
                                    item,
                                    json,
                                    responseBody,
                                    true,
                                    token
                                );

                                totalSuccess++;
                            }
                        }
                        // FAIL
                        else
                        {
                            foreach (var item in batch)
                            {
                                await SaveTyreplusUploadLog(
                                    batchId,
                                    jobId,
                                    item,
                                    json,
                                    responseBody,
                                    false,
                                    token
                                );

                                totalFail++;
                            }
                        }
                        // log
                        string logMessage = $"Batch {(i / batchSize) + 1} | " +
                            $"Count: {batch.Count} | " +
                            $"Status: {response.StatusCode}";

                        responseLogs.Add(logMessage);
                    }
                    catch (Exception ex)
                    {
                        // ERROR LEVEL เช่น timeout/network

                        foreach (var item in batch)
                        {
                            await SaveTyreplusUploadLog(
                                batchId,
                                "",
                                item,
                                "",
                                ex.ToString(),
                                false,
                                token
                            );

                            totalFail++;
                        }

                        responseLogs.Add(
                            $"Batch {(i / batchSize) + 1} ERROR : {ex.Message}"
                        );
                    }

                    // optional delay กัน API rate limit
                    await Task.Delay(300);
                }
            }

            // process ครบทั้งหมดแล้ว
            bool isAllSuccess = totalSuccess == totalProduct;

            bool isProcessCompleted = (totalSuccess + totalFail) == totalProduct;

            // ส่ง mail summary
            if (isProcessCompleted)
            {
                await SendSummaryEmaiProduct(
                    batchId,
                    totalProduct,
                    totalSuccess,
                    totalFail,
                    isAllSuccess
                );
            }

            return string.Join(
                Environment.NewLine,
                responseLogs
            );
        }
        //ดึงข้อมูลเพื่อเตรียมส่ง api
        private async Task<List<ProductTyreplusModel>> GetProductTyreplusData()
        {
            var result = new List<ProductTyreplusModel>();

            string connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("P_Get_Product_Tyreplus", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 240;

                await conn.OpenAsync();

                using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        result.Add(new ProductTyreplusModel
                        {
                            ProductId = dr["ProductId"]?.ToString(),
                            WarehouseId = dr["WarehouseId"]?.ToString(),
                            BasePrice = dr["BasePrice"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["BasePrice"]),
                            Stock = dr["Stock"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Stock"]),
                            IsActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"])
                        });
                    }
                }
            }

            return result;
        }
        //รับข้อมูลหลังจากส่ง Update
        private async Task SaveTyreplusUploadLog(Guid batchId, string jobId, ProductTyreplusModel item, string requestJson, string responseJson, bool isSuccess, string token)
        {
            var userLogin = _apiServerService.CurrentUser;
            string connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("P_Save_Product_Tyreplus_UploadLog", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@inBatchId", batchId);
                cmd.Parameters.AddWithValue("@inJobId", jobId);
                cmd.Parameters.AddWithValue("@inProductId", item.ProductId);
                cmd.Parameters.AddWithValue("@inWarehouseId", item.WarehouseId);
                cmd.Parameters.AddWithValue("@inBasePrice", item.BasePrice);
                cmd.Parameters.AddWithValue("@inStock", item.Stock);
                cmd.Parameters.AddWithValue("@inIsActive", item.IsActive);
                cmd.Parameters.AddWithValue("@inRequestJson", requestJson);
                cmd.Parameters.AddWithValue("@inResponseJson", responseJson);
                cmd.Parameters.AddWithValue("@inIsSuccess", isSuccess);
                cmd.Parameters.AddWithValue("@inTokenId", token);
                cmd.Parameters.AddWithValue("@inInsertBy", userLogin);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
        }
        //ส่ง email เมื่อส่งครบ
        private async Task SendSummaryEmaiProduct(Guid batchId, int totalProduct, int totalSuccess, int totalFail, bool isAllSuccess)
        {
            var userLogin = _apiServerService.CurrentUser;
            string connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("P_Summary_Mail_Product_Tyreplus", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@inBatchId", batchId);
                cmd.Parameters.AddWithValue("@inTotalProduct", totalProduct);
                cmd.Parameters.AddWithValue("@inTotalSuccess", totalSuccess);
                cmd.Parameters.AddWithValue("@inTotalFail", totalFail);
                cmd.Parameters.AddWithValue("@inIsAllSuccess", isAllSuccess);
                cmd.Parameters.AddWithValue("@inInsertBy", userLogin);

                await conn.OpenAsync();

                await cmd.ExecuteNonQueryAsync();
            }
        }
        //set formate product send
        public class ProductTyreplusModel
        {
            public string ProductId { get; set; }
            public string WarehouseId { get; set; }
            public decimal BasePrice { get; set; }
            public int Stock { get; set; }
            public bool IsActive { get; set; }
        }
        //set formate insert product
        public class ProductTyreplusResponseModel
        {
            [JsonProperty("job_id")]
            public string JobId { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("submitted_at")]
            public DateTime SubmittedAt { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }
        //[HttpPost]
        //[Route("WyzAuto/HandleStockUpdateResult")]
        //http://mst.aac.co.th/WyzAuto/HandleStockUpdateResult
        //Model
        public class WyzTokenResponse
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
        }
    }
}