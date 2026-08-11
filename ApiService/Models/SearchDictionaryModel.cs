using Newtonsoft.Json;

namespace ApiService.Models
{
    /// <summary>
    /// ตัวแทนของ 1 คำใน Dictionary ที่ดึงมาจาก Meilisearch
    /// </summary>
    public class SearchDictionaryModel
    {
        public int Id { get; set; }

        /// <summary>คำต้นฉบับใน dictionary (ก่อน normalize)</summary>
        public string Keyword { get; set; }

        /// <summary>คำหลัง normalize แล้ว (ใช้สร้าง Trie)</summary>
        public string Normalize { get; set; }

        /// <summary>ประเภทของคำ เช่น Brand, Model, PartName, Attribute ฯลฯ</summary>
        public string SearchType { get; set; }

        /// <summary>Table ต้นทางของคำนี้ เช่น "Brand", "ProductModel", "PartMaster"</summary>
        public string SourceTable { get; set; }

        /// <summary>
        /// Primary key ของ record ต้นทางในตาราง SourceTable (เช่น VIO_Model.Id, MsProductLine.Id)
        /// ใช้เป็น key สำหรับ WHERE/JOIN แบบ exact match ใน SP แทนการเดาด้วย LIKE ข้อความ
        /// เก็บเป็น string เพื่อรองรับทั้งกรณี key เป็นตัวเลขหรือรหัสอื่น ๆ (ปรับ type ได้ตามจริงถ้าจำเป็น)
        /// </summary>
        public string SourceId { get; set; }

        /// <summary>ความสำคัญของคำ ใช้ในการ rank เวลามีหลาย path แข่งกัน ยิ่งมากยิ่งสำคัญ</summary>
        public int Priority { get; set; }

        /// <summary>ภาษาของคำ เช่น "th", "en" (เผื่อ dictionary รองรับหลายภาษาในอนาคต)</summary>
        public string LanguageCode { get; set; }

        /// <summary>สถานะเปิด/ปิดใช้งานคำนี้ใน dictionary</summary>
        public bool IsActive { get; set; }

        /// <summary>วันที่เพิ่มคำนี้เข้า dictionary (ตรงกับ column InsertedDate ของตาราง MsSearchDictionary)</summary>
        public System.DateTime InsertedDate { get; set; }

        /// <summary>Ranking score ที่ Meilisearch คืนมา (ยิ่งมากยิ่งตรง) ต้องเปิด showRankingScore: true ตอน query</summary>
        [JsonProperty("_rankingScore")]
        public double _rankingScore { get; set; }
    }
}