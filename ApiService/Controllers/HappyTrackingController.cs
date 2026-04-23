using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json;
using RouteAttribute = System.Web.Http.RouteAttribute;
using ApiService.Filters;


namespace ApiService.Controllers
{
    public class HappyTrackingController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        // ตัวอย่างการสร้าง constructor ที่ไม่มีพารามิเตอร์
        public HappyTrackingController()
        {
            // สร้าง instance ของ IApiServerService แบบไหนก็ได้ หรือไม่ต้องสร้างก็ได้
            _apiServerService = new ApiServerController();
        }
        [Route("HappyTracking/ReceiveDataTracking")]
        [ApiKeyAuthorize]
        public IHttpActionResult ReceiveDataHappyTracking([FromBody] ReceiveData requestData)
        {
            var dataRes = new DataRespond
            {
                purchaseOrder = "",
                statusCode = 200,
                message = "success"
            };

            string requestDataLog = JsonConvert.SerializeObject(requestData);
            string jsonReturn = "";

            try
            {
                if (requestData == null)
                {
                    dataRes.statusCode = 400;
                    dataRes.message = "Request body is null.";
                    return ResponseMessage(Request.CreateResponse((HttpStatusCode)dataRes.statusCode, dataRes));
                }

                if (requestData.data == null || !requestData.data.Any())
                {
                    dataRes.statusCode = 400;
                    dataRes.message = "data is empty.";
                    return ResponseMessage(Request.CreateResponse((HttpStatusCode)dataRes.statusCode, dataRes));
                }

                foreach (var rowData in requestData.data)
                {
                    var purchaseOrder = rowData.purchaseOrder?.Trim();
                    var docNo = rowData.docNo?.Trim();

                    if (string.IsNullOrWhiteSpace(purchaseOrder))
                    {
                        dataRes.purchaseOrder = "";
                        dataRes.statusCode = 400;
                        dataRes.message = "purchaseOrder is required.";
                        return ResponseMessage(Request.CreateResponse((HttpStatusCode)dataRes.statusCode, dataRes));
                    }

                    dataRes.purchaseOrder = purchaseOrder;

                    if (rowData.shipmentTracking == null || !rowData.shipmentTracking.Any())
                    {
                        dataRes.statusCode = 400;
                        dataRes.message = "shipmentTracking must contain at least 1 item.";
                        return ResponseMessage(Request.CreateResponse((HttpStatusCode)dataRes.statusCode, dataRes));
                    }

                    bool hasValidItem = false;

                    foreach (var rowShipment in rowData.shipmentTracking)
                    {
                        if (string.IsNullOrWhiteSpace(rowShipment.statusCode))
                            continue;

                        if (string.IsNullOrWhiteSpace(rowShipment.statusName))
                            continue;

                        // validate datetime
                        DateTime tempDate;
                        if (!DateTime.TryParseExact(
                                rowShipment.timeStamp,
                                "dd/MM/yyyy HH:mm:ss",
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out tempDate))
                        {
                            continue;
                        }

                        var saveResult = saveDataTracking(
                            purchaseOrder,
                            docNo,
                            rowShipment.statusCode.Trim(),
                            rowShipment.statusName.Trim(),
                            rowShipment.timeStamp
                        );

                        if (saveResult != "Y")
                        {
                            dataRes.statusCode = 500;
                            dataRes.message = saveResult;
                            return ResponseMessage(Request.CreateResponse((HttpStatusCode)dataRes.statusCode, dataRes));
                        }

                        hasValidItem = true;
                    }

                    if (!hasValidItem)
                    {
                        dataRes.statusCode = 400;
                        dataRes.message = "No valid shipmentTracking data to save.";
                        return ResponseMessage(Request.CreateResponse((HttpStatusCode)dataRes.statusCode, dataRes));
                    }
                }
            }
            catch (Exception ex)
            {
                dataRes.statusCode = 500;
                dataRes.message = ex.Message;
            }
            finally
            {
                jsonReturn = JsonConvert.SerializeObject(dataRes);

                string lastId = _apiServerService.SaveApiResponse("HappyTracking/ReceiveDataTracking", requestDataLog, "");
                _apiServerService.UpdateApiRespone(lastId, jsonReturn);
            }

            return ResponseMessage(Request.CreateResponse((HttpStatusCode)dataRes.statusCode, dataRes));
        }
        public string saveDataTracking(string purchaseOrder, string doNo, string statusCode, string statusName, string timeStamp)
        {
            string txtRespond = "Y";
            DateTime? timeStampConv = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(timeStamp))
                {
                    timeStampConv = DateTime.ParseExact(
                        timeStamp,
                        "dd/MM/yyyy HH:mm:ss",
                        CultureInfo.InvariantCulture
                    );
                }
                var connectionString = ConfigurationManager.ConnectionStrings["APIDB_ConnectionString"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("P_Tracking_Happy", conn))
                {
                    conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@inPurchaseOrder", purchaseOrder ?? "");
                    cmd.Parameters.AddWithValue("@inDocNo", doNo ?? "");
                    cmd.Parameters.AddWithValue("@inStatusCode", statusCode);
                    cmd.Parameters.AddWithValue("@inStatusName", statusName ?? "");
                    cmd.Parameters.Add("@inStatusTimeStamp", SqlDbType.DateTime).Value = (object)timeStampConv ?? DBNull.Value;
                    SqlParameter p = new SqlParameter("@OutGenstatus", SqlDbType.NVarChar, 100);
                    p.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(p);
                    cmd.ExecuteNonQuery();
                    string storedResult = cmd.Parameters["@OutGenstatus"].Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(storedResult) && storedResult != "Y")
                    {
                        txtRespond = storedResult;
                    }
                }
            }
            catch (Exception ex)
            {
                txtRespond = ex.Message;
            }
            return txtRespond;
        }
        //Data  
        public class ReceiveData
        {
            public List<DataItemList> data { get; set; }
        }
        public class DataItemList
        {
            public string purchaseOrder { get; set; }
            public string docNo { get; set; }
            public List<ShipmentTrackingList> shipmentTracking { get; set; }
        }
        public class ShipmentTrackingList
        {
            public string statusCode { get; set; }
            public string statusName { get; set; }
            public string timeStamp { get; set; }
            public string remark { get; set; }
        }
        //
        public class DataRespond
        {
            public string purchaseOrder { get; set; }
            public int statusCode { get; set; }
            public string message { get; set; }
        }
        public class ResultApi
        {
            public string statusSave { get; set; }
        }
    }
}