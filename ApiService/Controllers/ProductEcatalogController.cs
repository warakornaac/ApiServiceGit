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
    public class ProductEcatalogController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        public ProductEcatalogController() {
            _apiServerService = new ApiServerController();
        }
        // GET: ProductCatalog
        //[HttpGet]
        //[Route("Ecatalog/GetKtypeByCar")]
        //[ApiKeyAuthorize]
        //public IHttpActionResult GetKtypeByCar(string marketSegmentId, string segmentId, string makerId, string rangeId, string bodyId, string engineId, string yearFrom, string yearTo, string driveType) {
        //    var responseList = new List<ProductKtypeDataResponse>();
        //    var ktypeList = new List<string>();
        //    string errorMessage = "Success";
        //    if (string.IsNullOrWhiteSpace(marketSegmentId) && string.IsNullOrWhiteSpace(segmentId) && string.IsNullOrWhiteSpace(makerId) && string.IsNullOrWhiteSpace(rangeId)) {
        //        return Json(new {
        //            statusCode = 400,
        //            errorMessage = "marketSegmentId, segmentId, makerId or rangeId is required",
        //            result = responseList
        //        });
        //    }
        //    try {
        //        string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
        //        using (SqlConnection conn = new SqlConnection(connectionString))
        //        using (SqlCommand cmd = new SqlCommand("P_Search_Ktype_By_Car", conn)) {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.Add("@inMarketseId", SqlDbType.VarChar, 50).Value = marketSegmentId;
        //            cmd.Parameters.Add("@inVehicleId", SqlDbType.VarChar, 50).Value = segmentId;
        //            cmd.Parameters.Add("@inMakerId", SqlDbType.VarChar, 50).Value = makerId;
        //            cmd.Parameters.Add("@inModelrangeId", SqlDbType.VarChar, 50).Value = "ALL";
        //            cmd.Parameters.Add("@inModelId", SqlDbType.VarChar, 50).Value = rangeId;
        //            cmd.Parameters.Add("@inBodyId", SqlDbType.VarChar, 50).Value = bodyId;
        //            cmd.Parameters.Add("@inEngineId", SqlDbType.VarChar, 50).Value = engineId;
        //            cmd.Parameters.Add("@inYearFrom", SqlDbType.VarChar, 50).Value = yearFrom;
        //            cmd.Parameters.Add("@inYearTo", SqlDbType.VarChar, 50).Value = yearTo;
        //            cmd.Parameters.Add("@inDriveType", SqlDbType.VarChar, 50).Value = driveType;
        //            conn.Open();
        //            using (SqlDataReader dr = cmd.ExecuteReader()) {
        //                while (dr.Read()) {
        //                    string ktypeTmp = dr["kType"] == DBNull.Value ? "" : dr["kType"].ToString();
        //                    ktypeList.Add(ktypeTmp);
        //                    responseList.Add(new ProductKtypeDataResponse {
        //                        marketSegmentId = dr["marketSegmentId"] == DBNull.Value ? "" : dr["marketSegmentId"].ToString(),
        //                        vehicleSegmentId = dr["vehicleSegmentId"] == DBNull.Value ? "" : dr["vehicleSegmentId"].ToString(),
        //                        makerId = dr["makerId"] == DBNull.Value ? "" : dr["makerId"].ToString(),
        //                        modelRangeId = dr["modelRangeId"] == DBNull.Value ? "" : dr["modelRangeId"].ToString(),
        //                        modelId = dr["modelId"] == DBNull.Value ? "" : dr["modelId"].ToString(),
        //                        bodyId = dr["bodyId"] == DBNull.Value ? "" : dr["bodyId"].ToString(),
        //                        driveType = dr["driveType"] == DBNull.Value ? "" : dr["driveType"].ToString(),
        //                        yearFrom = dr["yearFrom"] == DBNull.Value ? "" : dr["yearFrom"].ToString(),
        //                        yearTo = dr["yearTo"] == DBNull.Value ? "" : dr["yearTo"].ToString(),
        //                        kType = dr["kType"] == DBNull.Value ? "" : dr["kType"].ToString(),
        //                        truType = dr["truType"] == DBNull.Value ? "" : dr["truType"].ToString()
        //                    });
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex) {
        //        errorMessage = ex.Message;
        //    }
        //    string ktypeConvert = string.Join(",", ktypeList.Distinct());
        //    //var product = await GetProductByKtype(ktype.listKtype);
        //    var result = new {
        //        statusCode =
        //            errorMessage == "Success"
        //            ? 200
        //            : 500,

        //        errorMessage,

        //        result = responseList
        //    };
        //    //var jsonLog = JsonConvert.SerializeObject(new {
        //    //    moduleId = 3,
        //    //    marketSegmentId = marketSegmentId,
        //    //    segmentId = segmentId,
        //    //    makerId = makerId
        //    //});
        //    //string jsonReturn = JsonConvert.SerializeObject(result);
        //    //String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetMarketCar", jsonLog, "");
        //    //_apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

        //    return Json(result);
        //}

        //public IHttpActionResult GetProductByKtype(string listKtype) {
        //    var responseList = new List<ProductDetailDataResponse>();
        //    string errorMessage = "Success";
        //    if (string.IsNullOrWhiteSpace(listKtype)) {
        //        return Json(new {
        //            statusCode = 400,
        //            errorMessage = "listKtype is required",
        //            result = responseList
        //        });
        //    }
        //    try {
        //        string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
        //        using (SqlConnection conn = new SqlConnection(connectionString))
        //        using (SqlCommand cmd = new SqlCommand("P_Search_Product_By_Ktype", conn)) {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.Add("@inKtypeList", SqlDbType.VarChar, 50).Value = listKtype;
        //            conn.Open();
        //            using (SqlDataReader dr = cmd.ExecuteReader()) {
        //                while (dr.Read()) {
        //                    responseList.Add(new ProductDetailDataResponse {
        //                        stkcode = dr["stkcode"] == DBNull.Value ? "" : dr["stkcode"].ToString(),
        //                        description = dr["description"] == DBNull.Value ? "" : dr["description"].ToString(),
        //                        brand = dr["brand"] == DBNull.Value ? "" : dr["brand"].ToString(),
        //                        makerName = dr["makerName"] == DBNull.Value ? "" : dr["makerName"].ToString(),
        //                        modelName = dr["modelName"] == DBNull.Value ? "" : dr["modelName"].ToString(),
        //                        qtyReady = dr["qtyReady"] == DBNull.Value ? "" : dr["qtyReady"].ToString(),
        //                        price = dr["price"] == DBNull.Value ? "" : dr["price"].ToString()
        //                    });
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex) {
        //        errorMessage = ex.Message;
        //    }

        //    var result = new {
        //        statusCode =
        //            errorMessage == "Success"
        //            ? 200
        //            : 500,

        //        errorMessage,

        //        result = responseList
        //    };
        //    //var jsonLog = JsonConvert.SerializeObject(new {
        //    //    moduleId = 3,
        //    //    marketSegmentId = marketSegmentId,
        //    //    segmentId = segmentId,
        //    //    makerId = makerId
        //    //});
        //    //string jsonReturn = JsonConvert.SerializeObject(result);
        //    //String lastId = _apiServerService.SaveApiResponse("Ecatalog/GetMarketCar", jsonLog, "");
        //    //_apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

        //    return Json(result);
        //}
        //get kType
        private List<string> GetKtypeListByCar(string marketSegmentId, string segmentId, string makerId, string rangeId, string bodyId, string engineId, string yearFrom, string yearTo, string driveType) {
            List<string> ktypeList = new List<string>();

            string connString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("P_Search_Ktype_By_Car", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@inMarketseId", marketSegmentId);
                cmd.Parameters.AddWithValue("@inVehicleId", segmentId);
                cmd.Parameters.AddWithValue("@inMakerId", makerId);
                cmd.Parameters.AddWithValue("@inModelId", rangeId);
                cmd.Parameters.AddWithValue("@inBodyId", bodyId);
                cmd.Parameters.AddWithValue("@inEngineId", engineId);
                cmd.Parameters.AddWithValue("@inYearFrom", yearFrom);
                cmd.Parameters.AddWithValue("@inYearTo", yearTo);
                cmd.Parameters.AddWithValue("@inDriveType", driveType);

                conn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader()) {
                    while (dr.Read()) {
                        ktypeList.Add(
                            dr["kType"].ToString());
                    }
                }
            }

            return ktypeList;
        }
        //get product
        private List<ProductSearchVioDataResponse> GetProductsByKtype(List<string> ktypes) {
            var responseList = new List<ProductSearchVioDataResponse>();

            string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;

            // =====================
            // TVP
            // =====================

            DataTable dtKtype = new DataTable();

            dtKtype.Columns.Add("Ktype", typeof(string));

            foreach (string ktype in ktypes) {
                dtKtype.Rows.Add(
                    ktype);
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("P_Search_Product_By_Ktype", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;

                SqlParameter p = cmd.Parameters.AddWithValue("@inKtypeList", dtKtype);
                p.SqlDbType = SqlDbType.Structured;
                p.TypeName = "dbo.KtypeListTmp";

                conn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader()) {
                    while (dr.Read()) {
                        responseList.Add(
                            new ProductSearchVioDataResponse {
                                stkcode = dr["stkcode"] == DBNull.Value ? "" : dr["stkcode"].ToString(),
                                stkcodeDescription = dr["stkcodeDescription"] == DBNull.Value ? "" : dr["stkcodeDescription"].ToString(),
                                brand = dr["brand"] == DBNull.Value ? "" : dr["brand"].ToString(),
                                makerName = dr["makerName"] == DBNull.Value ? "" : dr["makerName"].ToString(),
                                modelName = dr["modelName"] == DBNull.Value ? "" : dr["modelName"].ToString(),
                                qtyReady = dr["qtyReady"] == DBNull.Value ? "" : dr["qtyReady"].ToString(),
                                price = dr["price"] == DBNull.Value ? "" : dr["price"].ToString(),
                                productGroup = dr["productGroup"] == DBNull.Value ? "" : dr["productGroup"].ToString(),
                                productLine = dr["productLine"] == DBNull.Value ? "" : dr["productLine"].ToString(),
                                imagePath = dr["imagePath"] == DBNull.Value ? "" : dr["imagePath"].ToString(),
                            });
                    }
                }
            }

            return responseList;
        }
        [HttpGet]
        [Route("Ecatalog/GetProductBySearchVio")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetProductBySearchVio(string marketSegmentId, string segmentId, string makerId, string rangeId, string bodyId, string engineId, string yearFrom, string yearTo, string driveType) {
            try {
                // หา Ktype
                List<string> ktypes =
                    GetKtypeListByCar(
                        marketSegmentId,
                        segmentId,
                        makerId,
                        rangeId,
                        bodyId,
                        engineId,
                        yearFrom,
                        yearTo,
                        driveType);

                if (ktypes.Count == 0) {
                    return Json(new {
                        statusCode = 200,
                        errorMessage = "Ktype not found",
                        result = new object[0]
                    });
                }

                // หา Product
                var products = GetProductsByKtype(ktypes.Distinct().ToList());

                return Json(new {
                    statusCode = 200,
                    errorMessage = "Success",
                    result = products
                });
            }
            catch (Exception ex) {
                return Json(new {
                    statusCode = 500,
                    errorMessage = ex.Message,
                    result = new object[0]
                });
            }
        }
        //get count by tab
        [HttpGet]
        [Route("Ecatalog/GetTabItemCountProduct")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetTabItemCountProduct(string stkcode) {
            var responseList = new List<ProductTabItemCountDataResponse>();
            string errorMessage = "Success";
            if (string.IsNullOrWhiteSpace(stkcode)) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "Stkcode is required",
                    result = responseList
                });
            }

            try {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Get_Product_Api_Count", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inStkcode", SqlDbType.VarChar, 50).Value = stkcode;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new ProductTabItemCountDataResponse {
                                stkcode = dr["stkcode"] == DBNull.Value ? "" : dr["stkcode"].ToString(),
                                stkcodeDescription = dr["stkcodeDescription"] == DBNull.Value ? "" : dr["stkcodeDescription"].ToString(),
                                brandName = dr["brandName"] == DBNull.Value ? "" : dr["brandName"].ToString(),
                                makerName = dr["makerName"] == DBNull.Value ? "" : dr["makerName"].ToString(),
                                modelName = dr["modelName"] == DBNull.Value ? "" : dr["modelName"].ToString(),
                                qtyReady = dr["qtyReady"] == DBNull.Value ? "" : dr["qtyReady"].ToString(),
                                price = dr["price"] == DBNull.Value ? "" : dr["price"].ToString(),
                                productGroup = dr["productGroup"] == DBNull.Value ? "" : dr["productGroup"].ToString(),
                                productLine = dr["productLine"] == DBNull.Value ? "" : dr["productLine"].ToString(),
                                imagePath = dr["imagePath"] == DBNull.Value ? "" : dr["imagePath"].ToString(),

                                countProductDes = dr["countProductDes"] == DBNull.Value ? 0 : Convert.ToInt32(dr["countProductDes"]),
                                countProductSpec = dr["countProductSpec"] == DBNull.Value ? 0 : Convert.ToInt32(dr["countProductSpec"]),
                                countProductImage = dr["countProductImage"] == DBNull.Value ? 0 : Convert.ToInt32(dr["countProductImage"]),
                                countProductOem = dr["countProductOem"] == DBNull.Value ? 0 : Convert.ToInt32(dr["countProductOem"]),
                                countProductCom = dr["countProductCom"] == DBNull.Value ? 0 : Convert.ToInt32(dr["countProductCom"]),
                                countProductLinkage = dr["countProductLinkage"] == DBNull.Value ? 0 : Convert.ToInt32(dr["countProductLinkage"])

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
            //keep log
            //var requestLog = JsonConvert.SerializeObject(new {
            //    stkcode = stkcode
            //});

            //var responseLog = JsonConvert.SerializeObject(result);

            //String lastId = _apiServerService.SaveApiResponse(
            //    "Ecatalog/GetTabItemCountProduct",
            //    requestLog,
            //    ""
            //);

            //_apiServerService.UpdateApiRespone(lastId, responseLog);

            return Json(result);
        }
        [HttpGet]
        [Route("Ecatalog/GetTabDescription")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetTabDescription(string stkcode) {
            var responseList = new List<ProductTabDescriptionResponse>();
            string errorMessage = "Success";
            if (string.IsNullOrWhiteSpace(stkcode)) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "Stkcode is required",
                    result = responseList
                });
            }

            try {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Get_Product_Description", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inStkcode", SqlDbType.VarChar, 50).Value = stkcode;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new ProductTabDescriptionResponse {
                                stkcode = dr["Stkcode"] == DBNull.Value ? "" : dr["Stkcode"].ToString(),
                                seqDescription = dr["SeqDescription"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SeqDescription"]),
                                title = dr["Title"] == DBNull.Value ? "" : dr["Title"].ToString(),
                                description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString(),
                                insertedBy = dr["InsertedBy"] == DBNull.Value ? "" : dr["InsertedBy"].ToString(),
                                insertedDate = dr["InsertedDate"] == DBNull.Value ? "" : dr["InsertedDate"].ToString(),
                                updatedBy = dr["UpdatedBy"] == DBNull.Value ? "" : dr["UpdatedBy"].ToString(),
                                updatedDate = dr["UpdatedDate"] == DBNull.Value ? "" : dr["UpdatedDate"].ToString()

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
            //keep log
            //var requestLog = JsonConvert.SerializeObject(new {
            //    stkcode = stkcode
            //});

            //var responseLog = JsonConvert.SerializeObject(result);

            //String lastId = _apiServerService.SaveApiResponse(
            //    "Ecatalog/GetTabDescription",
            //    requestLog,
            //    ""
            //);

            //_apiServerService.UpdateApiRespone(lastId, responseLog);

            return Json(result);
        }
        [HttpGet]
        [Route("Ecatalog/GetTabSpec")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetTabSpec(string stkcode) {
            var responseList = new List<ProductTabSpecResponse>();
            string errorMessage = "Success";
            if (string.IsNullOrWhiteSpace(stkcode)) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "Stkcode is required",
                    result = responseList
                });
            }

            try {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Get_Product_Spec", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inStkcode", SqlDbType.VarChar, 50).Value = stkcode;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new ProductTabSpecResponse {
                                stkcode = dr["Stkcode"] == DBNull.Value ? "" : dr["Stkcode"].ToString(),
                                seqSpec = dr["SeqSpec"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SeqSpec"]),
                                title = dr["Title"] == DBNull.Value ? "" : dr["Title"].ToString(),
                                description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString(),
                                insertedBy = dr["InsertedBy"] == DBNull.Value ? "" : dr["InsertedBy"].ToString(),
                                insertedDate = dr["InsertedDate"] == DBNull.Value ? "" : dr["InsertedDate"].ToString(),
                                updatedBy = dr["UpdatedBy"] == DBNull.Value ? "" : dr["UpdatedBy"].ToString(),
                                updatedDate = dr["UpdatedDate"] == DBNull.Value ? "" : dr["UpdatedDate"].ToString()

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
            //keep log
            //var requestLog = JsonConvert.SerializeObject(new {
            //    stkcode = stkcode
            //});

            //var responseLog = JsonConvert.SerializeObject(result);

            //String lastId = _apiServerService.SaveApiResponse(
            //    "Ecatalog/GetTabSpec",
            //    requestLog,
            //    ""
            //);

            //_apiServerService.UpdateApiRespone(lastId, responseLog);

            return Json(result);
        }
        [HttpGet]
        [Route("Ecatalog/GetTabImage")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetTabImage(string stkcode) {
            var responseList = new List<ProductTabImageResponse>();
            string errorMessage = "Success";
            if (string.IsNullOrWhiteSpace(stkcode)) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "Stkcode is required",
                    result = responseList
                });
            }

            try {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Get_Product_Image", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inStkcode", SqlDbType.VarChar, 50).Value = stkcode;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new ProductTabImageResponse {
                                stkcode = dr["Stkcode"]?.ToString() ?? "",
                                seqImage = dr["SeqImage"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SeqImage"]),
                                filename = dr["Filename"]?.ToString() ?? "",
                                url = dr["Url"]?.ToString() ?? "",
                                insertedBy = dr["InsertedBy"]?.ToString() ?? "",
                                insertedDate = dr["InsertedDate"]?.ToString() ?? "",
                                updatedBy = dr["UpdatedBy"]?.ToString() ?? "",
                                updatedDate = dr["UpdatedDate"]?.ToString() ?? ""

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
            //keep log
            //var requestLog = JsonConvert.SerializeObject(new {
            //    stkcode = stkcode
            //});

            //var responseLog = JsonConvert.SerializeObject(result);

            //String lastId = _apiServerService.SaveApiResponse(
            //    "Ecatalog/GetTabImage",
            //    requestLog,
            //    ""
            //);

            //_apiServerService.UpdateApiRespone(lastId, responseLog);

            return Json(result);
        }
        [HttpGet]
        [Route("Ecatalog/GetTabOem")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetTabOem(string stkcode) {
            var responseList = new List<ProductTabOemResponse>();
            string errorMessage = "Success";
            if (string.IsNullOrWhiteSpace(stkcode)) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "Stkcode is required",
                    result = responseList
                });
            }

            try {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Get_Product_Oem", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inStkcode", SqlDbType.VarChar, 50).Value = stkcode;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new ProductTabOemResponse {
                                stkcode = dr["Stkcode"] == DBNull.Value ? "" : dr["Stkcode"].ToString(),
                                seqOem = dr["SeqOem"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SeqOem"]),
                                oemNumber = dr["OemNumber"] == DBNull.Value ? "" : dr["OemNumber"].ToString(),
                                makerName = dr["MakerName"] == DBNull.Value ? "" : dr["MakerName"].ToString(),
                                insertedBy = dr["InsertedBy"] == DBNull.Value ? "" : dr["InsertedBy"].ToString(),
                                insertedDate = dr["InsertedDate"] == DBNull.Value ? "" : dr["InsertedDate"].ToString(),
                                updatedBy = dr["UpdatedBy"] == DBNull.Value ? "" : dr["UpdatedBy"].ToString(),
                                updatedDate = dr["UpdatedDate"] == DBNull.Value ? "" : dr["UpdatedDate"].ToString()

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
            //keep log
            //var requestLog = JsonConvert.SerializeObject(new {
            //    stkcode = stkcode
            //});

            //var responseLog = JsonConvert.SerializeObject(result);

            //String lastId = _apiServerService.SaveApiResponse(
            //    "Ecatalog/GetTabOem",
            //    requestLog,
            //    ""
            //);

            //_apiServerService.UpdateApiRespone(lastId, responseLog);

            return Json(result);
        }
        [HttpGet]
        [Route("Ecatalog/GetTabCompetitor")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetTabCompetitor(string stkcode) {
            var responseList = new List<ProductTabCompetitorResponse>();
            string errorMessage = "Success";
            if (string.IsNullOrWhiteSpace(stkcode)) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "Stkcode is required",
                    result = responseList
                });
            }

            try {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Get_Product_Competitor", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inStkcode", SqlDbType.VarChar, 50).Value = stkcode;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new ProductTabCompetitorResponse {
                                stkcode = dr["Stkcode"] == DBNull.Value ? "" : dr["Stkcode"].ToString(),
                                seqCompetitor = dr["SeqCompetitor"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SeqCompetitor"]),
                                partNo = dr["PartNo"] == DBNull.Value ? "" : dr["PartNo"].ToString(),
                                brandName = dr["BrandName"] == DBNull.Value ? "" : dr["BrandName"].ToString(),
                                insertedBy = dr["InsertedBy"] == DBNull.Value ? "" : dr["InsertedBy"].ToString(),
                                insertedDate = dr["InsertedDate"] == DBNull.Value ? "" : dr["InsertedDate"].ToString(),
                                updatedBy = dr["UpdatedBy"] == DBNull.Value ? "" : dr["UpdatedBy"].ToString(),
                                updatedDate = dr["UpdatedDate"] == DBNull.Value ? "" : dr["UpdatedDate"].ToString()

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
            //keep log
            //var requestLog = JsonConvert.SerializeObject(new {
            //    stkcode = stkcode
            //});

            //var responseLog = JsonConvert.SerializeObject(result);

            //String lastId = _apiServerService.SaveApiResponse(
            //    "Ecatalog/GetTabCompetitor",
            //    requestLog,
            //    ""
            //);

            //_apiServerService.UpdateApiRespone(lastId, responseLog);

            return Json(result);
        }
        [HttpGet]
        [Route("Ecatalog/GetTabLinkage")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetTabLinkage(string stkcode) {
            var responseList = new List<ProductTabLinkageResponse>();
            string errorMessage = "Success";
            if (string.IsNullOrWhiteSpace(stkcode)) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "Stkcode is required",
                    result = responseList
                });
            }

            try {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Get_LinkageData", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inSTKCOD", SqlDbType.VarChar, 50).Value = stkcode;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new ProductTabLinkageResponse {
                                stkcode = dr["Stkcode"] == DBNull.Value ? "" : dr["Stkcode"].ToString(),
                                seqLinkage = dr["SeqLinkage"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SeqLinkage"]),
                                kType = dr["KType"] == DBNull.Value ? "" : dr["KType"].ToString(),
                                productId = dr["ProductId"] == DBNull.Value ? "" : dr["ProductId"].ToString(),
                                truType = dr["TruType"] == DBNull.Value ? "" : dr["TruType"].ToString(),
                                maker = dr["Maker"] == DBNull.Value ? "" : dr["Maker"].ToString(),
                                model = dr["Model"] == DBNull.Value ? "" : dr["Model"].ToString(),
                                body = dr["Body"] == DBNull.Value ? "" : dr["Body"].ToString(),
                                engine = dr["Engine"] == DBNull.Value ? "" : dr["Engine"].ToString(),
                                driveType = dr["DriveType"] == DBNull.Value ? "" : dr["DriveType"].ToString(),
                                yearFrom = dr["YearFrom"] == DBNull.Value ? "" : dr["YearFrom"].ToString(),
                                yearTo = dr["YearTo"] == DBNull.Value ? "" : dr["YearTo"].ToString()

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
            //keep log
            //var requestLog = JsonConvert.SerializeObject(new {
            //    stkcode = stkcode
            //});

            //var responseLog = JsonConvert.SerializeObject(result);

            //String lastId = _apiServerService.SaveApiResponse(
            //    "Ecatalog/GetTabLinkage",
            //    requestLog,
            //    ""
            //);

            //_apiServerService.UpdateApiRespone(lastId, responseLog);

            return Json(result);
        }
        public class ProductKtypeDataResponse
        {
            public string marketSegmentId { get; set; }
            public string vehicleSegmentId { get; set; }
            public string makerId { get; set; }
            public string modelRangeId { get; set; }
            public string modelId { get; set; }
            public string bodyId { get; set; }
            public string driveType { get; set; }
            public string yearFrom { get; set; }
            public string yearTo { get; set; }
            public string kType { get; set; }
            public string truType { get; set; }
        }
        public class ProductSearchVioDataResponse
        {
            public string stkcode { get; set; }
            public string stkcodeDescription { get; set; }
            public string brand { get; set; }
            public string makerName { get; set; }
            public string modelName { get; set; }
            public string qtyReady { get; set; }
            public string price { get; set; }
            public string productGroup { get; set; }
            public string productLine { get; set; }
            public string imagePath { get; set; }

        }
        public class ProductTabItemCountDataResponse
        {
            public string stkcode { get; set; }
            public string stkcodeDescription { get; set; }
            public string brandName { get; set; }
            public string makerName { get; set; }
            public string modelName { get; set; }
            public string qtyReady { get; set; }
            public string price { get; set; }
            public string productGroup { get; set; }
            public string productLine { get; set; }
            public string imagePath { get; set; }
            public int countProductDes { get; set; }
            public int countProductSpec { get; set; }
            public int countProductImage { get; set; }
            public int countProductOem { get; set; }
            public int countProductCom { get; set; }
            public int countProductLinkage { get; set; }
        }
        public class ProductTabDescriptionResponse
        {
            public string stkcode { get; set; }
            public int seqDescription { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public string insertedBy { get; set; }
            public string insertedDate { get; set; }
            public string updatedBy { get; set; }
            public string updatedDate { get; set; }
        }
        public class ProductTabSpecResponse
        {
            public string stkcode { get; set; }
            public int seqSpec { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public string insertedBy { get; set; }
            public string insertedDate { get; set; }
            public string updatedBy { get; set; }
            public string updatedDate { get; set; }
        }
        public class ProductTabImageResponse
        {
            public string stkcode { get; set; }
            public int seqImage { get; set; }
            public string filename { get; set; }
            public string url { get; set; }
            public string insertedBy { get; set; }
            public string insertedDate { get; set; }
            public string updatedBy { get; set; }
            public string updatedDate { get; set; }
        }
        public class ProductTabOemResponse
        {
            public string stkcode { get; set; }
            public int seqOem { get; set; }
            public string oemNumber { get; set; }
            public string makerName { get; set; }
            public string insertedBy { get; set; }
            public string insertedDate { get; set; }
            public string updatedBy { get; set; }
            public string updatedDate { get; set; }
        }
        public class ProductTabCompetitorResponse
        {
            public string stkcode { get; set; }
            public int seqCompetitor { get; set; }
            public string partNo { get; set; }
            public string brandName { get; set; }
            public string insertedBy { get; set; }
            public string insertedDate { get; set; }
            public string updatedBy { get; set; }
            public string updatedDate { get; set; }
        }
        public class ProductTabLinkageResponse
        {
            public string stkcode { get; set; }
            public int seqLinkage { get; set; }
            public string kType { get; set; }
            public string productId { get; set; }
            public string truType { get; set; }
            public string maker { get; set; }
            public string model { get; set; }
            public string body { get; set; }
            public string engine { get; set; }
            public string driveType { get; set; }
            public string yearFrom { get; set; }
            public string yearTo { get; set; }
        }
    }
}