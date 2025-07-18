using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using RouteAttribute = System.Web.Http.RouteAttribute;
using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using ApiService.Filters;
using System.Net;


namespace ApiService.Controllers
{
    public class ItemsController : ApiController
    {
        private readonly ApiServerController _apiServerService;
        public ItemsController()
        {
            _apiServerService = new ApiServerController();
        }

        [HttpPost]
        [Route("api/items")]
        public IHttpActionResult PostItems([FromBody] List<ItemRequest> requests)
        {
            if (requests == null)
                return BadRequest("Invalid input");

            var responses = new List<ItemResponse>();
            var seenItemNos = new HashSet<string>();
            // int counter = 1;

            var connectionString = ConfigurationManager.ConnectionStrings["MobileOrder_ConnectionString"].ConnectionString;
            //string SQL = "select [CUSCOD],[CUSNAM] from [dbo].[v_CUSPROV] where CUSCOD = @cuscod";
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            try
            {
                foreach (var item in requests)
                {

                    SqlCommand cmd = new SqlCommand("P_Search_Item_Media", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@inSearch", item.key);

                    SqlDataReader read = cmd.ExecuteReader();
                    while (read.Read())
                    {
                        //responses.Add(new ItemResponse
                        //{
                        //    itemNo = read["STKCOD"] != DBNull.Value ? read["STKCOD"].ToString() : "",
                        //    description = read["STKDES"] != DBNull.Value ? read["STKDES"].ToString() : ""
                        //});

                        var itemNo = read["STKCOD"] != DBNull.Value ? read["STKCOD"].ToString() : "";
                        var description = read["STKDES"] != DBNull.Value ? read["STKDES"].ToString() : "";

                        if (!string.IsNullOrEmpty(itemNo) && seenItemNos.Add(itemNo))
                        {
                            responses.Add(new ItemResponse
                            {
                                itemNo = itemNo,
                                description = description
                            });
                        }
                    }
                    //responses.Add(new ItemResponse
                    //{
                    //    itemNo = counter.ToString("D3"),
                    //    description = $"Item {item.key} description"
                    //});
                    //counter++;

                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }


            return Ok(responses);
        }

        public class ItemRequest
        {
            public string key { get; set; }
        }
        public class ItemResponse
        {
            public string itemNo { get; set; }
            public string description { get; set; }
        }


    }
}