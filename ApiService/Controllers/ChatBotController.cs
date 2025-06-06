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
        public HttpResponseMessage GetSearchBO(string cus_code, string part_no, string order_number)
        {
            var bo = new List<NVBackOrder>();
            var connectionString = ConfigurationManager.ConnectionStrings["SaleAI_ConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            try
            {
                SqlCommand cmd = new SqlCommand("P_search_BackOrder", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@incuscod", cus_code);
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

                    var res = new ApiResponse<object>
                    {
                        Status = "Error",
                        Message = "Not found",
                        Data = null
                    };
                    var jsonLog = JsonConvert.SerializeObject(new
                    {
                        cus_code = cus_code,
                        part_no = part_no,
                        order_number = order_number,
                    });
                    //string lastres = _apiServerService.SaveApiResponse("Chatbot/GET", jsonLog.ToString(), "");
                    //_apiServerService.UpdateApiRespone(lastres, res.ToString());
                    return Request.CreateResponse(HttpStatusCode.NotFound, new ApiResponse<object>
                    {
                        Status = "Error",
                        Message = "Not found",
                        Data = null
                    });
                }

                return Request.CreateResponse(HttpStatusCode.OK, new ApiResponse<List<NVBackOrder>>
                {
                    Status = "success",
                    Message = "BackOrder retrieved successfully.",
                    Data = bo
                });

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ApiResponse<object>
                {
                    Status = "Error",
                    Message = ex.Message,
                    Data = null
                });
            }
        }
        [HttpGet]
        [Route("SearchCustomerMaster")]
        public HttpResponseMessage GetCustomer(string customer_code)
        {
            var cus = new List<Customer>();
            var connectionString = ConfigurationManager.ConnectionStrings["SaleAI_ConnectionString"].ConnectionString;
            string SQL = "select [CUSCOD],[CUSNAM] from [SALESAI].[dbo].[CUSPROV] where CUSCOD = @cuscod";
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
                    return Request.CreateResponse(HttpStatusCode.NotFound, new ApiResponse<object>
                    {
                        Status = "Error",
                        Message = "Not found",
                        Data = null
                    });
                }

                return Request.CreateResponse(HttpStatusCode.OK, new ApiResponse<List<Customer>>
                {
                    Status = "success",
                    Message = "Customer name retrieved successfully.",
                    Data = cus
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ApiResponse<object>
                {
                    Status = "Error",
                    Message = ex.Message,
                    Data = null
                });
            }
        }




    }
}