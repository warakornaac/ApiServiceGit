using ApiService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using ApiService.Controllers;
using RouteAttribute = System.Web.Http.RouteAttribute;
using System.Net;
using System.Web.Http.Results;
using Newtonsoft.Json.Linq;
using System.IO;

namespace ApiService.Controllers
{
    public class TechdocController : ApiController
    {
        private readonly ApiServerController _apiServerService;
        // GET: Techdoc
        public TechdocController()
        {
            _apiServerService = new ApiServerController();
        }
        [Route("Post/Articles")]
        public async Task<IHttpActionResult> PostAsync([FromBody] SendArticlesModels models)
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://vin.tecalliance-sea.com/api/tecdoc");

            // Authorization: Bearer or X-API-Key (ดูจาก Postman)
            request.Headers.Add("Authorization", "Aw4OMYTNcOzOYMgD4zN5MYgMkuO4DCDz");
            //// จำลอง User-Agent ของ Postman
            //request.Headers.UserAgent.ParseAdd("PostmanRuntime/7.29.0");
            //// Accept JSON
            //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // JSON Body ต้องตรงโครงสร้าง API
            var requestObj = new ArticlesRequestMain
            {
                getArticles = new ArticlesRequestSub
                {
                    articleCountry = "TH",
                    lang = "TH",
                    searchType = 0,
                    searchQuery = models.Partno,
                    dataSupplierIds = models.SupplierId,
                    includeAll = true,
                    searchExact = true,
                    includeLinkages = true,
                    perPage = 15,
                    page = 1,
                    includeLinks = true
                }
            };

            // JSON serialize พร้อม JsonProperty ด้านบน
            var json = JsonConvert.SerializeObject(requestObj);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            //if (!response.IsSuccessStatusCode)
            //{
            //    return Content(response.StatusCode, new
            //    {
            //        Success = false,
            //        StatusCode = response.StatusCode,
            //        Message = "Request failed.",
            //        ServerResponse = responseBody
            //    });
            //}

            // Deserialize the response body to get image data
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
            List<int> articleId = new List<int>();
            List<string> mfrName = new List<string>();
            List<string> articleNumber = new List<string>();
            List<string> additionalDescriptions = new List<string>();
            List<string> articleStatusDescription = new List<string>();
            List<string> quantityPerPackage = new List<string>();
            List<GenericArticle> genericArticles = new List<GenericArticle>();
            List<image> images = new List<image>();
            List<OemNumber> oemNumbers = new List<OemNumber>();
            List<ArticleCriteria> articleCriteria = new List<ArticleCriteria>();
            List<Pdf> pdfs = new List<Pdf>();
            List<TradeNumberDetail> tradeNumbersDetails = new List<TradeNumberDetail>();
            // Extract image URLs from the response
            if (apiResponse != null && apiResponse.articles != null)
            {
                foreach (var article in apiResponse.articles)
                {
                    //brand
                    if (article.mfrName != null)
                    {
                        mfrName.Add(article.mfrName);
                    }
                    //part no
                    if (article.articleNumber != null)
                    {
                        articleNumber.Add(article.articleNumber);
                    }
                    //type
                    if (article.misc != null)
                    {
                        additionalDescriptions.Add(article.misc.additionalDescription);
                        articleStatusDescription.Add(article.misc.articleStatusDescription);
                        quantityPerPackage.Add(article.misc.quantityPerPackage);
                    }
                    //list genericArticles
                    if (article.genericArticles != null)
                    {
                        genericArticles.AddRange(article.genericArticles);
                    }
                    //list image
                    if (article.images != null)
                    {
                        images.AddRange(article.images);
                    }
                    //list oem
                    if (article.oemNumbers != null)
                    {
                        oemNumbers.AddRange(article.oemNumbers);
                    }
                    //list criteria
                    if (article.articleCriteria != null)
                    {
                        articleCriteria.AddRange(article.articleCriteria);
                    }
                    //list pdf
                    if (article.pdfs != null)
                    {
                        pdfs.AddRange(article.pdfs);
                    }
                    //list tradeNumbersDetails
                    if (article.tradeNumbersDetails != null)
                    {
                        tradeNumbersDetails.AddRange(article.tradeNumbersDetails);
                    }
                }
                IHttpActionResult actionResult = await GetArticlesLinkTarget4(models.Partno, models.SupplierId);
                // Cast to the concrete result type
                var okResult = actionResult as OkNegotiatedContentResult<int>;
                // Read the integer (or 0 if cast failed)
                int articleIdResult = okResult?.Content ?? 0;
                if (articleIdResult != 0)
                {
                    articleId.Add(articleIdResult);
                }
            }
            return Json(new
            {
                ArticleId = articleId,
                MfrNames = mfrName,
                ArticleNumber = articleNumber,
                AdditionalDescriptions = additionalDescriptions,
                ArticleStatusDescription = articleStatusDescription,
                QuantityPerPackage = quantityPerPackage,
                GenericArticles = genericArticles,
                Images = images,
                OemNumbers = oemNumbers,
                ArticleCriteria = articleCriteria,
                Pdf = pdfs,
                TradeNumberDetail = tradeNumbersDetails
            });
        }
        //response
        public class OemNumber
        {
            public string articleNumber { get; set; }
            public int mfrId { get; set; }
            public string mfrName { get; set; }
            public bool matchesSearchQuery { get; set; }
        }
        public class image
        {
            public string imageURL50 { get; set; }
            public string imageURL100 { get; set; }
            public string imageURL200 { get; set; }
            public string imageURL400 { get; set; }
            public string imageURL800 { get; set; }
            public string imageURL1600 { get; set; }
            public string imageURL3200 { get; set; }
            public string fileName { get; set; }
            public string typeDescription { get; set; }
            public int typeKey { get; set; }
            public string headerDescription { get; set; }
            public int headerKey { get; set; }
            public int sortNumber { get; set; }
            public string assetSource { get; set; }
        }
        public class ArticleCriteria
        {
            public int criteriaId { get; set; }
            public string criteriaDescription { get; set; }
            public string criteriaAbbrDescription { get; set; }
            public string criteriaUnitDescription { get; set; }
            public string criteriaType { get; set; }
            public string rawValue { get; set; }
            public string formattedValue { get; set; }
            public bool immediateDisplay { get; set; }
            public bool isMandatory { get; set; }
            public bool isInterval { get; set; }
        }
        public class Pdf
        {
            public string url { get; set; }
            public string fileName { get; set; }
            public string typeDescription { get; set; }
            public string headerDescription { get; set; }
            public int sortNumber { get; set; }
            public string assetSource { get; set; }
        }
        public class TradeNumberDetail
        {
            public string tradeNumber { get; set; }
            public bool isImmediateDisplay { get; set; }
        }

        public class Misc
        {
            public string additionalDescription { get; set; }
            public string articleStatusDescription { get; set; }
            public string quantityPerPackage { get; set; }

        }
        public class GenericArticle
        {
            public int genericArticleId { get; set; }
            public string genericArticleDescription { get; set; }
            public int assemblyGroupNodeId { get; set; }
            public string assemblyGroupName { get; set; }
            public int legacyArticleId { get; set; }
            public List<string> linkageTargetTypes { get; set; }
        }
        public class Article
        {

            public string articleId { get; set; }
            public string mfrName { get; set; }
            public string articleNumber { get; set; }
            public List<GenericArticle> genericArticles { get; set; }
            public Misc misc { get; set; }
            public List<image> images { get; set; }
            public List<OemNumber> oemNumbers { get; set; }
            public List<ArticleCriteria> articleCriteria { get; set; }
            public List<Pdf> pdfs { get; set; }
            public List<TradeNumberDetail> tradeNumbersDetails { get; set; }
        }
        public class ApiResponse
        {
            public List<Article> articles { get; set; }
        }
        //request articles
        public class SendArticlesModels
        {
            public string Partno { get; set; }
            public int SupplierId { get; set; }
        }
        public class ArticlesRequestMain
        {
            public ArticlesRequestSub getArticles { get; set; }
        }
        public class ArticlesRequestSub
        {
            public string articleCountry { get; set; }
            public string lang { get; set; }
            public int searchType { get; set; }
            public string searchQuery { get; set; }
            public int dataSupplierIds { get; set; }
            public bool includeAll { get; set; }
            public bool searchExact { get; set; }
            public bool includeLinkages { get; set; }
            public int perPage { get; set; }
            public int page { get; set; }
            public bool includeLinks { get; set; }
        }

        [Route("Post/ArticlesLinkTarget4")]
        public async Task<IHttpActionResult> GetArticlesLinkTarget4(string Partno, int SupplierId)
        {
            //Partno = "0242135553";
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vin.tecalliance-sea.com/api/tecdoc");
            request.Headers.Add("Authorization", "Aw4OMYTNcOzOYMgD4zN5MYgMkuO4DCDz");
            //request.Headers.UserAgent.ParseAdd("PostmanRuntime/7.29.0");
            //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var requestObj = new ArticlesRequestTarget4Main
            {
                getArticleDirectSearchAllNumbersWithState = new ArticlesRequestTarget4Sub
                {
                    articleCountry = "TH",
                    articleNumber = Partno,
                    brandId = SupplierId,
                    lang = "TH",
                    numberType = 0,
                    searchExact = 1
                }
            };
            // สร้าง StringContent สำหรับ body ของคำขอ
            var json = JsonConvert.SerializeObject(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var full = JsonConvert.DeserializeObject<ArticleLinkTarget4Response>(responseBody);

            // Acquire all ArticleItem objects
            var items = full.Data?.Array ?? new List<ArticleLinkTarget4>();
            // Extract just the IDs
            List<int> ArticleId = items.Select(i => i.ArticleId).ToList();
            int firstId = full.Data?.Array?.FirstOrDefault()?.ArticleId ?? 0; ;
            return Ok(firstId);
        }
        //request
        public class ArticlesRequestTarget4Main
        {
            public ArticlesRequestTarget4Sub getArticleDirectSearchAllNumbersWithState { get; set; }
        }
        public class ArticlesRequestTarget4Sub
        {
            public string articleCountry { get; set; }
            public string articleNumber { get; set; }
            public int brandId { get; set; }
            public string lang { get; set; }
            public int numberType { get; set; }
            public int searchExact { get; set; }
        }
        //response 
        public class ArticleLinkTarget4
        {
            [JsonProperty("articleId")]
            public int ArticleId { get; set; }

            [JsonProperty("articleName")]
            public string ArticleName { get; set; }

            // …other props if you need them
        }

        public class DataContainer
        {
            [JsonProperty("array")]
            public List<ArticleLinkTarget4> Array { get; set; }
        }

        public class ArticleLinkTarget4Response
        {
            [JsonProperty("data")]
            public DataContainer Data { get; set; }

            [JsonProperty("status")]
            public int Status { get; set; }
        }
        [Route("Post/ArticlesPartNumberNearby")]
        public async Task<IHttpActionResult> ArticlesPartNumberNearby([FromBody] SendArticlesModels models)
        {
            //Partno = "0242135553";
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vin.tecalliance-sea.com/api/tecdoc");
            request.Headers.Add("Authorization", "Aw4OMYTNcOzOYMgD4zN5MYgMkuO4DCDz");
            //request.Headers.UserAgent.ParseAdd("PostmanRuntime/7.29.0");
            //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var requestObj = new ArticlesRequestTarget4Main
            {
                getArticleDirectSearchAllNumbersWithState = new ArticlesRequestTarget4Sub
                {
                    articleCountry = "TH",
                    articleNumber = models.Partno,
                    brandId = 0,//models.SupplierId,
                    lang = "TH",
                    numberType = 10,
                    searchExact = 1
                }
            };
            // สร้าง StringContent สำหรับ body ของคำขอ
            var json = JsonConvert.SerializeObject(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            // Parse JSON response
            var jsonResponse = JObject.Parse(responseBody);

            var partList = jsonResponse["data"]?["array"]
               ?.Select(token => new
               {
                   articleId = (long?)token["articleId"],
                   articleName = (string)token["articleName"],
                   articleNo = (string)token["articleNo"],
                   articleSearchNo = (string)token["articleSearchNo"],
                   articleStateId = (int?)token["articleStateId"],
                   brandName = (string)token["brandName"],
                   brandNo = (int?)token["brandNo"],
                   genericArticleId = (int?)token["genericArticleId"],
                   numberType = (int?)token["numberType"]
               })
               .ToList();

            return Json(new
            {
                PartNumberNearby = partList
            });
        }
        [Route("Post/ArticlesLinkedAll")]
        public async Task<IHttpActionResult> ArticlesLinkedAll([FromBody] SendArticlesArticleId models)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vin.tecalliance-sea.com/api/tecdoc");
            request.Headers.Add("Authorization", "Aw4OMYTNcOzOYMgD4zN5MYgMkuO4DCDz");
            //request.Headers.UserAgent.ParseAdd("PostmanRuntime/7.29.0");
            //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var requestObj = new ArticlesLinkedAllMain
            {
                getArticleLinkedAllLinkingTarget4 = new ArticlesLinkedAllSub
                {
                    articleCountry = "TH",
                    country = "TH",
                    articleId = models.articleId,
                    lang = "EN",
                    linkingTargetType = "P"
                }
            };
            // สร้าง StringContent สำหรับ body ของคำขอ
            var json = JsonConvert.SerializeObject(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            // Parse JSON response
            var jsonResponse = JObject.Parse(responseBody);

            // Extract fields into a list of anonymous objects
            var linkages = jsonResponse["data"]?["array"]?[0]?["articleLinkages"]?["array"]
                ?.Select(token => new
                {
                    ArticleLinkId = (long?)token["articleLinkId"],
                    Linked = (bool?)token["linked"],
                    LinkingTargetId = (long?)token["linkingTargetId"],
                    LinkingTargetType = (string)token["linkingTargetType"]
                })
                ?.Where(x => x.ArticleLinkId.HasValue && x.LinkingTargetId.HasValue)
                ?.ToList();
            //get data in linkages array
            var linkageDetailsList = new List<object>();
            if (linkages != null)
            {
                foreach (var item in linkages)
                {
                    var linkageTargets = await FetchLinkageTargetsAsync(item.LinkingTargetId.ToString());
                    linkageDetailsList.AddRange((IEnumerable<object>)linkageTargets);
                }
            }
            return Json(new
            {
                LinkageDetails = linkageDetailsList
            });
        }
        public class SendArticlesArticleId
        {
            public string articleId { get; set; }
        }
        //request
        public class ArticlesLinkedAllMain
        {
            public ArticlesLinkedAllSub getArticleLinkedAllLinkingTarget4 { get; set; }
        }
        public class ArticlesLinkedAllSub
        {
            public string articleCountry { get; set; }
            public string country { get; set; }
            public string articleId { get; set; }
            public string lang { get; set; }
            public string linkingTargetType { get; set; }
        }
        [Route("Post/LinkageTargetsAll")]
        public async Task<List<object>> FetchLinkageTargetsAsync(string linkingTargetId /*[FromBody] SendLinkageTargets models*/)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vin.tecalliance-sea.com/api/tecdoc");
            request.Headers.Add("Authorization", "Aw4OMYTNcOzOYMgD4zN5MYgMkuO4DCDz");
            //request.Headers.UserAgent.ParseAdd("PostmanRuntime/7.29.0");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var requestObj = new LinkageTargetsMain
            {
                getLinkageTargets = new LinkageTargetsSub
                {
                    linkageTargetCountry = "TH",
                    lang = "TH",
                    linkageTargetIds = new ListId
                    {
                        id = linkingTargetId,
                        type = "P"
                    }
                }
            };
            // สร้าง StringContent สำหรับ body ของคำขอ
            var json = JsonConvert.SerializeObject(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            // Parse JSON response
            var jsonResponse = JObject.Parse(responseBody);
            var linkageTargetsArray = jsonResponse["linkageTargets"] as JArray;

            var linkageTargets = linkageTargetsArray?
                .Select(item => new
                {
                    LinkageTargetId = (int?)item["linkageTargetId"],
                    LinkageTargetType = (string)item["linkageTargetType"],
                    SubLinkageTargetType = (string)item["subLinkageTargetType"],
                    MfrName = (string)item["mfrName"],
                    VehicleModelSeriesName = (string)item["vehicleModelSeriesName"],
                    BeginYearMonth = (string)item["beginYearMonth"],
                    EndYearMonth = (string)item["endYearMonth"],
                    ImageURL = item["vehicleImages"]?.FirstOrDefault()?["imageURL400"]?.ToString(),
                    DriveType = (string)item["driveType"],
                    BodyStyle = (string)item["bodyStyle"],
                    FuelMixtureFormationType = (string)item["fuelMixtureFormationType"],
                    FuelType = (string)item["fuelType"],
                    EngineType = (string)item["engineType"],
                    Engines = item["engines"]?.FirstOrDefault()?["code"]?.ToString(),
                })
                .Cast<object>()
                .ToList();
            return linkageTargets ?? new List<object>();
        }
        public class SendLinkageTargets
        {
            public string linkingTargetId { get; set; }
        }
        public class LinkageTargetsMain
        {
            public LinkageTargetsSub getLinkageTargets { get; set; }
        }
        public class LinkageTargetsSub
        {
            public string linkageTargetCountry { get; set; }
            public string lang { get; set; }
            public ListId linkageTargetIds { get; set; }
        }

        public class ListId
        {
            public string id { get; set; }
            public string type { get; set; }
        }
        //
        //Vehicle
        //
        [Route("Post/VehicleAll")]
        public async Task<IHttpActionResult> VehicleAll()
        {
            //Partno = "0242135553";
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vin.tecalliance-sea.com/api/tecdoc");
            request.Headers.Add("Authorization", "Aw4OMYTNcOzOYMgD4zN5MYgMkuO4DCDz");

            var requestObj = new requestVehicleAll
            {
                getManufacturers = new requestVehicleAllSub
                {
                    country = "TH",
                    lang = "EN",
                    linkingTargetType = "po"
                }
            };
            // สร้าง StringContent สำหรับ body ของคำขอ
            var json = JsonConvert.SerializeObject(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            // Parse JSON response
            var jsonResponse = JObject.Parse(responseBody);

            var vehicleList = jsonResponse["data"]?["array"]
               ?.Select(token => new
               {
                   manuId = (long?)token["manuId"],
                   manuName = (string)token["manuName"]
               })
               .ToList();

            return Json(new
            {
                VehicleListData = vehicleList
            });
        }
        public class requestVehicleAll
        {
            public requestVehicleAllSub getManufacturers { get; set; }
        }
        public class requestVehicleAllSub
        {
            public string country { get; set; }
            public string lang { get; set; }
            public string linkingTargetType { get; set; }
        }
        [Route("Post/VehicleModel")]
        public async Task<IHttpActionResult> VehicleModel([FromBody] SendVehicleModel models)
        {
            //Partno = "0242135553";
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vin.tecalliance-sea.com/api/tecdoc");
            request.Headers.Add("Authorization", "Aw4OMYTNcOzOYMgD4zN5MYgMkuO4DCDz");

            var requestObj = new requestVehicleModelAll
            {
                getModelSeries = new requestVehicleModelAllSub
                {
                    country = "TH",
                    lang = "EN",
                    linkingTargetType = "po",
                    manuId = models.manuId
                }
            };
            // สร้าง StringContent สำหรับ body ของคำขอ
            var json = JsonConvert.SerializeObject(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            // Parse JSON response
            var jsonResponse = JObject.Parse(responseBody);
            var vehicleModelList = jsonResponse["data"]?["array"]
               ?.Select(token => new
               {
                   modelId = (long?)token["modelId"],
                   modelname = (string)token["modelname"],
                   yearOfConstrFrom = (string)token["yearOfConstrFrom"],
                   yearOfConstrTo = (string)token["yearOfConstrTo"]
               })
               .ToList();

            return Json(new
            {
                VehicleModelListData = vehicleModelList
            });
        }
        public class SendVehicleModel
        {
            public int manuId { get; set; }
        }
        public class requestVehicleModelAll
        {
            public requestVehicleModelAllSub getModelSeries { get; set; }
        }
        public class requestVehicleModelAllSub
        {
            public string country { get; set; }
            public string lang { get; set; }
            public string linkingTargetType { get; set; }
            public int manuId { get; set; }

        }
        [Route("Post/LinkageTargetsModel")]
        public async Task<IHttpActionResult> LinkageTargetsModel([FromBody] SendTargetsModel models)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vin.tecalliance-sea.com/api/tecdoc");
            request.Headers.Add("Authorization", "Aw4OMYTNcOzOYMgD4zN5MYgMkuO4DCDz");

            var requestObj = new LinkageTargetsModelMain
            {
                getLinkageTargets = new LinkageTargetsModelSub
                {
                    linkageTargetCountry = "TH",
                    lang = "EN",
                    linkingTargetType = "P",
                    mfrIds = models.mfrIds,
                    vehicleModelSeriesIds = models.vehicleModelSeriesIds
                }
            };
            var json = JsonConvert.SerializeObject(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            // Parse JSON response
            var jsonResponse = JObject.Parse(responseBody);
            var linkageTargetsArray = jsonResponse["linkageTargets"] as JArray;

            var linkageTargets = linkageTargetsArray?
                .Select(item => new
                {
                    LinkageTargetId = (int?)item["linkageTargetId"],
                    LinkageTargetType = (string)item["linkageTargetType"],
                    SubLinkageTargetType = (string)item["subLinkageTargetType"],
                    MfrName = (string)item["mfrName"],
                    VehicleModelSeriesName = (string)item["vehicleModelSeriesName"],
                    BeginYearMonth = (string)item["beginYearMonth"],
                    EndYearMonth = (string)item["endYearMonth"],
                    ImageURL = item["vehicleImages"]?.FirstOrDefault()?["imageURL400"]?.ToString(),
                    DriveType = (string)item["driveType"],
                    BodyStyle = (string)item["bodyStyle"],
                    FuelMixtureFormationType = (string)item["fuelMixtureFormationType"],
                    FuelType = (string)item["fuelType"],
                    EngineType = (string)item["engineType"],
                    Engines = item["engines"]?.FirstOrDefault()?["code"]?.ToString(),
                })
                .Cast<object>()
                .ToList();
            return Json(new
            {
                TargetsModelListData = linkageTargets
            });
        }
        public class SendTargetsModel
        {
            public int mfrIds { get; set; }
            public int vehicleModelSeriesIds { get; set; }
        }
        //request
        public class LinkageTargetsModelMain
        {
            public LinkageTargetsModelSub getLinkageTargets { get; set; }
        }
        public class LinkageTargetsModelSub
        {
            public string linkageTargetCountry { get; set; }
            public string lang { get; set; }
            public string linkingTargetType { get; set; }
            public int mfrIds { get; set; }
            public int vehicleModelSeriesIds { get; set; }
        }

        [Route("Post/SupplierAll")]
        public async Task<IHttpActionResult> SupplierAll()
        {
            //Partno = "0242135553";
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vin.tecalliance-sea.com/api/tecdoc");
            request.Headers.Add("Authorization", "Aw4OMYTNcOzOYMgD4zN5MYgMkuO4DCDz");

            var requestObj = new requestSupplierAllMain
            {
                getAmBrands = new requestSupplierAlllSub
                {
                    articleCountry = "TH",
                    lang = "EN"
                }
            };
            // สร้าง StringContent สำหรับ body ของคำขอ
            var json = JsonConvert.SerializeObject(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            // Parse JSON response
            var jsonResponse = JObject.Parse(responseBody);

            var supplierlist = jsonResponse["data"]?["array"]
               ?.Select(token => new
               {
                   brandId = (long?)token["brandId"],
                   brandName = (string)token["brandName"]
               })
               .ToList();

            return Json(new
            {
                SupplierListData = supplierlist
            });
        }
        public class requestSupplierAllMain
        {
            public requestSupplierAlllSub getAmBrands { get; set; }
        }
        public class requestSupplierAlllSub
        {
            public string articleCountry { get; set; }
            public string lang { get; set; }
        }
        [Route("Post/GroupProductAll")]
        public async Task<IHttpActionResult> GroupProductAll()
        {
            //Partno = "0242135553";
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vin.tecalliance-sea.com/api/tecdoc");
            request.Headers.Add("Authorization", "Aw4OMYTNcOzOYMgD4zN5MYgMkuO4DCDz");

            var requestObj = new requestGroupProductAllMain
            {
                getGenericArticles = new requestGroupProductAllSub
                {
                    articleCountry = "TH",
                    lang = "TH",
                    searchTreeNodes = true
                }
            };
            // สร้าง StringContent สำหรับ body ของคำขอ
            var json = JsonConvert.SerializeObject(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            // Parse JSON response
            var jsonResponse = JObject.Parse(responseBody);

            var groupProductList = jsonResponse["data"]?["array"]
               ?.Select(token => new
               {
                   assemblyGroup = (string)token["assemblyGroup"],
                   designation = (string)token["designation"],
                   genericArticleId = (string)token["genericArticleId"],
                   masterDesignation = (string)token["masterDesignation"],
               })
               .ToList();

            return Json(new
            {
                GroupProductListData = groupProductList
            });
        }
        public class requestGroupProductAllMain
        {
            public requestGroupProductAllSub getGenericArticles { get; set; }
        }
        public class requestGroupProductAllSub
        {
            public string articleCountry { get; set; }
            public string lang { get; set; }
            public Boolean searchTreeNodes { get; set; }
        }
        [Route("Post/ProductByBrandGroup")]
        public async Task<IHttpActionResult> ProductByBrandGroup([FromBody] SendArticlesProductModels models)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vin.tecalliance-sea.com/api/tecdoc");
            request.Headers.Add("Authorization", "Aw4OMYTNcOzOYMgD4zN5MYgMkuO4DCDz");

            var requestObj = new ArticlesRequestProductMain
            {
                getArticles = new ArticlesRequestProductSub
                {
                    articleCountry = "TH",
                    lang = "EN",
                    dataSupplierIds = models.brandId,
                    genericArticleIds = models.groupId,
                    articleStatusIds = 1,
                    page = 1,
                    perPage = 15,
                    includeAll = true
                }
            };
            // สร้าง StringContent สำหรับ body ของคำขอ
            var json = JsonConvert.SerializeObject(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            // Deserialize the response body to get image data
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
            List<ArticleInfo> articleInfos = new List<ArticleInfo>();
            // Extract image URLs from the response
            if (apiResponse != null && apiResponse.articles != null)
            {
                foreach (var article in apiResponse.articles)
                {
                    var info = new ArticleInfo
                    {
                        MfrName = article.mfrName,
                        ArticleNumber = article.articleNumber,
                        AdditionalDescription = article.misc?.additionalDescription ?? "",
                        ArticleStatusDescription = article.misc?.articleStatusDescription ?? "",
                        GenericArticles = article.genericArticles ?? new List<GenericArticle>(),
                        OemNumbers = article.oemNumbers ?? new List<OemNumber>(),
                        ArticleCriteria = article.articleCriteria ?? new List<ArticleCriteria>(),
                        TradeNumberDetail = article.tradeNumbersDetails ?? new List<TradeNumberDetail>()
                    };

                    articleInfos.Add(info);
                }
            }
            return Json(articleInfos);
        }
        public class SendArticlesProductModels
        {
            public int brandId { get; set; }
            public int groupId { get; set; }
        }
        public class ArticlesRequestProductMain
        {
            public ArticlesRequestProductSub getArticles { get; set; }
        }
        public class ArticlesRequestProductSub
        {
            public string articleCountry { get; set; }
            public string lang { get; set; }
            public int dataSupplierIds { get; set; }
            public int genericArticleIds { get; set; }
            public int articleStatusIds { get; set; }
            public int page { get; set; }
            public int perPage { get; set; }
            public Boolean includeAll { get; set; }
        }
        //response
        public class ArticleInfo
        {
            public string MfrName { get; set; }
            public string ArticleNumber { get; set; }
            public string AdditionalDescription { get; set; }
            public string ArticleStatusDescription { get; set; }
            public List<GenericArticle> GenericArticles { get; set; }
            public List<OemNumber> OemNumbers { get; set; }
            public List<ArticleCriteria> ArticleCriteria { get; set; }
            public List<TradeNumberDetail> TradeNumberDetail { get; set; }
        }
        [HttpPost]
        [Route("Techdoc/SaveImageFromUrl")]
        public async Task<IHttpActionResult> SaveImageFromUrl([FromBody] SaveImageRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Url))
                    return BadRequest("url is required");

                var url = req.Url;
                var folder = string.IsNullOrWhiteSpace(req.Folder) ? "Images" : req.Folder;

                // domain
                var uri = new Uri(url);
                if (!uri.Host.Contains("tecalliance.services"))
                {
                    return Content(HttpStatusCode.BadRequest, new { Status = "Error", Message = "Domain not allowed" });
                }

                // folder
                folder = folder.Replace("\\", "/").Trim('/');
                if (folder.Contains(".."))
                {
                    return Content(HttpStatusCode.BadRequest, new { Status = "Error", Message = "Invalid folder path" });
                }

                var rootPath = HttpContext.Current.Server.MapPath("~/" + folder);

                bool created = false;
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                    created = true;
                }

                var fileName = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = Guid.NewGuid() + ".jpg";
                }

                fileName = Path.GetFileName(fileName);
                var savePath = Path.Combine(rootPath, fileName);

                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(15);

                    var res = await http.GetAsync(url);
                    if (!res.IsSuccessStatusCode)
                    {
                        return Content(HttpStatusCode.BadGateway, new { Status = "Error", Message = "Download failed" });
                    }

                    var bytes = await res.Content.ReadAsByteArrayAsync();
                    System.IO.File.WriteAllBytes(savePath, bytes);
                }

                return Ok(new
                {
                    Status = "OK",
                    FileName = fileName,
                    Path = "/" + folder + "/" + fileName,
                    Warning = created ? "Folder not found. Created automatically." : null
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                new
                {
                    Status = "Error",
                    Message = ex.Message,
                    Detail = ex.InnerException?.Message
                });
            }
        }
        public class SaveImageRequest
        {
            public string Url { get; set; }
            public string Folder { get; set; }
        }
    }
}