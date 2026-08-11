using System.Collections.Generic;
using System.Linq;
using ApiService.Models;

namespace ApiService.Services
{
    /// <summary>
    /// Ranking: ทำหน้าที่คั่นกลางระหว่าง SearchBatch กับ SearchParser
    /// รับผล SearchBatch (ผลค้นหาแยกทีละ token) มา "เติม" ข้อมูลที่แม่นยำกว่าเดิมให้ token
    /// ที่ได้จาก AutomotiveTokenizer โดยยึดคะแนนจากคอลัมน์ Priority ในตาราง MsSearchDictionary เป็นหลัก
    ///
    /// สำคัญ: ถ้า 1 token match กับ dictionary ได้หลาย record ที่ SearchType/SourceTable ต่างกัน
    /// (เช่น "civic" match ได้ทั้ง { model, VIO_Model, Priority 95 } และ { synonym, custom, Priority 100 })
    /// จะ "เก็บไว้ทั้งหมด" ใน MatchedEntries ไม่ตัดทิ้งตัวที่ Priority ต่ำกว่า เพราะแต่ละ record
    /// อาจต้องใช้ query กับคนละตาราง/คนละ column ใน SP — ถ้าตัดทิ้งจะทำให้ผลลัพธ์จากตารางนั้นหายไปทั้งหมด
    /// Priority ใช้แค่เลือก "ตัวแทน" (Priority/SearchType/NormalizedValue เดี่ยว ๆ บน token) สำหรับ debug/scoring เท่านั้น
    /// </summary>
    public class RankingService
    {
        /// <summary>
        /// เติมข้อมูลจาก SearchBatch ให้แต่ละ token (เก็บทุก record ที่ match ไว้ใน MatchedEntries)
        /// คืน list เดิม (เรียงตามตำแหน่งในคำค้นหา) แต่ enrich ข้อมูลแล้ว
        /// </summary>
        public List<TokenMatch> Rank(List<TokenMatch> tokens, Dictionary<string, List<SearchDictionaryModel>> batchHitsByToken) {
            if (tokens == null || tokens.Count == 0)
                return new List<TokenMatch>();

            foreach (var token in tokens) {
                if (token.IsUnknown) {
                    // token ที่ไม่เจอใน dictionary เลย ไม่มี Priority ให้ rank ตั้งเป็น 0 ไปเลย
                    token.Score = 0;
                    continue;
                }

                List<SearchDictionaryModel> hits;
                if (batchHitsByToken != null && batchHitsByToken.TryGetValue(token.Token, out hits) && hits.Count > 0) {
                    // มีผลจาก SearchBatch (ตรง token เป๊ะ ๆ) ให้ใช้ "ทั้งชุด" นี้แทนของเดิมจาก Trie
                    // เพราะเป็นข้อมูลล่าสุด/แม่นยำที่สุดจาก MsSearchDictionary — ไม่ตัดทิ้งตัวไหนเลย
                    token.MatchedEntries = hits;
                }
                // ถ้าไม่มีผลจาก SearchBatch เลย (เช่น network error, Meilisearch index ยังไม่ได้ตั้งค่า
                // searchable attributes ให้ครบ, หรือ token หลุด filter isActive) จะยังคง MatchedEntries
                // เดิมที่ AutomotiveTokenizer เก็บไว้จาก Trie ตอน DP ต่อไป (ดีกว่าไม่มีข้อมูลเลย)

                // เลือก "ตัวแทน" ที่ Priority สูงสุดไว้ใช้แสดงผลเดี่ยว ๆ (debug/scoring) เท่านั้น
                // ตัวแทนนี้ "ไม่ใช่" ตัวตัดสินว่า SearchType อื่นจะถูกทิ้ง — SearchParser จะใช้ MatchedEntries
                // ทั้งหมดสร้าง SqlRequest ไม่ใช่ใช้แค่ตัวแทนตัวนี้
                if (token.MatchedEntries != null && token.MatchedEntries.Count > 0) {
                    var representative = token.MatchedEntries
                        .OrderByDescending(h => h.Priority)
                        .ThenByDescending(h => h._rankingScore)
                        .First();

                    token.Priority = representative.Priority;
                    token.SearchType = representative.SearchType;
                    token.MeiliRankingScore = representative._rankingScore;
                    token.NormalizedValue = !string.IsNullOrEmpty(representative.Normalize)
                        ? representative.Normalize
                        : representative.Keyword;
                }

                // Score ของ token = Priority ของตัวแทน ใช้เป็นเกณฑ์ debug/แสดงผลเท่านั้น
                token.Score = token.Priority;
            }

            return tokens;
        }
    }
}