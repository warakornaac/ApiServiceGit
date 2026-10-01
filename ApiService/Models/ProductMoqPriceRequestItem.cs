using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Models
{
    public class ProductMoqPriceRequestItem
    {
        public string stkcode { get; set; }
        public string company { get; set; }
    }
    public class ProductMoqPriceRequest
    {
        public List<ProductMoqPriceRequestItem> items { get; set; }
        public string CusCode { get; set; }
        public string Company { get; set; }
    }
}