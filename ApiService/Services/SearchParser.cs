using System;
using System.Collections.Generic;
using System.Linq;
using ApiService.Models;

namespace ApiService.Services
{
    /// <summary>
    /// รับ path ของ token ที่ดีที่สุดจาก AutomotiveTokenizer/RankingService แล้วแปลงเป็น
    /// SearchTypes (distinct) + SearchSqlRequest ที่พร้อมส่งเข้า SP P_Search_Product_Global
    ///
    /// สำคัญ: ใช้ MatchedEntries "ทั้งหมด" ของทุก token (ไม่ใช่แค่ตัวแทน Priority สูงสุด 1 ตัวต่อ token)
    /// เพราะ 1 token อาจ match ได้หลาย SearchType/SourceTable พร้อมกัน (เช่น "civic" ทั้ง model/VIO_Model
    /// และ synonym/custom) — ถ้าใช้แค่ตัวแทนจะทำให้ผลลัพธ์จากบาง table หายไปจาก SP ทั้งหมด
    /// </summary>
    public class SearchParser
    {
        public SearchSqlRequest Parse(string normalizeKeyword, List<TokenMatch> tokens) {
            var request = new SearchSqlRequest {
                NormalizeKeyword = normalizeKeyword
            };

            if (tokens == null || tokens.Count == 0)
                return request;

            // เอาเฉพาะ token ที่รู้จัก (มี dictionary entry จริง ๆ) ไม่เอา unknown token
            // (single char ที่ fallback มา) มาปนตอนสร้าง SearchTypes/entries เพราะไม่มีความหมาย
            var knownTokens = tokens.Where(t => !t.IsUnknown && t.MatchedEntries != null && t.MatchedEntries.Count > 0).ToList();
            var unknownTokens = tokens.Where(t => t.IsUnknown).ToList();

            // Flatten entry ทั้งหมดจากทุก token เข้าด้วยกัน (union ข้าม token) แล้ว dedupe ด้วย Id
            var allEntries = knownTokens
                .SelectMany(t => t.MatchedEntries)
                .GroupBy(e => e.Id)
                .Select(g => g.First())
                .ToList();

            // SearchTypes: distinct ทุก SearchType ที่เจอจริง เรียงตาม Priority สูงสุดของ type นั้นก่อน
            // (ไม่ตัดตัวไหนทิ้งแล้ว — "model" กับ "synonym" จะอยู่ด้วยกันได้ถ้า token เดียวกัน match ทั้งคู่)
            request.SearchTypes = allEntries
                .GroupBy(e => e.SearchType, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Max(e => e.Priority))
                .Select(g => g.Key)
                .ToList();

            // SourceTables: distinct ตารางต้นทางทั้งหมดที่เกี่ยวข้อง เผื่อ SP อยากรู้ว่าต้อง join ตารางไหนเพิ่ม
            // เช่นเจอ SourceTable "VIO_Model" ก็รู้ว่าต้อง join ตาราง model เข้ามาด้วย ไม่ใช่ query แค่ custom alias
            request.SourceTables = allEntries
                .Where(e => !string.IsNullOrEmpty(e.SourceTable))
                .Select(e => e.SourceTable)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // SourceIdsByTable: จับคู่ SourceTable -> SourceId ที่มีค่าจริง (ไม่ null/ว่าง) เท่านั้น
            // ใช้ให้ SP ทำ WHERE/JOIN แบบ exact match (WHERE Id IN (...)) แทนการเดาด้วย LIKE ข้อความ
            // เช่น token "civic" match ได้ record SourceTable=VIO_Model, SourceId="205" -> SP ใช้
            // "WHERE VIO_Model.Id IN (205)" ได้ตรง ๆ แม่นยำกว่าและเร็วกว่า LIKE '%Civic%' มาก
            request.SourceIdsByTable = allEntries
                .Where(e => !string.IsNullOrEmpty(e.SourceTable) && !string.IsNullOrEmpty(e.SourceId))
                .GroupBy(e => e.SourceTable, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.SourceId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    StringComparer.OrdinalIgnoreCase
                );

            // SourceIdsBySearchType: จับคู่ SearchType -> SourceId ใช้สำหรับ routing ไปยัง endpoint ที่ถูกต้อง
            // (GetProductBySearchVio ต้องการ makerId/rangeId, GetProductBySearchCatagory ต้องการ
            // productLineId/productGroupId ฯลฯ) ดู SearchRouterService
            request.SourceIdsBySearchType = allEntries
                .Where(e => !string.IsNullOrEmpty(e.SearchType) && !string.IsNullOrEmpty(e.SourceId))
                .GroupBy(e => e.SearchType, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.SourceId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    StringComparer.OrdinalIgnoreCase
                );

            // TokensBySearchType: จับคู่ SearchType -> token ดิบ ใช้กับกลุ่มที่ไม่มี SourceId (description/oe/competitor)
            // ต้องรวม token จาก "ทุก entry" ที่มี SearchType นั้น (ไม่ใช่แค่ entry ที่มี SourceId) เพราะ
            // การค้นแบบ full-text ไม่จำเป็นต้องมี SourceId เลย
            request.TokensBySearchType = knownTokens
                .SelectMany(t => (t.MatchedEntries ?? new List<SearchDictionaryModel>())
                    .Where(e => !string.IsNullOrEmpty(e.SearchType))
                    .Select(e => new { e.SearchType, t.Token }))
                .GroupBy(x => x.SearchType, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Token).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    StringComparer.OrdinalIgnoreCase
                );

            // Tokens ที่ส่งเข้า SP: รวมทั้ง Normalize (ค่ามาตรฐาน เช่น "CIVIC") และ Keyword ดิบ (เช่น "Civic")
            // ของทุก entry ที่ match ได้ทั้งหมด เพื่อให้ WHERE เทียบได้ทั้ง 2 column ไม่ใช่แค่ Normalize อย่างเดียว
            // (ตามที่ต้องการ: "civic" ต้อง where ทั้ง col Keyword ด้วยเพื่อดึง VIO_Model มาด้วย)
            var normalizedTokens = allEntries
                .SelectMany(e => new[] { e.Normalize, e.Keyword })
                .Where(v => !string.IsNullOrWhiteSpace(v));

            var unknownLiterals = unknownTokens.Select(t => t.Token);

            request.Tokens = normalizedTokens
                .Concat(unknownLiterals)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // TokenBySearchType: ต่อ 1 SearchType เก็บค่า Normalize ของ entry ที่ Priority สูงสุดของ type นั้น
            // (ใช้เป็นค่าตัวแทนเดี่ยว ๆ เผื่อ SP อยากรู้คำตัวแทนของแต่ละ type แบบเร็ว ๆ)
            request.TokenBySearchType = allEntries
                .GroupBy(e => e.SearchType, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(e => e.Priority).First().Normalize,
                    StringComparer.OrdinalIgnoreCase
                );

            return request;
        }
    }
}