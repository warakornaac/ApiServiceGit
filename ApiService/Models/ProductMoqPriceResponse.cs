using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Models
{
    public class ProductMoqPriceResponse
    {
        public string stkcode { get; set; }
        public string moq { get; set; }
        public decimal price { get; set; }
        public string cuscode { get; set; }
        public string company { get; set; }
    }
}