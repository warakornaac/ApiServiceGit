using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using RouteAttribute = System.Web.Http.RouteAttribute;
using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using ApiService.Filters;
using System.Net;


namespace ApiService.Controllers
{
    public class ItemsController : ApiController
    {
        private readonly ApiServerController _apiServerService;
        public ItemsController()
        {
            _apiServerService = new ApiServerController();
        }

        [HttpPost]
        [Route("Items/SearchByDescription")]
        [ApiKeyAuthorize]
        public IHttpActionResult PostItems([FromBody] List<ItemRequest> requests)
        {
            if (requests == null) { 
                return BadRequest("Empty data request");
            }
            var responses = new List<ItemResponse>();
            var seenItemNos = new HashSet<string>();
            string errorMessageTxt = "success";
            string[] keySearchArray = requests.Select(r => r.key).ToArray();
            var connectionString = ConfigurationManager.ConnectionStrings["MobileOrder_ConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(connectionString);
            var okresp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                ReasonPhrase = "Success"
            };
            try
            {
                conn.Open();
                // var keySearchArray = string.Join(" ", requests.Select(r => r.key));
                string concatenatedKeys = string.Join(" ", keySearchArray);
                SqlCommand cmd = new SqlCommand("P_Search_Item_Media", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@inSearch", concatenatedKeys);
                SqlDataReader read = cmd.ExecuteReader();
                while (read.Read())
                {
                    var itemNo = read["STKCOD"] != DBNull.Value ? read["STKCOD"].ToString() : "";
                    var description = read["STKDES"] != DBNull.Value ? read["STKDES"].ToString() : "";
                    if (!string.IsNullOrEmpty(itemNo) && seenItemNos.Add(itemNo))
                    {
                        responses.Add(new ItemResponse
                        {
                            itemNo = itemNo,
                            fullDescription = description
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessageTxt = ex.Message.ToString();
            }
            DataRespond dataRes = new DataRespond();
            dataRes.statusCode = Convert.ToInt32(okresp.StatusCode);
            dataRes.errorMessage = errorMessageTxt;
            dataRes.keySearch = string.Join(",  ", keySearchArray);
            dataRes.result = responses;

            String requestDataLog = JsonConvert.SerializeObject(requests);
            string jsonReturn = JsonConvert.SerializeObject(dataRes);

            String lastId = _apiServerService.SaveApiResponse("Items/SearchByDescription", requestDataLog.ToString(), "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(dataRes);
        }

        public class ItemRequest
        {
            public string key { get; set; }
        }
        public class ItemResponse
        {
            public string itemNo { get; set; }
            public string fullDescription { get; set; }
        }
        public class DataRespond
        {
            public int statusCode { get; set; }
            public string errorMessage { get; set; }
            public string keySearch { get; set; }
            public List<ItemResponse> result { get; set; }
        }

    }
}