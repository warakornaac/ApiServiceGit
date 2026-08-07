using System;
using System.Runtime.Caching;
using ApiService.Models;
using System.Collections.Generic;

namespace ApiService.Services
{
    /// <summary>
    /// Cache layer หลัก ๆ เก็บ "Trie ที่สร้างเสร็จแล้ว" จาก dictionary ทั้งหมดใน MsSearchDictionary
    /// เหตุผลที่ cache ตัว Trie (ไม่ใช่ cache แค่ raw list) เพราะการโหลด dictionary ทั้งหมด +
    /// สร้าง Trie ใหม่ทุก request จะช้ามาก (dictionary มีเป็นหมื่น record) ในเมื่อ dictionary
    /// เปลี่ยนแปลงไม่บ่อย (เพิ่ม/แก้คำใหม่เป็นครั้งคราว) การ cache ไว้สัก 15-30 นาทีจึงคุ้มค่ามาก
    /// </summary>
    public class SearchCache
    {
        private static readonly MemoryCache Cache = MemoryCache.Default;

        private const string TrieRootKey = "search_trie_root_v1";
        private const string TrieEntryCountKey = "search_trie_entry_count_v1";
        private const string NormalizedDictionaryKey = "search_normalized_dictionary_v1";
        private const string DictionaryKeyPrefix = "meili_dict_"; // เผื่อใช้ cache ผลค้นหารายคำอื่น ๆ ในอนาคต

        /// <summary>
        /// อายุ cache ของ Trie อ่านจาก appSettings key "Meili.DictionaryCacheMinutes" ถ้าไม่ตั้งค่าไว้ default 15 นาที
        /// ปรับสั้นลงได้ถ้า dictionary มีการแก้ไขบ่อย หรือยาวขึ้นถ้า dictionary นิ่งและอยากลด load Meilisearch
        /// </summary>
        private static TimeSpan TrieCacheDuration {
            get {
                var raw = System.Configuration.ConfigurationManager.AppSettings["Meili.DictionaryCacheMinutes"];
                int minutes;
                if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out minutes) && minutes > 0)
                    return TimeSpan.FromMinutes(minutes);

                return TimeSpan.FromMinutes(15);
            }
        }

        public TokenizerNode GetTrieRoot() {
            return Cache.Get(TrieRootKey) as TokenizerNode;
        }

        public void SetTrieRoot(TokenizerNode root) {
            var policy = new CacheItemPolicy {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(TrieCacheDuration)
            };
            Cache.Set(TrieRootKey, root, policy);
        }

        /// <summary>จำนวน record (IsActive) ที่ถูกใช้สร้าง Trie ล่าสุด ไว้เทียบกับจำนวนจริงในตาราง MsSearchDictionary</summary>
        public int? GetTrieEntryCount() {
            var value = Cache.Get(TrieEntryCountKey);
            return value == null ? (int?)null : (int)value;
        }

        public void SetTrieEntryCount(int count) {
            var policy = new CacheItemPolicy {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(TrieCacheDuration)
            };
            Cache.Set(TrieEntryCountKey, count, policy);
        }

        /// <summary>
        /// List ของ dictionary ที่ normalize ไว้ล่วงหน้าแล้ว (Keyword/Normalize) ใช้สำหรับ
        /// substring "contains" fallback search ใน AliasResolverService — cache คู่กับ Trie เสมอ
        /// (build/invalidate พร้อมกัน) เพื่อไม่ต้อง normalize ข้อมูลหลักแสน record ซ้ำทุก query
        /// </summary>
        public List<NormalizedDictionaryEntry> GetNormalizedDictionary() {
            return Cache.Get(NormalizedDictionaryKey) as List<NormalizedDictionaryEntry>;
        }

        public void SetNormalizedDictionary(List<NormalizedDictionaryEntry> entries) {
            var policy = new CacheItemPolicy {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(TrieCacheDuration)
            };
            Cache.Set(NormalizedDictionaryKey, entries, policy);
        }

        /// <summary>
        /// ล้าง cache ของ Trie ทันที ใช้เวลาแก้ dictionary ใน MsSearchDictionary/Meilisearch
        /// แล้วอยากให้ระบบ reload dictionary ใหม่ทันทีโดยไม่ต้องรอ TTL หมดอายุ
        /// </summary>
        public void InvalidateTrieCache() {
            Cache.Remove(TrieRootKey);
            Cache.Remove(TrieEntryCountKey);
            Cache.Remove(NormalizedDictionaryKey);
        }

        public List<SearchDictionaryModel> GetDictionary(string normalizeKeyword) {
            var key = DictionaryKeyPrefix + normalizeKeyword;
            return Cache.Get(key) as List<SearchDictionaryModel>;
        }

        public void SetDictionary(string normalizeKeyword, List<SearchDictionaryModel> data) {
            var key = DictionaryKeyPrefix + normalizeKeyword;
            var policy = new CacheItemPolicy {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(30)
            };
            Cache.Set(key, data, policy);
        }

        public void Remove(string normalizeKeyword) {
            var key = DictionaryKeyPrefix + normalizeKeyword;
            Cache.Remove(key);
        }
    }
}