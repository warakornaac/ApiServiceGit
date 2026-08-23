using ApiService.Filters;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Http;
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using RouteAttribute = System.Web.Http.RouteAttribute;
using System.Threading.Tasks;
using System.DirectoryServices;
using ApiService.Services;
using ApiService.Models;
using System.Net.Http;

namespace ApiService.Controllers
{
    public class ProductEcatalogController : ApiController
    {

        private readonly ApiServerController _apiServerService;

        private static readonly int SqlCommandTimeoutSeconds = int.TryParse(ConfigurationManager.AppSettings["SqlCommandTimeoutSeconds"], out int t) ? t : 120;

        public ProductEcatalogController() {
            _apiServerService = new ApiServerController();
        }
        //get kType
        private List<typeListInfo> GetKtypeListByCar(string marketSegmentId, string segmentId, string makerId, string rangeId, string bodyId, string engineId, string yearFrom, string yearTo, string driveType, string SlmCode, string CusCode, string Company) {
            List<typeListInfo> ktypeList = new List<typeListInfo>();
           
            string connString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("P_Search_Ktype_By_Car", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = SqlCommandTimeoutSeconds;

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
                        ktypeList.Add(new typeListInfo {
                            KType = dr["kType"] == DBNull.Value ? null : dr["kType"].ToString(),
                            TruType = dr["truType"] == DBNull.Value ? null : dr["truType"].ToString()
                        });
                    }
                }
            }

            return ktypeList;
        }
        private DataTable BuildCompanyTable(List<string> companies) {
            DataTable dt = new DataTable();
            dt.Columns.Add("CompanyCode", typeof(string));

            if (companies != null) {
                foreach (string company in companies) {
                    if (!string.IsNullOrEmpty(company)) {
                        dt.Rows.Add(company);
                    }
                }
            }
            return dt;
        }
        //get product
        private List<ProductSearchVioDataResponse> GetProductsByKtype(
            List<string> ktypes,
            List<string> trutypes,
            List<string> companies,
            string slmCode,
            string cusCode)
        {
            var responseList = new List<ProductSearchVioDataResponse>();
            string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;

            DataTable dtKtype = new DataTable();
            dtKtype.Columns.Add("Ktype", typeof(string));
            foreach (string ktype in ktypes)
            {
                dtKtype.Rows.Add(ktype);
            }

            DataTable dtTruType = new DataTable();
            dtTruType.Columns.Add("TruType", typeof(string));
            foreach (string trutype in trutypes)
            {
                if (!string.IsNullOrEmpty(trutype))
                {
                    dtTruType.Rows.Add(trutype);
                }
            }

            DataTable dtCompany = new DataTable();
            dtCompany.Columns.Add("CompanyCode", typeof(string));
            if (companies != null)
            {
                foreach (string company in companies)
                {
                    if (!string.IsNullOrEmpty(company))
                    {
                        dtCompany.Rows.Add(company);
                    }
                }
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("P_Search_Product_By_Ktype", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = SqlCommandTimeoutSeconds;

                SqlParameter pKtype = cmd.Parameters.AddWithValue("@inKtypeList", dtKtype);
                pKtype.SqlDbType = SqlDbType.Structured;
                pKtype.TypeName = "dbo.KtypeListTmp";

                SqlParameter pTruType = cmd.Parameters.AddWithValue("@inTruTypeList", dtTruType);
                pTruType.SqlDbType = SqlDbType.Structured;
                pTruType.TypeName = "dbo.TrutypeListTmp";

                SqlParameter pCompany = cmd.Parameters.AddWithValue("@inCompanyList", dtCompany);
                pCompany.SqlDbType = SqlDbType.Structured;
                pCompany.TypeName = "dbo.CompanyListTmp";

                // ← เพิ่ม 2 parameter นี้
                cmd.Parameters.Add("@inSlmCode", SqlDbType.NVarChar, 50).Value =
                    string.IsNullOrEmpty(slmCode) ? (object)DBNull.Value : slmCode;
                cmd.Parameters.Add("@inCusCode", SqlDbType.NVarChar, 50).Value =
                    string.IsNullOrEmpty(cusCode) ? (object)DBNull.Value : cusCode;

                conn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        responseList.Add(
                            new ProductSearchVioDataResponse
                            {
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
                                fittingDescription = dr["FittingDescription"] == DBNull.Value ? "" : dr["FittingDescription"].ToString()
                            });
                    }
                }
            }

            return responseList;
        }
        [HttpGet]
        [Route("Ecatalog/GetProductBySearchVio")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetProductBySearchVio(string marketSegmentId, string segmentId, string makerId, string rangeId, string bodyId, string engineId, string yearFrom, string yearTo, string driveType, string SlmCode, string CusCode, [FromUri] List<string> Company = null) {
            try {

                string companyParam = Company.Any()
                    ? string.Join(",", Company)
                    : string.Empty;

                // หา Ktype
                List<typeListInfo> ktypeInfoList =
                GetKtypeListByCar(
                      marketSegmentId,
                      segmentId,
                      makerId,
                      rangeId,
                      bodyId,
                      engineId,
                      yearFrom,
                      yearTo,
                      driveType,
                      SlmCode, 
                      CusCode,
                      companyParam
                      );

                if (ktypeInfoList.Count == 0) {
                    return Json(new {
                        statusCode = 200,
                        errorMessage = "Ktype not found",
                        result = new object[0]
                    });
                }

                List<typeListInfo> distinctKtypeInfoList = ktypeInfoList
                                                            .GroupBy(x => x.KType)
                                                            .Select(g => g.First())
                                                            .ToList();

                List<string> ktypeList = distinctKtypeInfoList.Select(x => x.KType).ToList();
                List<string> trutypeList = distinctKtypeInfoList.Select(x => x.TruType).ToList();

                var products = GetProductsByKtype(ktypeList, trutypeList, Company, SlmCode, CusCode);

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
        private DataTable BuildSearchCatagoryTable(ProductSearchCatagoryDataRequest request)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("CategoryFilterType", typeof(string));
            dt.Columns.Add("CategoryFilterValue", typeof(string));

            if (request.productGroupId != null)
                foreach (var item in request.productGroupId)
                    dt.Rows.Add("productGroupId", item);

            if (request.productLineId != null)
                foreach (var item in request.productLineId)
                    dt.Rows.Add("productLineId", item);

            if (request.brandId != null)
                foreach (var item in request.brandId)
                    dt.Rows.Add("brandId", item);

            if (request.fittingFilter != null)
                foreach (var item in request.fittingFilter)
                    dt.Rows.Add("fittingFilter", item);

            return dt;
        }
    
        [HttpPost]
        [Route("Ecatalog/GetProductBySearchCatagory")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetProductBySearchCatagory([FromBody] ProductSearchCatagoryDataRequest request) {
            var responseList = new List<ProductSearchVioDataResponse>();
            string errorMessage = "Success";

            if (request == null) {
                return Json(new { statusCode = 400, errorMessage = "Request is required", result = responseList });
            }

            if (
                (request.productGroupId == null || !request.productGroupId.Any()) &&
                (request.productLineId == null || !request.productLineId.Any()) &&
                (request.brandId == null || !request.brandId.Any())
            ) {
                return Json(new { statusCode = 400, errorMessage = "At least one filter is required", result = responseList });
            }

            try {
                bool hasVehicleFilter = HasVehicleFilter(request);

                List<string> ktypeList = null;
                List<string> trutypeList = null;

                if (hasVehicleFilter) {
                    string companyParam = request.Company != null
                        ? string.Join(",", request.Company)
                        : string.Empty;

                    List<typeListInfo> ktypeInfoList = GetKtypeListByCar(
                        request.marketSegmentId, request.segmentId, request.makerId,
                        request.rangeId, request.bodyId, request.engineId,
                        request.yearFrom, request.yearTo, request.driveType,
                        request.SlmCode, request.CusCode, companyParam
                    );

                    if (ktypeInfoList.Count == 0) {
                        return Json(new {
                            statusCode = 200,
                            errorMessage = "Ktype not found",
                            result = responseList
                        });
                    }

                    List<typeListInfo> distinctKtypeInfoList = ktypeInfoList
                        .GroupBy(x => x.KType)
                        .Select(g => g.First())
                        .ToList();

                    ktypeList = distinctKtypeInfoList.Select(x => x.KType).ToList();
                    trutypeList = distinctKtypeInfoList.Select(x => x.TruType).ToList();
                }

                DataTable tvp = BuildSearchCatagoryTable(request);
                DataTable tvpKtype = BuildKtypeTable(ktypeList);
                DataTable tvpTruType = BuildTruTypeTable(trutypeList);
                DataTable tvpCompany = BuildCompanyTable(request.Company); 

                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Search_Product_By_Catagory", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = SqlCommandTimeoutSeconds;

                    SqlParameter param = cmd.Parameters.AddWithValue("@inCategoryFilter", tvp);
                    param.SqlDbType = SqlDbType.Structured;
                    param.TypeName = "dbo.CategoryFilterType";

                    SqlParameter pKtype = cmd.Parameters.AddWithValue("@inKtypeList", tvpKtype);
                    pKtype.SqlDbType = SqlDbType.Structured;
                    pKtype.TypeName = "dbo.KtypeListTmp";

                    SqlParameter pTruType = cmd.Parameters.AddWithValue("@inTruTypeList", tvpTruType);
                    pTruType.SqlDbType = SqlDbType.Structured;
                    pTruType.TypeName = "dbo.TrutypeListTmp";

                    // ★★★ เพิ่มใหม่ตามที่ต้องการ ★★★
                    SqlParameter pCompany = cmd.Parameters.AddWithValue("@inCompanyList", tvpCompany);
                    pCompany.SqlDbType = SqlDbType.Structured;
                    pCompany.TypeName = "dbo.CompanyListTmp";

                    cmd.Parameters.AddWithValue("@inSlmCode",
                        string.IsNullOrEmpty(request.SlmCode) ? (object)DBNull.Value : request.SlmCode);

                    cmd.Parameters.AddWithValue("@inCusCode",
                        string.IsNullOrEmpty(request.CusCode) ? (object)DBNull.Value : request.CusCode);
                    // ★★★ จบส่วนที่เพิ่ม ★★★

                    conn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(new ProductSearchVioDataResponse {
                                stkcode = dr["stkcode"] == DBNull.Value ? "" : dr["stkcode"].ToString(),
                                stkcodeDescription = dr["stkcodeDescription"] == DBNull.Value ? "" : dr["stkcodeDescription"].ToString(),
                                brand = dr["BrandName"] == DBNull.Value ? "" : dr["BrandName"].ToString(),
                                makerName = dr["makerName"] == DBNull.Value ? "" : dr["makerName"].ToString(),
                                modelName = dr["modelName"] == DBNull.Value ? "" : dr["modelName"].ToString(),
                                qtyReady = dr["qtyReady"] == DBNull.Value ? "" : dr["qtyReady"].ToString(),
                                price = dr["price"] == DBNull.Value ? "" : dr["price"].ToString(),
                                productGroup = dr["productGroupName"] == DBNull.Value ? "" : dr["productGroupName"].ToString(),
                                productLine = dr["productLineName"] == DBNull.Value ? "" : dr["productLineName"].ToString(),
                                imagePath = dr["imagePath"] == DBNull.Value ? "" : dr["imagePath"].ToString(),
                                fittingDescription = dr["FittingDescription"] == DBNull.Value ? "" : dr["FittingDescription"].ToString(),
                                slmCode = request.SlmCode,
                                cusCode = request.CusCode,
                                company = request.Company
                            });
                        }
                    }
                }
            }
            catch (Exception ex) {
                errorMessage = ex.Message;
            }

            var result = new {
                statusCode = errorMessage == "Success" ? 200 : 500,
                errorMessage,
                result = responseList
            };

            return Json(result);
        }

        [HttpPost]
        [Route("Ecatalog/GetProductBySearchCatagoryPostman")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetProductBySearchCatagoryPostman(ProductFilterRequest request) {
            return Json(new {
                IsSuccess = true,
                Data = request.CategoryFilters
            });
        }
        private DataTable BuildSearchFieldTable(ProductSearchFieldDataRequest request) {
            DataTable dt = new DataTable();

            dt.Columns.Add("SearchField", typeof(string));

            if (request.searchFields != null) {
                foreach (var item in request.searchFields) {
                    dt.Rows.Add(item);
                }
            }

            return dt;
        }
        [HttpPost]
        [Route("Ecatalog/GetProductBySearchField")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetProductBySearchField([FromBody] ProductSearchFieldDataRequest request) {
            var responseList = new List<ProductSearchVioDataResponse>();
            string errorMessage = "Success";

            if (request == null) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "Request is null",
                    result = responseList
                });
            }

            if (String.IsNullOrWhiteSpace(request.searchText)) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "SearchText is required",
                    result = responseList
                });
            }

            if (request.searchFields == null || !request.searchFields.Any()) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "SearchFields is required",
                    result = responseList
                });
            }

            try {
                bool hasVehicleFilter = HasVehicleFieldFilter(request);
                List<string> ktypeList = null;
                List<string> trutypeList = null;

                if (hasVehicleFilter) {
                    string companyParam = request.Company != null
                        ? string.Join(",", request.Company)
                        : string.Empty;

                    List<typeListInfo> ktypeInfoList = GetKtypeListByCar(
                        request.marketSegmentId,
                        request.segmentId,
                        request.makerId,
                        request.rangeId,
                        request.bodyId,
                        request.engineId,
                        request.yearFrom,
                        request.yearTo,
                        request.driveType,
                        request.SlmCode,
                        request.CusCode,
                        companyParam
                    );

                    // ★ ลบเงื่อนไข early-return ออก
                    // แม้หา ktype ไม่เจอ (ktypeInfoList.Count == 0) ก็ไม่หยุด เพราะ SP รองรับ ktype/trutype ว่างได้
                    // (SP จะไม่กรอง ktype แล้วค้นหาต่อด้วย searchText/searchField ตามปกติ)

                    List<typeListInfo> distinctKtypeInfoList = ktypeInfoList
                        .GroupBy(x => x.KType)
                        .Select(g => g.First())
                        .ToList();

                    // ถ้า ktypeInfoList ว่าง -> distinctKtypeInfoList ก็จะว่างตาม -> ได้ list ว่างเปล่า (ไม่ error)
                    ktypeList = distinctKtypeInfoList.Select(x => x.KType).ToList();
                    trutypeList = distinctKtypeInfoList.Select(x => x.TruType).ToList();
                }
                // hasVehicleFilter = false -> ktypeList/trutypeList ยังเป็น null -> BuildKtypeTable/BuildTruTypeTable จะสร้างตารางว่าง

                DataTable tvp = BuildSearchFieldTable(request);
                DataTable tvpKtype = BuildKtypeTable(ktypeList);
                DataTable tvpTruType = BuildTruTypeTable(trutypeList);

                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Search_Product_By_Field", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = SqlCommandTimeoutSeconds;

                    cmd.Parameters.Add("@inSearchText", SqlDbType.VarChar, 200).Value = request.searchText;

                    SqlParameter tvpParam = cmd.Parameters.AddWithValue("@inSearchField", tvp);
                    tvpParam.SqlDbType = SqlDbType.Structured;
                    tvpParam.TypeName = "dbo.SearchFieldType";

                    SqlParameter pKtype = cmd.Parameters.AddWithValue("@inKtypeList", tvpKtype);
                    pKtype.SqlDbType = SqlDbType.Structured;
                    pKtype.TypeName = "dbo.KtypeListTmp";

                    SqlParameter pTruType = cmd.Parameters.AddWithValue("@inTruTypeList", tvpTruType);
                    pTruType.SqlDbType = SqlDbType.Structured;
                    pTruType.TypeName = "dbo.TrutypeListTmp";

                    cmd.Parameters.AddWithValue("@inCuscode",
                        string.IsNullOrEmpty(request.CusCode) ? "111B0005" : request.CusCode);

                    conn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            responseList.Add(
                                new ProductSearchVioDataResponse {
                                    stkcode = dr["stkcode"] == DBNull.Value ? "" : dr["stkcode"].ToString(),
                                    stkcodeDescription = dr["stkcodeDescription"] == DBNull.Value ? "" : dr["stkcodeDescription"].ToString(),
                                    brand = dr["BrandName"] == DBNull.Value ? "" : dr["BrandName"].ToString(),
                                    makerName = dr["makerName"] == DBNull.Value ? "" : dr["makerName"].ToString(),
                                    modelName = dr["modelName"] == DBNull.Value ? "" : dr["modelName"].ToString(),
                                    qtyReady = dr["qtyReady"] == DBNull.Value ? "" : dr["qtyReady"].ToString(),
                                    price = dr["price"] == DBNull.Value ? "" : dr["price"].ToString(),
                                    productGroup = dr["productGroupName"] == DBNull.Value ? "" : dr["productGroupName"].ToString(),
                                    productLine = dr["productLineName"] == DBNull.Value ? "" : dr["productLineName"].ToString(),
                                    imagePath = dr["imagePath"] == DBNull.Value ? "" : dr["imagePath"].ToString(),
                                    fittingDescription = dr["FittingDescription"] == DBNull.Value ? "" : dr["FittingDescription"].ToString(),
                                    
                                    slmCode = request.SlmCode,
                                    cusCode = request.CusCode,
                                    company = request.Company
                                });
                        }
                    }
                }
            }
            catch (Exception ex) {
                errorMessage = ex.Message;
            }

            var result = new {
                statusCode = errorMessage == "Success" ? 200 : 500,
                errorMessage,
                result = responseList
            };
            var json = JsonConvert.SerializeObject(responseList);
            System.Diagnostics.Debug.WriteLine(json);
            return Json(result);
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


        [HttpPost]
        [Route("Ecatalog/AddProductToCart")]
        [ApiKeyAuthorize]
        public IHttpActionResult AddProductToCart(string cuscode="", string stkcod = "", string company = "", string price = "", string qty = "", string username = "", string backorder = "0")
        {

            //var responseList = new List<ProductTabLinkageResponse>();
            string errorMessage = "Success";
            var validations = new[]
            {
                new KeyValuePair<string, string>(cuscode,   "Cuscode"),
                new KeyValuePair<string, string>(stkcod,   "Stkcod"),
                new KeyValuePair<string, string>(company,  "Company"),
                new KeyValuePair<string, string>(price,    "Price"),
                new KeyValuePair<string, string>(qty,      "Qty"),
                new KeyValuePair<string, string>(username, "Username"),
            };

            foreach (var item in validations)
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                    return Json(new { statusCode = 400, errorMessage = $"{item.Value} is required", result = new { } });
            }
            try {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("p_SaveOrderCart", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@Customer", SqlDbType.VarChar, 50).Value = cuscode;
                    cmd.Parameters.Add("@STKCOD", SqlDbType.VarChar, 50).Value = stkcod;
                    cmd.Parameters.Add("@Company", SqlDbType.VarChar, 50).Value = company;

                    cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = price;
                    cmd.Parameters.Add("@SPrice", SqlDbType.Decimal).Value = 0.0;
                    cmd.Parameters.Add("@Expect_Price", SqlDbType.Decimal).Value = 0.0;
                    cmd.Parameters.Add("@specprice", SqlDbType.Decimal).Value = 0.0;
                    cmd.Parameters.Add("@promoprice", SqlDbType.Decimal).Value = 0.0;
                    cmd.Parameters.Add("@lastinvprice", SqlDbType.Decimal).Value = 0.0;

                    cmd.Parameters.Add("@Qty", SqlDbType.Int).Value = qty;
                    cmd.Parameters.Add("@Bckorder", SqlDbType.Int).Value = backorder;
                    cmd.Parameters.Add("@InsertedBy", SqlDbType.VarChar, 50).Value = username;
                    cmd.Parameters.Add("@LineNote", SqlDbType.VarChar, 50).Value = "";

                    cmd.Parameters.Add("@ProCode", SqlDbType.VarChar, 50).Value = "";
                    cmd.Parameters.Add("@FOC", SqlDbType.Int).Value = 0;

                    cmd.Parameters.Add("@prclstno", SqlDbType.VarChar).Value = "";
                    cmd.Parameters.Add("@promodesc", SqlDbType.VarChar).Value = "";
                    cmd.Parameters.Add("@lastinvdate", SqlDbType.VarChar).Value = "";
                    cmd.Parameters.Add("@sop", SqlDbType.VarChar).Value = "";

                    conn.Open();
                    cmd.ExecuteNonQuery();

                }
            }
            catch (Exception ex) {
                errorMessage = ex.Message;
            }
            var cartResult = new CartAddResponse
            {
                cuscode = cuscode,
                stkcod = stkcod,
                company = company,
                price = price,
                qty = qty,
                username = username,
                backorder = backorder
            };
            var result = new
            {

                statusCode =
                errorMessage == "Success"
                ? 200
                : 500,

                errorMessage,

                result = errorMessage == "Success"
                ? (object)cartResult : new { }
            };

            return Json(result);


        }

        [HttpPost]
        [Route("Ecatalog/EditProductToCart")]
        [ApiKeyAuthorize]
        public IHttpActionResult EditProductToCart(string ordid = "", string cuscode="", int qty = 0 ,decimal price = 0 ,string username = "")
        {

            var responseList = new List<CartEditResponse>();
            string errorMessage = "Success";

            if (string.IsNullOrWhiteSpace(ordid))
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "OrdId is required",
                    result = new { }
                });
            }
            if (string.IsNullOrWhiteSpace(cuscode))
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "Cuscode is required",
                    result = new { }
                });
            }
            if (qty == 0 || qty < 0)
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "qty invalide",
                    result = new { }
                });
            }
            if (price == 0 || price < 0)
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "Price invalide",
                    result = new { }
                });
            }
            if (string.IsNullOrWhiteSpace(username))
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "Username is required",
                    result = new { }
                });
            }
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_EditOrderCart", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inOrdId", SqlDbType.Int).Value = ordid;
                    cmd.Parameters.Add("@inCUSCOD", SqlDbType.VarChar, 50).Value = cuscode;
                    cmd.Parameters.Add("@inQty", SqlDbType.Int).Value = qty;
                    cmd.Parameters.Add("@inPrice", SqlDbType.Decimal).Value = price;
                    cmd.Parameters.Add("@inUsername", SqlDbType.VarChar, 50).Value = username;

                    conn.Open();
                    // cmd.ExecuteNonQuery();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            responseList.Add(new CartEditResponse
                            {
                                ordid = dr["OrdID"] == DBNull.Value ? "" : dr["OrdID"].ToString(),
                                company = dr["Company"] == DBNull.Value ? "" : dr["Company"].ToString(),
                                cuscode = dr["CUSCOD"] == DBNull.Value ? "" : dr["CUSCOD"].ToString(),
                                stkcod = dr["STKCOD"] == DBNull.Value ? "" : dr["STKCOD"].ToString(),
                                qty = dr["Qty"] == DBNull.Value ? "" : dr["Qty"].ToString(),
                                price = dr["Price"] == DBNull.Value ? "" : dr["Price"].ToString(),
                                backorder = dr["BackOrder"] == DBNull.Value ? "" : dr["BackOrder"].ToString(),
                                username = dr["EditedBy"] == DBNull.Value ? "" : dr["EditedBy"].ToString()


                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            if (errorMessage != "Success")
            {
                return Json(new
                {
                    statusCode = 500,
                    errorMessage,
                    result = new { }
                });
            }
            if (responseList.Count == 0)
            {
                return Json(new
                {
                    statusCode = 404,
                    errorMessage = "Order not found.",
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

            return Json(result);
        }

        [HttpPost]
        [Route("Ecatalog/DeleteProductToCart")]
        [ApiKeyAuthorize]
        public IHttpActionResult DeleteProductToCart(string ordid = "", string username = "")
        {

            var responseList = new List<CartDeleteResponse>();
            string errorMessage = "Success";

            if (string.IsNullOrWhiteSpace(ordid))
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "OrdId is required",
                    result = new { }
                });
            }
            if (string.IsNullOrWhiteSpace(username))
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "Username is required",
                    result = new { }
                });
            }
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Delete_Ordering_Cart", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inOrdId", SqlDbType.Int).Value = ordid;
                    cmd.Parameters.Add("@inUsername", SqlDbType.VarChar, 50).Value = username;

                    conn.Open();
                   // cmd.ExecuteNonQuery();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            responseList.Add(new CartDeleteResponse
                            {
                                company = dr["Company"] == DBNull.Value ? "" : dr["Company"].ToString(),
                                cuscode = dr["CUSCOD"] == DBNull.Value ? "" : dr["CUSCOD"].ToString(),
                                stkcod = dr["STKCOD"] == DBNull.Value ? "" : dr["STKCOD"].ToString(),
                                qty = dr["Qty"] == DBNull.Value ? "" : dr["Qty"].ToString(),
                                price = dr["Price"] == DBNull.Value ? "" : dr["Price"].ToString(),
                                backorder = dr["BackOrder"] == DBNull.Value ? "" : dr["BackOrder"].ToString(),
                                username = dr["DeletedBy"] == DBNull.Value ? "" : dr["DeletedBy"].ToString()


                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            if (errorMessage != "Success")
            {
                return Json(new
                {
                    statusCode = 500,
                    errorMessage,
                    result = new { }
                });
            }
            if (responseList.Count == 0)
            {
                return Json(new
                {
                    statusCode = 404,
                    errorMessage = "Order not found.",
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

            return Json(result);
        }

        [HttpGet]
        [Route("Ecatalog/GetProductToCart")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetProductToCart(string cuscode="", string username="")
        {
            var responseList = new List<OrderCartProductResponse>();
            string errorMessage = "Success";
            if (string.IsNullOrWhiteSpace(cuscode)) {
                return Json(new {
                    statusCode = 400,
                    errorMessage = "cuscode is required",
                    result = responseList
                });
            }
            if (string.IsNullOrWhiteSpace(username))
            {
                return Json(new
                {
                    statusCode = 400,
                    errorMessage = "username is required",
                    result = responseList
                });
            }

            try {
                string connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_ShoppingCart_list_Catalog", conn)) {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inCUSCOD", SqlDbType.VarChar, 50).Value = cuscode;
                    cmd.Parameters.Add("@usrlogin", SqlDbType.VarChar, 50).Value = username;
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            responseList.Add(new OrderCartProductResponse
                            {
                                ordId = dr["id"] == DBNull.Value ? "" : dr["id"].ToString(),
                                company = dr["Company"] == DBNull.Value ? "" : dr["Company"].ToString(),
                                cuscod = dr["CUSCOD"] == DBNull.Value ? "" : dr["CUSCOD"].ToString(),
                                stkcod = dr["STKCOD"] == DBNull.Value ? "" : dr["STKCOD"].ToString(),
                                stkdes = dr["STKDES"] == DBNull.Value ? "" : dr["STKDES"].ToString(),
                                stkgrp = dr["STKGRP"] == DBNull.Value ? "" : dr["STKGRP"].ToString(),
                                minord = dr["MINORD"] == DBNull.Value ? "" : dr["MINORD"].ToString(),
                                price = dr["Price"] == DBNull.Value ? "" : dr["Price"].ToString(),
                                backOrder = dr["BackOrder"] == DBNull.Value ? "" : dr["BackOrder"].ToString(),
                                qty = dr["Qty"] == DBNull.Value ? "" : dr["Qty"].ToString(),
                                amt = dr["Amt"] == DBNull.Value ? "" : dr["Amt"].ToString(),
                                uom = dr["UOM"] == DBNull.Value ? "" : dr["UOM"].ToString(),
                                status = dr["Status"] == DBNull.Value ? "" : dr["Status"].ToString(),
                                orddat = dr["ORDDAT"] == DBNull.Value ? "" : dr["ORDDAT"].ToString(),                                
                                insertedBy = dr["Inserted By"] == DBNull.Value ? "" : dr["Inserted By"].ToString(),
                                insertedDate = dr["Inserted Date"] == DBNull.Value ? "" : dr["Inserted Date"].ToString(),
                                updatedBy = dr["Updated By"] == DBNull.Value ? "" : dr["Updated By"].ToString(),
                                updatedDate = dr["Updated Date"] == DBNull.Value ? "" : dr["Updated Date"].ToString(),
                                imagePath = dr["imagePath"] == DBNull.Value ? "" : dr["imagePath"].ToString()


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
            return Json(result);
        }
        [HttpGet]
        [Route("Ecatalog/GetProductBySearchGlobal")]
        [ApiKeyAuthorize]
        public async Task<IHttpActionResult> GetProductBySearchGlobal(string Keyword, bool Debug = false) {
            if (Keyword == null || String.IsNullOrWhiteSpace(Keyword)) {
                return Ok(new {
                    statusCode = 400,
                    errorMessage = "Please input keyword.",
                    result = new object[0]
                });
            }

            SearchService service = new SearchService();
            GlobalSearchResult result = await service.GlobalSearch(Keyword, Debug);

            if (Debug) {
                // โหมด debug: คืนรายละเอียดเต็มทั้ง pipeline (TokenDetails, BatchHits, RouteDebug ฯลฯ)
                // เพื่อ diagnose ว่า tokenize/route ยังไงถึงได้ผลแบบนี้ — โครงสร้างต่างจากโหมดปกติโดยตั้งใจ
                return Ok(result);
            }

            // โหมดปกติ (Debug=false): คืน format เดียวกับ endpoint อื่น ๆ ในระบบ
            // (GetProductBySearchVio / GetProductBySearchCatagory / GetProductBySearchField)
            // เพื่อให้ frontend ใช้ parsing logic เดียวกันได้กับทุก endpoint โดยไม่ต้องแยก case
            return Ok(new {
                statusCode = result.Success ? 200 : 500,
                errorMessage = result.Success ? "Success" : (result.Message ?? "Error"),
                result = result.Items
            });
        }
        /// <summary>
        /// เรียกหลังจากแก้ไข/เพิ่มคำใน MsSearchDictionary (หรือ re-sync เข้า Meilisearch แล้ว)
        /// เพื่อล้าง cache ของ Trie ให้ระบบ reload dictionary ใหม่ทันที ไม่ต้องรอ TTL หมดอายุ
        /// </summary>
        [HttpPost]
        [Route("Ecatalog/RefreshSearchDictionaryCache")]
        [ApiKeyAuthorize]
        public IHttpActionResult RefreshSearchDictionaryCache() {
            SearchService service = new SearchService();
            service.InvalidateDictionaryCache();
            return Ok(new { Success = true, Message = "Dictionary cache invalidated." });
        }
        // เช็คว่า request มีการส่งเงื่อนไขรุ่นรถมาไหม (แม้แค่ field เดียวก็ถือว่ามี)
        private bool HasVehicleFilter(ProductSearchCatagoryDataRequest request) {
            return !string.IsNullOrEmpty(request.marketSegmentId)
                || !string.IsNullOrEmpty(request.segmentId)
                || !string.IsNullOrEmpty(request.makerId)
                || !string.IsNullOrEmpty(request.rangeId)
                || !string.IsNullOrEmpty(request.bodyId)
                || !string.IsNullOrEmpty(request.engineId)
                || !string.IsNullOrEmpty(request.yearFrom)
                || !string.IsNullOrEmpty(request.yearTo)
                || !string.IsNullOrEmpty(request.driveType);
        }

        private bool HasVehicleFieldFilter(ProductSearchFieldDataRequest request) {
            return !string.IsNullOrEmpty(request.marketSegmentId)
                || !string.IsNullOrEmpty(request.segmentId)
                || !string.IsNullOrEmpty(request.makerId)
                || !string.IsNullOrEmpty(request.rangeId)
                || !string.IsNullOrEmpty(request.bodyId)
                || !string.IsNullOrEmpty(request.engineId)
                || !string.IsNullOrEmpty(request.yearFrom)
                || !string.IsNullOrEmpty(request.yearTo)
                || !string.IsNullOrEmpty(request.driveType);
        }

        // สร้าง TVP ktype - ถ้า ktypes เป็น null (ไม่มีเงื่อนไขรถ) จะได้ตารางว่าง = SP จะไม่กรอง
        private DataTable BuildKtypeTable(List<string> ktypes) {
            DataTable dt = new DataTable();
            dt.Columns.Add("Ktype", typeof(string));

            if (ktypes != null) {
                foreach (string ktype in ktypes) {
                    dt.Rows.Add(ktype);
                }
            }
            return dt;
        }

        // สร้าง TVP trutype - เช่นเดียวกับ ktype
        private DataTable BuildTruTypeTable(List<string> trutypes) {
            DataTable dt = new DataTable();
            dt.Columns.Add("TruType", typeof(string));

            if (trutypes != null) {
                foreach (string trutype in trutypes) {
                    if (!string.IsNullOrEmpty(trutype)) {
                        dt.Rows.Add(trutype);
                    }
                }
            }
            return dt;
        }
        public class typeListInfo
        {
            public string KType { get; set; }
            public string TruType { get; set; }
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
        //test postman
        public class ProductFilterRequest
        {
            public List<ProductFilterItem> CategoryFilters { get; set; }
        }
        public class ProductFilterItem
        {
            public string CategoryFilterType { get; set; }

            public string CategoryFilterValue { get; set; }
        }

        public class ProductSearchCatagoryDataRequest
        {
            public List<string> productGroupId { get; set; }
            public List<string> productLineId { get; set; }
            public List<string> brandId { get; set; }
            public List<string> fittingFilter { get; set; }
            public string SlmCode { get; set; }
            public string CusCode { get; set; }
            public List<string> Company { get; set; }
            public string marketSegmentId { get; set; }
            public string segmentId { get; set; }
            public string makerId { get; set; }
            public string rangeId { get; set; }
            public string bodyId { get; set; }
            public string engineId { get; set; }
            public string yearFrom { get; set; }
            public string yearTo { get; set; }
            public string driveType { get; set; }
        }
        public class ProductSearchFieldDataRequest
        {
            public string searchText { get; set; }
            public List<string> searchFields { get; set; }
            public string SlmCode { get; set; }
            public string CusCode { get; set; }
            public List<string> Company { get; set; }
            public string marketSegmentId { get; set; }
            public string segmentId { get; set; }
            public string makerId { get; set; }
            public string rangeId { get; set; }
            public string bodyId { get; set; }
            public string engineId { get; set; }
            public string yearFrom { get; set; }
            public string yearTo { get; set; }
            public string driveType { get; set; }

        }
        public class OrderCartProductResponse
        {
            public string ordId { get; set; }
            public string company { get; set; }
            public string cuscod { get; set; }
            public string orddat { get; set; }
            public string stkcod { get; set; }
            public string stkdes { get; set; }
            public string stkgrp { get; set; }
            public string minord { get; set; }
            public string price { get; set; }
            public string backOrder { get; set; }
            public string qty { get; set; }
            public string amt { get; set; }
            public string uom { get; set; }
            public string status { get; set; }
            public string imagePath { get; set; }
            public string insertedDate { get; set; }
            public string insertedBy { get; set; }
            public string updatedDate { get; set; }
            public string updatedBy { get; set; }
        }
        public class CartAddResponse
        {
            public string cuscode { get; set; }
            public string stkcod { get; set; }
            public string company { get; set; }
            public string price { get; set; }
            public string qty { get; set; }
            public string username { get; set; }
            public string backorder { get; set; }
        }

        public class CartEditResponse
        {
            public string ordid { get; set; }
            public string cuscode { get; set; }
            public string stkcod { get; set; }
            public string company { get; set; }
            public string price { get; set; }
            public string qty { get; set; }
            public string username { get; set; }
            public string backorder { get; set; }
        }
        public class CartDeleteResponse
        {
            public string cuscode { get; set; }
            public string stkcod { get; set; }
            public string company { get; set; }
            public string price { get; set; }
            public string qty { get; set; }
            public string username { get; set; }
            public string backorder { get; set; }
        }
    }
}