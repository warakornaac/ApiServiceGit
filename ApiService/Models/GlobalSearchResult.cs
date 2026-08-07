using System.Collections.Generic;

namespace ApiService.Models
{
    public class GlobalSearchResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string NormalizeKeyword { get; set; }
        public List<string> SearchTypes { get; set; }
        public List<SearchDictionaryModel> MeiliHits { get; set; }
        public List<ProductSearchVioDataResponse> Items { get; set; }

        // Debug
        public List<string> Tokens { get; set; }

        /// <summary>
        /// รายละเอียดเต็มของแต่ละ token หลังผ่าน Ranking แล้ว (ก่อนส่งเข้า SearchParser)
        /// ดูได้ว่าแต่ละ token ถูกตัดที่ตำแหน่งไหน (StartIndex/EndIndex), ได้ Priority/SearchType อะไร,
        /// Score สุดท้ายเท่าไหร่ และเป็น unknown token (ไม่เจอใน dictionary) หรือไม่
        /// </summary>
        public List<TokenMatch> TokenDetails { get; set; }

        /// <summary>
        /// ผลลัพธ์ดิบจาก SearchBatch: token -> list ของ dictionary record ที่ match ทั้งหมด
        /// (ก่อนที่ RankingService จะเลือกตัวที่ Priority สูงสุดมาใช้)
        /// มีประโยชน์เวลาสงสัยว่าทำไม token นี้ถึงได้ SearchType ที่ไม่คาดคิด
        /// </summary>
        public Dictionary<string, List<SearchDictionaryModel>> BatchHits { get; set; }

        /// <summary>
        /// จำนวน record ทั้งหมด (ที่ IsActive) ที่ระบบโหลดมาจาก Meilisearch เพื่อสร้าง Trie
        /// มีประโยชน์เวลาสงสัยว่าทำไมบางคำถึงไม่ match — เช็คตัวเลขนี้เทียบกับจำนวนจริงในตาราง
        /// MsSearchDictionary (WHERE IsActive = 1) ถ้าต่างกันมาก แปลว่า Meilisearch sync ไม่ครบ
        /// หรือ pagination ใน GetAllDictionaryAsync มีปัญหา
        /// </summary>
        public int DictionaryEntryCount { get; set; }

        /// <summary>
        /// Breakdown การ route ไปยัง SP ทั้ง 3 กลุ่ม (Field/Vio/Category) — บอกว่ากลุ่มไหนถูก trigger,
        /// ใช้ parameter อะไรยิงเข้า SP จริง, และแต่ละกลุ่มคืนอะไรกลับมาก่อน merge เป็น Items สุดท้าย
        /// มีค่าเมื่อ Debug=true เท่านั้น
        /// </summary>
        public RouteDebugResult RouteDebug { get; set; }

        public SearchSqlRequest SqlRequest { get; set; }

        public GlobalSearchResult() {
            SearchTypes = new List<string>();
            MeiliHits = new List<SearchDictionaryModel>();
            Items = new List<ProductSearchVioDataResponse>();
            Tokens = new List<string>();
            TokenDetails = new List<TokenMatch>();
            BatchHits = new Dictionary<string, List<SearchDictionaryModel>>();
        }
    }
}