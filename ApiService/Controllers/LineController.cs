using ApiService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Mvc;
using ApiService.Controllers;
using RouteAttribute = System.Web.Http.RouteAttribute;
using System.Net;

namespace ApiService.Controllers
{
    public class LineController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        // ตัวอย่างการสร้าง constructor ที่ไม่มีพารามิเตอร์
        public LineController()
        {
            // สร้าง instance ของ IApiServerService แบบไหนก็ได้ หรือไม่ต้องสร้างก็ได้
            _apiServerService = new ApiServerController(); // หรือใช้วิธี dependency injection อื่น ๆ
        }
        //POST: Sms
        [Route("Post/PushMessage")]
        public async Task<string> Post([FromBody] SendDeliveryModels models)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var HttpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/push");
                var jsonText = "";
                //TAC@AAC
                request.Headers.Add("Authorization", "Bearer OWaGHbr6csAoiOdfn7Nhz67ZtquKDkCznPXDog/dVMJ1mCO8ZifPdiJ7crXQD3iS4mCTEEkpkhmQ2Z/BSxHBL7nF8+qJtTIsSX7JahYAyqmXzbqLyOhfLmMGbTLfvOsRU7Y65Lj20AWMsb9ulGXJzQdB04t89/1O/w1cDnyilFU=");
                //StatusMobile 
                //request.Headers.Add("Authorization", "Bearer vuBOI4p+ni6h4cUy4xRoad5D8GzqheZ7b8pyxEmcUtBq4NEaJtXPjCwnyRWvn75v5FMJH/0h6U7SagkcM/UVFLdGKdfz0bGk7mHbueizQwyJ7vDTs3ta8K2DI+h5y/xFpd6eHawQvIvREHXjXLBRewdB04t89/1O/w1cDnyilFU=");                                                                                                                                                                                                                  
                if (models.DeliveryId == "1") //express
                {
                    jsonText = "{\r\n    \"to\": \"" + models.Uid + "\",\r\n    \"messages\": [\r\n   {\r\n\"type\": \"flex\",\r\n \"altText\": \"สถานะการส่งสินค้า\",\r\n  \"contents\": {\r\n \"type\": \"bubble\",\r\n   \"header\": {\r\n                    \"type\": \"box\",\r\n                    \"layout\": \"vertical\",\r\n                    \"contents\": [\r\n                        {\r\n                            \"type\": \"text\",\r\n                            \"text\": \"กำลังจัดเตรียมสินค้าด่วน\",\r\n                            \"weight\": \"bold\",\r\n                            \"color\": \"#FFFFFF\",\r\n                            \"margin\": \"none\",\r\n                            \"position\": \"relative\",\r\n                            \"align\": \"start\",\r\n                            \"style\": \"normal\"\r\n                        },\r\n                        {\r\n                            \"type\": \"box\",\r\n                            \"layout\": \"vertical\",\r\n                            \"contents\": [\r\n                                {\r\n                                    \"type\": \"image\",\r\n                                    \"url\": \"https://mst.aac.co.th/MobileCatalog_Test/images/icons/fast-03.png\",\r\n                                    \"offsetBottom\": \"1px\"\r\n                                }\r\n                            ],\r\n                            \"position\": \"absolute\",\r\n                            \"offsetEnd\": \"15px\",\r\n                            \"width\": \"47px\",\r\n                            \"flex\": 0,\r\n                            \"paddingAll\": \"none\",\r\n                            \"paddingEnd\": \"sm\",\r\n                            \"offsetTop\": \"0px\"\r\n                        }\r\n                    ],\r\n                    \"maxHeight\": \"40px\",\r\n                    \"paddingTop\": \"md\",\r\n                    \"flex\": 0,\r\n                    \"paddingBottom\": \"md\"\r\n                },\r\n                \"body\": {\r\n                    \"type\": \"box\",\r\n                    \"layout\": \"vertical\",\r\n                    \"contents\": [\r\n                        {\r\n                            \"type\": \"box\",\r\n                            \"layout\": \"vertical\",\r\n                            \"margin\": \"none\",\r\n                            \"spacing\": \"sm\",\r\n                            \"contents\": [\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"เลขที่ออเดอร์:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": true,\r\n                                            \"margin\": \"none\",\r\n                                            \"flex\": 3\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Docno + "\",\r\n                                            \"wrap\": true,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 6,\r\n                                            \"weight\": \"bold\"\r\n                                        }\r\n                                    ]\r\n                                },\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"วันที่สั่งซื้อ:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"flex\": 1,\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": false\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Docdate + "\",\r\n                                            \"wrap\": false,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 3,\r\n                                            \"weight\": \"regular\"\r\n                                        }\r\n                                    ]\r\n                                },\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"ร้านค้า:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"flex\": 2,\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": false\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Cusname + "\",\r\n                                            \"wrap\": false,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 9,\r\n                                            \"weight\": \"regular\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            ]\r\n                        },\r\n                        {\r\n                            \"type\": \"separator\",\r\n                            \"margin\": \"lg\"\r\n                        },\r\n                        {\r\n                            \"type\": \"box\",\r\n                            \"layout\": \"vertical\",\r\n                            \"contents\": [\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"สถานะ:\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"margin\": \"none\",\r\n                                            \"flex\": 1,\r\n                                            \"wrap\": false,\r\n                                            \"align\": \"start\"\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Delivery + "\",\r\n                                            \"margin\": \"none\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"size\": \"sm\",\r\n                                            \"color\": \"#04B431\",\r\n                                            \"flex\": 2\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"รายละเอียด\",\r\n                                            \"size\": \"sm\",\r\n                                            \"position\": \"relative\",\r\n                                            \"align\": \"end\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"flex\": 2,\r\n                                            \"color\": \"#6ea8fe\",\r\n                                            \"action\": {\r\n                                                \"type\": \"uri\",\r\n                                                \"label\": \"action\",\r\n                                                \"uri\": \"" + models.Urldetail + "\"\r\n                                            }\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            ],\r\n                            \"paddingTop\": \"md\"\r\n                        }\r\n                    ],\r\n                    \"paddingBottom\": \"lg\",\r\n                    \"paddingTop\": \"lg\"\r\n                },\r\n                \"styles\": {\r\n                    \"header\": {\r\n                        \"backgroundColor\": \"#44bcd8\",\r\n                        \"separator\": true\r\n                    },\r\n                    \"hero\": {\r\n                        \"backgroundColor\": \"#44bcd8\"\r\n                    }\r\n                }\r\n            }\r\n        }\r\n    ]\r\n}";
                }
                else //std
                {
                    jsonText = "{\r\n    \"to\": \"" + models.Uid + "\",\r\n    \"messages\": [\r\n    {\r\n \"type\": \"flex\",\r\n \"altText\": \"สถานะการส่งสินค้า\",\r\n  \"contents\": {\r\n   \"type\": \"bubble\",\r\n   \"header\": {\r\n                   \"type\": \"box\",\r\n                    \"layout\": \"vertical\",\r\n                    \"contents\": [\r\n                        {\r\n                            \"type\": \"text\",\r\n                            \"text\": \"สถานะการส่งสินค้า\",\r\n                            \"weight\": \"bold\",\r\n                            \"color\": \"#FFFFFF\",\r\n                            \"margin\": \"none\",\r\n                            \"position\": \"relative\",\r\n                            \"align\": \"start\",\r\n                            \"style\": \"normal\"\r\n                        }\r\n                    ],\r\n                    \"maxHeight\": \"40px\",\r\n                    \"paddingTop\": \"md\",\r\n                    \"flex\": 0,\r\n                    \"paddingBottom\": \"md\"\r\n                },\r\n                \"body\": {\r\n                    \"type\": \"box\",\r\n                    \"layout\": \"vertical\",\r\n                    \"contents\": [\r\n                        {\r\n                            \"type\": \"box\",\r\n                            \"layout\": \"vertical\",\r\n                            \"margin\": \"none\",\r\n                            \"spacing\": \"sm\",\r\n                            \"contents\": [\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"เลขที่ออเดอร์:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": true,\r\n                                            \"margin\": \"none\",\r\n                                            \"flex\": 3\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                          \"text\": \"" + models.Docno + "\",\r\n                                             \"wrap\": true,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 6,\r\n                                            \"weight\": \"bold\"\r\n                                        }\r\n                                    ]\r\n                                },\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"วันที่สั่งซื้อ:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"flex\": 1,\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": false\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                           \"text\": \"" + models.Docdate + "\",\r\n                                          \"wrap\": false,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 3,\r\n                                            \"weight\": \"regular\"\r\n                                        }\r\n                                    ]\r\n                                },\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"ร้านค้า:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"flex\": 2,\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": false\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                             \"text\": \"" + models.Cusname + "\",\r\n                                           \"wrap\": false,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 9,\r\n                                            \"weight\": \"regular\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            ]\r\n                        },\r\n                        {\r\n                            \"type\": \"separator\",\r\n                            \"margin\": \"lg\"\r\n                        },\r\n                        {\r\n                            \"type\": \"box\",\r\n                            \"layout\": \"vertical\",\r\n                            \"contents\": [\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"สถานะ:\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"margin\": \"none\",\r\n                                            \"flex\": 1,\r\n                                            \"wrap\": false,\r\n                                            \"align\": \"start\"\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                           \"text\": \"" + models.Delivery + "\",\r\n                                             \"margin\": \"none\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"size\": \"sm\",\r\n                                            \"color\": \"#04B431\",\r\n                                            \"flex\": 2\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"รายละเอียด\",\r\n                                            \"size\": \"sm\",\r\n                                            \"position\": \"relative\",\r\n                                            \"align\": \"end\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"flex\": 2,\r\n                                            \"color\": \"#6ea8fe\",\r\n                                            \"action\": {\r\n                                            \"type\": \"uri\",\r\n                                            \"label\": \"action\",\r\n                                            \"uri\": \"" + models.Urldetail + "\"\r\n                                      }\r\n                                    }\r\n                                ]\r\n                            }\r\n                            ],\r\n                            \"paddingTop\": \"md\"\r\n                        }\r\n                        ],\r\n                    \"paddingBottom\": \"lg\",\r\n                    \"paddingTop\": \"lg\"\r\n                },\r\n                \"styles\": {\r\n                    \"header\": {\r\n                        \"backgroundColor\": \"#44bcd8\",\r\n                        \"separator\": true\r\n                    },\r\n                    \"hero\": {\r\n                        \"backgroundColor\": \"#44bcd8\"\r\n                    }\r\n                }\r\n            }\r\n        }\r\n    ]\r\n}";
                }
                var content = new StringContent(jsonText, System.Text.Encoding.UTF8, "application/json");
                request.Content = content;
                var response = await HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                //end send api
                //keep log
                String modelsJson = JsonConvert.SerializeObject(models);
                String lastId = _apiServerService.SaveApiResponse("Post/PushMessage", modelsJson.ToString(), models.User.ToString());
                _apiServerService.UpdateApiRespone(lastId, responseBody.ToString());
                return responseBody;
            }
            catch (Exception ex)
            {
                // Handle the exception
                //return ex.Message;
                // throw new HttpResponseException(HttpStatusCode.InternalServerError);
                var res = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                res.Content = new StringContent(ex.Message);
                throw new HttpResponseException(res);

            }
        }


        [Route("Post/PushMessageSale")]
        public async Task<string> PostSale([FromBody] SendPmApproved models)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var HttpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/push");
                var jsonText = "";
                //TAC@AAC
                request.Headers.Add("Authorization", "Bearer OWaGHbr6csAoiOdfn7Nhz67ZtquKDkCznPXDog/dVMJ1mCO8ZifPdiJ7crXQD3iS4mCTEEkpkhmQ2Z/BSxHBL7nF8+qJtTIsSX7JahYAyqmXzbqLyOhfLmMGbTLfvOsRU7Y65Lj20AWMsb9ulGXJzQdB04t89/1O/w1cDnyilFU=");
                //StatusMobile 
                //request.Headers.Add("Authorization", "Bearer vuBOI4p+ni6h4cUy4xRoad5D8GzqheZ7b8pyxEmcUtBq4NEaJtXPjCwnyRWvn75v5FMJH/0h6U7SagkcM/UVFLdGKdfz0bGk7mHbueizQwyJ7vDTs3ta8K2DI+h5y/xFpd6eHawQvIvREHXjXLBRewdB04t89/1O/w1cDnyilFU=");                                                                                                                                                                                                                  
                if (models.Sta.Trim() != "C")
                {
                    if (models.SPrice == "0") //express
                    {
                        //jsonText = "{\r\n    \"to\": \"" + models.Uid + "\",\r\n    \"messages\": [\r\n   {\r\n\"type\": \"flex\",\r\n \"altText\": \"สถานะการส่งสินค้า\",\r\n  \"contents\": {\r\n \"type\": \"bubble\",\r\n   \"header\": {\r\n                    \"type\": \"box\",\r\n                    \"layout\": \"vertical\",\r\n                    \"contents\": [\r\n                        {\r\n                            \"type\": \"text\",\r\n                            \"text\": \"กำลังจัดเตรียมสินค้าด่วน\",\r\n                            \"weight\": \"bold\",\r\n                            \"color\": \"#FFFFFF\",\r\n                            \"margin\": \"none\",\r\n                            \"position\": \"relative\",\r\n                            \"align\": \"start\",\r\n                            \"style\": \"normal\"\r\n                        },\r\n                        {\r\n                            \"type\": \"box\",\r\n                            \"layout\": \"vertical\",\r\n                            \"contents\": [\r\n                                {\r\n                                    \"type\": \"image\",\r\n                                    \"url\": \"https://mst.aac.co.th/MobileCatalog_Test/images/icons/fast-03.png\",\r\n                                    \"offsetBottom\": \"1px\"\r\n                                }\r\n                            ],\r\n                            \"position\": \"absolute\",\r\n                            \"offsetEnd\": \"15px\",\r\n                            \"width\": \"47px\",\r\n                            \"flex\": 0,\r\n                            \"paddingAll\": \"none\",\r\n                            \"paddingEnd\": \"sm\",\r\n                            \"offsetTop\": \"0px\"\r\n                        }\r\n                    ],\r\n                    \"maxHeight\": \"40px\",\r\n                    \"paddingTop\": \"md\",\r\n                    \"flex\": 0,\r\n                    \"paddingBottom\": \"md\"\r\n                },\r\n                \"body\": {\r\n                    \"type\": \"box\",\r\n                    \"layout\": \"vertical\",\r\n                    \"contents\": [\r\n                        {\r\n                            \"type\": \"box\",\r\n                            \"layout\": \"vertical\",\r\n                            \"margin\": \"none\",\r\n                            \"spacing\": \"sm\",\r\n                            \"contents\": [\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"เลขที่ออเดอร์:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": true,\r\n                                            \"margin\": \"none\",\r\n                                            \"flex\": 3\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Docno + "\",\r\n                                            \"wrap\": true,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 6,\r\n                                            \"weight\": \"bold\"\r\n                                        }\r\n                                    ]\r\n                                },\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"วันที่สั่งซื้อ:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"flex\": 1,\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": false\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Docdate + "\",\r\n                                            \"wrap\": false,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 3,\r\n                                            \"weight\": \"regular\"\r\n                                        }\r\n                                    ]\r\n                                },\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"ร้านค้า:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"flex\": 2,\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": false\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Cusname + "\",\r\n                                            \"wrap\": false,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 9,\r\n                                            \"weight\": \"regular\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            ]\r\n                        },\r\n                        {\r\n                            \"type\": \"separator\",\r\n                            \"margin\": \"lg\"\r\n                        },\r\n                        {\r\n                            \"type\": \"box\",\r\n                            \"layout\": \"vertical\",\r\n                            \"contents\": [\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"สถานะ:\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"margin\": \"none\",\r\n                                            \"flex\": 1,\r\n                                            \"wrap\": false,\r\n                                            \"align\": \"start\"\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Delivery + "\",\r\n                                            \"margin\": \"none\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"size\": \"sm\",\r\n                                            \"color\": \"#04B431\",\r\n                                            \"flex\": 2\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"รายละเอียด\",\r\n                                            \"size\": \"sm\",\r\n                                            \"position\": \"relative\",\r\n                                            \"align\": \"end\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"flex\": 2,\r\n                                            \"color\": \"#6ea8fe\",\r\n                                            \"action\": {\r\n                                                \"type\": \"uri\",\r\n                                                \"label\": \"action\",\r\n                                                \"uri\": \"" + models.Urldetail + "\"\r\n                                            }\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            ],\r\n                            \"paddingTop\": \"md\"\r\n                        }\r\n                    ],\r\n                    \"paddingBottom\": \"lg\",\r\n                    \"paddingTop\": \"lg\"\r\n                },\r\n                \"styles\": {\r\n                    \"header\": {\r\n                        \"backgroundColor\": \"#44bcd8\",\r\n                        \"separator\": true\r\n                    },\r\n                    \"hero\": {\r\n                        \"backgroundColor\": \"#44bcd8\"\r\n                    }\r\n                }\r\n            }\r\n        }\r\n    ]\r\n}";
                        jsonText = $@"
                                        {{
                                            ""to"": ""{models.Uid}"",
                                            ""messages"": [
                                            {{
                                                ""type"": ""flex"",
                                                ""altText"": ""รายการถูกอนุมัติ"",
                                                ""contents"": {{
                                                ""type"": ""bubble"",
                                                ""header"": {{
                                                    ""type"": ""box"",
                                                    ""layout"": ""vertical"",
                                                    ""paddingTop"": ""md"",
                                                    ""paddingBottom"": ""md"",
                                                    ""contents"": [
                                                    {{
                                                        ""type"": ""text"",
                                                        ""text"": ""รายการถูกอนุมัติ"",
                                                        ""weight"": ""bold"",
                                                        ""color"": ""#FFFFFF"",
                                                        ""align"": ""start""
                                                    }},
                                                    {{
                                                      ""type"": ""box"",
                                                      ""layout"": ""vertical"",
                                                      ""position"": ""absolute"",
                                                      ""offsetEnd"": ""15px"",
                                                      ""offsetTop"": ""0px"",
                                                      ""width"": ""47px"",
                                                      ""flex"": 0,
                                                      ""paddingEnd"": ""sm"",
                                                      ""contents"": [
                                                        {{
                                                          ""type"": ""image"",
                                                          ""url"": ""https://mst.aac.co.th/MobileCatalog_Test/images/icons/approve.png"",
                                                          ""offsetBottom"": ""1px""
                                                        }}
                                                      ]
                                                    }}
                                                    ]
                                                }},
                                                ""body"": {{
                                                    ""type"": ""box"",
                                                    ""layout"": ""vertical"",
                                                    ""contents"": [
                                                    {{
                                                        ""type"": ""box"",
                                                        ""layout"": ""vertical"",
                                                        ""spacing"": ""sm"",
                                                        ""contents"": [
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""รายการ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Topic}"", ""size"": ""sm"", ""weight"": ""bold"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""สินค้า:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Stkcod} | {models.Stkdes}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""ร้านค้า:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Cuscod} | {models.Cusname}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""จำนวน:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Qty}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""ผู้อนุมัติ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.ApprvBy}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }}
                                                        ]
                                                    }},
                                                    {{
                                                        ""type"": ""separator"",
                                                        ""margin"": ""lg""
                                                    }},
                                                    {{
                                                        ""type"": ""box"",
                                                        ""layout"": ""baseline"",
                                                        ""contents"": [
                                                        {{ ""type"": ""text"", ""text"": ""วันที่อนุมัติ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"":1 }},
                                                        {{ ""type"": ""text"", ""text"": ""{models.ApprvDate}"", ""size"": ""sm"", ""flex"": 2 }},
                                                        {{
                                                            ""type"": ""text"",
                                                            ""text"": ""MobileOrder"",
                                                            ""size"": ""sm"",
                                                            ""color"": ""#6ea8fe"",
                                                            ""weight"": ""bold"",
                                                            ""flex"": 3,
                                                            ""align"": ""end"",
                                                            ""action"": {{
                                                            ""type"": ""uri"",
                                                            ""label"": ""action"",
                                                            ""uri"": ""https://mst.aac.co.th/MobileCatalog""
                                                            }}
                                                        }}
                                                        ]
                                                    }}
                                                    ]
                                                }},
                                                ""styles"": {{
                                                    ""header"": {{
                                                    ""backgroundColor"": ""#44bcd8""
                                                    }}
                                                }}
                                                }}
                                            }}
                                            ]
                                        }}";
                    }
                    else //std
                    {
                        jsonText = $@"
                                        {{
                                            ""to"": ""{models.Uid}"",
                                            ""messages"": [
                                            {{
                                                ""type"": ""flex"",
                                                ""altText"": ""รายการถูกอนุมัติ"",
                                                ""contents"": {{
                                                ""type"": ""bubble"",
                                                ""header"": {{
                                                    ""type"": ""box"",
                                                    ""layout"": ""vertical"",
                                                    ""paddingTop"": ""md"",
                                                    ""paddingBottom"": ""md"",
                                                    ""contents"": [
                                                    {{
                                                        ""type"": ""text"",
                                                        ""text"": ""รายการถูกอนุมัติ"",
                                                        ""weight"": ""bold"",
                                                        ""color"": ""#FFFFFF"",
                                                        ""align"": ""start""
                                                    }},
                                                    {{
                                                      ""type"": ""box"",
                                                      ""layout"": ""vertical"",
                                                      ""position"": ""absolute"",
                                                      ""offsetEnd"": ""15px"",
                                                      ""offsetTop"": ""0px"",
                                                      ""width"": ""47px"",
                                                      ""flex"": 0,
                                                      ""paddingEnd"": ""sm"",
                                                      ""contents"": [
                                                        {{
                                                          ""type"": ""image"",
                                                          ""url"": ""https://mst.aac.co.th/MobileCatalog_Test/images/icons/approve.png"",
                                                          ""offsetBottom"": ""1px""
                                                        }}
                                                      ]
                                                    }}
                                                    ]
                                                }},
                                                ""body"": {{
                                                    ""type"": ""box"",
                                                    ""layout"": ""vertical"",
                                                    ""contents"": [
                                                    {{
                                                        ""type"": ""box"",
                                                        ""layout"": ""vertical"",
                                                        ""spacing"": ""sm"",
                                                        ""contents"": [
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""รายการ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Topic}"", ""size"": ""sm"", ""weight"": ""bold"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""สินค้า:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Stkcod} | {models.Stkdes}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""ร้านค้า:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Cuscod} | {models.Cusname}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""จำนวน:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Qty}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""ผู้อนุมัติ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.ApprvBy}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }}
                                                        ]
                                                    }},
                                                    {{
                                                        ""type"": ""separator"",
                                                        ""margin"": ""lg""
                                                    }},
                                                    {{
                                                        ""type"": ""box"",
                                                        ""layout"": ""baseline"",
                                                        ""contents"": [
                                                        {{ ""type"": ""text"", ""text"": ""วันที่อนุมัติ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"":1 }},
                                                        {{ ""type"": ""text"", ""text"": ""{models.ApprvDate}"", ""size"": ""sm"", ""flex"": 2 }},
                                                        {{
                                                            ""type"": ""text"",
                                                            ""text"": ""MobileOrder"",
                                                            ""size"": ""sm"",
                                                            ""color"": ""#6ea8fe"",
                                                            ""weight"": ""bold"",
                                                            ""flex"": 3,
                                                            ""align"": ""end"",
                                                            ""action"": {{
                                                            ""type"": ""uri"",
                                                            ""label"": ""action"",
                                                            ""uri"": ""https://mst.aac.co.th/MobileCatalog""
                                                            }}
                                                        }}
                                                        ]
                                                    }}
                                                    ]
                                                }},
                                                ""styles"": {{
                                                    ""header"": {{
                                                    ""backgroundColor"": ""#44bcd8""
                                                    }}
                                                }}
                                                }}
                                            }}
                                            ]
                                        }}";
                    }
                }
                else
                {
                    if (models.SPrice == "0") //express
                    {
                        //jsonText = "{\r\n    \"to\": \"" + models.Uid + "\",\r\n    \"messages\": [\r\n   {\r\n\"type\": \"flex\",\r\n \"altText\": \"สถานะการส่งสินค้า\",\r\n  \"contents\": {\r\n \"type\": \"bubble\",\r\n   \"header\": {\r\n                    \"type\": \"box\",\r\n                    \"layout\": \"vertical\",\r\n                    \"contents\": [\r\n                        {\r\n                            \"type\": \"text\",\r\n                            \"text\": \"กำลังจัดเตรียมสินค้าด่วน\",\r\n                            \"weight\": \"bold\",\r\n                            \"color\": \"#FFFFFF\",\r\n                            \"margin\": \"none\",\r\n                            \"position\": \"relative\",\r\n                            \"align\": \"start\",\r\n                            \"style\": \"normal\"\r\n                        },\r\n                        {\r\n                            \"type\": \"box\",\r\n                            \"layout\": \"vertical\",\r\n                            \"contents\": [\r\n                                {\r\n                                    \"type\": \"image\",\r\n                                    \"url\": \"https://mst.aac.co.th/MobileCatalog_Test/images/icons/fast-03.png\",\r\n                                    \"offsetBottom\": \"1px\"\r\n                                }\r\n                            ],\r\n                            \"position\": \"absolute\",\r\n                            \"offsetEnd\": \"15px\",\r\n                            \"width\": \"47px\",\r\n                            \"flex\": 0,\r\n                            \"paddingAll\": \"none\",\r\n                            \"paddingEnd\": \"sm\",\r\n                            \"offsetTop\": \"0px\"\r\n                        }\r\n                    ],\r\n                    \"maxHeight\": \"40px\",\r\n                    \"paddingTop\": \"md\",\r\n                    \"flex\": 0,\r\n                    \"paddingBottom\": \"md\"\r\n                },\r\n                \"body\": {\r\n                    \"type\": \"box\",\r\n                    \"layout\": \"vertical\",\r\n                    \"contents\": [\r\n                        {\r\n                            \"type\": \"box\",\r\n                            \"layout\": \"vertical\",\r\n                            \"margin\": \"none\",\r\n                            \"spacing\": \"sm\",\r\n                            \"contents\": [\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"เลขที่ออเดอร์:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": true,\r\n                                            \"margin\": \"none\",\r\n                                            \"flex\": 3\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Docno + "\",\r\n                                            \"wrap\": true,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 6,\r\n                                            \"weight\": \"bold\"\r\n                                        }\r\n                                    ]\r\n                                },\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"วันที่สั่งซื้อ:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"flex\": 1,\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": false\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Docdate + "\",\r\n                                            \"wrap\": false,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 3,\r\n                                            \"weight\": \"regular\"\r\n                                        }\r\n                                    ]\r\n                                },\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"spacing\": \"sm\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"ร้านค้า:\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"flex\": 2,\r\n                                            \"weight\": \"bold\",\r\n                                            \"wrap\": false\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Cusname + "\",\r\n                                            \"wrap\": false,\r\n                                            \"color\": \"#666666\",\r\n                                            \"size\": \"sm\",\r\n                                            \"flex\": 9,\r\n                                            \"weight\": \"regular\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            ]\r\n                        },\r\n                        {\r\n                            \"type\": \"separator\",\r\n                            \"margin\": \"lg\"\r\n                        },\r\n                        {\r\n                            \"type\": \"box\",\r\n                            \"layout\": \"vertical\",\r\n                            \"contents\": [\r\n                                {\r\n                                    \"type\": \"box\",\r\n                                    \"layout\": \"baseline\",\r\n                                    \"contents\": [\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"สถานะ:\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"color\": \"#aaaaaa\",\r\n                                            \"size\": \"xs\",\r\n                                            \"margin\": \"none\",\r\n                                            \"flex\": 1,\r\n                                            \"wrap\": false,\r\n                                            \"align\": \"start\"\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"" + models.Delivery + "\",\r\n                                            \"margin\": \"none\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"size\": \"sm\",\r\n                                            \"color\": \"#04B431\",\r\n                                            \"flex\": 2\r\n                                        },\r\n                                        {\r\n                                            \"type\": \"text\",\r\n                                            \"text\": \"รายละเอียด\",\r\n                                            \"size\": \"sm\",\r\n                                            \"position\": \"relative\",\r\n                                            \"align\": \"end\",\r\n                                            \"weight\": \"bold\",\r\n                                            \"flex\": 2,\r\n                                            \"color\": \"#6ea8fe\",\r\n                                            \"action\": {\r\n                                                \"type\": \"uri\",\r\n                                                \"label\": \"action\",\r\n                                                \"uri\": \"" + models.Urldetail + "\"\r\n                                            }\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            ],\r\n                            \"paddingTop\": \"md\"\r\n                        }\r\n                    ],\r\n                    \"paddingBottom\": \"lg\",\r\n                    \"paddingTop\": \"lg\"\r\n                },\r\n                \"styles\": {\r\n                    \"header\": {\r\n                        \"backgroundColor\": \"#44bcd8\",\r\n                        \"separator\": true\r\n                    },\r\n                    \"hero\": {\r\n                        \"backgroundColor\": \"#44bcd8\"\r\n                    }\r\n                }\r\n            }\r\n        }\r\n    ]\r\n}";
                        jsonText = $@"
                                        {{
                                            ""to"": ""{models.Uid}"",
                                            ""messages"": [
                                            {{
                                                ""type"": ""flex"",
                                                ""altText"": ""รายการถูกปฏิเสธ"",
                                                ""contents"": {{
                                                ""type"": ""bubble"",
                                                ""header"": {{
                                                    ""type"": ""box"",
                                                    ""layout"": ""vertical"",
                                                    ""paddingTop"": ""md"",
                                                    ""paddingBottom"": ""md"",
                                                    ""contents"": [
                                                    {{
                                                        ""type"": ""text"",
                                                        ""text"": ""รายการถูกปฏิเสธ"",
                                                        ""weight"": ""bold"",
                                                        ""color"": ""#FFFFFF"",
                                                        ""align"": ""start""
                                                    }},
                                                    {{
                                                      ""type"": ""box"",
                                                      ""layout"": ""vertical"",
                                                      ""position"": ""absolute"",
                                                      ""offsetEnd"": ""15px"",
                                                      ""offsetTop"": ""0px"",
                                                      ""width"": ""47px"",
                                                      ""flex"": 0,
                                                      ""paddingEnd"": ""sm"",
                                                      ""contents"": [
                                                        {{
                                                          ""type"": ""image"",
                                                          ""url"": ""https://mst.aac.co.th/MobileCatalog_Test/images/icons/decline.png"",
                                                          ""offsetBottom"": ""1px""
                                                        }}
                                                      ]
                                                    }}
                                                    ]
                                                }},
                                                ""body"": {{
                                                    ""type"": ""box"",
                                                    ""layout"": ""vertical"",
                                                    ""contents"": [
                                                    {{
                                                        ""type"": ""box"",
                                                        ""layout"": ""vertical"",
                                                        ""spacing"": ""sm"",
                                                        ""contents"": [
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""รายการ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Topic}"", ""size"": ""sm"", ""weight"": ""bold"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""สินค้า:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Stkcod} | {models.Stkdes}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""ร้านค้า:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Cuscod} | {models.Cusname}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""จำนวน:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Qty}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""ผู้อนุมัติ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.ApprvBy}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }}
                                                        ]
                                                    }},
                                                    {{
                                                        ""type"": ""separator"",
                                                        ""margin"": ""lg""
                                                    }},
                                                    {{
                                                        ""type"": ""box"",
                                                        ""layout"": ""baseline"",
                                                        ""contents"": [
                                                        {{ ""type"": ""text"", ""text"": ""วันที่อนุมัติ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"":1 }},
                                                        {{ ""type"": ""text"", ""text"": ""{models.ApprvDate}"", ""size"": ""sm"", ""flex"": 2 }},
                                                        {{
                                                            ""type"": ""text"",
                                                            ""text"": ""MobileOrder"",
                                                            ""size"": ""sm"",
                                                            ""color"": ""#6ea8fe"",
                                                            ""weight"": ""bold"",
                                                            ""flex"": 3,
                                                            ""align"": ""end"",
                                                            ""action"": {{
                                                            ""type"": ""uri"",
                                                            ""label"": ""action"",
                                                            ""uri"": ""https://mst.aac.co.th/MobileCatalog""
                                                            }}
                                                        }}
                                                        ]
                                                    }}
                                                    ]
                                                }},
                                                ""styles"": {{
                                                    ""header"": {{
                                                    ""backgroundColor"": ""#F28585""
                                                    }}
                                                }}
                                                }}
                                            }}
                                            ]
                                        }}";
                    }
                    else //std
                    {
                        jsonText = $@"
                                        {{
                                            ""to"": ""{models.Uid}"",
                                            ""messages"": [
                                            {{
                                                ""type"": ""flex"",
                                                ""altText"": ""รายการถูกปฏิเสธ"",
                                                ""contents"": {{
                                                ""type"": ""bubble"",
                                                ""header"": {{
                                                    ""type"": ""box"",
                                                    ""layout"": ""vertical"",
                                                    ""paddingTop"": ""md"",
                                                    ""paddingBottom"": ""md"",
                                                    ""contents"": [
                                                    {{
                                                        ""type"": ""text"",
                                                        ""text"": ""รายการถูกปฏิเสธ"",
                                                        ""weight"": ""bold"",
                                                        ""color"": ""#FFFFFF"",
                                                        ""align"": ""start""
                                                    }},
                                                    {{
                                                      ""type"": ""box"",
                                                      ""layout"": ""vertical"",
                                                      ""position"": ""absolute"",
                                                      ""offsetEnd"": ""15px"",
                                                      ""offsetTop"": ""0px"",
                                                      ""width"": ""47px"",
                                                      ""flex"": 0,
                                                      ""paddingEnd"": ""sm"",
                                                      ""contents"": [
                                                        {{
                                                          ""type"": ""image"",
                                                          ""url"": ""https://mst.aac.co.th/MobileCatalog_Test/images/icons/decline.png"",
                                                          ""offsetBottom"": ""1px""
                                                        }}
                                                      ]
                                                    }}
                                                    ]
                                                }},
                                                ""body"": {{
                                                    ""type"": ""box"",
                                                    ""layout"": ""vertical"",
                                                    ""contents"": [
                                                    {{
                                                        ""type"": ""box"",
                                                        ""layout"": ""vertical"",
                                                        ""spacing"": ""sm"",
                                                        ""contents"": [
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""รายการ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Topic}"", ""size"": ""sm"", ""weight"": ""bold"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""สินค้า:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Stkcod} | {models.Stkdes}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""ร้านค้า:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Cuscod} | {models.Cusname}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""จำนวน:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.Qty}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }},
                                                        {{
                                                            ""type"": ""box"",
                                                            ""layout"": ""baseline"",
                                                            ""contents"": [
                                                            {{ ""type"": ""text"", ""text"": ""ผู้อนุมัติ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"": 2 }},
                                                            {{ ""type"": ""text"", ""text"": ""{models.ApprvBy}"", ""size"": ""sm"", ""flex"": 4 }}
                                                            ]
                                                        }}
                                                        ]
                                                    }},
                                                    {{
                                                        ""type"": ""separator"",
                                                        ""margin"": ""lg""
                                                    }},
                                                    {{
                                                        ""type"": ""box"",
                                                        ""layout"": ""baseline"",
                                                        ""contents"": [
                                                        {{ ""type"": ""text"", ""text"": ""วันที่อนุมัติ:"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""weight"": ""bold"", ""flex"":1 }},
                                                        {{ ""type"": ""text"", ""text"": ""{models.ApprvDate}"", ""size"": ""sm"", ""flex"": 2 }},
                                                        {{
                                                            ""type"": ""text"",
                                                            ""text"": ""MobileOrder"",
                                                            ""size"": ""sm"",
                                                            ""color"": ""#6ea8fe"",
                                                            ""weight"": ""bold"",
                                                            ""flex"": 3,
                                                            ""align"": ""end"",
                                                            ""action"": {{
                                                            ""type"": ""uri"",
                                                            ""label"": ""action"",
                                                            ""uri"": ""https://mst.aac.co.th/MobileCatalog""
                                                            }}
                                                        }}
                                                        ]
                                                    }}
                                                    ]
                                                }},
                                                ""styles"": {{
                                                    ""header"": {{
                                                    ""backgroundColor"": ""#F28585""
                                                    }}
                                                }}
                                                }}
                                            }}
                                            ]
                                        }}";
                    }
                }

                var content = new StringContent(jsonText, System.Text.Encoding.UTF8, "application/json");
                request.Content = content;
                var response = await HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                //end send api
                //keep log
                String modelsJson = JsonConvert.SerializeObject(models);
                String lastId = _apiServerService.SaveApiResponse("Post/PushMessageSale", modelsJson.ToString(), models.User.ToString());
                _apiServerService.UpdateApiRespone(lastId, responseBody.ToString());
                return responseBody;
            }
            catch (Exception ex)
            {
                // Handle the exception
                //return ex.Message;
                // throw new HttpResponseException(HttpStatusCode.InternalServerError);
                var res = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                res.Content = new StringContent(ex.Message);
                throw new HttpResponseException(res);

            }
        }
    }
}
