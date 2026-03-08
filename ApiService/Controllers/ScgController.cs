using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using RouteAttribute = System.Web.Http.RouteAttribute;
using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using ApiService.Filters;
using System.Globalization;

namespace ApiService.Controllers
{
    public class ScgController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        // ตัวอย่างการสร้าง constructor ที่ไม่มีพารามิเตอร์
        public ScgController()
        {
            // สร้าง instance ของ IApiServerService แบบไหนก็ได้ หรือไม่ต้องสร้างก็ได้
            _apiServerService = new ApiServerController();
        }
        [Route("SCG/ReceiveDataTracking")]
        [ApiKeyAuthorize]
        public IHttpActionResult ReceiveDataTracking([FromBody] ResponseData requestData)
        {
            string errorMessageTxt = "success";
            string purchaseOrderTxt = string.Empty;
            try
            {
                if (requestData == null)
                {
                    return BadRequest("Data is null.");
                }
                else
                {
                    var status_code1 = string.Empty;
                    foreach (var rowData in requestData.data)
                    {
                        foreach (var rowShipment in rowData.shipmentTracking)
                        {
                            if (string.IsNullOrEmpty(rowData.purchaseOrder)/* || rowShipment.status_code != 7*/)
                            {
                                purchaseOrderTxt = "Data purchaseOrder or status_code invaild.";
                                errorMessageTxt = "error";
                            }
                            else
                            {
                                //save data
                                var statusApi = saveDataTracking(rowData.purchaseOrder, rowShipment.status_code, rowShipment.status, rowShipment.time_stamp);
                                if (statusApi.ToString() == "Y")
                                {
                                    purchaseOrderTxt = rowData.purchaseOrder;
                                }
                                else 
                                {
                                    purchaseOrderTxt = statusApi.ToString();
                                    errorMessageTxt = "error";
                                }
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
            dataRes.purchaseOrder = purchaseOrderTxt;
            dataRes.status = errorMessageTxt;

            String requestDataLog = JsonConvert.SerializeObject(requestData);
            string jsonReturn = JsonConvert.SerializeObject(dataRes);

            String lastId = _apiServerService.SaveApiResponse("SCG/ReceiveStatusOrder", requestDataLog.ToString(), "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());

            return Json(dataRes);
        }
        public object saveDataTracking(string purchaseOrder, int statusCode, string statusName, string timeStamp)
        {
            var storedResult = string.Empty;
            var flagResult = string.Empty;
            var txtResult = string.Empty;
            var txtRespond = "Y";
            DateTime? timeStampConv = null;

            if (!string.IsNullOrWhiteSpace(timeStamp))
            {
                timeStampConv = DateTime.ParseExact(
                    timeStamp,
                    "dd/MM/yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture
                );
            }
            var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(connectionString);
            try
            {
                conn.Open();
                var cmd = new SqlCommand("P_Tracking_Scg", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@purchaseOrder", purchaseOrder);
                cmd.Parameters.AddWithValue("@statusCode", statusCode);
                cmd.Parameters.AddWithValue("@statusName", statusName);
                cmd.Parameters.AddWithValue("@statusTimeStamp", timeStampConv);

                SqlParameter p = new SqlParameter("@OutGenstatus", SqlDbType.NVarChar, 100);
                p.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(p);
                cmd.ExecuteNonQuery();
                storedResult = cmd.Parameters["@OutGenstatus"].Value.ToString();
                cmd.Dispose();
            }
            catch (Exception ex)
            {
                txtRespond = ex.ToString();
            }
            conn.Close();
            return txtRespond;
        }
        public class DataReceive
        {
            public string response { get; set; }
            public List<data> data { get; set; }
        }
        //array list result
        public class data
        {
            public string customerCode { get; set; }
            public string deliveryNumber { get; set; }
            public string purchaseOrder { get; set; }
            public List<shipmentTracking> shipmentTracking { get; set; }

        }
        public class shipmentTracking
        {
            public string status_code { get; set; }
            public string status { get; set; }
            public string time_stamp { get; set; }
            public string value { get; set; }
            public string remark { get; set; }

        }
        public class recShipmentTracking
        {
            public string purchaseOrder { get; set; }
            public List<listShipment> listShipment { get; set; }
        }
        public class listShipment
        {
            public string status_code { get; set; }
            public string time_stamp { get; set; }

        }
        //
        public class ShipmentTracking
        {
            public int status_code { get; set; }
            public string status { get; set; }
            public string time_stamp { get; set; }
            public string value { get; set; }
            public string remark { get; set; }
        }
        public class DataItem
        {
            public string customerCode { get; set; }
            public string deliveryNumber { get; set; }
            public string purchaseOrder { get; set; }
            public List<ShipmentTracking> shipmentTracking { get; set; }
        }
        public class ResponseData
        {
            public List<DataItem> data { get; set; }
        }
        //
        public class DataRespond
        {
            public string purchaseOrder { get; set; }
            public string status { get; set; }
        }
        public class ResultApi
        {
            public string statusSave { get; set; }
        }
    }
}