using ApiService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiService.Services
{
    /// <summary>
    /// Automotive Tokenizer
    ///
    /// Flow
    /// -----------------------
    /// User Input
    ///     ↓
    /// Normalize
    ///     ↓
    /// Dynamic Programming
    ///     ↓
    /// Longest Match
    ///     ↓
    /// Token List
    /// </summary>
    public class AutomotiveTokenizer
    {
        //---------------------------------------------------------
        // Dependency
        //---------------------------------------------------------

        private readonly MeiliSearchClient _meili;

        //---------------------------------------------------------
        // Constructor
        //---------------------------------------------------------

        public AutomotiveTokenizer() {
            _meili = new MeiliSearchClient();
        }

        //---------------------------------------------------------
        // Public API
        //---------------------------------------------------------

        /// <summary>
        /// ตัดคำสำหรับ Automotive Search
        /// </summary>
        public async Task<List<string>> Tokenize(string keyword) {
            keyword = NormalizeService.Normalize(keyword);

            if (String.IsNullOrWhiteSpace(keyword))
                return new List<string>();

            List<string> cache =
                TokenizerCache.Get(keyword);

            if (cache != null)
                return cache;

            Dictionary<int, TokenNode> memo =
                new Dictionary<int, TokenNode>();

            TokenNode node =
                await DP(keyword, 0, memo);

            List<string> result =
                node == null
                ? new List<string>()
                : node.Tokens;

            if (result.Count == 0)
                result.Add(keyword);

            TokenizerCache.Set(keyword, result);

            return result;
        }

        //---------------------------------------------------------
        // Dynamic Programming Entry
        //---------------------------------------------------------

        /// <summary>
        /// DP Entry
        /// จะ Implement ใน Part 4.2
        /// </summary>
        //---------------------------------------------------------
        // Dynamic Programming
        //---------------------------------------------------------

        private async Task<TokenNode> DP(
     string text,
     int start,
     Dictionary<int, TokenNode> memo) {
            //------------------------------------
            // End
            //------------------------------------

            if (start >= text.Length) {
                return new TokenNode {
                    Score = 0,
                    Tokens = new List<string>()
                };
            }

            //------------------------------------
            // Memo
            //------------------------------------

            if (memo.ContainsKey(start))
                return memo[start];

            //------------------------------------
            // Current Text
            //------------------------------------

            string remain =
                text.Substring(start);

            //------------------------------------
            // Prefix
            //------------------------------------

            List<string> prefixes =
                BuildPrefixes(remain);

            //------------------------------------
            // Search ครั้งเดียว
            //------------------------------------

            List<SearchDictionaryModel> hits =
                await _meili.SearchBatch(prefixes);

            //------------------------------------
            // Candidate ทั้งหมด
            //------------------------------------

            List<SearchDictionaryModel> candidates =
                hits
                .Where(x =>
                    !String.IsNullOrWhiteSpace(x.Keyword))
                .Where(x =>
                    remain.StartsWith(
                        x.Keyword,
                        StringComparison.OrdinalIgnoreCase))
                .Distinct(new KeywordComparer())
                .ToList();

            //------------------------------------
            // ไม่เจอ
            //------------------------------------

            if (candidates.Count == 0) {
                TokenNode fail =
                    new TokenNode {
                        Score = Double.MinValue,
                        Tokens = new List<string>()
                    };

                memo[start] = fail;

                return fail;
            }

            //------------------------------------
            // Evaluate ทุก Candidate
            //------------------------------------

            TokenNode bestNode = null;

            foreach (SearchDictionaryModel item in candidates) {
                TokenNode next =
                    await DP(
                        text,
                        start + item.Keyword.Length,
                        memo);

                if (next.Score == Double.MinValue)
                    continue;

                double score =
                    CalculateScore(item) +
                    next.Score;

                if (bestNode == null ||
                    score > bestNode.Score) {
                    bestNode =
                        new TokenNode {
                            Score = score,
                            Tokens = new List<string>()
                        };

                    bestNode.Tokens.Add(item.Keyword);

                    bestNode.Tokens.AddRange(next.Tokens);
                }
            }

            //------------------------------------
            // Save Memo
            //------------------------------------

            if (bestNode == null) {
                bestNode =
                    new TokenNode {
                        Score = Double.MinValue,
                        Tokens = new List<string>()
                    };
            }

            memo[start] = bestNode;

            return bestNode;
        }
        private async Task<List<SearchDictionaryModel>>FindCandidates(string text) {
            List<string> prefixes =
                BuildPrefixes(text);

            List<SearchDictionaryModel> hits =
                await _meili.SearchBatch(prefixes);

            return hits
                .Where(x =>
                    text.StartsWith(
                        x.Keyword,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        //---------------------------------------------------------
        // Find Longest Word
        //---------------------------------------------------------

        private async Task<string> FindLongestWord(string text) {
            if (String.IsNullOrWhiteSpace(text))
                return null;

            text = NormalizeService.Normalize(text);

            //------------------------------------------
            // Build Prefix
            //------------------------------------------

            List<string> prefixes = BuildPrefixes(text);
            System.Diagnostics.Debug.WriteLine(
    "Prefixes : " + String.Join(", ", prefixes));

            //------------------------------------------
            // Search Meili ครั้งเดียว
            //------------------------------------------

            List<SearchDictionaryModel> hits = await _meili.SearchBatch(prefixes);
            System.Diagnostics.Debug.WriteLine("Hit Count = " + hits.Count);

            foreach (var h in hits) {
                System.Diagnostics.Debug.WriteLine(
                    h.Keyword +
                    " | " +
                    h.SearchType);
            }

            if (hits == null || hits.Count == 0)
                return null;

            //------------------------------------------
            // Normalize Dictionary
            //------------------------------------------

            foreach (var h in hits) {
                h.Keyword =
                    NormalizeService.Normalize(h.Keyword);
            }

            //------------------------------------------
            // Exact Match ก่อน
            //------------------------------------------

            SearchDictionaryModel exact =
                hits.FirstOrDefault(x =>
                    x.Keyword.Equals(
                        text,
                        StringComparison.OrdinalIgnoreCase));

            if (exact != null) {

                return exact.Keyword;
            }

            //------------------------------------------
            // Longest Prefix Match
            //------------------------------------------

            SearchDictionaryModel best =
                hits
                .Where(x =>
                    !String.IsNullOrWhiteSpace(x.Keyword))
                .Where(x =>
                    text.StartsWith(
                        x.Keyword,
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Keyword.Length)
                .ThenByDescending(x => x.Priority)
                .ThenByDescending(x => x._rankingScore)
                .FirstOrDefault();

            if (best == null)
                return null;

            if (best != null) {
                System.Diagnostics.Debug.WriteLine(
                    "Best = " + best.Keyword);
            }
            else {
                System.Diagnostics.Debug.WriteLine("Best = NULL");
            }
            return best.Keyword;
        }
        //---------------------------------------------------------
        // Prefix Builder
        //---------------------------------------------------------

        private List<string> BuildPrefixes(string text) {
            text = NormalizeService.Normalize(text);

            List<string> prefixes =
                new List<string>();

            int max =
                Math.Min(text.Length, 8);

            for (int len = max; len >= 2; len--) {
                prefixes.Add(
                    text.Substring(0, len));
            }

            return prefixes;
        }
        //---------------------------------------------------------
        // Ranking Score
        //---------------------------------------------------------

        private double CalculateScore(SearchDictionaryModel item) {
            double score = 0;

            //-------------------------------------
            // Business Priority จาก Database
            //-------------------------------------

            score += item.Priority * 1000;

            //-------------------------------------
            // Meilisearch Ranking
            //-------------------------------------

            score += item._rankingScore * 100;

            //-------------------------------------
            // Longer keyword is slightly better
            //-------------------------------------

            score += item.Keyword.Length;

            return score;
        }
    }
}