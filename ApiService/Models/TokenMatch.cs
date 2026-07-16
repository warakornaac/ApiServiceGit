using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Models
{
    public class TokenMatch
    {
        public string Token { get; set; }

        public int StartIndex { get; set; }

        public int Length { get; set; }

        public int Priority { get; set; }

        public double RankingScore { get; set; }

        public string SearchType { get; set; }

        public double Score { get; set; }
    }
}