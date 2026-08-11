using System.Collections.Generic;

namespace ApiService.Models
{
    /// <summary>
    /// พารามิเตอร์ที่ SearchParser เตรียมไว้เพื่อยิงเข้า SP P_Search_Product_Global
    /// </summary>
    public class SearchSqlRequest
    {
        /// <summary>คำค้นหาที่ normalize แล้วทั้งก้อน (เผื่อ SP ต้องการ full text ด้วย)</summary>
        public string NormalizeKeyword { get; set; }

        /// <summary>
        /// List ของค่าที่จะใช้ WHERE/LIKE กับตาราง product จริง — เป็น union ของทั้งค่า Normalize
        /// (มาตรฐาน เช่น "CIVIC") และ Keyword ดิบ (เช่น "Civic") ของทุก dictionary entry ที่ token
        /// ทั้งหมดใน query นี้ match ได้ (dedupe แบบ case-insensitive) บวก literal ของ unknown token
        /// (คำที่ไม่เจอใน dictionary เลย แต่ยังอาจมีความหมาย เช่น รหัสสินค้าที่ dictionary ยังไม่ครอบคลุม)
        /// </summary>
        public List<string> Tokens { get; set; }

        /// <summary>List ของ SearchType ที่เกี่ยวข้องทั้งหมด (distinct, ไม่ตัดตัวไหนทิ้งแม้ Priority ต่ำกว่า)</summary>
        public List<string> SearchTypes { get; set; }

        /// <summary>
        /// List ของ SourceTable ที่เกี่ยวข้องทั้งหมด (distinct) เช่น "VIO_Model", "custom", "MsProductLine"
        /// เผื่อ SP ต้องการรู้ว่าต้อง join ตารางไหนเพิ่มบ้างนอกเหนือจากตาราง Product หลัก
        /// </summary>
        public List<string> SourceTables { get; set; }

        /// <summary>
        /// จับคู่ SourceTable -> list ของ SourceId (primary key จริงในตารางนั้น) ที่เกี่ยวข้องกับ query นี้
        /// เช่น { "VIO_Model": ["101","205"], "MsProductLine": ["55"] }
        /// ใช้ WHERE/JOIN แบบ exact match (WHERE Id IN (...)) แทนการเดาด้วย LIKE ข้อความ แม่นยำและเร็วกว่ามาก
        /// เฉพาะ entry ที่มี SourceId จริง (ไม่ null/ว่าง) เท่านั้นที่จะถูกรวมไว้ที่นี่
        /// </summary>
        public Dictionary<string, List<string>> SourceIdsByTable { get; set; }

        /// <summary>
        /// จับคู่ SearchType -> list ของ SourceId เช่น { "model": ["205"], "maker": ["H04"] }
        /// ใช้สำหรับ routing ไปยัง endpoint ที่ถูกต้อง (GetProductBySearchVio/Catagory/Field)
        /// ตาม SearchType ที่พบ — ดู SearchRouterService
        /// </summary>
        public Dictionary<string, List<string>> SourceIdsBySearchType { get; set; }

        /// <summary>Token ที่ map กับ SearchType แบบคู่ ๆ เผื่อ SP ต้องการ filter แยกฟิลด์ เช่น Brand = "วีออส"</summary>
        public Dictionary<string, string> TokenBySearchType { get; set; }

        /// <summary>
        /// จับคู่ SearchType -> list ของ token ดิบ (t.Token) ที่ match ได้ ใช้กับกลุ่ม SearchType ที่ไม่มี
        /// SourceId แบบ id-based (เช่น description/oe/competitor) ซึ่งต้องใช้ข้อความไปค้นแบบ full-text
        /// ผ่าน GetProductBySearchField แทนที่จะ join ด้วย id
        /// </summary>
        public Dictionary<string, List<string>> TokensBySearchType { get; set; }

        public int TopN { get; set; }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public SearchSqlRequest() {
            Tokens = new List<string>();
            SearchTypes = new List<string>();
            SourceTables = new List<string>();
            SourceIdsByTable = new Dictionary<string, List<string>>();
            SourceIdsBySearchType = new Dictionary<string, List<string>>();
            TokensBySearchType = new Dictionary<string, List<string>>();
            TokenBySearchType = new Dictionary<string, string>();
            TopN = 200;
            PageIndex = 1;
            PageSize = 50;
        }
    }
}