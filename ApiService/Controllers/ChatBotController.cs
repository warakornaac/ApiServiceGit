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
        //API 1.2
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
            if (string.IsNullOrWhiteSpace(customer_code))
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


        //API 2
        [HttpGet]
        [Route("price-stock")]
        public HttpResponseMessage GetPriceStk(string customer_code = "", string part_no = "", Boolean? stock_flag = false)
        {
            var stk = new List<object>();
            var jsonLog = JsonConvert.SerializeObject(new
            {
                customer_code = customer_code,
                part_no = part_no,
                stock_flag = stock_flag
            });
            if (string.IsNullOrEmpty(customer_code) || string.IsNullOrEmpty(part_no))
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
                    if (stock_flag == true)
                    {
                        var item = new StkPrice
                        {
                            customer_code = reader["PEOPLE"] != DBNull.Value ? reader["PEOPLE"].ToString() : "",
                            part_no = reader["STKCOD"] != DBNull.Value ? reader["STKCOD"].ToString() : "",
                            product_name = reader["STKDES"] != DBNull.Value ? reader["STKDES"].ToString() : "",
                            company = reader["company"] != DBNull.Value ? reader["company"].ToString() : "",
                            structure_price = reader["SalePrice"] != DBNull.Value ? Convert.ToDecimal(reader["SalePrice"]) : 0,
                            special_price = reader["Special_Price"] != DBNull.Value ? Convert.ToDecimal(reader["Special_Price"]) : 0,
                            previous_price = reader["LastSalesPrice"] != DBNull.Value ? Convert.ToDecimal(reader["LastSalesPrice"]) : 0,
                            stock_quantity = reader["TOTBAL"] != DBNull.Value ? Convert.ToInt32(reader["TOTBAL"]) : 0,
                            moq = reader["MOQ"] != DBNull.Value ? Convert.ToInt32(reader["MOQ"]) : 0,
                            sales_packing_standard = reader["sales_packing_standard"] != DBNull.Value ? Convert.ToInt32(reader["sales_packing_standard"]) : 0,
                            is_eop = reader["is_eop"] != DBNull.Value ? (Convert.ToInt32(reader["is_eop"]) == 1 ? true : false) : false,
                            subtitute_product = reader["subtitute_product"] != DBNull.Value ? reader["subtitute_product"].ToString() : "",
                            //estimated_arrival_date = reader["Estimate_Date_Arrival"] != DBNull.Value ? Convert.ToDateTime(reader["Estimate_Date_Arrival"]) : DateTime.MinValue
                            estimated_arrival_date = reader["Estimate_Date_Arrival"] != DBNull.Value
                                ? (DateTime?)Convert.ToDateTime(reader["Estimate_Date_Arrival"])
                                : null
                        };
                        stk.Add(item);
                    }
                    else
                    {
                        var item = new StkPrice_false
                        {
                            customer_code = reader["PEOPLE"] != DBNull.Value ? reader["PEOPLE"].ToString() : "",
                            part_no = reader["STKCOD"] != DBNull.Value ? reader["STKCOD"].ToString() : "",
                            product_name = reader["STKDES"] != DBNull.Value ? reader["STKDES"].ToString() : "",
                            company = reader["company"] != DBNull.Value ? reader["company"].ToString() : "",
                            structure_price = reader["SalePrice"] != DBNull.Value ? Convert.ToDecimal(reader["SalePrice"]) : 0,
                            special_price = reader["Special_Price"] != DBNull.Value ? Convert.ToDecimal(reader["Special_Price"]) : 0,
                            previous_price = reader["LastSalesPrice"] != DBNull.Value ? Convert.ToDecimal(reader["LastSalesPrice"]) : 0,
                            moq = reader["MOQ"] != DBNull.Value ? Convert.ToInt32(reader["MOQ"]) : 0,
                            sales_packing_standard = reader["sales_packing_standard"] != DBNull.Value ? Convert.ToInt32(reader["sales_packing_standard"]) : 0,
                            is_eop = reader["is_eop"] != DBNull.Value ? (Convert.ToInt32(reader["is_eop"]) == 1 ? true : false) : false,
                            subtitute_product = reader["subtitute_product"] != DBNull.Value ? reader["subtitute_product"].ToString() : "",
                            estimated_arrival_date = reader["Estimate_Date_Arrival"] != DBNull.Value
                                ? (DateTime?)Convert.ToDateTime(reader["Estimate_Date_Arrival"])
                                : null
                        };
                        stk.Add(item);
                    }

                }
                if (stk.Count() == 0)
                {
                    var resFail = new ApiResponse<object>
                    {
                        Status = "Not Found",
                        Message = "No product found matching the provided Part No.",
                        Data = null
                    };
                    string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchPriceStock", jsonLog, "");
                    _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                    return Request.CreateResponse(HttpStatusCode.NotFound, resFail);
                }
                else
                {
                    var resOk = new ApiResponse<List<object>>
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

        ////API 4
        [HttpGet]
        [Route("delivery-status/search")]
        public HttpResponseMessage GetDeliveryStatus(string customer_code = "", string purchase_date = "", string part_no = "", string order_status = "", string order_number = "")
        {
            var header = new List<StkDeliveryHead<List<product_detail>>>();
            var _Details = new List<product_detail>();
            var jsonLog = JsonConvert.SerializeObject(new
            {
                customer_code = customer_code,
                purchase_date = purchase_date,
                part_no = part_no,
                order_status = order_status,
                order_number = order_number
            });
            if (string.IsNullOrEmpty(customer_code))
            {
                var resFail = new ApiResponse<object>
                {
                    Status = "Bad Request",
                    Message = "Invalid or missing parameters in the request. ",
                    Data = null
                };
                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchDeliveryStatus", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                return Request.CreateResponse(HttpStatusCode.BadRequest, resFail);
            }
            else if (string.IsNullOrEmpty(purchase_date) && string.IsNullOrEmpty(part_no))
            {
                var resFail = new ApiResponse<object>
                {
                    Status = "Bad Request",
                    Message = "Invalid or missing parameters in the request. ",
                    Data = null
                };
                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchDeliveryStatus", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                return Request.CreateResponse(HttpStatusCode.BadRequest, resFail);
            }

            var connectionString = ConfigurationManager.ConnectionStrings["MobileOrder_ConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            try
            {
                SqlCommand cmd = new SqlCommand("p_Order_Status_API", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@inCuscod", customer_code);
                cmd.Parameters.AddWithValue("@inOrdDat", purchase_date);
                cmd.Parameters.AddWithValue("@inSTKCOD", part_no);
                cmd.Parameters.AddWithValue("@instatus", order_status);
                cmd.Parameters.AddWithValue("@inOrdNum", order_number);

                SqlDataReader read = cmd.ExecuteReader();
                while (read.Read())
                {
                    string currentOrder = read["Order_number"] != DBNull.Value ? read["Order_number"].ToString() : "";

                    _Details.Add(new product_detail
                    {
                        order_number = currentOrder,
                        part_no = read["Part_no"] != DBNull.Value ? read["Part_no"].ToString() : "",
                        product_name = read["Name"] != DBNull.Value ? read["Name"].ToString() : "",
                        order_quantity = read["quantity"] != DBNull.Value ? Convert.ToInt32(read["quantity"]) : 0,
                        total = read["total"] != DBNull.Value ? Convert.ToDecimal(read["total"]) : 0
                    });

                    if (!header.Any(h => h.order_number == currentOrder))
                    {
                        header.Add(new StkDeliveryHead<List<product_detail>>
                        {
                            order_number = currentOrder,
                            purchase_date = read["Purchase_date"] != DBNull.Value ? Convert.ToDateTime(read["Purchase_date"]) : DateTime.MinValue,
                            customer_name = read["Customer_Name"] != DBNull.Value ? read["Customer_Name"].ToString() : "",
                            customer_code = read["Customer_Code"] != DBNull.Value ? read["Customer_Code"].ToString() : "",
                            deivery_status = read["Delivery_Status"] != DBNull.Value ? read["Delivery_Status"].ToString() : "",
                            esimated_arrival_date = read["Estimate to Arrival"] != DBNull.Value
                                ? (DateTime?)Convert.ToDateTime(read["Estimate to Arrival"])
                                : null
                        });
                    }
                }
                foreach (var head in header)
                {
                    head.product = _Details
                        .Where(d => d.order_number == head.order_number)
                        .ToList();
                }

                if (header.Count == 0)
                {
                    var resFail = new ApiResponse<object>
                    {
                        Status = "Not Found",
                        Message = " No tracking information found for the provided order ID or delivery ID.",
                        Data = null
                    };
                    string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchDeliveryStatus", jsonLog, "");
                    _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                    return Request.CreateResponse(HttpStatusCode.NotFound, resFail);
                }
                else
                {
                    var resOk = new ApiResponse<List<StkDeliveryHead<List<product_detail>>>>
                    {
                        Status = "OK",
                        Message = "The request was successful and product information is returned.",
                        Data = header
                    };

                    string lastres = _apiServerService.SaveApiResponse("Chatbot/SearchDeliveryStatus", jsonLog, ""); // ✅ แก้ชื่อให้ตรง
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
                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchDeliveryStatus", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));
                return Request.CreateResponse(HttpStatusCode.InternalServerError, resFail);
            }
        }

        //API 5
        [HttpGet]
        [Route("SearchCustomerMaster")]
        public HttpResponseMessage GetCustomer(string customer_code = "")
        {
            var cus = new List<Customer>();
            var jsonLog = JsonConvert.SerializeObject(new
            {
                customer_code = customer_code
            });

            if (string.IsNullOrEmpty(customer_code))
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
            string SQL = "select [CUSCOD],[CUSNAM] from [dbo].[v_CUSPROV] where CUSCOD = @cuscod";
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