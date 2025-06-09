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

namespace ApiService.Controllers
{

    public class ChatBotController : ApiController
    {
        private readonly ApiServerController _apiServerService;
        public ChatBotController()
        {
            // สร้าง instance ของ IApiServerService แบบไหนก็ได้ หรือไม่ต้องสร้างก็ได้
            _apiServerService = new ApiServerController();
        }

        // GET: ChatBot
        [HttpGet]
        [Route("orders/search")]
        public HttpResponseMessage GetSearchBO(string customer_code = "", string part_no = "", string order_number = "")
        {
            var bo = new List<NVBackOrder>();
            var jsonLog = JsonConvert.SerializeObject(new
            {
                cus_code = customer_code,
                part_no = part_no,
                order_number = order_number
            });
            if (customer_code == "")
            {
                var resFail = new ApiResponse<object>
                {
                    Status = "Bad Request",
                    Message = "Invalid or missing parameters in the request.",
                    Data = null
                };
                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchBO", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                return Request.CreateResponse(HttpStatusCode.BadRequest, resFail);
            }
            var connectionString = ConfigurationManager.ConnectionStrings["MobileOrder_ConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            try
            {
                SqlCommand cmd = new SqlCommand("P_Search_BackOrder_ChatBot", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@incuscod", customer_code);
                cmd.Parameters.AddWithValue("@inpart_no", part_no);
                cmd.Parameters.AddWithValue("@inOrd_num", order_number);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    bo.Add(new NVBackOrder()
                    {
                        order_number = reader["order_number"] != DBNull.Value ? reader["order_number"].ToString() : "",
                        recorded_date = (DateTime)(reader["recorded_date"] != DBNull.Value ? (DateTime?)reader["recorded_date"] : null),
                        customer_name = reader["customer_name"] != DBNull.Value ? reader["customer_name"].ToString() : "",
                        customer_code = reader["customer_code"] != DBNull.Value ? reader["customer_code"].ToString() : "",
                        part_no = reader["part_no"] != DBNull.Value ? reader["part_no"].ToString() : "",
                        product_name = reader["product_name"] != DBNull.Value ? reader["product_name"].ToString() : "",
                        bo_quantity = (int)(reader["bo_quantity"] != DBNull.Value ? (int?)reader["bo_quantity"] : null),
                        stock_available_for_sale = (int)(reader["stock_available_for_sale"] != DBNull.Value ? (int?)reader["stock_available_for_sale"] : null),
                        amount = (decimal)(reader["amount"] != DBNull.Value ? (decimal?)reader["amount"] : null)
                    });

                }
                reader.Close();
                cmd.Dispose();
                if (bo.Count == 0)
                {

                    var resFail = new ApiResponse<object>
                    {
                        Status = "Not Found",
                        Message = "The product was not found in the Back Order system.",
                        Data = null
                    };



                    string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchBO", jsonLog, "");
                    _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));

                    return Request.CreateResponse(HttpStatusCode.NotFound, resFail);
                }

                var resOk = new ApiResponse<List<NVBackOrder>>
                {
                    Status = "OK",
                    Message = "The request was successful and product information is returned.",
                    Data = bo
                };

                string lastres = _apiServerService.SaveApiResponse("Chatbot/SearchBO", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastres, JsonConvert.SerializeObject(resOk));
                return Request.CreateResponse(HttpStatusCode.OK, resOk);

            }
            catch (Exception ex)
            {
                var resFail = new ApiResponse<object>
                {
                    Status = "Internal Server Error",
                    Message = " Something went wrong on the server.",
                    Data = null
                };
                string lastres = _apiServerService.SaveApiResponse("Chatbot/SearchBO", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastres, JsonConvert.SerializeObject(resFail));
                return Request.CreateResponse(HttpStatusCode.InternalServerError, resFail);
            }
        }

        [HttpGet]
        [Route("price-stock")]
        public HttpResponseMessage GetPriceStk(string customer_code = "", string part_no = "", Boolean stock_flag = false)
        {
            var stk = new List<StkPrice>();
            var jsonLog = JsonConvert.SerializeObject(new
            {
                customer_code = customer_code,
                part_no = part_no,
                stock_flag = stock_flag
            });
            if (customer_code == "" || part_no == "")
            {
                var resFail = new ApiResponse<object>
                {
                    Status = "Bad Request",
                    Message = "Invalid or missing parameters in the request. ",
                    Data = null
                };
                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchPriceStock", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                return Request.CreateResponse(HttpStatusCode.BadRequest, resFail);
            }
            var connectionString = ConfigurationManager.ConnectionStrings["MobileOrder_ConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            try
            {
                SqlCommand cmd = new SqlCommand("P_Search_PriceStock_ChatBot", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@incuscod", customer_code);
                cmd.Parameters.AddWithValue("@inpart_no", part_no);
                cmd.Parameters.AddWithValue("@inStk_flag", stock_flag);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    stk.Add(new StkPrice
                    {
                        customer_code = reader["PEOPLE"] != DBNull.Value ? reader["PEOPLE"].ToString() : "",
                        part_no = reader["STKCOD"] != DBNull.Value ? reader["STKCOD"].ToString() : "",
                        product_name = reader["STKDES"] != DBNull.Value ? reader["STKDES"].ToString() : "",
                        company = reader["company"] != DBNull.Value ? reader["company"].ToString() : "",
                        structure_price = reader["SalePrice"] != DBNull.Value ? Convert.ToDecimal(reader["SalePrice"]) : 0,
                        special_price = reader["Special_Price"] != DBNull.Value ? Convert.ToDecimal(reader["Special_Price"]) : 0,
                        previous_price = reader["LastSalesPrice"] != DBNull.Value ? Convert.ToDecimal(reader["LastSalesPrice"]) : 0,
                        //previous_price = (decimal)(reader["LastSalesPrice"] != DBNull.Value ? (decimal?)reader["LastSalesPrice"] : 0),
                        //previous_price = 123,
                        stock_quantity = reader["TOTBAL"] != DBNull.Value ? Convert.ToInt32(reader["TOTBAL"]) : 0,

                        //available_stock = reader["AvailableSTK"] != DBNull.Value ? reader["AvailableSTK"].ToString() : "",
                        estimated_arrival_date = reader["Estimate_Date_Arrival"] != DBNull.Value ? Convert.ToDateTime(reader["Estimate_Date_Arrival"]) : DateTime.MinValue
                        //estimated_arrival_date = DateTime.Now,
                    });
                }
                if (stk.Count() == 0)
                {
                    var resFail = new ApiResponse<object>
                    {
                        Status = "Not Found",
                        Message = "No product found matching the provided OE No. or Part No.",
                        Data = null
                    };
                    string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchPriceStock", jsonLog, "");
                    _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                    return Request.CreateResponse(HttpStatusCode.NotFound, resFail);
                }
                else
                {
                    var resOk = new ApiResponse<List<StkPrice>>
                    {
                        Status = "OK",
                        Message = "The request was successful and product information is returned.",
                        Data = stk
                    };

                    string lastres = _apiServerService.SaveApiResponse("Chatbot/SearchPriceStock", jsonLog, "");
                    _apiServerService.UpdateApiRespone(lastres, JsonConvert.SerializeObject(resOk));
                    return Request.CreateResponse(HttpStatusCode.OK, resOk);
                }

            }
            catch (Exception ex)
            {
                var resFail = new ApiResponse<object>
                {
                    Status = "Internal Server Error",
                    Message = "Something went wrong on the server.",
                    Data = null
                };
                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchPriceStock", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                return Request.CreateResponse(HttpStatusCode.InternalServerError, resFail);
            }
        }



        [HttpGet]
        [Route("SearchCustomerMaster")]
        public HttpResponseMessage GetCustomer(string customer_code = "")
        {
            var cus = new List<Customer>();
            var jsonLog = JsonConvert.SerializeObject(new
            {
                customer_code = customer_code
            });

            if (customer_code == "")
            {
                var resFail = new ApiResponse<object>
                {
                    Status = "Bad Request",
                    Message = "Invalid or missing parameters in the request. ",
                    Data = null
                };
                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchCustomer", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                return Request.CreateResponse(HttpStatusCode.BadRequest, resFail);
            }
            var connectionString = ConfigurationManager.ConnectionStrings["MobileOrder_ConnectionString"].ConnectionString;
            string SQL = "select [CUSCOD],[CUSNAM] from [dbo].[CUSPROV] where CUSCOD = @cuscod";
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            try
            {
                SqlCommand cmd = new SqlCommand(SQL, conn);
                cmd.Parameters.AddWithValue("@cuscod", customer_code);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    cus.Add(new Customer()
                    {
                        customer_name = reader["CUSNAM"] != DBNull.Value ? reader["CUSNAM"].ToString() : "",
                        customer_code = reader["CUSCOD"] != DBNull.Value ? reader["CUSCOD"].ToString() : "",
                    });
                }
                reader.Close();
                cmd.Dispose();

                if (cus.Count == 0)
                {


                    var resFail = new ApiResponse<object>
                    {
                        Status = "Not Found",
                        Message = "The customer code provided does not match any customer records.",
                        Data = null
                    };
                    string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchCustomer", jsonLog, "");
                    _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                    return Request.CreateResponse(HttpStatusCode.NotFound, resFail);
                }
                else
                {
                    var resOk = new ApiResponse<List<Customer>>
                    {
                        Status = "OK",
                        Message = "The request was successful, and the customer name is returned.",
                        Data = cus
                    };

                    string lastres = _apiServerService.SaveApiResponse("Chatbot/SearchCustomer", jsonLog, "");
                    _apiServerService.UpdateApiRespone(lastres, JsonConvert.SerializeObject(resOk));
                    return Request.CreateResponse(HttpStatusCode.OK, resOk);
                }

                //return Request.CreateResponse(HttpStatusCode.OK, new ApiResponse<List<Customer>>
                //{
                //    Status = "success",
                //    Message = "Customer name retrieved successfully.",
                //    Data = cus
                //});
            }
            catch (Exception ex)
            {


                var resFail = new ApiResponse<object>
                {
                    Status = "Internal Server Error",
                    Message = "Something went wrong on the server. ",
                    Data = null
                };
                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchCustomer", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                return Request.CreateResponse(HttpStatusCode.InternalServerError, resFail);
            }
        }

    }
}