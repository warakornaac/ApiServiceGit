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
        [HttpGet]
        [Route("Ecatalog/GetBody")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetBody(string marketSegmentId, string segmentId, string makerId, string rangeId) {
            var responseList = new List<MasterBodyeDataResponse>();
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
                    cmd.Parameters.Add("@inModule", SqlDbType.VarChar, 50).Value = 5;
                    cmd.Parameters.Add("@inMarketseId", SqlDbType.VarChar, 50).Value = marketSegmentId;
                    cmd.Parameters.Add("@inSegmentId", SqlDbType.VarChar, 50).Value = segmentId;
                    cmd.Parameters.Add("@inMakerId", SqlDbType.VarChar, 50).Value = makerId;
                    cmd.Parameters.Add("@inModelRangeId", SqlDbType.VarChar, 50).Value = rangeId;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new MasterBodyeDataResponse {
                                id = Convert.ToString(dr["Id"]),
                                body = Convert.ToString(dr["body"]),
                                bodyType = Convert.ToString(dr["BodyType"]),
                                modelId = Convert.ToString(dr["modelId"]),
                                modelRangeId = Convert.ToString(dr["modelRangeId"]),
                                makerId = Convert.ToString(dr["makerId"]),
                                vehicleSegmentId = Convert.ToString(dr["vehicleSegmentId"]),
                                marketSegmentId = Convert.ToString(dr["marketSegmentId"])
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
        [HttpGet]
        [Route("Ecatalog/GetEngine")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetEngine(string marketSegmentId, string segmentId, string makerId, string rangeId, string bodyId) {
            var responseList = new List<MasterEngineDataResponse>();
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
                    cmd.Parameters.Add("@inModule", SqlDbType.VarChar, 50).Value = 6;
                    cmd.Parameters.Add("@inMarketseId", SqlDbType.VarChar, 50).Value = marketSegmentId;
                    cmd.Parameters.Add("@inSegmentId", SqlDbType.VarChar, 50).Value = segmentId;
                    cmd.Parameters.Add("@inMakerId", SqlDbType.VarChar, 50).Value = makerId;
                    cmd.Parameters.Add("@inModelRangeId", SqlDbType.VarChar, 50).Value = rangeId;
                    cmd.Parameters.Add("@inBodyId", SqlDbType.VarChar, 50).Value = bodyId;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new MasterEngineDataResponse {
                                id = Convert.ToString(dr["Id"]),
                                engineType = Convert.ToString(dr["engineType"]),
                                fuelType = Convert.ToString(dr["fuelType"]),
                                strokes = Convert.ToString(dr["Strokes"]),
                                makerId = Convert.ToString(dr["makerId"]),
                                modelRangeId = Convert.ToString(dr["modelRangeId"]),
                                modelId = Convert.ToString(dr["modelId"]),
                                bodyId = Convert.ToString(dr["bodyId"])
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
        public class MasterBodyeDataResponse
        {
            public string id { get; set; }
            public string body { get; set; }
            public string bodyType { get; set; }
            public string modelId { get; set; }
            public string modelRangeId { get; set; }
            public string makerId { get; set; }
            public string vehicleSegmentId { get; set; }
            public string marketSegmentId { get; set; }
        }
        public class MasterEngineDataResponse
        {
            public string id { get; set; }
            public string engineType { get; set; }
            public string fuelType { get; set; }
            public string strokes { get; set; }
            public string makerId { get; set; }
            public string modelRangeId { get; set; }
            public string modelId { get; set; }
            public string bodyId { get; set; }
        }
    }
}