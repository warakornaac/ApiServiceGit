using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Models
{
    public class StockItem
    {
        public string Company { get; set; }
        public string ItemNo { get; set; }
        public decimal ReadyQty { get; set; }
    }
}