using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using RouteAttribute = System.Web.Http.RouteAttribute;
using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using ApiService.Filters;

namespace ApiService.Controllers
{
    public class SaleOrderController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        public SaleOrderController()
        {
            _apiServerService = new ApiServerController();
        }
        [Route("SaleOrder/Add")]
        [ApiKeyAuthorize]
        public IHttpActionResult Add([FromBody] OrderData requestData)
        {
            string getEnv = _apiServerService.getEnv();
            string errorMessageTxt = "success";
            string warningTxt = string.Empty;
            string stkcodeError = string.Empty;
            string countRowWarning = string.Empty;
            int rowRequest = 0;
            int rowInsert = 0;
            int countRow = 0;
            List<string> stkcodeList = new List<string>();
            List<string> warningTxtList = new List<string>();
            try
            {
                if (requestData == null)
                {
                    return BadRequest("Invalid or empty json data.");
                }
                else
                {
                    rowRequest = requestData.data.Count();
                    foreach (var rowData in requestData.data)
                    {
                        ++countRow;
                        if (countRow > 0) {
                            countRowWarning = "Row(" + countRow + ") ";
                        }
                        if (!string.IsNullOrEmpty(rowData.Stkcode))
                        {
                            stkcodeError = rowData.Stkcode + " ";
                        }
                        if (string.IsNullOrEmpty(rowData.Company))
                        {
                            warningTxt = countRowWarning + stkcodeError + "Data Company Empty.";
                            warningTxtList.Add(warningTxt);
                            errorMessageTxt = "error";
                        }
                        else if (string.IsNullOrEmpty(rowData.Cuscode))
                        {
                            warningTxt = countRowWarning + stkcodeError + "Data Cuscode Empty.";
                            warningTxtList.Add(warningTxt);
                            errorMessageTxt = "error";
                        }
                        else if (string.IsNullOrEmpty(rowData.Stkcode))
                        {
                            warningTxt = countRowWarning + stkcodeError + "Data Stkcod Empty.";
                            warningTxtList.Add(warningTxt);
                            errorMessageTxt = "error";
                        }
                        else if (string.IsNullOrEmpty(rowData.SalesPrice))
                        {
                            warningTxt = countRowWarning + stkcodeError + "Data SalesPrice Empty.";
                            warningTxtList.Add(warningTxt);
                            errorMessageTxt = "error";
                        }
                        else if (rowData.Quantity == 0)
                        {
                            warningTxt = countRowWarning + stkcodeError + "Data Quantity Empty.";
                            warningTxtList.Add(warningTxt);
                            errorMessageTxt = "error";
                        }
                        else if (string.IsNullOrEmpty(rowData.InsertBy))
                        {
                            warningTxt = countRowWarning + stkcodeError + "Data InsertBy Empty.";
                            warningTxtList.Add(warningTxt);
                            errorMessageTxt = "error";
                        }
                        else if (rowData.IsBargain.ToUpper() != "TRUE" && rowData.IsBargain.ToUpper() != "FALSE")
                        {
                            warningTxt = countRowWarning + stkcodeError + "Data IsBargain Invalid.";
                            warningTxtList.Add(warningTxt);
                            errorMessageTxt = "error";
                        }
                        else
                        {
                            //save data
                            var statusApi = saveDataOrder(rowData.Company, rowData.Cuscode, rowData.Stkcode, rowData.SalesPrice, rowData.Quantity, rowData.InsertBy, rowData.IsBargain.ToUpper());
                            if (statusApi.ToString() == "Y")
                            {
                                ++rowInsert;
                                stkcodeList.Add(rowData.Stkcode);
                            }
                            else
                            {
                                if (getEnv == "dev")
                                {
                                    warningTxt = statusApi.ToString();
                                }
                                warningTxtList.Add(warningTxt);
                                errorMessageTxt = "error";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //statusReceive = false;
                errorMessageTxt = ex.Message.ToString();
            }

            DataRespond dataRes = new DataRespond();
            dataRes.rowRequest = rowRequest;
            dataRes.rowInsert = rowInsert;
            dataRes.stkcodeList = string.Join(", ", stkcodeList);
            dataRes.status = errorMessageTxt;
            dataRes.statusMessage = string.Join(", ", warningTxtList);

            String requestDataLog = JsonConvert.SerializeObject(requestData);
            string jsonReturn = JsonConvert.SerializeObject(dataRes);

            String lastId = _apiServerService.SaveApiResponse("SaleOrder/Add", requestDataLog.ToString(), "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(dataRes);
        }
        //save data
        public object saveDataOrder(string Company, string Cuscode, string Stkcode, string SalesPrice, int Quantity, string InsertBy, string IsBargain)
        {
            var storedResult = string.Empty;
            var flagResult = string.Empty;
            var txtResult = string.Empty;
            var txtRespond = "Y";
            var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(connectionString);
            try
            {
                conn.Open();
                var cmd = new SqlCommand("P_Save_Ai_Order_Cart", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Company", Company);
                cmd.Parameters.AddWithValue("@Cuscode", Cuscode);
                cmd.Parameters.AddWithValue("@Stkcode", Stkcode);
                cmd.Parameters.AddWithValue("@SalesPrice", SalesPrice);
                cmd.Parameters.AddWithValue("@Quantity", Quantity);
                cmd.Parameters.AddWithValue("@IsBargain", IsBargain);
                cmd.Parameters.AddWithValue("@InsertBy", InsertBy);

                SqlParameter p = new SqlParameter("@OutGenstatus", SqlDbType.NVarChar, 100);
                p.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(p);
                cmd.ExecuteNonQuery();
                storedResult = cmd.Parameters["@OutGenstatus"].Value.ToString();
                if (!string.IsNullOrEmpty(storedResult))
                {
                    if (storedResult != "Y") {
                        txtRespond = storedResult;
                    }
                }
                cmd.Dispose();
            }
            catch (Exception ex)
            {
                txtRespond = ex.ToString();
            }
            conn.Close();
            return txtRespond;
        }
        public class OrderData
        {
            public List<DataList> data { get; set; }
        }
        public class DataList
        {
            public string Company { get; set; }
            public string Cuscode { get; set; }
            public string Stkcode { get; set; }
            public string SalesPrice { get; set; }
            public int Quantity { get; set; }
            public string IsBargain { get; set; }
            public string InsertBy { get; set; }
        }
        public class DataRespond
        {
            public int rowRequest { get; set; }
            public int rowInsert { get; set; }
            public string stkcodeList { get; set; }
            public string status { get; set; } 
            public string statusMessage { get; set; }
        }
    }
}
