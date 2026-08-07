using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ApiService.Models;

namespace ApiService.Services
{
    /// <summary>
    /// รับผลจาก Tokenize/Ranking/SearchParser (SearchSqlRequest) แล้ว "route" ไปยัง SP จริงที่มีอยู่แล้ว
    /// ในระบบ 3 กลุ่ม ตาม SearchType ที่เจอ แทนที่จะยิงเข้า SP รวม P_Search_Product_Global ตัวเดียว:
    ///
    ///   SearchType: description, oe, competitor       -> P_Search_Product_By_Field   (full-text ตาม field)
    ///   SearchType: model, maker                       -> P_Search_Ktype_By_Car + P_Search_Product_By_Ktype
    ///   SearchType: productline, productgroup, brand   -> P_Search_Product_By_Catagory
    ///
    /// ถ้า query เดียวมีหลายกลุ่มพร้อมกัน (เช่น "ผ้าเบรกhondacity" มีทั้งกลุ่ม Field และกลุ่ม Vio)
    /// จะยิงทุกกลุ่มแบบขนาน (แม้กลุ่มที่ไม่ trigger ก็ return เร็ว ๆ โดยไม่ยิง DB) แล้ว union ผลลัพธ์
    /// เข้าด้วยกัน (dedupe ด้วย stkcode)
    /// </summary>
    public class SearchRouterService
    {
        private static readonly HashSet<string> FieldSearchTypes =
            new HashSet<string>(new[] { "description", "oe", "competitor" }, StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> VioSearchTypes =
            new HashSet<string>(new[] { "model", "maker" }, StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> CategorySearchTypes =
            new HashSet<string>(new[] { "productline", "productgroup", "brand" }, StringComparer.OrdinalIgnoreCase);

        private readonly string _connectionString;

        public SearchRouterService(string connectionString) {
            _connectionString = connectionString;
        }

        /// <summary>
        /// จุดเข้าหลักสำหรับใช้งานจริง (production path) — คืนแค่ผลลัพธ์รวมสุดท้าย
        /// </summary>
        public async Task<List<ProductSearchVioDataResponse>> RouteAsync(SearchSqlRequest request) {
            var debugResult = await RouteWithDebugAsync(request).ConfigureAwait(false);
            return debugResult.MergedItems;
        }

        /// <summary>
        /// จุดเข้าแบบละเอียด — ยิงทั้ง 3 กลุ่มแบบขนานเสมอ (กลุ่มที่ไม่เข้าเกณฑ์จะ return ทันทีโดยไม่ยิง DB)
        /// คืนทั้งผลลัพธ์รวม และ breakdown รายกลุ่ม (params ที่ใช้ยิงจริง + ผลดิบก่อน merge) ไว้ debug
        /// </summary>
        public async Task<RouteDebugResult> RouteWithDebugAsync(SearchSqlRequest request) {
            var fieldTask = BuildFieldGroupAsync(request);
            var vioTask = BuildVioGroupAsync(request);
            var categoryTask = BuildCategoryGroupAsync(request);

            await Task.WhenAll(fieldTask, vioTask, categoryTask).ConfigureAwait(false);

            var field = fieldTask.Result;
            var vio = vioTask.Result;
            var category = categoryTask.Result;

            var merged = field.Items
                .Concat(vio.Items)
                .Concat(category.Items)
                .GroupBy(p => p.stkcode)
                .Select(g => g.First())
                .ToList();

            return new RouteDebugResult {
                Field = field,
                Vio = vio,
                Category = category,
                MergedItems = merged
            };
        }

        // =====================================================================
        // กลุ่ม 1: description / oe / competitor -> P_Search_Product_By_Field
        // =====================================================================
        private async Task<FieldRouteDebug> BuildFieldGroupAsync(SearchSqlRequest request) {
            var debug = new FieldRouteDebug();

            var searchFields = request.SearchTypes
                .Where(t => FieldSearchTypes.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            debug.SearchFields = searchFields;

            if (searchFields.Count == 0)
                return debug; // Triggered = false (default), Items ว่างเปล่า

            // searchText: ใช้ token ดิบของกลุ่มนี้ (ถ้ามีมากกว่า 1 token เอาตัวแรกที่ยาวที่สุดเป็นตัวแทน
            // เพราะ P_Search_Product_By_Field รับ searchText เดียว ไม่ใช่ list)
            var candidateTexts = searchFields
                .Where(request.TokensBySearchType.ContainsKey)
                .SelectMany(t => request.TokensBySearchType[t])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var searchText = candidateTexts.OrderByDescending(t => t.Length).FirstOrDefault();
            debug.SearchText = searchText;

            if (string.IsNullOrWhiteSpace(searchText))
                return debug; // Triggered = false

            debug.Triggered = true;
            debug.Items = await ExecuteSearchFieldAsync(searchText, searchFields).ConfigureAwait(false);

            return debug;
        }

        private async Task<List<ProductSearchVioDataResponse>> ExecuteSearchFieldAsync(string searchText, List<string> searchFields) {
            var responseList = new List<ProductSearchVioDataResponse>();

            var dt = new DataTable();
            dt.Columns.Add("SearchField", typeof(string));
            foreach (var field in searchFields)
                dt.Rows.Add(field);

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("P_Search_Product_By_Field", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@inSearchText", SqlDbType.VarChar, 200).Value = searchText;

                var tvpParam = cmd.Parameters.AddWithValue("@inSearchField", dt);
                tvpParam.SqlDbType = SqlDbType.Structured;
                tvpParam.TypeName = "dbo.SearchFieldType";

                await conn.OpenAsync().ConfigureAwait(false);

                using (var dr = await cmd.ExecuteReaderAsync().ConfigureAwait(false)) {
                    while (await dr.ReadAsync().ConfigureAwait(false)) {
                        responseList.Add(MapReaderToResponse(dr, brandColumn: "BrandName",
                            productGroupColumn: "productGroupName", productLineColumn: "productLineName"));
                    }
                }
            }

            return responseList;
        }

        // =====================================================================
        // กลุ่ม 2: model / maker -> P_Search_Ktype_By_Car + P_Search_Product_By_Ktype
        // =====================================================================
        private async Task<VioRouteDebug> BuildVioGroupAsync(SearchSqlRequest request) {
            var debug = new VioRouteDebug();

            var makerId = FirstOrDefault(request.SourceIdsBySearchType, "maker");
            var rangeId = FirstOrDefault(request.SourceIdsBySearchType, "model");

            debug.MakerId = makerId;
            debug.RangeId = rangeId;

            if (string.IsNullOrEmpty(makerId) && string.IsNullOrEmpty(rangeId))
                return debug; // Triggered = false ไม่มีข้อมูลอะไรให้ค้นเลย

            debug.Triggered = true;

            // ค่าที่เหลือ (marketSegmentId, segmentId, bodyId, engineId, yearFrom, yearTo, driveType)
            // ไม่สามารถ derive จาก free-text token ได้ ส่งเป็น null (ไม่ใช่ "") เพราะถ้า parameter
            // ฝั่ง SP เป็น type ตัวเลข (INT) การส่ง "" จะทำให้ SQL Server convert ล้มเหลว
            // (Conversion failed when converting the varchar value '' to data type int)
            var ktypes = await GetKtypeListByCarAsync(
                marketSegmentId: null, segmentId: null, makerId: makerId, rangeId: rangeId,
                bodyId: null, engineId: null, yearFrom: null, yearTo: null, driveType: null
            ).ConfigureAwait(false);

            debug.Ktypes = ktypes;

            if (ktypes.Count == 0)
                return debug;

            debug.Items = await GetProductsByKtypeAsync(ktypes.Distinct().ToList()).ConfigureAwait(false);

            return debug;
        }

        private async Task<List<string>> GetKtypeListByCarAsync(
            string marketSegmentId, string segmentId, string makerId, string rangeId,
            string bodyId, string engineId, string yearFrom, string yearTo, string driveType) {
            var ktypeList = new List<string>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("P_Search_Ktype_By_Car", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@inMarketseId", (object)marketSegmentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@inVehicleId", (object)segmentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@inMakerId", (object)makerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@inModelId", (object)rangeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@inBodyId", (object)bodyId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@inEngineId", (object)engineId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@inYearFrom", (object)yearFrom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@inYearTo", (object)yearTo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@inDriveType", (object)driveType ?? DBNull.Value);

                await conn.OpenAsync().ConfigureAwait(false);

                using (var dr = await cmd.ExecuteReaderAsync().ConfigureAwait(false)) {
                    while (await dr.ReadAsync().ConfigureAwait(false))
                        ktypeList.Add(dr["kType"].ToString());
                }
            }

            return ktypeList;
        }

        private async Task<List<ProductSearchVioDataResponse>> GetProductsByKtypeAsync(List<string> ktypes) {
            var responseList = new List<ProductSearchVioDataResponse>();

            var dtKtype = new DataTable();
            dtKtype.Columns.Add("Ktype", typeof(string));
            foreach (var ktype in ktypes)
                dtKtype.Rows.Add(ktype);

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("P_Search_Product_By_Ktype", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;

                var p = cmd.Parameters.AddWithValue("@inKtypeList", dtKtype);
                p.SqlDbType = SqlDbType.Structured;
                p.TypeName = "dbo.KtypeListTmp";

                await conn.OpenAsync().ConfigureAwait(false);

                using (var dr = await cmd.ExecuteReaderAsync().ConfigureAwait(false)) {
                    while (await dr.ReadAsync().ConfigureAwait(false))
                        responseList.Add(MapReaderToResponse(dr, brandColumn: "brand",
                            productGroupColumn: "productGroup", productLineColumn: "productLine"));
                }
            }

            return responseList;
        }

        // =====================================================================
        // กลุ่ม 3: productline / productgroup / brand -> P_Search_Product_By_Catagory
        // =====================================================================
        private async Task<CategoryRouteDebug> BuildCategoryGroupAsync(SearchSqlRequest request) {
            var debug = new CategoryRouteDebug();

            debug.ProductLineIds = ValuesOrEmpty(request.SourceIdsBySearchType, "productline");
            debug.ProductGroupIds = ValuesOrEmpty(request.SourceIdsBySearchType, "productgroup");
            debug.BrandIds = ValuesOrEmpty(request.SourceIdsBySearchType, "brand");

            if (debug.ProductLineIds.Count == 0 && debug.ProductGroupIds.Count == 0 && debug.BrandIds.Count == 0)
                return debug; // Triggered = false

            debug.Triggered = true;

            var dt = new DataTable();
            dt.Columns.Add("CategoryFilterType", typeof(string));
            dt.Columns.Add("CategoryFilterValue", typeof(string));

            foreach (var id in debug.ProductGroupIds)
                dt.Rows.Add("productGroupId", id);

            foreach (var id in debug.ProductLineIds)
                dt.Rows.Add("productLineId", id);

            foreach (var id in debug.BrandIds)
                dt.Rows.Add("brandId", id);

            debug.Items = await ExecuteSearchCatagoryAsync(dt).ConfigureAwait(false);

            return debug;
        }

        private async Task<List<ProductSearchVioDataResponse>> ExecuteSearchCatagoryAsync(DataTable filterTable) {
            var responseList = new List<ProductSearchVioDataResponse>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("P_Search_Product_By_Catagory", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;

                var param = cmd.Parameters.AddWithValue("@inCategoryFilter", filterTable);
                param.SqlDbType = SqlDbType.Structured;
                param.TypeName = "dbo.CategoryFilterType";

                await conn.OpenAsync().ConfigureAwait(false);

                using (var dr = await cmd.ExecuteReaderAsync().ConfigureAwait(false)) {
                    while (await dr.ReadAsync().ConfigureAwait(false))
                        responseList.Add(MapReaderToResponse(dr, brandColumn: "BrandName",
                            productGroupColumn: "productGroupName", productLineColumn: "productLineName"));
                }
            }

            return responseList;
        }

        // =====================================================================
        // Helpers
        // =====================================================================
        private static string FirstOrDefault(Dictionary<string, List<string>> map, string key) {
            List<string> values;
            if (map != null && map.TryGetValue(key, out values) && values.Count > 0)
                return values[0];

            return null;
        }

        private static List<string> ValuesOrEmpty(Dictionary<string, List<string>> map, string key) {
            List<string> values;
            if (map != null && map.TryGetValue(key, out values))
                return values;

            return new List<string>();
        }

        /// <summary>
        /// แต่ละ SP ตั้งชื่อ column brand/productGroup/productLine ไม่เหมือนกัน (บางตัว BrandName/productGroupName
        /// บางตัว brand/productGroup ตรง ๆ) รวม mapping ไว้ที่เดียวกันตรงนี้ กัน copy-paste ผิดที่ละจุด
        /// </summary>
        private static ProductSearchVioDataResponse MapReaderToResponse(
            SqlDataReader dr, string brandColumn, string productGroupColumn, string productLineColumn) {
            return new ProductSearchVioDataResponse {
                stkcode = SafeGet(dr, "stkcode"),
                stkcodeDescription = SafeGet(dr, "stkcodeDescription"),
                brand = SafeGet(dr, brandColumn),
                makerName = SafeGet(dr, "makerName"),
                modelName = SafeGet(dr, "modelName"),
                qtyReady = SafeGet(dr, "qtyReady"),
                price = SafeGet(dr, "price"),
                productGroup = SafeGet(dr, productGroupColumn),
                productLine = SafeGet(dr, productLineColumn),
                imagePath = SafeGet(dr, "imagePath")
            };
        }

        private static string SafeGet(SqlDataReader dr, string columnName) {
            var ordinal = dr.GetOrdinal(columnName);
            return dr.IsDBNull(ordinal) ? "" : dr.GetValue(ordinal).ToString();
        }
    }
}