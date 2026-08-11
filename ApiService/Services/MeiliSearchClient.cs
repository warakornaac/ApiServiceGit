using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ApiService.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiService.Services
{
    /// <summary>
    /// Client ที่คุยกับ Meilisearch เพื่อดึง dictionary candidates
    /// (Meilisearch ในที่นี้ทำหน้าที่เป็น "Dictionary Retrieval" ไม่ใช่ product search โดยตรง)
    /// </summary>
    public class MeiliSearchClient
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _dictionaryIndex;
        private static readonly HttpClient HttpClientInstance = CreateHttpClient();

        public MeiliSearchClient() {
            // อ่านค่าจาก web.config / app.config
            // <add key="Meili.BaseUrl" value="http://localhost:7700" />
            // <add key="Meili.ApiKey" value="xxxxx" />
            // <add key="Meili.DictionaryIndex" value="search_dictionary" />
            _baseUrl = ConfigurationManager.AppSettings["Meili.BaseUrl"];
            _apiKey = ConfigurationManager.AppSettings["Meili.ApiKey"];
            _dictionaryIndex = ConfigurationManager.AppSettings["Meili.DictionaryIndex"] ?? "MsSearchDictionary";
        }

        private static HttpClient CreateHttpClient() {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            return client;
        }

        /// <summary>
        /// ดึง dictionary candidates ทั้งหมดที่ "อาจจะเกี่ยวข้อง" กับ keyword
        /// หมายเหตุ: เราไม่ได้พึ่ง Meilisearch ในการตัดคำ เราแค่ใช้มันหา candidate คำที่ "ใกล้เคียง"
        /// ผ่าน prefix/typo-tolerant search แล้วเอาไปสร้าง Trie เอง เพื่อให้ DP ตัดสินใจ path ที่ดีที่สุด
        /// </summary>
        /// <summary>
        /// SearchBatch: ยิง Meilisearch แยกทีละ token แบบขนาน (parallel)
        /// ทำไมต้องยิงซ้ำอีกรอบทั้ง ๆ ที่ตอน Tokenize ก็ query มาแล้ว?
        /// เพราะตอน Tokenize เราค้นด้วย "คำเต็มที่ยังไม่ตัด" (เช่น "เบรควีออส")
        /// ผล candidate ที่ได้จึงเป็นแค่ prefix/typo match กว้าง ๆ เอาไว้สร้าง Trie เท่านั้น
        /// แต่พอ DP ตัดคำเสร็จแล้วรู้ token ที่แท้จริง ("เบรค", "วีออส") การ query แยกทีละคำ
        /// แบบ exact-ish จะได้ _rankingScore และรายการ SearchType ที่แม่นยำกว่ามาก
        /// เอาไว้ใช้ตัดสินใจตอน Ranking ว่าคำนี้ควรตีความเป็น SearchType ไหนกันแน่
        /// </summary>
        public async Task<Dictionary<string, List<SearchDictionaryModel>>> SearchBatchAsync(List<string> tokens, int limitPerToken = 10) {
            var result = new Dictionary<string, List<SearchDictionaryModel>>();

            if (tokens == null || tokens.Count == 0)
                return result;

            var distinctTokens = tokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();

            // ยิงพร้อมกันทุก token แทนที่จะ loop แบบ sequential เพื่อลด latency รวม
            var tasks = distinctTokens
                .Select(async token => new {
                    Token = token,
                    Hits = await SearchDictionaryAsync(token, limitPerToken).ConfigureAwait(false)
                })
                .ToList();

            var allResults = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var item in allResults) {
                result[item.Token] = item.Hits;
            }

            return result;
        }

        /// <summary>
        /// โหลด dictionary ที่ IsActive ทั้งหมดจาก Meilisearch แบบ paginate ผ่าน Documents endpoint
        /// (ไม่ใช่ Search endpoint) เพราะเราต้องการ "ทุก record" มาสร้าง Trie ครั้งเดียวแล้ว cache ไว้
        /// ไม่ใช่แค่ผลที่ relevant กับ query ใด query หนึ่ง — full-text search ของ Meilisearch ไม่เหมาะกับ
        /// การหาคำสั้น ๆ ที่ซ่อนอยู่ในคำยาวที่ user พิมพ์ติดกัน (เช่น "เบรค" ใน "เบรควีออส")
        /// เพราะมันมองหา document ที่ "ขึ้นต้นด้วย query" ไม่ใช่ "เป็นส่วนหนึ่งของ query"
        /// จึงต้องโหลดทั้ง dictionary มาสร้าง Trie เองแทน แล้วให้ DP เป็นคนตัดสินใจ boundary
        /// </summary>
        public async Task<List<SearchDictionaryModel>> GetAllDictionaryAsync() {
            var result = new List<SearchDictionaryModel>();
            const int pageSize = 1000;
            int offset = 0;

            while (true) {
                var url = string.Format("{0}/indexes/{1}/documents?limit={2}&offset={3}",
                    _baseUrl.TrimEnd('/'), _dictionaryIndex, pageSize, offset);

                using (var request = new HttpRequestMessage(HttpMethod.Get, url)) {
                    if (!string.IsNullOrEmpty(_apiKey)) {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                    }

                    HttpResponseMessage response;
                    try {
                        response = await HttpClientInstance.SendAsync(request).ConfigureAwait(false);
                    }
                    catch (Exception ex) {
                        System.Diagnostics.Trace.TraceError("MeiliSearchClient.GetAllDictionaryAsync error: " + ex);
                        break;
                    }

                    if (!response.IsSuccessStatusCode) {
                        var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        System.Diagnostics.Trace.TraceError(
                            "MeiliSearchClient.GetAllDictionaryAsync non-success status " +
                            (int)response.StatusCode + ": " + errorBody);
                        break;
                    }

                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var root = JObject.Parse(responseBody);
                    var pageResults = root["results"] as JArray;

                    if (pageResults == null || pageResults.Count == 0)
                        break;

                    foreach (var item in pageResults) {
                        var model = item.ToObject<SearchDictionaryModel>();
                        if (model != null && model.IsActive)
                            result.Add(model);
                    }

                    // หน้าสุดท้ายจะได้จำนวนน้อยกว่า pageSize เสมอ (หรือเท่ากับ 0) ให้หยุด loop
                    if (pageResults.Count < pageSize)
                        break;

                    offset += pageSize;
                }
            }

            return result;
        }

        public async Task<List<SearchDictionaryModel>> SearchDictionaryAsync(string normalizeKeyword, int limit = 50) {
            if (string.IsNullOrWhiteSpace(normalizeKeyword))
                return new List<SearchDictionaryModel>();

            var requestUrl = string.Format("{0}/indexes/{1}/search", _baseUrl.TrimEnd('/'), _dictionaryIndex);

            var payload = new {
                q = normalizeKeyword,
                limit = limit,
                // ให้ Meilisearch คืนเฉพาะ field ที่ต้องใช้ ลด payload
                attributesToRetrieve = new[]
                {
                    "id", "keyword", "normalize", "searchType",
                    "sourceTable", "sourceId", "priority", "languageCode", "isActive", "insertedDate"
                },
                // หมายเหตุ: ไม่ส่ง filter "isActive = true" มาที่นี่ เพราะต้องตั้งค่า
                // filterableAttributes ที่ index ก่อนถึงจะ filter แบบนี้ได้ (ดู README ข้อ 3.1)
                // ถ้ายังไม่ได้ตั้งค่า Meilisearch จะตอบ 400 ทันที ทำให้ query ล้มเหลวทั้งหมด
                // ตอนนี้พึ่งการ filter isActive ฝั่ง client ใน ParseHits() แทนไปก่อน ปลอดภัยกว่า
                // (ถ้าตั้งค่า filterableAttributes เรียบร้อยแล้ว แนะนำเปิด filter กลับมาเพื่อลด payload)
                showRankingScore = true
                // typo tolerance ของ Meilisearch เปิดอยู่แล้ว default ช่วยจับคำที่พิมพ์เพี้ยนเล็กน้อย
            };

            var json = JsonConvert.SerializeObject(payload);

            using (var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)) {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                if (!string.IsNullOrEmpty(_apiKey)) {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                }

                HttpResponseMessage response;
                try {
                    response = await HttpClientInstance.SendAsync(request).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    // Meilisearch ล่ม/timeout ไม่ควรทำให้ทั้ง search พัง แค่คืน list ว่าง
                    // แล้วปล่อยให้ Tokenizer fallback เป็น literal token แทน
                    System.Diagnostics.Trace.TraceError("MeiliSearchClient.SearchDictionaryAsync error: " + ex);
                    return new List<SearchDictionaryModel>();
                }

                if (!response.IsSuccessStatusCode) {
                    // Log error body ไว้ด้วย เพราะ error ประเภทนี้ (เช่น filterableAttributes ไม่ได้ตั้งค่า,
                    // index ไม่มีอยู่จริง, apiKey ผิด) มักไม่ throw exception แต่ตอบ 4xx กลับมาเฉย ๆ
                    // ถ้าไม่ log ไว้จะดูจากภายนอกไม่ออกเลยว่าทำไม dictionary ถึงว่างเปล่าตลอด
                    var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    System.Diagnostics.Trace.TraceError(
                        "MeiliSearchClient.SearchDictionaryAsync non-success status " +
                        (int)response.StatusCode + ": " + errorBody);
                    return new List<SearchDictionaryModel>();
                }

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ParseHits(responseBody);
            }
        }

        private List<SearchDictionaryModel> ParseHits(string responseBody) {
            var result = new List<SearchDictionaryModel>();
            if (string.IsNullOrWhiteSpace(responseBody))
                return result;

            var root = JObject.Parse(responseBody);
            var hits = root["hits"] as JArray;
            if (hits == null)
                return result;

            foreach (var hit in hits) {
                // ใช้ JsonConvert deserialize ตรง ๆ เพราะ field ชื่อและ [JsonProperty("_rankingScore")]
                // ตรงกับ SearchDictionaryModel อยู่แล้ว ลดโอกาส map ผิดเวลา schema เปลี่ยน
                var model = hit.ToObject<SearchDictionaryModel>();

                if (model != null && model.IsActive && !string.IsNullOrWhiteSpace(model.Normalize))
                    result.Add(model);
            }

            return result;
        }
    }
}