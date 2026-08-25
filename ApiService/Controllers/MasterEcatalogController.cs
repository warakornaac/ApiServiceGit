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
using System.Data.Common;

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
        [Route("Ecatalog/GetMakerCar")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetMakerCar(string marketSegmentId = "ALL", string vehicleSegmentId = "ALL")
        {
            var responseList = new List<MasterMarkerDataResponse>();
            string errorMessage = "Success";
            //if (string.IsNullOrWhiteSpace(moduleId))
            //{
            //    return Json(new
            //    {
            //        statusCode = 400,
            //        errorMessage = "ModuleId is required",
            //        result = responseList
            //    });
            //}


            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_SearchVIO_Selector_Dev", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inModule", SqlDbType.VarChar, 50).Value = "3";
                    cmd.Parameters.Add("@inMarketseId", SqlDbType.VarChar,50).Value = marketSegmentId;
                    cmd.Parameters.Add("@inSegmentId", SqlDbType.VarChar,50).Value = vehicleSegmentId;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            responseList.Add(new MasterMarkerDataResponse
                            {
                                Id = Convert.ToString(dr["Id"]),
                                Name = Convert.ToString(dr["Name"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            var result = new
            {
                statusCode =
                    errorMessage == "Success"
                    ? 200
                    : 500,

                errorMessage,

                result = responseList
            };
            var jsonLog = JsonConvert.SerializeObject(new
            {
                marketSegmentId = marketSegmentId
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetMakerCar", jsonLog, "");
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

        [HttpGet]
        [Route("Ecatalog/GetBrands")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetBrands(string brandId = "")
        {
            var responseList = new List<MasterbrandsDataResponse>();
            string errorMessage = "Success";
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using(SqlCommand cmd = new SqlCommand("P_Get_Brands", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@brandId", SqlDbType.VarChar, 50).Value = brandId;
                        conn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                responseList.Add(new MasterbrandsDataResponse
                                {
                                    id = Convert.ToString(dr["brandId"]),
                                    name = Convert.ToString(dr["brandName"]),
                                });
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                errorMessage = ex.Message;
            }
            var result = new
            {
                statusCode =
                   errorMessage == "Success"
                   ? 200
                   : 500,

                errorMessage,

                result = responseList
            };
            var jsonLog = JsonConvert.SerializeObject(new
            {
                brandId = brandId,
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetBrands", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(result);

        }
        [HttpGet]
        [Route("Ecatalog/GetProductGroup")]
        [ApiKeyAuthorize]
        public IHttpActionResult GeProductGroups()
        {
            var responseList = new List<MasterCatProductGroupDataResponse>();
            string errorMessage = "Success";
            //if (string.IsNullOrWhiteSpace(cuscod))
            //{
            //    return Json(new
            //    {
            //        statusCode = 400,
            //        errorMessage = "cuscod is required",
            //        result = responseList
            //    });
            //}
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("P_Get_CatProductGroup", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        //cmd.Parameters.Add("@inCUSCOD", SqlDbType.VarChar, 50).Value = cuscod;
                        conn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                responseList.Add(new MasterCatProductGroupDataResponse
                                {
                                    prodgrpid = Convert.ToString(dr["prodGrpId"]),
                                    prodgrpname = Convert.ToString(dr["prodGrpName"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            var result = new
            {
                statusCode =
                   errorMessage == "Success"
                   ? 200
                   : 500,

                errorMessage,

                result = responseList
            };
            var jsonLog = JsonConvert.SerializeObject(new
            {
                //productLine = cuscod,
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetProductGroup", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(result);
        }

        [HttpGet]
        [Route("Ecatalog/GetProductLine")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetProductLine(string prodGrpId = "0")
        {
            var responseList = new List<MasterCatProductLineDataResponse>();
            string errorMessage = "Success";
            //if (string.IsNullOrWhiteSpace(cuscod))
            //{
            //    return Json(new
            //    {
            //        statusCode = 400,
            //        errorMessage = "cuscod is required",
            //        result = responseList
            //    });
            //}
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("P_Get_CatProductLine", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@inProdGrpId", SqlDbType.Int).Value = int.Parse(prodGrpId); 
                        conn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                responseList.Add(new MasterCatProductLineDataResponse
                                {
                                    prodlineid = Convert.ToString(dr["prodLineId"]),
                                    prodlinename = Convert.ToString(dr["prodLineName"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            var result = new
            {
                statusCode =
                   errorMessage == "Success"
                   ? 200
                   : 500,

                errorMessage,

                result = responseList
            };
            var jsonLog = JsonConvert.SerializeObject(new
            {
                prodGrpId = prodGrpId,
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetProductLine", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(result);
        }

        [HttpGet]
        [Route("Ecatalog/GetProductGroupMatched")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetProductGroupMatched(string prodGrpId = "0", string prodLineId = "0")
        {
            var responseList = new List<ProductMatchDataResponse>();
            string errorMessage = "Success";
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("P_Get_ProductGrp_Match", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@inProdGrpId", SqlDbType.Int).Value = prodGrpId;
                        cmd.Parameters.Add("@inProdLineId", SqlDbType.Int).Value = prodLineId;
                        conn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                responseList.Add(new ProductMatchDataResponse
                                {
                                    prodgrpid = Convert.ToString(dr["prodGrpId"]),
                                    prodlineid = Convert.ToString(dr["prodLineId"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            var result = new
            {
                statusCode =
                   errorMessage == "Success"
                   ? 200
                   : 500,

                errorMessage,

                result = responseList
            };
            var jsonLog = JsonConvert.SerializeObject(new
            {
                prodGrpId = prodGrpId,
                prodLineId = prodLineId,
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetProductGroupMatched", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(result);
        }

        [HttpGet]
        [Route("Ecatalog/GetShiptoByCuscode")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetShiptoByCuscode(string cusCode = "")
        {
            var responseList = new List<ShiptoDataResponse>();
            string errorMessage = "Success";
            if (string.IsNullOrWhiteSpace(cusCode))
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "Cuscode is required",
                    result = responseList
                });
            }
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("P_Get_Shipto", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@inCUSCOD", SqlDbType.VarChar, 50).Value = cusCode;                        
                        conn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                responseList.Add(new ShiptoDataResponse
                                {
                                    //cusCode = Convert.ToString(dr["prodGrpId"]),
                                    cusCode = dr["Customer No_"] == DBNull.Value ? "" : dr["Customer No_"].ToString(),
                                    shipCode = dr["Code"] == DBNull.Value ? "" : dr["Code"].ToString(),
                                    name  = dr["Name"] == DBNull.Value ? "" : dr["Name"].ToString(),
                                    address = dr["Address"] == DBNull.Value ? "" : dr["Address"].ToString(),
                                    address2 = dr["Address 2"] == DBNull.Value ? "" : dr["Address 2"].ToString(),
                                    city = dr["City"] == DBNull.Value ? "" : dr["City"].ToString(),
                                    contact = dr["Contact"] == DBNull.Value ? "" : dr["Contact"].ToString(),
                                    phone = dr["Phone No_"] == DBNull.Value ? "" : dr["Phone No_"].ToString(),
                                    postCode = dr["Post Code"] == DBNull.Value ? "" : dr["Post Code"].ToString(),
                                    shipFromWarehowse = dr["Ship From WH"] == DBNull.Value ? "" : dr["Ship From WH"].ToString(),
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            if (responseList.Count == 0)
            {
                return Json(new
                {
                    statusCode = 404,
                    errorMessage = "ShipTo not found.",
                    result = new { }
                });
            }
            var result = new
            {
                statusCode =
                   errorMessage == "Success"
                   ? 200
                   : 500,

                errorMessage,

                result = responseList
            };
            var jsonLog = JsonConvert.SerializeObject(new
            {
                cusCode = cusCode
                //prodGrpId = prodGrpId,
                //prodLineId = prodLineId,
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetShiptoByCuscode", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(result);
        }

        [HttpGet]
        [Route("Ecatalog/GetSalesmanName")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetSalesmanAll(string slmcode = "")
        {
            var responseList = new List<GetSalesmanRespone>();
            string errorMessage = "Success";
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {                    
                    using (SqlCommand cmd = new SqlCommand("P_Get_SalesmanAll", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@inSLMCOD", SqlDbType.VarChar, 50).Value = slmcode;
                        conn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                responseList.Add(new GetSalesmanRespone
                                {
                                    slmCode = dr["SLMCOD"] == DBNull.Value ? "" : dr["SLMCOD"].ToString(),
                                    slmName = dr["SLMNAM"] == DBNull.Value ? "" : dr["SLMNAM"].ToString()
                                });
                            }
                        }
                    }
                }

            }
            catch (Exception message)
            {
                errorMessage = message.Message;
            }
            if (responseList.Count == 0)
            {
                return Json(new
                {
                    statusCode = 404,
                    errorMessage = "SaleMan not found.",
                    result = new { }
                });
            }
            var result = new
            {
                statusCode =
                errorMessage == "Success"
                ? 200
                : 500,

                errorMessage,

                result = responseList
            };
            var jsonLog = JsonConvert.SerializeObject(new
            {
                
                //prodGrpId = prodGrpId,
                //prodLineId = prodLineId,
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetSalesmanName", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(result);
        }

        [HttpGet]
        [Route("Ecatalog/CustomerbySalesman")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetCustomerbySalesman(string slmcode = "")
        {
            var responseList = new List<GetCustomerRespone>();
            string errorMessage = "Success";
            if (string.IsNullOrEmpty(slmcode))
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "Slmcode is required",
                    result = responseList
                });
            }
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("P_Get_CustomerbySalesman", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@inSLMCOD", SqlDbType.VarChar, 50).Value = slmcode;
                        conn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                responseList.Add(new GetCustomerRespone
                                {
                                    company = dr["Company"] == DBNull.Value ? "" : dr["Company"].ToString(),
                                    cuscode = dr["CUSCOD"] == DBNull.Value ? "" : dr["CUSCOD"].ToString(),
                                    cusname = dr["CUSNAM"] == DBNull.Value ? "" : dr["CUSNAM"].ToString(),
                                    slmcode = dr["SLMCOD"] == DBNull.Value ? "" : dr["SLMCOD"].ToString(),                                
                                });
                            }
                        }
                    }
                }

            }
            catch (Exception message)
            {
                errorMessage = message.Message;
            }
            if (responseList.Count == 0)
            {
                return Json(new
                {
                    statusCode = 404,
                    errorMessage = "Customer not found.",
                    result = new { }
                });
            }
            var result = new
            {
                statusCode =
                errorMessage == "Success"
                ? 200
                : 500,

                errorMessage,

                result = responseList
            };
            var jsonLog = JsonConvert.SerializeObject(new
            {
                slmcode= slmcode
                //prodGrpId = prodGrpId,
                //prodLineId = prodLineId,
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/CustomerbySalesman", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(result);
        }

        [HttpGet]
        [Route("Ecatalog/GetInfomantionCustomer")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetInfomantionCustomer(string cuscode = "")
        {
            var responseList = new List<GetCustomerInformationRespone>();
            string errorMessage = "Success";
            if (string.IsNullOrEmpty(cuscode))
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "Cuscode is required",
                    result = responseList
                });
            }
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("P_Get_CustomerInformation", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@inCUSCOD", SqlDbType.VarChar, 50).Value = cuscode;
                        conn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                responseList.Add(new GetCustomerInformationRespone
                                {                                    
                                    cuscode = dr["CUSCOD"] == DBNull.Value ? "" : dr["CUSCOD"].ToString(),
                                    cusname = dr["CUSNAM"] == DBNull.Value ? "" : dr["CUSNAM"].ToString(),
                                    pro = dr["PRO"] == DBNull.Value ? "" : dr["PRO"].ToString(),
                                    address = dr["ADDR_01"] == DBNull.Value ? "" : dr["ADDR_01"].ToString(),
                                    address2 = dr["ADDR_02"] == DBNull.Value ? "" : dr["ADDR_02"].ToString(),
                                    custype = dr["CUSTYP"] == DBNull.Value ? "" : dr["CUSTYP"].ToString(),
                                    slmcode = dr["SLMCOD"] == DBNull.Value ? "" : dr["SLMCOD"].ToString(),
                                    inactive = dr["INACTIVE"] == DBNull.Value ? "" : dr["INACTIVE"].ToString(),
                                    block = dr["BLOCKED"] == DBNull.Value ? "" : dr["BLOCKED"].ToString(),
                                    aacpaytrm = dr["AACPAYTRM"] == DBNull.Value ? "" : dr["AACPAYTRM"].ToString(),
                                    tacpaytrm = dr["TACPAYTRM"] == DBNull.Value ? "" : dr["TACPAYTRM"].ToString(),
                                    phone = dr["TELNUM"] == DBNull.Value ? "" : dr["TELNUM"].ToString(),
                                    rating = dr["Rating"] == DBNull.Value ? "" : dr["Rating"].ToString(),
                                });
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            if (responseList.Count == 0)
            {
                return Json(new
                {
                    statusCode = 404,
                    errorMessage = "CustomerInformation not found.",
                    result = new { }
                });
            }
            var result = new
            {
                statusCode =
                errorMessage == "Success"
                ? 200
                : 500,

                errorMessage,

                result = responseList
            };
            var jsonLog = JsonConvert.SerializeObject(new
            {
                cuscode= cuscode
                //prodGrpId = prodGrpId,
                //prodLineId = prodLineId,
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetInfomantionCustomer", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(result);

        }


        [HttpGet]
        [Route("Ecatalog/GetPicMeiaPortal")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetPicMeiaPortal(string stkcode)
        {
            var responseList = new List<GetPictureMediaPortalRespone>();
            string errorMessage = "Success";
            if (string.IsNullOrEmpty(stkcode))
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "STKCODE is required",
                    result = responseList
                });
            }
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("P_Search_Pic_MediaPortal", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@inSTKCOD", SqlDbType.VarChar, 50).Value = stkcode;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                responseList.Add(new GetPictureMediaPortalRespone
                                {
                                    Stkcode = reader["Stkcode"] == DBNull.Value ? "" : reader["Stkcode"].ToString(),
                                    Url = reader["Url"] == DBNull.Value ? "" : reader["Url"].ToString(),
                                    Filename = reader["Filename"] == DBNull.Value ? "" : reader["Filename"].ToString(),
                                    Source = reader["Source"] == DBNull.Value ? "" : reader["Source"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                errorMessage = ex.Message;
                return Json(new
                {
                    statusCode = 500,
                    errorMessage = errorMessage,
                    result = new { }
                });
            }

            
            var result = new
            {
                statusCode =
                errorMessage == "Success"
                ? 200
                : 500,

                errorMessage,

                result = responseList
            };

            if (responseList.Count == 0)
            {
                return Json(new
                {
                    statusCode = 404,
                    errorMessage = "Picture not found.",
                    result = new { }
                });
            }
            var jsonLog = JsonConvert.SerializeObject(new
            {
                STKCOD = stkcode
                //prodGrpId = prodGrpId,
                //prodLineId = prodLineId,
            });
            string jsonReturn = JsonConvert.SerializeObject(result);
            String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetPicMeiaPortal", jsonLog, "");
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


        public class MasterbrandsDataResponse
        {
            public string id { get; set; }
            public string name { get; set; }
        }
        public class MasterCatProductGroupDataResponse
        {
            public string prodgrpid { get; set; }
            public string prodgrpname { get; set; }
        }
        public class MasterCatProductLineDataResponse
        {
            public string prodlineid { get; set; }
            public string prodlinename { get; set; }
        }
        public class ProductMatchDataResponse
        {
            public string prodgrpid { get; set; }
            public string prodlineid { get; set; }
        }
        public class ShiptoDataResponse
        {
            public string cusCode { get; set; }
            public string shipCode { get; set; }
            public string name { get; set; }
            public string address { get; set; }
            public string address2 { get; set; }
            public string city { get; set; }
            public string contact { get; set; }
            public string phone { get; set; }
            public string postCode { get; set; }
            public string shipFromWarehowse { get; set; }

        }
        public class GetSalesmanRespone 
        { 
            public string slmCode { get; set; }
            public string slmName { get; set; }
        }
        public class GetCustomerRespone
        {
            public string company {  get; set; }
            public string cuscode {  get; set; }
            public string cusname { get; set; }
            public string slmcode { get; set; }
        }
        public class GetCustomerInformationRespone
        {
            public string cuscode { get; set; }
            public string cusname { get; set; }
            public string pro { get; set; }
            public string address { get; set; }
            public string address2 { get; set; }
            public string custype { get; set; }
            public string slmcode { get; set; }
            public string inactive { get; set; }
            public string block { get; set; }
            public string aacpaytrm { get; set; }
            public string tacpaytrm { get; set; }
            public string phone { get; set; }
            public string rating { get; set; }
        }
        public class GetPictureMediaPortalRespone
        {
            public string Stkcode { get; set; } 
            public string Url { get; set; }
            public string Filename { get; set; }
            public string Source { get; set; }
        }

    }
}