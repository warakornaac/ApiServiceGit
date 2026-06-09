using ApiService.Filters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Http;
using RouteAttribute = System.Web.Http.RouteAttribute;
using System.Web.Mvc;
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using Newtonsoft.Json;

namespace ApiService.Controllers
{
    public class MasterEcatalogController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        public MasterEcatalogController() {
            _apiServerService = new ApiServerController();
        }
        // GET: GetMarketSegment 
        // Market, Vehicle, Marker
        [HttpGet]
        [Route("Ecatalog/GetMarketCar")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetMarketCar(string moduleId = "1") {
            var responseList = new List<MasterMarkerDataResponse>();
            string errorMessage = "Success";
            if (string.IsNullOrWhiteSpace(moduleId)) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "ModuleId is required",
                    result = responseList
                });
            }

            try {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_SearchVIO_Selector_Dev", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inModule", SqlDbType.VarChar, 50).Value = moduleId;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new MasterMarkerDataResponse {
                               Id = Convert.ToString(dr["Id"]),
                               Name = Convert.ToString(dr["Name"])
                           });
                        }
                    }
                }
            }
            catch (Exception ex) {
                errorMessage = ex.Message;
            }

            var result = new {
                statusCode =
                    errorMessage == "Success"
                    ? 200
                    : 500,

                errorMessage,

                result = responseList
            };
            var jsonLog = JsonConvert.SerializeObject(new {
                moduleId = moduleId
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetMarketCar", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(result);
        }
        [HttpGet]
        [Route("Ecatalog/GetModelRange")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetModelRange(string marketSegmentId, string segmentId, string makerId) {
            var responseList = new List<MasterModelRangeDataResponse>();
            string errorMessage = "Success";
            if (string.IsNullOrWhiteSpace(marketSegmentId)) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "ModuleId is required",
                    result = responseList
                });
            }

            try {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_SearchVIO_Selector_Dev", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inModule", SqlDbType.VarChar, 50).Value = 4;
                    cmd.Parameters.Add("@inMarketseID", SqlDbType.VarChar, 50).Value = marketSegmentId;
                    cmd.Parameters.Add("@inSegmentID", SqlDbType.VarChar, 50).Value = segmentId;
                    cmd.Parameters.Add("@inMakerID", SqlDbType.VarChar, 50).Value = makerId;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new MasterModelRangeDataResponse {
                                Id = Convert.ToString(dr["Id"]),
                                Model = Convert.ToString(dr["Model"]),
                                MakerId = Convert.ToString(dr["MakerId"]),
                                MarketSegmentId = Convert.ToString(dr["MarketSegmentId"]),
                                ModelRangeId = Convert.ToString(dr["ModelRangeId"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex) {
                errorMessage = ex.Message;
            }

            var result = new {
                statusCode =
                    errorMessage == "Success"
                    ? 200
                    : 500,

                errorMessage,

                result = responseList
            };
            var jsonLog = JsonConvert.SerializeObject(new {
                moduleId = 3,
                marketSegmentId = marketSegmentId,
                segmentId = segmentId,
                makerId = makerId
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetMarketCar", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(result);
        }
        public class MasterMarkerDataResponse
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
        public class MasterModelRangeDataResponse
        {
            public string Id { get; set; }
            public string Model { get; set; }
            public string MakerId { get; set; }
            public string MarketSegmentId { get; set; }
            public string ModelRangeId { get; set; }
        }
    }
}