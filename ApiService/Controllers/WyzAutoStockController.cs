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

namespace ApiService.Controllers
{
    public class WyzAutoStockController : Controller
    {
        private readonly ApiServerController _apiServerService;

        // ตัวอย่างการสร้าง constructor ที่ไม่มีพารามิเตอร์
        public WyzAutoStockController()
        {
            // สร้าง instance ของ IApiServerService แบบไหนก็ได้ หรือไม่ต้องสร้างก็ได้
            _apiServerService = new ApiServerController(); // หรือใช้วิธี dependency injection อื่น ๆ
        }
        // GET: WyzAutoStock
        public ActionResult Index()
        {
            return View();
        }
        //call get token
        //private static readonly ObjectCache cache = MemoryCache.Default;
        //private const string TokenCacheKey = "WyzAuto_AccessToken";

        /// <summary>
        /// ขอ Token ใหม่จาก API
        /// </summary>
        private async Task<WyzTokenResponse> RequestNewTokenAsync()
        {
            string grantType = ConfigurationManager.AppSettings["WyzAuto_GrantType"];
            string clientId = ConfigurationManager.AppSettings["WyzAuto_ClientId"];
            string clientSecret = ConfigurationManager.AppSettings["WyzAuto_ClientSecret"];
            string tokenUrl = ConfigurationManager.AppSettings["WyzAuto_TokenUrl"];

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

                var formData = new Dictionary<string, string>
            {
                { "grant_type", grantType },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };

                request.Content = new FormUrlEncodedContent(formData);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                response.EnsureSuccessStatusCode();

                var tokenResponse = JsonConvert.DeserializeObject<WyzTokenResponse>(responseBody);
                return tokenResponse;
            }
        }

        /// <summary>
        /// ดึง Token จาก Cache ถ้ายังไม่หมดอายุ
        /// ถ้าไม่มีหรือหมดอายุ ให้ขอใหม่แล้วเก็บ Cache
        /// </summary>
        //public async Task<string> GetAccessTokenAsync()
        //{
        //    var cachedToken = cache.Get(TokenCacheKey) as string;

        //    if (!string.IsNullOrEmpty(cachedToken))
        //    {
        //        return cachedToken;
        //    }

        //    var tokenResponse = await RequestNewTokenAsync();

        //    if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
        //    {
        //        throw new Exception("ไม่สามารถรับ access token ได้");
        //    }

        //    // กัน token หมดอายุระหว่างใช้งาน เลยหักเวลาออก 60 วินาที
        //    int cacheMinutes = Math.Max((tokenResponse.expires_in - 60) / 60, 1);

        //    CacheItemPolicy policy = new CacheItemPolicy
        //    {
        //        AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(cacheMinutes)
        //    };

        //    cache.Set(TokenCacheKey, tokenResponse.access_token, policy);

        //    return tokenResponse.access_token;
        //}

        ///// <summary>
        ///// สร้าง HttpClient ที่มี Bearer Token ให้พร้อมใช้งาน
        ///// </summary>
        //public async Task<HttpClient> CreateHttpClientWithTokenAsync()
        //{
        //    string accessToken = await GetAccessTokenAsync();

        //    var client = new HttpClient();
        //    client.DefaultRequestHeaders.Authorization =
        //        new AuthenticationHeaderValue("Bearer", accessToken);

        //    return client;
        //}

        ///// <summary>
        ///// ตัวอย่างเรียก API Products
        ///// </summary>
        //public async Task<string> GetProductsAsync()
        //{
        //    string productUrl = "https://partner.platform.wyzauto.dev/v2/products";

        //    using (var client = await CreateHttpClientWithTokenAsync())
        //    {
        //        var response = await client.GetAsync(productUrl);
        //        var responseBody = await response.Content.ReadAsStringAsync();

        //        response.EnsureSuccessStatusCode();

        //        return responseBody;
        //    }
        //}
        //Model
        public class WyzTokenResponse
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
        }
    }
}