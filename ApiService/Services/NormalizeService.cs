using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace ApiService.Services
{
    public class NormalizeService
    {
        public static string Normalize(string keyword) {
            if (string.IsNullOrWhiteSpace(keyword))
                return string.Empty;

            keyword = keyword.Trim();

            //---------------------------------------
            // Replace Multiple Space
            //---------------------------------------

            keyword = Regex.Replace(
                keyword,
                @"\s+",
                " ");

            //---------------------------------------
            // Lower Case English
            //---------------------------------------

            keyword = keyword.ToLowerInvariant();

            return keyword;
        }
    }
}