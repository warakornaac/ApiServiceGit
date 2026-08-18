using ApiService.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Web.Configuration;
using ApiService.Filters;
using ApiService.Services;

namespace ApiService.Controllers
{
    public class StockJobController : ApiController
    {
        [HttpPost]
        [Route("StockJob/stock")]
        [ApiKeyAuthorize]
        public IHttpActionResult GetStockQty(string itemNo) {
            if (string.IsNullOrWhiteSpace(itemNo))
                return BadRequest("item_no is required");

            var item = StockCacheService.Instance.GetByItemNo(itemNo);

            if (item == null)
                return NotFound();

            return Ok(new {
                company = item.Company,
                item_no = item.ItemNo,
                qty = item.ReadyQty,
                as_of = StockCacheService.Instance.LastUpdated
            });
        }
    }
}