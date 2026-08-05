using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Models
{
    public class ProductSearchVioDataResponse
    {

        public string stkcode { get; set; }
        public string stkcodeDescription { get; set; }
        public string brand { get; set; }
        public string makerName { get; set; }
        public string modelName { get; set; }
        public string qtyReady { get; set; }
        public string price { get; set; }
        public string productGroup { get; set; }
        public string productLine { get; set; }
        public string imagePath { get; set; }
        public string fittingDescription { get; set; }
    }
}