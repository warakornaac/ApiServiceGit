using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Services
{
    internal class TokenNode
    {
        public double Score { get; set; }

        public List<string> Tokens { get; set; }
    }
}