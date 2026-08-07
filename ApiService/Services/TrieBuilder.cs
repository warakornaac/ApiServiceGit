using System;
using System.Collections.Generic;
using ApiService.Models;

namespace ApiService.Services
{
    /// <summary>
    /// สร้าง Trie จาก list ของ SearchDictionaryModel เพื่อให้ AutomotiveTokenizer เดินหา match ได้แบบ O(1) ต่อตัวอักษร
    ///
    /// สำคัญ: ข้อมูลจริงในตาราง MsSearchDictionary มี 2 field ที่ความหมายต่างกัน
    ///   - Keyword   = คำตามที่คนพิมพ์จริง เช่น "เบรค", "จานเบรค", "วีออส", "วีออสส์"
    ///   - Normalize = ค่ามาตรฐาน/synonym ที่แท้จริง เช่น "เบรก" (สะกดถูก), "VIOS" (รหัสรุ่นภาษาอังกฤษ)
    /// สอง field นี้อาจสะกดคนละแบบกันเลย (แม้กระทั่งคนละภาษา) ดังนั้น Trie ต้องสร้างจาก "Keyword"
    /// เป็นหลัก (เพราะ DP เดินตามตัวอักษรที่ user พิมพ์) และสร้างจาก "Normalize" เพิ่มด้วย
    /// เผื่อ user พิมพ์คำที่ถูกต้องตามมาตรฐาน หรือพิมพ์เป็นภาษาอังกฤษตรง ๆ (เช่นพิมพ์ "vios" ตรง ๆ)
    /// </summary>
    public class TrieBuilder
    {
        private readonly NormalizeService _normalizeService;

        public TrieBuilder() : this(new NormalizeService()) {
        }

        public TrieBuilder(NormalizeService normalizeService) {
            _normalizeService = normalizeService;
        }

        public TokenizerNode Build(List<SearchDictionaryModel> candidates) {
            var root = new TokenizerNode();

            if (candidates == null)
                return root;

            foreach (var candidate in candidates) {
                if (!candidate.IsActive)
                    continue;

                // Path หลัก: จากคำที่คนพิมพ์จริง
                InsertIfValid(root, candidate, candidate.Keyword);

                // Path เสริม: จากค่ามาตรฐาน/synonym ถ้าสะกดต่างจาก Keyword
                // (ป้องกัน insert ซ้ำโดยเปล่าประโยชน์ถ้าสองค่าเหมือนกันอยู่แล้ว)
                if (!string.IsNullOrEmpty(candidate.Normalize) &&
                    !string.Equals(candidate.Keyword, candidate.Normalize, StringComparison.OrdinalIgnoreCase)) {
                    InsertIfValid(root, candidate, candidate.Normalize);
                }
            }

            return root;
        }

        /// <summary>
        /// Normalize ข้อความ (ด้วย NormalizeService ตัวเดียวกับที่ใช้ normalize keyword ของ user)
        /// แล้ว insert ลง Trie ทีละตัวอักษร เพื่อให้ character sequence ตรงกับที่ DP จะเดินค้นหา
        /// </summary>
        private void InsertIfValid(TokenizerNode root, SearchDictionaryModel candidate, string rawText) {
            var normalized = _normalizeService.Normalize(rawText);
            if (string.IsNullOrEmpty(normalized))
                return;

            var node = root;
            for (int i = 0; i < normalized.Length; i++) {
                node = node.GetOrAddChild(normalized[i]);
            }

            node.IsEndOfWord = true;
            node.DictionaryEntries.Add(candidate);
        }
    }
}