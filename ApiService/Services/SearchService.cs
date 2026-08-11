using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using ApiService.Models;

namespace ApiService.Services
{
    /// <summary>
    /// Entry point ของทั้ง Global Search pipeline
    /// User Keyword -> Normalize -> Trie (จาก dictionary ทั้งหมดที่ cache ไว้) -> DP Tokenize
    /// -> SearchBatch -> Ranking -> AliasResolve -> CategoryExpansion -> SearchParser -> SearchRouterService
    /// (route ไปยัง P_Search_Product_By_Field / P_Search_Ktype_By_Car+P_Search_Product_By_Ktype /
    ///  P_Search_Product_By_Catagory ตาม SearchType ที่เจอ) -> GlobalSearchResult
    /// </summary>
    public class SearchService
    {
        private readonly NormalizeService _normalizeService;
        private readonly MeiliSearchClient _meiliClient;
        private readonly TrieBuilder _trieBuilder;
        private readonly AutomotiveTokenizer _tokenizer;
        private readonly RankingService _rankingService;
        private readonly AliasResolverService _aliasResolverService;
        private readonly CategoryExpansionService _categoryExpansionService;
        private readonly SearchParser _searchParser;
        private readonly SearchRouterService _searchRouterService;
        private readonly SearchCache _cache;

        public SearchService() {
            _normalizeService = new NormalizeService();
            _meiliClient = new MeiliSearchClient();
            _trieBuilder = new TrieBuilder(_normalizeService);
            _tokenizer = new AutomotiveTokenizer();
            _rankingService = new RankingService();
            _aliasResolverService = new AliasResolverService(_normalizeService);
            _categoryExpansionService = new CategoryExpansionService(_normalizeService);
            _searchParser = new SearchParser();
            _cache = new SearchCache();

            var connectionString = ConfigurationManager.ConnectionStrings["Ecatalog_ConnectionString"].ConnectionString;
            _searchRouterService = new SearchRouterService(connectionString);
        }

        /// <summary>
        /// บังคับ reload dictionary จาก Meilisearch ใหม่ทันที (ล้าง cache ของ Trie)
        /// เรียกใช้ตอนแก้ไข/เพิ่มคำใน MsSearchDictionary แล้วต้องการให้ระบบเห็นผลทันทีโดยไม่ต้องรอ TTL
        /// </summary>
        public void InvalidateDictionaryCache() {
            _cache.InvalidateTrieCache();
        }

        /// <summary>
        /// เรียกทั้ง pipeline แบบ end-to-end จาก keyword ดิบที่ user พิมพ์
        /// ชื่อ method ตรงกับที่ Controller เรียกใช้: service.GlobalSearch(Keyword)
        /// </summary>
        /// <param name="rawKeyword">คำค้นหาดิบจาก user</param>
        /// <param name="debug">
        /// true = ยัง route ไปยัง SP จริงทั้ง 3 กลุ่มตามปกติ (ไม่ข้าม) แต่แนบ breakdown การ route
        /// (RouteDebug: กลุ่มไหน trigger, ใช้ parameter อะไร, แต่ละกลุ่มคืนอะไรกลับมาก่อน merge)
        /// รวมถึง TokenDetails/BatchHits เข้าไปด้วย เพื่อ debug ทั้ง pipeline ตั้งแต่ tokenize จนถึงผลจริง
        /// false (default) = รัน pipeline ปกติ ไม่แนบข้อมูล debug เพิ่ม
        /// </param>
        public async Task<GlobalSearchResult> GlobalSearch(string rawKeyword, bool debug = false) {
            var result = new GlobalSearchResult();

            // 1) Normalize
            var normalizeKeyword = _normalizeService.Normalize(rawKeyword);
            result.NormalizeKeyword = normalizeKeyword;

            if (string.IsNullOrEmpty(normalizeKeyword)) {
                result.Success = false;
                result.Message = "กรุณากรอกคำค้นหา";
                return result;
            }

            // 2) Dictionary Retrieval + Build Trie
            // สำคัญ: ไม่ query Meilisearch ด้วยคำเต็มที่ user พิมพ์อีกต่อไป เพราะ full-text search
            // จะหา document ที่ "ขึ้นต้นด้วย query" ไม่ใช่ "เป็นส่วนหนึ่งของ query" ทำให้คำสั้น ๆ
            // อย่าง "เบรค"/"วีออส" ไม่มีทาง match กับ query ยาว ๆ ที่พิมพ์ติดกันอย่าง "เบรควีออส" เลย
            // แทนที่จะทำแบบนั้น เราโหลด dictionary ที่ active ทั้งหมดมาสร้าง Trie ครั้งเดียว แล้ว cache ไว้
            // (TTL ปรับได้ที่ appSettings Meili.DictionaryCacheMinutes) ให้ DP เป็นคนตัดสินใจ boundary เอง
            var trieRoot = _cache.GetTrieRoot();
            if (trieRoot == null) {
                var fullDictionary = await _meiliClient.GetAllDictionaryAsync().ConfigureAwait(false);
                trieRoot = _trieBuilder.Build(fullDictionary);
                _cache.SetTrieRoot(trieRoot);
                _cache.SetTrieEntryCount(fullDictionary.Count);

                // สร้าง normalized dictionary list พร้อมกันตอนนี้เลย (ทำครั้งเดียวตอน cache miss)
                // ใช้สำหรับ substring fallback ใน AliasResolverService — ไม่ต้อง normalize
                // ข้อมูลหลักแสน record ซ้ำทุก query เพราะ normalize ไว้ล่วงหน้าแล้วตรงนี้ที่เดียว
                var normalizedDictionary = fullDictionary
                    .Where(e => e.IsActive)
                    .Select(e => new NormalizedDictionaryEntry {
                        NormalizedKeyword = _normalizeService.Normalize(e.Keyword),
                        NormalizedNormalize = _normalizeService.Normalize(e.Normalize),
                        Entry = e
                    })
                    .ToList();
                _cache.SetNormalizedDictionary(normalizedDictionary);
            }

            // 3) Dynamic Programming หา best path ของการตัดคำ
            var bestPath = _tokenizer.Tokenize(normalizeKeyword, trieRoot);

            // 4) SearchBatch: ยิงค้นหาแยกทีละ token แบบขนาน เพื่อเอาข้อมูลที่แม่นยำกว่ามา rank
            //    ข้าม token ที่เป็น unknown (ไม่เจอใน dictionary เลย) เพราะยิงไปก็ไม่มีประโยชน์
            var knownTokenTexts = bestPath.Where(t => !t.IsUnknown)
                                           .Select(t => t.Token)
                                           .Distinct()
                                           .ToList();

            var batchHits = await _meiliClient.SearchBatchAsync(knownTokenTexts).ConfigureAwait(false);

            // เอาเฉพาะ record ที่เกี่ยวข้องกับ token ที่ตัดได้จริงมาใส่ MeiliHits (ไม่ใช่ dictionary ทั้งก้อน
            // ที่โหลดมาสร้าง Trie ซึ่งอาจมีเป็นหมื่น record) ให้ response มีขนาดพอเหมาะและเป็นประโยชน์จริง
            result.MeiliHits = batchHits.Values
                                         .SelectMany(list => list)
                                         .GroupBy(h => h.Id)
                                         .Select(g => g.First())
                                         .ToList();

            // 5) Ranking: เติมข้อมูลแม่นยำจาก SearchBatch ให้แต่ละ token แล้วคำนวณ Score สุดท้าย
            var rankedTokens = _rankingService.Rank(bestPath, batchHits);

            // 5.1) Alias Resolve: ถ้า token ไหน match ได้แค่ entry ที่ SourceTable = "custom"
            // (ข้อมูลแก้คำสะกดผิด/ชื่อเล่น เช่น "เบรค" -> normalize เป็น "เบรก") ให้เอาค่า Normalize
            // นั้นไปค้นหาต่อใน Trie แบบ exact ก่อน ถ้ายังไม่เจอ entry จริงเลย (เจอแต่ custom) จะ fallback
            // เป็น substring "contains" search ทั้ง dictionary ต่อ (เช่น "เบรก" ไปเจอใน "ผ้าเบรก" เพราะ
            // เป็นส่วนหนึ่งของคำ ถึงจะไม่ใช่ prefix ก็ตาม) เพราะ "custom" เองไม่ใช่หมวดหมู่ที่ route ต่อได้จริง
            _aliasResolverService.Resolve(rankedTokens, trieRoot, _cache.GetNormalizedDictionary());

            // 5.2) Category Expansion: entry ที่เป็นหมวดหมู่กว้าง ๆ (productLine/productGroup) เช่น
            // "ผ้าเบรก" (SourceId 172) ให้ดึงหมวดย่อยที่ชื่อขึ้นต้นด้วยคำเดียวกันมาด้วย (เช่น "ผ้าเบรก NISSIN"
            // SourceId 173, "ผ้าเบรก ก้ามเบรก GIRLING" SourceId 174 ฯลฯ) เพราะ user ที่ค้นหาคำกว้าง ๆ
            // มักอยากได้สินค้าทุกหมวดย่อยด้วย ไม่ใช่แค่ record ที่ตรงเป๊ะ ๆ ตัวเดียว
            _categoryExpansionService.Expand(rankedTokens, trieRoot);

            result.Tokens = rankedTokens.Select(t => t.Token).ToList();

            // 6) SearchParser: token ที่ rank แล้ว -> SearchTypes + SqlRequest (รวม SourceIdsBySearchType
            //    และ TokensBySearchType ที่ SearchRouterService ใช้ตัดสินใจ route ต่อไป)
            var sqlRequest = _searchParser.Parse(normalizeKeyword, rankedTokens);
            result.SearchTypes = sqlRequest.SearchTypes;
            result.SqlRequest = sqlRequest;

            // 7) SearchRouterService: route ไปยัง SP จริงตาม SearchType ที่เจอ ยิงจริงเสมอทั้ง Debug=true/false
            //    - description/oe/competitor -> P_Search_Product_By_Field
            //    - model/maker                -> P_Search_Ktype_By_Car + P_Search_Product_By_Ktype
            //    - productline/productgroup/brand -> P_Search_Product_By_Catagory
            //    ถ้าเจอหลายกลุ่มพร้อมกัน จะยิงขนานแล้ว union ผลลัพธ์ (dedupe ด้วย stkcode)
            try {
                var routeResult = await _searchRouterService.RouteWithDebugAsync(sqlRequest).ConfigureAwait(false);
                result.Items = routeResult.MergedItems;
                result.Success = true;

                if (debug) {
                    // แนบ breakdown การ route (กลุ่มไหน trigger, ใช้ parameter อะไร, ผลดิบก่อน merge)
                    // พร้อมข้อมูล tokenize/ranking ไว้ debug ทั้ง pipeline
                    result.RouteDebug = routeResult;
                    result.TokenDetails = rankedTokens;
                    result.BatchHits = batchHits;
                    result.DictionaryEntryCount = _cache.GetTrieEntryCount() ?? -1;
                }

                if (result.Items.Count == 0 && sqlRequest.SearchTypes.Count == 0) {
                    result.Message = "ไม่พบคำที่ตรงกับ dictionary เลย (ทุก token เป็น unknown)";
                }
            }
            catch (Exception ex) {
                result.Success = false;
                // Debug=true: โชว์ message จริงเพื่อช่วย diagnose (เช่น SQL type conversion error,
                // SP/TVP ไม่มีอยู่จริง ฯลฯ) — production (Debug=false) ยังคง generic message ไว้กันข้อมูล
                // ภายในหลุด (connection string, ชื่อ SP, structure ของ DB) ออกไปหน้าบ้าน
                result.Message = debug
                    ? "เกิดข้อผิดพลาดระหว่างค้นหาสินค้า: " + ex.Message
                    : "เกิดข้อผิดพลาดระหว่างค้นหาสินค้า";
                System.Diagnostics.Trace.TraceError("SearchService.GlobalSearch route error: " + ex);
            }

            return result;
        }
    }
}