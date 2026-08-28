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
using ApiService.Models;
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
        [ApiKeyAuthorize]
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

                string logId = _apiServerService.SaveApiResponse("Chatbot/SearchBO", jsonLog, "");
                _apiServerService.UpdateApiRespone(logId, JsonConvert.SerializeObject(resFail));
                return Request.CreateResponse(HttpStatusCode.BadRequest, resFail);
            }

            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["MobileOrder_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("P_Search_BackOrder_ChatBot", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 30;
                        cmd.Parameters.Add("@incuscod", SqlDbType.VarChar, 50).Value = customer_code ?? "";
                        cmd.Parameters.Add("@inpart_no", SqlDbType.VarChar, 50).Value = part_no ?? "";
                        cmd.Parameters.Add("@inOrd_num", SqlDbType.VarChar, 50).Value = order_number ?? "";
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                bo.Add(new NVBackOrder()
                                {
                                    order_number = reader["order_number"] != DBNull.Value ? reader["order_number"].ToString() : "",
                                    recorded_date = reader["recorded_date"] != DBNull.Value ? Convert.ToDateTime(reader["recorded_date"]) : DateTime.MinValue,
                                    customer_name = reader["customer_name"] != DBNull.Value ? reader["customer_name"].ToString() : "",
                                    customer_code =  reader["customer_code"] != DBNull.Value? reader["customer_code"].ToString() : "",
                                    part_no = reader["part_no"] != DBNull.Value ? reader["part_no"].ToString() : "",
                                    product_name = reader["product_name"] != DBNull.Value ? reader["product_name"].ToString() : "",
                                    bo_quantity = reader["bo_quantity"] == DBNull.Value ? 0 : Convert.ToInt32(reader["bo_quantity"]),
                                    stock_available_for_sale = reader["stock_available_for_sale"] != DBNull.Value ? Convert.ToInt32(reader["stock_available_for_sale"]) : 0,
                                    amount = reader["amount"] != DBNull.Value ? Convert.ToDecimal(reader["amount"]) : 0
                                });
                            }
                        }
                    }
                }

                if (bo.Count == 0)
                {
                    var resFail = new ApiResponse<object>
                    {
                        Status = "Not Found",
                        Message = "The product was not found in the Back Order system.",
                        Data = null
                    };

                    string logId = _apiServerService.SaveApiResponse(
                        "Chatbot/SearchBO",
                        jsonLog,
                        ""
                    );

                    _apiServerService.UpdateApiRespone(logId, JsonConvert.SerializeObject(resFail));

                    return Request.CreateResponse(HttpStatusCode.NotFound, resFail);
                }

                var resOk = new ApiResponse<List<NVBackOrder>>
                {
                    Status = "OK",
                    Message = "The request was successful and product information is returned.",
                    Data = bo
                };

                string successLogId = _apiServerService.SaveApiResponse("Chatbot/SearchBO", jsonLog, "");

                _apiServerService.UpdateApiRespone(successLogId, JsonConvert.SerializeObject(resOk));

                return Request.CreateResponse(HttpStatusCode.OK, resOk);
            }
            catch (SqlException sqlEx)
            {
                var errorLog = JsonConvert.SerializeObject(new
                {
                    Type = "SQL ERROR",
                    Message = sqlEx.Message,
                    StackTrace = sqlEx.StackTrace,
                    customer_code = customer_code,
                    part_no = part_no,
                    order_number = order_number,
                    Time = DateTime.Now
                });

                string logId = _apiServerService.SaveApiResponse(
                    "Chatbot/SearchBO",
                    jsonLog,
                    errorLog
                );

                var resFail = new ApiResponse<object>
                {
                    Status = "Internal Server Error",
                    Message = "Database error.",
                    Data = null
                };

                _apiServerService.UpdateApiRespone(
                    logId,
                    JsonConvert.SerializeObject(resFail)
                );

                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    resFail
                );
            }
            catch (Exception ex)
            {
                var errorLog = JsonConvert.SerializeObject(new
                {
                    Type = "SYSTEM ERROR",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    customer_code = customer_code,
                    part_no = part_no,
                    order_number = order_number,
                    Time = DateTime.Now
                });

                string logId = _apiServerService.SaveApiResponse(
                    "Chatbot/SearchBO",
                    jsonLog,
                    errorLog
                );

                var resFail = new ApiResponse<object>
                {
                    Status = "Internal Server Error",
                    Message = "Something went wrong on the server.",
                    Data = null
                };

                _apiServerService.UpdateApiRespone(
                    logId,
                    JsonConvert.SerializeObject(resFail)
                );

                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    resFail
                );
            }
        }

        //API 2
        [HttpGet]
        [Route("price-stock")]
        [ApiKeyAuthorize]
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
                    Message = "Invalid or missing parameters in the request.",
                    Data = null
                };

                string logId = _apiServerService.SaveApiResponse("Chatbot/SearchPriceStock", jsonLog, "");

                _apiServerService.UpdateApiRespone(
                    logId,
                    JsonConvert.SerializeObject(resFail)
                );

                return Request.CreateResponse(HttpStatusCode.BadRequest, resFail);
            }

            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["MobileOrder_ConnectionString"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("P_Search_PriceStock_ChatBot", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 30;

                        cmd.Parameters.Add("@incuscod", SqlDbType.VarChar, 50).Value = customer_code;
                        cmd.Parameters.Add("@inpart_no", SqlDbType.VarChar, 50).Value = part_no;
                        cmd.Parameters.Add("@inStk_flag", SqlDbType.Bit).Value = stock_flag ?? false;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (stock_flag == true)
                                {
                                    var item = new StkPrice
                                    {
                                        customer_code = reader["PEOPLE"] != DBNull.Value? reader["PEOPLE"].ToString(): "",
                                        part_no = reader["STKCOD"] != DBNull.Value? reader["STKCOD"].ToString(): "",
                                        product_name =reader["STKDES"] != DBNull.Value? reader["STKDES"].ToString(): "",
                                        brand = reader["brand"] != DBNull.Value? reader["brand"].ToString(): "",
                                        company =reader["company"] != DBNull.Value? reader["company"].ToString(): "",
                                        structure_price = reader["SalePrice"] != DBNull.Value? Convert.ToDecimal(reader["SalePrice"]): 0,
                                        special_price = reader["Special_Price"] != DBNull.Value? Convert.ToDecimal(reader["Special_Price"]) : 0,
                                        previous_price = reader["LastSalesPrice"] != DBNull.Value? Convert.ToDecimal(reader["LastSalesPrice"]) : 0,
                                        stock_quantity = reader["TOTBAL"] != DBNull.Value? Convert.ToInt32(reader["TOTBAL"]): 0,
                                        estimated_arrival_date = reader["Estimate_Date_Arrival"] != DBNull.Value? (DateTime?)Convert.ToDateTime(reader["Estimate_Date_Arrival"]): null,
                                        moq = reader["MOQ"] != DBNull.Value ? Convert.ToInt32(reader["MOQ"]) : 0,
                                        sales_packing_standard = reader["sales_packing_standard"] != DBNull.Value ? Convert.ToInt32(reader["sales_packing_standard"]) : 0,
                                        is_eop = reader["is_eop"] != DBNull.Value ? Convert.ToInt32(reader["is_eop"]) == 1 : false,
                                        subtitute_product = reader["subtitute_product"] != DBNull.Value ? reader["subtitute_product"].ToString() : null
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
                                        brand = reader["brand"] != DBNull.Value ? reader["brand"].ToString() : "",
                                        company = reader["company"] != DBNull.Value ? reader["company"].ToString() : "",
                                        structure_price = reader["SalePrice"] != DBNull.Value ? Convert.ToDecimal(reader["SalePrice"]) : 0,
                                        special_price = reader["Special_Price"] != DBNull.Value ? Convert.ToDecimal(reader["Special_Price"]) : 0,
                                        previous_price = reader["LastSalesPrice"] != DBNull.Value ? Convert.ToDecimal(reader["LastSalesPrice"]) : 0,
                                        estimated_arrival_date = reader["Estimate_Date_Arrival"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["Estimate_Date_Arrival"]) : null,
                                        moq = reader["MOQ"] != DBNull.Value ? Convert.ToInt32(reader["MOQ"]) : 0,
                                        sales_packing_standard = reader["sales_packing_standard"] != DBNull.Value ? Convert.ToInt32(reader["sales_packing_standard"]) : 0,
                                        is_eop = reader["is_eop"] != DBNull.Value ? Convert.ToInt32(reader["is_eop"]) == 1 : false,
                                        subtitute_product = reader["subtitute_product"] != DBNull.Value ? reader["subtitute_product"].ToString() : null
                                    };

                                    stk.Add(item);
                                }
                            }
                        }
                    }
                }

                if (stk.Count == 0)
                {
                    var resFail = new ApiResponse<object>
                    {
                        Status = "Not Found",
                        Message = "No product found matching the provided Part No.",
                        Data = null
                    };

                    return Request.CreateResponse(HttpStatusCode.NotFound, resFail);
                }

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
            catch (SqlException sqlEx)
            {
                var errorLog = JsonConvert.SerializeObject(new
                {
                    Type = "SQL ERROR",
                    Message = sqlEx.Message,
                    StackTrace = sqlEx.StackTrace,
                    customer_code,
                    part_no,
                    stock_flag,
                    Time = DateTime.Now
                });

                var resFail = new ApiResponse<object>
                {
                    Status = "Internal Server Error",
                    Message = "Database error.",
                    Data = null
                };

                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchPriceStock", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));

                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    resFail
                );
            }
            catch (Exception ex)
            {
                var errorLog = JsonConvert.SerializeObject(new
                {
                    Type = "SYSTEM ERROR",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    customer_code,
                    part_no,
                    stock_flag,
                    Time = DateTime.Now
                });


                var resFail = new ApiResponse<object>
                {
                    Status = "Internal Server Error",
                    Message = "Something went wrong on the server.",
                    Data = null
                };

                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchPriceStock", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));

                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    resFail
                );
            }
        }

        ////API 4
        [HttpGet]
        [Route("delivery-status/search")]
        [ApiKeyAuthorize]
        public HttpResponseMessage GetDeliveryStatus(
            string customer_code = "",
            string purchase_date = "",
            string part_no = "",
            string order_status = "",
            string order_number = "")
        {
            var header = new List<StkDeliveryHead<List<product_detail>>>();
            var details = new List<product_detail>();

            var jsonLog = JsonConvert.SerializeObject(new
            {
                customer_code,
                purchase_date,
                part_no,
                order_status,
                order_number
            });

            if (string.IsNullOrEmpty(customer_code))
            {
                return Request.CreateResponse(
                    HttpStatusCode.BadRequest,
                    new ApiResponse<object>
                    {
                        Status = "Bad Request",
                        Message = "Invalid or missing parameters in the request.",
                        Data = null
                    });
            }

            if (string.IsNullOrEmpty(purchase_date) &&
                string.IsNullOrEmpty(part_no))
            {
                return Request.CreateResponse(
                    HttpStatusCode.BadRequest,
                    new ApiResponse<object>
                    {
                        Status = "Bad Request",
                        Message = "Invalid or missing parameters in the request.",
                        Data = null
                    });
            }

            try
            {
                var connectionString =
                    ConfigurationManager
                    .ConnectionStrings["MobileOrder_ConnectionString"]
                    .ConnectionString;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(
                        "p_Order_Status_API",
                        conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 30;

                        cmd.Parameters.Add("@inCuscod", SqlDbType.VarChar, 50).Value = customer_code;
                        cmd.Parameters.Add("@inOrdDat", SqlDbType.VarChar, 50).Value = purchase_date ?? "";
                        cmd.Parameters.Add("@inSTKCOD", SqlDbType.VarChar, 50).Value = part_no ?? "";
                        cmd.Parameters.Add("@instatus", SqlDbType.VarChar, 50).Value = order_status ?? "";
                        cmd.Parameters.Add("@inOrdNum", SqlDbType.VarChar, 50).Value = order_number ?? "";

                        using (SqlDataReader read = cmd.ExecuteReader())
                        {
                            var orderMap = new Dictionary<string,
                                StkDeliveryHead<List<product_detail>>>();

                            while (read.Read())
                            {
                                string currentOrder =
                                    read["Order_number"] != DBNull.Value
                                    ? read["Order_number"].ToString()
                                    : "";

                                var detail = new product_detail
                                {
                                    order_number = currentOrder,
                                    part_no = read["Part_no"] != DBNull.Value ? read["Part_no"].ToString() : "",
                                    product_name = read["Name"] != DBNull.Value ? read["Name"].ToString() : "",
                                    order_quantity = read["quantity"] != DBNull.Value ? Convert.ToInt32(read["quantity"]) : 0,
                                    total = read["total"] != DBNull.Value ? Convert.ToDecimal(read["total"]) : 0
                                };

                                details.Add(detail);

                                if (!orderMap.ContainsKey(currentOrder))
                                {
                                    var head =
                                        new StkDeliveryHead<List<product_detail>>
                                        {
                                            order_number = currentOrder,
                                            purchase_date = read["Purchase_date"] != DBNull.Value ? Convert.ToDateTime(read["Purchase_date"]) : DateTime.MinValue,
                                            customer_name = read["Customer_Name"] != DBNull.Value ? read["Customer_Name"].ToString() : "",
                                            customer_code = read["Customer_Code"] != DBNull.Value ? read["Customer_Code"].ToString() : "",
                                            deivery_status = read["Delivery_Status"] != DBNull.Value ? read["Delivery_Status"].ToString() : "",
                                            esimated_arrival_date = read["Estimate to Arrival"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(read["Estimate to Arrival"]) : null,
                                            product = new List<product_detail>()
                                        };

                                    orderMap.Add(currentOrder, head);
                                    header.Add(head);
                                }

                                orderMap[currentOrder]
                                    .product
                                    .Add(detail);
                            }
                        }
                    }
                }

                if (header.Count == 0)
                {
                    var resFail = new ApiResponse<object>
                    {
                        Status = "Not Found",
                        Message = "No tracking information found for the provided order ID or delivery ID.",
                        Data = null
                    };
                    string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchDeliveryStatus", jsonLog, "");
                    _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));

                    return Request.CreateResponse(
                        HttpStatusCode.NotFound,
                        resFail);
                }

                var resOk =
                    new ApiResponse<List<StkDeliveryHead<List<product_detail>>>>
                    {
                        Status = "OK",
                        Message = "The request was successful.",
                        Data = header
                    };

                return Request.CreateResponse(HttpStatusCode.OK, resOk);
            }
            catch (SqlException sqlEx)
            {
                var errorLog = JsonConvert.SerializeObject(new
                {
                    Type = "SQL ERROR",
                    Message = sqlEx.Message,
                    StackTrace = sqlEx.StackTrace,
                    customer_code,
                    purchase_date,
                    part_no,
                    order_status,
                    order_number,
                    Time = DateTime.Now
                });

                var resFail = new ApiResponse<object>
                {
                    Status = "Internal Server Error",
                    Message = "Database error.",
                    Data = null
                };
                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchDeliveryStatus", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));

                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    resFail);
            }
            catch (Exception ex)
            {
                var errorLog = JsonConvert.SerializeObject(new
                {
                    Type = "SYSTEM ERROR",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    customer_code,
                    purchase_date,
                    part_no,
                    order_status,
                    order_number,
                    Time = DateTime.Now
                });

                var resFail = new ApiResponse<object>
                {
                    Status = "Internal Server Error",
                    Message = "Something went wrong on the server.",
                    Data = null
                };

                string lastresFail = _apiServerService.SaveApiResponse("Chatbot/SearchDeliveryStatus", jsonLog, "");
                _apiServerService.UpdateApiRespone(lastresFail, JsonConvert.SerializeObject(resFail));

                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    resFail);
            }
        }

        //API 5
        [HttpGet]
        [Route("SearchCustomerMaster")]
        [ApiKeyAuthorize]
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
                    Message = "Invalid or missing parameters in the request.",
                    Data = null
                };

                string logId = _apiServerService.SaveApiResponse("Chatbot/SearchCustomer", jsonLog, "");
                _apiServerService.UpdateApiRespone(logId, JsonConvert.SerializeObject(resFail));
                return Request.CreateResponse(HttpStatusCode.BadRequest, resFail);
            }

            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("P_Get_CustomerName_By_Code", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@inCusCode", customer_code);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cus.Add(new Customer()
                                {
                                    customer_name = reader["CUSNAM"] != DBNull.Value ? reader["CUSNAM"].ToString() : "",
                                    customer_code = reader["CUSCOD"] != DBNull.Value ? reader["CUSCOD"].ToString() : "",
                                    club = reader["Club"] != DBNull.Value ? reader["Club"].ToString() : "",
                                });
                            }
                        }
                    }
                }

                if (cus.Count == 0)
                {
                    var resFail = new ApiResponse<object>
                    {
                        Status = "Not Found",
                        Message = "The customer code provided does not match any customer records.",
                        Data = null
                    };

                    string logId = _apiServerService.SaveApiResponse("Chatbot/SearchCustomer", jsonLog, "");
                    _apiServerService.UpdateApiRespone(logId, JsonConvert.SerializeObject(resFail));

                    return Request.CreateResponse(HttpStatusCode.NotFound, resFail);
                }

                var resOk = new ApiResponse<List<Customer>>
                {
                    Status = "OK",
                    Message = "The request was successful, and the customer name is returned.",
                    Data = cus
                };

                string successLogId = _apiServerService.SaveApiResponse("Chatbot/SearchCustomer", jsonLog, "");

                _apiServerService.UpdateApiRespone(successLogId, JsonConvert.SerializeObject(resOk));

                return Request.CreateResponse(HttpStatusCode.OK, resOk);
            }
            catch (SqlException sqlEx)
            {
                var errorLog = JsonConvert.SerializeObject(new
                {
                    Type = "SQL ERROR",
                    Message = sqlEx.Message,
                    StackTrace = sqlEx.StackTrace,
                    customer_code = customer_code,
                    Time = DateTime.Now
                });

                string logId = _apiServerService.SaveApiResponse("Chatbot/SearchCustomer", jsonLog, "");

                var resFail = new ApiResponse<object>
                {
                    Status = "Internal Server Error",
                    Message = "Database connection error.",
                    Data = null
                };
                _apiServerService.UpdateApiRespone(logId, JsonConvert.SerializeObject(errorLog));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, resFail);
            }
            catch (Exception ex)
            {
                var errorLog = JsonConvert.SerializeObject(new
                {
                    Type = "SYSTEM ERROR",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    customer_code = customer_code,
                    Time = DateTime.Now
                });

                string logId = _apiServerService.SaveApiResponse("Chatbot/SearchCustomer", jsonLog, errorLog);

                var resFail = new ApiResponse<object>
                {
                    Status = "Internal Server Error",
                    Message = "Something went wrong on the server.",
                    Data = null
                };

                _apiServerService.UpdateApiRespone(logId, JsonConvert.SerializeObject(resFail));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, resFail);
            }
        }

    }
}