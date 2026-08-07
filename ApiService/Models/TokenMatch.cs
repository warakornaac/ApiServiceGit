using System.Collections.Generic;

namespace ApiService.Models
{
    /// <summary>
    /// ตัวแทนของ token 1 ตัวที่ถูกตัดออกมาจากคำค้นหา (ผลลัพธ์ของ AutomotiveTokenizer)
    /// เช่น "เบรค" ที่ตำแหน่ง 0-4 ของ "เบรควีออส"
    /// </summary>
    public class TokenMatch
    {
        /// <summary>คำที่ตัดได้ เช่น "เบรค"</summary>
        public string Token { get; set; }

        /// <summary>ตำแหน่งเริ่มต้นใน keyword ที่ normalize แล้ว (inclusive)</summary>
        public int StartIndex { get; set; }

        /// <summary>ตำแหน่งสิ้นสุดใน keyword ที่ normalize แล้ว (exclusive)</summary>
        public int EndIndex { get; set; }

        /// <summary>Priority ของคำนี้ (มาจาก dictionary)</summary>
        public int Priority { get; set; }

        /// <summary>ประเภทคำ เช่น Brand, Model, PartName</summary>
        public string SearchType { get; set; }

        /// <summary>Ranking score จาก Meilisearch</summary>
        public double MeiliRankingScore { get; set; }

        /// <summary>
        /// true = คำนี้ไม่พบใน dictionary เลย (fallback เป็น literal token ตัวเดียวหรือทั้งก้อน)
        /// ใช้เพื่อลด priority ตอน ranking
        /// </summary>
        public bool IsUnknown { get; set; }

        /// <summary>
        /// คะแนนรวมหลังผ่านขั้นตอน Ranking (Priority + SearchType weight + Length + Meili ranking)
        /// ใช้สำหรับ debug และให้ SearchParser ตัดสินใจว่าจะเชื่อ token นี้แค่ไหน
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// ค่า "มาตรฐาน" ของ token นี้ตาม column Normalize ใน MsSearchDictionary
        /// เช่น Token="เบรค" (ตามที่ user พิมพ์) แต่ NormalizedValue="เบรก" (สะกดมาตรฐาน)
        /// หรือ Token="วีออส" แต่ NormalizedValue="VIOS" (รหัสรุ่นที่ใช้ค้นในตาราง product จริง)
        /// ค่านี้มาจาก entry ที่ Priority สูงสุดใน MatchedEntries เป็นค่าตัวแทนเดี่ยว ๆ (ใช้ตอน scoring/debug)
        /// แต่ตอนสร้าง SqlRequest จริง SearchParser จะใช้ MatchedEntries ทั้งหมด ไม่ใช่แค่ค่านี้ค่าเดียว
        /// </summary>
        public string NormalizedValue { get; set; }

        /// <summary>
        /// dictionary entry "ทั้งหมด" ที่ token นี้ match ได้ (ไม่ใช่แค่ตัวที่ Priority สูงสุด)
        /// เช่น token "civic" อาจ match ได้ทั้ง { SearchType: "model", SourceTable: "VIO_Model" }
        /// และ { SearchType: "synonym", SourceTable: "custom" } พร้อมกัน — เก็บไว้ทั้งคู่ที่นี่
        /// เพื่อให้ SearchParser ใช้สร้าง SearchTypes/Tokens ที่ครอบคลุมทุกแหล่งข้อมูล ไม่ใช่เลือกแค่ตัวเดียว
        /// แล้วทำให้ผลลัพธ์จาก table อื่น (เช่น VIO_Model) หายไปจาก pipeline
        /// </summary>
        public List<SearchDictionaryModel> MatchedEntries { get; set; }

        public int Length {
            get { return EndIndex - StartIndex; }
        }
    }
}