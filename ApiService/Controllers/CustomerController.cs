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
    public class CustomerController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        public CustomerController()
        {
            _apiServerService = new ApiServerController();
        }
        [Route("Customer/CustomerByUsername")]
        [ApiKeyAuthorize]
        public IHttpActionResult CustomerByUsername([FromBody] RequestData requestData)
        {
            string getEnv = _apiServerService.getEnv();
            string errorMessageTxt = "success";
            string storedResult = string.Empty;
            string fullnameSales = string.Empty;
            string departmentSales = string.Empty;
            string salesmanCode = string.Empty;
            string warningTxt = string.Empty;
            List<ListCustomer> customerList = new List<ListCustomer>();
            var okresp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                ReasonPhrase = "Success"
            };
            try
            {
                if (string.IsNullOrEmpty(requestData.username))
                {
                    return BadRequest("Invalid or username empty data.");
                }
                else
                {
                    var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;
                    SqlConnection Connection = new SqlConnection(connectionString);
                    Connection.Open();
                    var command = new SqlCommand("P_Get_Customer_By_Username", Connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@inUsername", requestData.username);

                    SqlParameter p = new SqlParameter("@OutGenstatus", SqlDbType.NVarChar, 100);
                    SqlParameter p2 = new SqlParameter("@OutFullnameSales", SqlDbType.NVarChar, 100);
                    SqlParameter p3 = new SqlParameter("@OutDepartmentSales", SqlDbType.NVarChar, 100);
                    SqlParameter p4 = new SqlParameter("@OutSalesmancode", SqlDbType.NVarChar, 100);
                    p.Direction = ParameterDirection.Output;
                    p2.Direction = ParameterDirection.Output;
                    p3.Direction = ParameterDirection.Output;
                    p4.Direction = ParameterDirection.Output;
                    command.Parameters.Add(p);
                    command.Parameters.Add(p2);
                    command.Parameters.Add(p3);
                    command.Parameters.Add(p4);
                    command.ExecuteNonQuery();
                    storedResult = command.Parameters["@OutGenstatus"].Value.ToString();
                    fullnameSales = command.Parameters["@OutFullnameSales"].Value.ToString();
                    departmentSales = command.Parameters["@OutDepartmentSales"].Value.ToString();
                    salesmanCode = command.Parameters["@OutSalesmancode"].Value.ToString();
                    SqlDataReader rowRes = command.ExecuteReader();
                    while (rowRes.Read())
                    {
                        customerList.Add(new ListCustomer()
                        {
                            customerCode = rowRes["CUSCOD"].ToString(),
                            customerName = rowRes["CUSNAM"].ToString()
                        });
                    }
                    //ถ้าใน stored error ให้แสดงข้อความ error ด้วย
                    if (storedResult != "Y") {
                        errorMessageTxt = storedResult.ToString();
                    }
                    rowRes.Dispose();
                    command.Dispose();
                    Connection.Dispose();
                    Connection.Close();
                }
            }
            catch (Exception ex)
            {
                errorMessageTxt = ex.Message.ToString();
            }

            DataRespond dataRes = new DataRespond();
            dataRes.statusCode = Convert.ToInt32(okresp.StatusCode);
            dataRes.errorMessage = errorMessageTxt;
            dataRes.salesmanCode = salesmanCode;
            dataRes.fullnameSales = fullnameSales;
            dataRes.departmentSales = departmentSales;
            dataRes.result = customerList;

            String requestDataLog = JsonConvert.SerializeObject(requestData);
            string jsonReturn = JsonConvert.SerializeObject(dataRes);

            String lastId = _apiServerService.SaveApiResponse("Customer/CustomerBySalesman", requestDataLog.ToString(), "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(dataRes);
        }

        public class RequestData
        {
            public string username { get; set; }
        }
        public class ListCustomer
        {
            public string customerCode { get; set; }
            public string customerName { get; set; }
        }
        public class DataRespond
        {
            public int statusCode { get; set; }
            public string errorMessage { get; set; }
            public string salesmanCode { get; set; }
            public string fullnameSales { get; set; }
            public string departmentSales { get; set; }
            public List<ListCustomer> result { get; set; }
        }
    }
}