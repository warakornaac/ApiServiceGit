using System.Text;
using System.Text.RegularExpressions;

namespace ApiService.Services
{
    /// <summary>
    /// รับผิดชอบการ normalize คำค้นหาก่อนเข้า pipeline ทั้งหมด
    /// เป้าหมาย: ทำให้คำที่ user พิมพ์ (ไม่ว่าจะมีช่องว่าง, ตัวใหญ่เล็กปน, สระ/วรรณยุกต์ผิดตำแหน่ง)
    /// กลายเป็น canonical form เดียวกันเสมอ เพื่อให้ match กับ dictionary ใน Meilisearch ได้แม่นยำ
    /// </summary>
    public class NormalizeService
    {
        // เก็บ regex ไว้ static เพื่อไม่ต้อง compile ซ้ำทุกครั้งที่เรียก
        private static readonly Regex MultiSpaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        // อักขระที่อนุญาตให้เหลืออยู่: ตัวอักษรไทย, a-z, 0-9, และช่องว่าง (จะถูกตัดออกอีกทีตอนท้าย)
        private static readonly Regex InvalidCharRegex =
            new Regex(@"[^\u0E00-\u0E7Fa-zA-Z0-9\s]", RegexOptions.Compiled);

        /// <summary>
        /// Normalize คำค้นหาแบบเต็ม pipeline
        /// 1. Trim + collapse whitespace ซ้ำ
        /// 2. ลบอักขระพิเศษ (เครื่องหมายวรรคตอน, สัญลักษณ์)
        /// 3. ลบช่องว่างทั้งหมดออก (เพราะ user มักพิมพ์ติดกันหรือเว้นวรรคไม่สม่ำเสมอ
        ///    การตัดคำจริง ๆ จะไปเกิดที่ Trie + DP อีกที)
        /// 4. Lower-case (สำหรับส่วนที่เป็นอังกฤษ เช่น รุ่นรถ, ยี่ห้อ)
        /// </summary>
        public string Normalize(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return string.Empty;

            var text = keyword.Trim();
            text = MultiSpaceRegex.Replace(text, " ");
            text = InvalidCharRegex.Replace(text, string.Empty);
            text = text.Replace(" ", string.Empty);
            text = text.ToLowerInvariant();
            text = NormalizeThaiVowelOrder(text);

            return text;
        }

        /// <summary>
        /// แก้ปัญหาสระนำหน้า/วรรณยุกต์ที่ผู้ใช้พิมพ์ผิดลำดับ Unicode
        /// (เคสที่พบบ่อยจากการพิมพ์ผ่านคีย์บอร์ดบางตัว หรือ copy-paste มาจากแหล่งที่เข้ารหัสไม่ตรง)
        /// ตัวอย่างเช่น สระ + วรรณยุกต์สลับตำแหน่งกัน
        /// หมายเหตุ: ปรับ mapping เพิ่มเติมได้ตามชุดปัญหาจริงที่เจอจากข้อมูล production
        /// </summary>
        private string NormalizeThaiVowelOrder(string text)
        {
            var sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // สระลอย เช่น ฤ ฦ หรือ zero-width char ที่หลุดมา ให้ข้ามทิ้ง
                if (c == '\u200B' || c == '\uFEFF')
                    continue;

                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
