using System;
using System.Collections.Generic;
using System.Linq;
using ApiService.Models;

namespace ApiService.Services
{
    /// <summary>
    /// เก็บ entry 1 ตัวพร้อม field ที่ normalize ไว้ล่วงหน้าแล้ว (ทำครั้งเดียวตอน build Trie)
    /// ใช้สำหรับ substring "contains" fallback search ใน AliasResolverService โดยไม่ต้อง normalize
    /// ซ้ำทุก query (dictionary มีหลักแสน record — normalize ซ้ำทุกครั้งจะช้าเกินไป)
    /// </summary>
    public class NormalizedDictionaryEntry
    {
        public string NormalizedKeyword { get; set; }
        public string NormalizedNormalize { get; set; }
        public SearchDictionaryModel Entry { get; set; }
    }

    /// <summary>
    /// แก้ปัญหา entry ที่มาจาก SourceTable "custom" ซึ่งเป็นข้อมูลที่ทำขึ้นเพื่อ "แก้คำสะกดผิด/ชื่อเล่น"
    /// เท่านั้น (SearchType มักเป็น alias/synonym) เช่น user พิมพ์ "เบรค" ระบบ normalize เป็น "เบรก"
    /// (สะกดถูก) — แต่ "เบรก" เองก็ยังไม่ใช่ "หมวดหมู่จริง" (productline/model/brand ฯลฯ)
    ///
    /// ทำงาน 2 ระดับ:
    ///   1) Exact lookup: ค้นหา "เบรก" แบบเป๊ะ ๆ ใน Trie ก่อน (เร็ว แม่นยำ)
    ///   2) ถ้า (1) ไม่เจอ entry จริงเลย (เจอแต่ custom) -> fallback เป็น substring "contains" search
    ///      ทั้ง dictionary (กว้างกว่า แต่ช่วยเจอเคสแบบ "เบรก" อยู่ใน "ผ้าเบรก" ที่ exact/prefix หาไม่เจอ)
    /// </summary>
    public class AliasResolverService
    {
        private const string CustomSourceTable = "custom";

        // กันกรณี alias ชี้วนกันเป็นวงจนไม่รู้จบ (เช่น A -> B -> A) จำกัดความลึกไว้ไม่เกินนี้
        private const int MaxResolveDepth = 3;

        // จำกัดจำนวนผลลัพธ์จาก substring fallback กันกรณีคำสั้นเกินไปจนมี match เยอะผิดปกติ
        // (เช่นถ้า "เบรก" ไปแมตช์กับคำที่มีคำว่า "เบรก" ประกอบอยู่เป็นร้อย ๆ คำ)
        private const int SubstringFallbackLimit = 30;

        private readonly NormalizeService _normalizeService;

        public AliasResolverService(NormalizeService normalizeService) {
            _normalizeService = normalizeService;
        }

        /// <summary>
        /// วนดูทุก token ที่มี MatchedEntries เป็น SourceTable "custom" แล้วค้นหาต่อใน Trie
        /// (exact) ก่อน ถ้ายังไม่เจอ entry จริงเลยค่อย fallback เป็น substring search จาก
        /// normalizedDictionary (ถ้ามีให้ — ถ้าไม่ส่งมาจะข้าม fallback ไปเฉย ๆ ไม่ error)
        /// </summary>
        public void Resolve(List<TokenMatch> tokens, TokenizerNode trieRoot, List<NormalizedDictionaryEntry> normalizedDictionary = null) {
            if (tokens == null || trieRoot == null)
                return;

            foreach (var token in tokens) {
                if (token.MatchedEntries == null || token.MatchedEntries.Count == 0)
                    continue;

                ResolveEntries(token, trieRoot, 0);

                // หลัง exact resolve แล้ว เช็คว่ายังไม่มี entry จริง (ไม่ใช่ custom) เลยหรือเปล่า
                // ถ้ายังไม่มี ค่อยลอง fallback แบบ substring (กว้างกว่า แพงกว่า ใช้เป็นทางเลือกสุดท้าย)
                bool hasRealEntry = token.MatchedEntries
                    .Any(e => !string.Equals(e.SourceTable, CustomSourceTable, StringComparison.OrdinalIgnoreCase));

                if (!hasRealEntry && normalizedDictionary != null) {
                    ResolveBySubstring(token, normalizedDictionary);
                }
            }
        }

        private void ResolveEntries(TokenMatch token, TokenizerNode trieRoot, int depth) {
            if (depth >= MaxResolveDepth)
                return;

            var customEntries = token.MatchedEntries
                .Where(e => string.Equals(e.SourceTable, CustomSourceTable, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (customEntries.Count == 0)
                return; // ไม่มี alias ให้ตาม ไม่ต้องทำอะไรต่อ

            bool foundNew = false;

            foreach (var customEntry in customEntries) {
                // เอาค่า Normalize (คำที่สะกดถูกต้อง) ไปค้นหาต่อ ถ้าไม่มีค่อย fallback ไปที่ Keyword
                var targetText = !string.IsNullOrWhiteSpace(customEntry.Normalize)
                    ? customEntry.Normalize
                    : customEntry.Keyword;

                if (string.IsNullOrWhiteSpace(targetText))
                    continue;

                var normalizedTarget = _normalizeService.Normalize(targetText);
                var resolvedEntries = ExactLookup(trieRoot, normalizedTarget);

                foreach (var resolved in resolvedEntries) {
                    // ข้าม entry ที่ยังเป็น "custom" (alias ซ้อน alias) ไม่เอามาปนใน MatchedEntries
                    // สนใจแค่ entry "จริง" ที่จะเอาไปใช้ query สินค้าต่อได้เท่านั้น
                    if (string.Equals(resolved.SourceTable, CustomSourceTable, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // กันเพิ่มซ้ำ (Id เดียวกับที่มีอยู่แล้วใน MatchedEntries)
                    if (token.MatchedEntries.Any(existing => existing.Id == resolved.Id))
                        continue;

                    token.MatchedEntries.Add(resolved);
                    foundNew = true;
                }
            }

            // เผื่อกรณี alias ชี้ไปหา alias อีกต่อ (ไม่ค่อยเกิดแต่กันไว้) วนซ้ำอีกรอบแบบจำกัดความลึก
            if (foundNew)
                ResolveEntries(token, trieRoot, depth + 1);
        }

        /// <summary>
        /// Fallback เมื่อ exact lookup หา entry จริงไม่เจอเลย — ค้นหาแบบ "ประกอบด้วยคำนี้" (contains)
        /// ในทั้ง dictionary แทน เช่น "เบรก" หาไม่เจอ exact แต่ไปเจอใน "ผ้าเบรก" เพราะเป็นส่วนหนึ่งของคำ
        /// ⚠️ กว้างกว่า exact/prefix มาก อาจได้ผลลัพธ์ที่ไม่เกี่ยวข้องปนมาถ้าคำสั้นเกินไป จึงจำกัดจำนวน
        /// ผลลัพธ์ไว้ที่ SubstringFallbackLimit และเรียงตาม Priority มากไปน้อยก่อนตัด
        /// </summary>
        private void ResolveBySubstring(TokenMatch token, List<NormalizedDictionaryEntry> normalizedDictionary) {
            var customEntries = token.MatchedEntries
                .Where(e => string.Equals(e.SourceTable, CustomSourceTable, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (customEntries.Count == 0)
                return;

            var addedIds = new HashSet<int>();

            foreach (var customEntry in customEntries) {
                var targetText = !string.IsNullOrWhiteSpace(customEntry.Normalize)
                    ? customEntry.Normalize
                    : customEntry.Keyword;

                if (string.IsNullOrWhiteSpace(targetText))
                    continue;

                var normalizedTarget = _normalizeService.Normalize(targetText);
                if (string.IsNullOrEmpty(normalizedTarget))
                    continue;

                var matches = normalizedDictionary
                    .Where(n => !string.Equals(n.Entry.SourceTable, CustomSourceTable, StringComparison.OrdinalIgnoreCase))
                    .Where(n =>
                        (!string.IsNullOrEmpty(n.NormalizedNormalize) && n.NormalizedNormalize.IndexOf(normalizedTarget, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(n.NormalizedKeyword) && n.NormalizedKeyword.IndexOf(normalizedTarget, StringComparison.OrdinalIgnoreCase) >= 0))
                    .OrderByDescending(n => n.Entry.Priority)
                    .Take(SubstringFallbackLimit)
                    .Select(n => n.Entry);

                foreach (var match in matches) {
                    if (addedIds.Contains(match.Id))
                        continue;

                    if (token.MatchedEntries.Any(existing => existing.Id == match.Id))
                        continue;

                    token.MatchedEntries.Add(match);
                    addedIds.Add(match.Id);
                }
            }
        }

        /// <summary>
        /// เดิน Trie ตามตัวอักษรของ normalizedText แบบเป๊ะ ๆ (exact match เท่านั้น ไม่ prefix)
        /// คืน dictionary entries ทั้งหมดที่อยู่ที่ node ปลายทาง (ถ้ามี)
        /// </summary>
        private List<SearchDictionaryModel> ExactLookup(TokenizerNode trieRoot, string normalizedText) {
            if (string.IsNullOrEmpty(normalizedText))
                return new List<SearchDictionaryModel>();

            var node = trieRoot;
            foreach (var c in normalizedText) {
                TokenizerNode next;
                if (!node.TryGetChild(c, out next))
                    return new List<SearchDictionaryModel>();

                node = next;
            }

            return node.IsEndOfWord ? node.DictionaryEntries : new List<SearchDictionaryModel>();
        }
    }
}