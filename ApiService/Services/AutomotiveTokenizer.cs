using System;
using System.Collections.Generic;
using System.Linq;
using ApiService.Models;

namespace ApiService.Services
{
    /// <summary>
    /// ตัดคำค้นหาที่ไม่มีช่องว่าง (เช่น "เบรควีออส") ออกเป็น token ที่มีความหมาย
    /// โดยใช้ Trie (จาก TrieBuilder) + Dynamic Programming เพื่อหา "path" ที่ดีที่สุด
    /// แนวคิดเดียวกับ word-segmentation ของภาษาที่ไม่มีช่องว่าง (เช่น ตัดคำภาษาไทย/จีน/ญี่ปุ่น)
    ///
    /// dp[i]  = คะแนนสะสมที่ดีที่สุดของการตัดคำ text[0..i)
    /// back[i]= TokenMatch ตัวสุดท้ายที่ทำให้ไปถึงตำแหน่ง i ได้ (ใช้ reconstruct path)
    /// </summary>
    public class AutomotiveTokenizer
    {
        // น้ำหนักคะแนนแต่ละปัจจัย ปรับ tune ได้ตามข้อมูลจริง
        private const double WeightPriority = 1.0;
        private const double WeightLength = 15.0;      // ให้รางวัลกับคำที่ยาวกว่า (กันการตัดคำสั้นเกินจำเป็น)
        private const double WeightMeiliScore = 80.0;  // meili ranking score ปกติอยู่ช่วง 0-1

        // โทษของ "ตัวอักษรที่ไม่พบใน dictionary เลย" (unknown token)
        // ตั้งให้เป็นลบมาก ๆ เพื่อบีบให้ DP เลือก path ที่ match กับ dictionary ก่อนเสมอถ้าเป็นไปได้
        private const double UnknownCharPenalty = -1000.0;

        public List<TokenMatch> Tokenize(string normalizeKeyword, TokenizerNode trieRoot) {
            if (string.IsNullOrEmpty(normalizeKeyword) || trieRoot == null)
                return new List<TokenMatch>();

            int n = normalizeKeyword.Length;

            var dp = new double[n + 1];
            var back = new TokenMatch[n + 1];
            var reachable = new bool[n + 1];

            for (int i = 1; i <= n; i++)
                dp[i] = double.NegativeInfinity;

            dp[0] = 0;
            reachable[0] = true;

            for (int i = 0; i < n; i++) {
                if (!reachable[i])
                    continue;

                // ป้องกันพิเศษ: ถ้าตำแหน่งนี้อยู่ "กลาง" กลุ่มตัวเลขที่ติดกัน (ตัวก่อนหน้าก็เป็นเลขด้วย)
                // ห้ามเริ่ม token ใหม่จากตรงนี้เด็ดขาด บังคับให้กลุ่มตัวเลขต้องถูกตัดสินใจจาก
                // "ตำแหน่งเริ่มต้นของกลุ่ม" เท่านั้น (ดู block 1 และ 3) เหตุผล: dictionary บางครั้งมี
                // ตัวเลขเดี่ยว ๆ ("2","0","4") ถูก index เป็นคำแยกโดยไม่ตั้งใจ (ข้อมูลผิดปกติ/import พัง)
                // ถ้าไม่กันตรงนี้ DP จะเห็นว่า match ตัวเลขเดี่ยว ๆ กับ dictionary ได้คะแนนดีกว่า
                // "รวมเป็นก้อน" (เพราะ dictionary match มี priority จริง ส่วน unknown โดน flat penalty)
                // ทำให้ "2014" ถูกแยกเป็น "2","0","4" ทั้งที่ควรรวมเป็นก้อนเดียว
                bool isMidDigitRun = i > 0
                    && char.IsDigit(normalizeKeyword[i])
                    && char.IsDigit(normalizeKeyword[i - 1]);

                if (isMidDigitRun)
                    continue;

                // 1) เดิน Trie จากตำแหน่ง i ไปข้างหน้าเรื่อย ๆ เพื่อหาคำที่ match ได้ทุกความยาว
                var node = trieRoot;
                for (int j = i; j < n; j++) {
                    TokenizerNode nextNode;
                    if (!node.TryGetChild(normalizeKeyword[j], out nextNode))
                        break; // ไม่มี path นี้ใน trie แล้ว หยุดเดินต่อ

                    node = nextNode;

                    if (node.IsEndOfWord) {
                        // มีคำใน dictionary จบพอดีที่ตำแหน่ง j (inclusive) -> token คือ [i, j+1)
                        // node.DictionaryEntries อาจมีหลาย record พร้อมกัน (เช่น "civic" ทั้ง SearchType
                        // "model"/VIO_Model และ "synonym"/custom) เก็บไว้ "ทั้งหมด" ใน MatchedEntries
                        // ส่วน bestEntry ใช้แค่เป็นตัวแทนสำหรับคำนวณคะแนน DP และ Priority/SearchType เบื้องต้นเท่านั้น
                        var bestEntry = PickBestEntry(node.DictionaryEntries);
                        var token = new TokenMatch {
                            Token = normalizeKeyword.Substring(i, j - i + 1),
                            StartIndex = i,
                            EndIndex = j + 1,
                            Priority = bestEntry.Priority,
                            SearchType = bestEntry.SearchType,
                            MeiliRankingScore = bestEntry._rankingScore,
                            NormalizedValue = !string.IsNullOrEmpty(bestEntry.Normalize) ? bestEntry.Normalize : bestEntry.Keyword,
                            MatchedEntries = new List<SearchDictionaryModel>(node.DictionaryEntries),
                            IsUnknown = false
                        };

                        double score = dp[i] + ScoreToken(token);
                        int endPos = j + 1;

                        if (score > dp[endPos]) {
                            dp[endPos] = score;
                            back[endPos] = token;
                            reachable[endPos] = true;
                        }
                    }
                }

                // 2) เผื่อกรณีไม่มี match ใน dictionary เลยตรงตำแหน่ง i
                //    ให้ fallback เป็น "unknown token" ความยาว 1 ตัวอักษร เพื่อให้ DP เดินต่อได้เสมอ
                //    (ป้องกันเคส dictionary ไม่ครอบคลุมคำใหม่ ๆ เช่น รุ่นรถออกใหม่ที่ยังไม่ได้ index)
                {
                    var unknownToken = new TokenMatch {
                        Token = normalizeKeyword.Substring(i, 1),
                        StartIndex = i,
                        EndIndex = i + 1,
                        Priority = 0,
                        SearchType = null,
                        MeiliRankingScore = 0,
                        NormalizedValue = normalizeKeyword.Substring(i, 1),
                        MatchedEntries = new List<SearchDictionaryModel>(),
                        IsUnknown = true
                    };

                    double score = dp[i] + UnknownCharPenalty;
                    int endPos = i + 1;

                    if (score > dp[endPos]) {
                        dp[endPos] = score;
                        back[endPos] = unknownToken;
                        reachable[endPos] = true;
                    }
                }

                // 3) กรณีพิเศษ: ถ้าตำแหน่ง i เป็น "ตัวเลข" ให้เพิ่ม option รวมตัวเลขที่ติดกันทั้งหมด
                //    เป็น unknown token เดียว (เช่น "2014" ไม่อยากให้แตกเป็น "2","0","1","4")
                //    เลขรุ่นปี/รหัสสินค้าแทบไม่มีทางอยู่ใน dictionary คำศัพท์อยู่แล้ว จึงต้องกันไว้เป็นพิเศษ
                //    Penalty เท่ากับ unknown ปกติ (คงที่ ไม่ได้คูณตามความยาว) ทำให้รวมเป็นก้อนเดียวได้คะแนน
                //    ดีกว่าการตัดทีละหลัก (ซึ่งโดน UnknownCharPenalty ซ้ำหลายรอบ) เสมอ DP จะเลือกแบบรวมเอง
                if (char.IsDigit(normalizeKeyword[i])) {
                    int j = i;
                    while (j < n && char.IsDigit(normalizeKeyword[j]))
                        j++;

                    if (j > i + 1) // มีเลขติดกันมากกว่า 1 หลัก ถึงจะคุ้มที่จะเพิ่ม option นี้
                    {
                        var numberToken = new TokenMatch {
                            Token = normalizeKeyword.Substring(i, j - i),
                            StartIndex = i,
                            EndIndex = j,
                            Priority = 0,
                            SearchType = null,
                            MeiliRankingScore = 0,
                            NormalizedValue = normalizeKeyword.Substring(i, j - i),
                            MatchedEntries = new List<SearchDictionaryModel>(),
                            IsUnknown = true
                        };

                        double score = dp[i] + UnknownCharPenalty;
                        int endPos = j;

                        if (score > dp[endPos]) {
                            dp[endPos] = score;
                            back[endPos] = numberToken;
                            reachable[endPos] = true;
                        }
                    }
                }

                // 4) กรณีทั่วไป: ถ้าตำแหน่ง i เป็นตัวอักษรกลุ่มเดียวกัน (ไทยล้วน หรือ อังกฤษล้วน)
                //    ให้เพิ่ม option รวมตัวอักษรกลุ่มเดียวกันที่เหลือ (นับจาก i ไปจนกว่าจะเปลี่ยนกลุ่ม)
                //    เป็น unknown token เดียว เช่น "หน้า" (ไม่มีใน dictionary เลย) ไม่อยากให้แตกเป็น
                //    "ห","น","้","า" ทีละตัวอักษร
                //
                //    สำคัญ: คำนวณที่ "ทุกตำแหน่ง" ในกลุ่ม ไม่ใช่แค่ตำแหน่งเริ่มต้นของกลุ่มทั้งก้อน
                //    เพราะถ้าจำกัดแค่ตำแหน่งเริ่มต้น จะพลาดเคสที่มี dictionary word จริงอยู่ตรงกลาง
                //    (เช่น "ผ้าเบรกหน้า" ที่ "ผ้าเบรก" เป็น dictionary word จริง ตามด้วย "หน้า" ที่ไม่มี
                //    ใน dictionary — ทั้งสองคำเป็นภาษาไทยล้วนเหมือนกัน ไม่มีการเปลี่ยนกลุ่มตัวอักษรให้สังเกตได้)
                //    การคำนวณทุกตำแหน่งทำให้ DP มีตัวเลือก "ผ้าเบรก"(match จริง) + "หน้า"(รวมเป็นก้อน)
                //    ให้เทียบกับตัวเลือก "รวมทั้งหมดเป็น unknown ก้อนเดียว" ได้อย่างเป็นธรรม แล้ว DP จะเลือก
                //    ทางที่คะแนนดีกว่าเอง (ปกติทางที่มี dictionary match จริงจะชนะเสมอ)
                //    ⚠️ ไม่บล็อกการเริ่ม token ใหม่กลางกลุ่มเหมือนตัวเลข เพราะข้อความไทย/อังกฤษต้องแยกคำได้
                var currentClass = GetCharClass(normalizeKeyword[i]);

                if (currentClass == CharClass.Thai || currentClass == CharClass.Latin) {
                    int runEnd = i;
                    while (runEnd < n && GetCharClass(normalizeKeyword[runEnd]) == currentClass)
                        runEnd++;

                    if (runEnd > i + 1) // มีตัวอักษรกลุ่มเดียวกันติดกันมากกว่า 1 ตัว ถึงจะคุ้มที่จะเพิ่ม option นี้
                    {
                        var runToken = new TokenMatch {
                            Token = normalizeKeyword.Substring(i, runEnd - i),
                            StartIndex = i,
                            EndIndex = runEnd,
                            Priority = 0,
                            SearchType = null,
                            MeiliRankingScore = 0,
                            NormalizedValue = normalizeKeyword.Substring(i, runEnd - i),
                            MatchedEntries = new List<SearchDictionaryModel>(),
                            IsUnknown = true
                        };

                        double score = dp[i] + UnknownCharPenalty;
                        int endPos = runEnd;

                        if (score > dp[endPos]) {
                            dp[endPos] = score;
                            back[endPos] = runToken;
                            reachable[endPos] = true;
                        }
                    }
                }
            }

            return ReconstructPath(back, n);
        }

        /// <summary>
        /// จำแนกประเภทตัวอักษรอย่างหยาบ ๆ เพื่อใช้กำหนดขอบเขต "กลุ่มตัวอักษรประเภทเดียวกัน"
        /// สำหรับ fallback รวม unknown token (ดู block 3 และ 4 ใน Tokenize())
        /// </summary>
        private enum CharClass { Digit, Thai, Latin, Other }

        private static CharClass GetCharClass(char c) {
            if (char.IsDigit(c))
                return CharClass.Digit;

            if (c >= '\u0E00' && c <= '\u0E7F')
                return CharClass.Thai;

            if (char.IsLetter(c))
                return CharClass.Latin; // เหลือแค่ a-z เพราะผ่าน NormalizeService.Normalize มาแล้ว

            return CharClass.Other;
        }

        /// <summary>
        /// คำนวณคะแนนของ 1 token จาก priority, ความยาว, และ meili ranking score
        /// </summary>
        private double ScoreToken(TokenMatch token) {
            return (token.Priority * WeightPriority)
                 + (token.Length * WeightLength)
                 + (token.MeiliRankingScore * WeightMeiliScore);
        }

        /// <summary>
        /// ถ้าคำเดียวกันมีหลาย entry ใน dictionary (หลาย SearchType)
        /// เลือกตัวที่ priority สูงสุดมาใช้คำนวณ DP ก่อน
        /// (ตัวอื่น ๆ ยังอยู่ครบใน MeiliHits เผื่อ SearchParser ต้องใช้ตอน map SearchTypes)
        /// </summary>
        private SearchDictionaryModel PickBestEntry(List<SearchDictionaryModel> entries) {
            return entries.OrderByDescending(e => e.Priority)
                           .ThenByDescending(e => e._rankingScore)
                           .First();
        }

        /// <summary>
        /// เดินย้อนกลับจาก back[n] ไปจนถึง back[0] เพื่อ reconstruct path ที่ดีที่สุด
        /// แล้วกลับ list ให้เรียงจากซ้ายไปขวาตามคำเดิม
        /// </summary>
        private List<TokenMatch> ReconstructPath(TokenMatch[] back, int n) {
            var path = new List<TokenMatch>();
            int pos = n;

            while (pos > 0) {
                var token = back[pos];
                if (token == null) {
                    // ไม่ควรเกิดขึ้นเพราะ unknown-token fallback การันตีว่า reachable เสมอ
                    // แต่กันไว้เผื่อ edge case กันโปรแกรม crash
                    break;
                }

                path.Add(token);
                pos = token.StartIndex;
            }

            path.Reverse();
            return path;
        }
    }
}