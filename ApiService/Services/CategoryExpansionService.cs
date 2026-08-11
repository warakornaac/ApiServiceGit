using System;
using System.Collections.Generic;
using System.Linq;
using ApiService.Models;

namespace ApiService.Services
{
    /// <summary>
    /// ขยายผลของ entry ที่เป็น "หมวดหมู่กว้าง ๆ" (เช่น productLine/productGroup) ให้ครอบคลุมหมวดย่อย
    /// ที่ชื่อขึ้นต้นด้วยคำเดียวกันด้วย เช่น token "ผ้าเบรก" match ตรง ๆ กับ record Id 52211
    /// (SourceId 172) แต่ dictionary ยังมี "ผ้าเบรก NISSIN" (173), "ผ้าเบรก ก้ามเบรก GIRLING" (174) ฯลฯ
    /// ที่ user น่าจะอยากได้ผลลัพธ์รวมมาด้วย เพราะเป็นหมวดย่อยของ "ผ้าเบรก" ทั้งหมด
    ///
    /// วิธีทำ: เดิน Trie ไปยัง node ของคำที่ match ตรง ๆ ก่อน (เช่น "ผ้าเบรก") แล้ว DFS ลงไปในทุก
    /// subtree ใต้ node นั้น เก็บทุก entry ที่เป็น IsEndOfWord มา (คือทุกคำที่ขึ้นต้นด้วย "ผ้าเบรก" นั่นเอง)
    /// ใช้ Trie ที่โหลด/cache ไว้แล้วในหน่วยความจำ ไม่ต้องยิง Meilisearch เพิ่ม
    /// </summary>
    public class CategoryExpansionService
    {
        // SearchType ที่ควรขยายแบบ prefix — เฉพาะหมวดหมู่ที่มีลำดับชั้นกว้าง->แคบจริง ๆ
        // (maker/model ไม่ควรขยายแบบนี้ เช่น "Honda" ไม่ควรลากเอา "Honda Civic" มาด้วยอัตโนมัติ)
        private static readonly HashSet<string> ExpandableSearchTypes =
            new HashSet<string>(new[] { "productLine", "productGroup" }, StringComparer.OrdinalIgnoreCase);

        // กันกรณี prefix สั้น/กว้างเกินไปจนมี descendant เยอะผิดปกติ (ป้องกัน response บวมโดยไม่ตั้งใจ)
        private const int MaxDescendantsPerEntry = 500;

        private readonly NormalizeService _normalizeService;

        public CategoryExpansionService(NormalizeService normalizeService) {
            _normalizeService = normalizeService;
        }

        /// <summary>
        /// วนดูทุก token ที่มี entry ในกลุ่ม ExpandableSearchTypes แล้วเติมหมวดย่อยที่ prefix ตรงกันเข้าไป
        /// </summary>
        public void Expand(List<TokenMatch> tokens, TokenizerNode trieRoot) {
            if (tokens == null || trieRoot == null)
                return;

            foreach (var token in tokens) {
                if (token.MatchedEntries == null || token.MatchedEntries.Count == 0)
                    continue;

                // ทำ snapshot ก่อน เพราะกำลังจะ Add เข้า MatchedEntries ระหว่าง loop (กัน modify ระหว่าง iterate)
                var expandableEntries = token.MatchedEntries
                    .Where(e => !string.IsNullOrEmpty(e.SearchType) && ExpandableSearchTypes.Contains(e.SearchType))
                    .ToList();

                foreach (var entry in expandableEntries) {
                    ExpandOneEntry(token, entry, trieRoot);
                }
            }
        }

        private void ExpandOneEntry(TokenMatch token, SearchDictionaryModel entry, TokenizerNode trieRoot) {
            var prefixText = !string.IsNullOrEmpty(entry.Normalize) ? entry.Normalize : entry.Keyword;
            if (string.IsNullOrWhiteSpace(prefixText))
                return;

            var normalizedPrefix = _normalizeService.Normalize(prefixText);
            var prefixNode = NavigateToNode(trieRoot, normalizedPrefix);
            if (prefixNode == null)
                return;

            var descendants = new List<SearchDictionaryModel>();
            CollectDescendants(prefixNode, descendants);

            foreach (var descendant in descendants) {
                if (descendants.Count > MaxDescendantsPerEntry)
                    break;

                // เอาเฉพาะตัวที่ SearchType/SourceTable เดียวกับต้นฉบับ กัน prefix ไปโดนหมวดอื่นที่ไม่เกี่ยวข้อง
                // โดยบังเอิญ (เช่นคำที่ขึ้นต้นเหมือนกันแต่เป็นคนละ SourceTable)
                if (!string.Equals(descendant.SearchType, entry.SearchType, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals(descendant.SourceTable, entry.SourceTable, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (token.MatchedEntries.Any(x => x.Id == descendant.Id))
                    continue;

                token.MatchedEntries.Add(descendant);
            }
        }

        /// <summary>เดิน Trie ตามตัวอักษรของ normalizedPrefix แบบเป๊ะ ๆ คืน node ปลายทาง (ถ้ามี)</summary>
        private TokenizerNode NavigateToNode(TokenizerNode root, string normalizedPrefix) {
            var node = root;
            foreach (var c in normalizedPrefix) {
                TokenizerNode next;
                if (!node.TryGetChild(c, out next))
                    return null;

                node = next;
            }

            return node;
        }

        /// <summary>DFS เก็บ dictionary entry ทั้งหมดที่อยู่ใต้ node นี้ (รวมตัว node เองถ้าเป็น IsEndOfWord ด้วย)</summary>
        private void CollectDescendants(TokenizerNode node, List<SearchDictionaryModel> result) {
            if (result.Count > MaxDescendantsPerEntry)
                return;

            if (node.IsEndOfWord)
                result.AddRange(node.DictionaryEntries);

            foreach (var child in node.Children.Values) {
                if (result.Count > MaxDescendantsPerEntry)
                    break;

                CollectDescendants(child, result);
            }
        }
    }
}