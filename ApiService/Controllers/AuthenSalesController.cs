using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.DirectoryServices;
using System.Configuration;
using System.Data.SqlClient;
using Newtonsoft.Json;
using RouteAttribute = System.Web.Http.RouteAttribute;
using AuthorizeAttribute = System.Web.Http.AuthorizeAttribute;
using ApiService.Filters;
using ApiService.Services;
using ApiService.Models;
using System.Data;

namespace ApiService.Controllers
{
    public class AuthenSalesController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        // ตัวอย่างการสร้าง constructor ที่ไม่มีพารามิเตอร์
        public AuthenSalesController() {
            // สร้าง instance ของ IApiServerService แบบไหนก็ได้ หรือไม่ต้องสร้างก็ได้
            _apiServerService = new ApiServerController();
        }
        [HttpPost]
        [Route("AuthenUserSales")]
        [ApiKeyAuthorize]
        public IHttpActionResult AuthenUserSales(string username, string password) {
            string errorMessage = "Success";
            string fullnameAd = string.Empty;
            string departmentSales = string.Empty;
            Boolean verifyAd = true;
            //call function success
            var okresp = new HttpResponseMessage(HttpStatusCode.OK) {
                ReasonPhrase = "Success"
            };
            if (string.IsNullOrEmpty(username)) {
                errorMessage = "Username not null";
            }
            if (string.IsNullOrEmpty(password)) {
                errorMessage = "Password not null";
            }
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password)) //have user/pass
            {
                try {
                    DirectoryEntry directionEntry = new DirectoryEntry("LDAP://ADSRV2016-01/dc=Automotive,dc=com", username, password);
                    if (directionEntry != null) {
                        DirectorySearcher search = new DirectorySearcher(directionEntry);
                        search.Filter = "(SAMAccountName=" + username + ")";
                        SearchResult result = search.FindOne();
                        if (result != null) {
                            DirectoryEntry userEntry = result.GetDirectoryEntry();
                            if (userEntry != null) {
                                fullnameAd = userEntry.Properties["Name"].Value.ToString();
                                departmentSales = userEntry.Properties["Department"].Value.ToString();
                            }
                        }
                        else {
                            errorMessage = result.ToString();
                            verifyAd = false;
                        }
                    }
                }
                catch (Exception ex) {
                    verifyAd = false;
                    errorMessage = ex.Message.ToString();
                }
            }
            DataRespond dataRes = new DataRespond();
            dataRes.statusCode = Convert.ToInt32(okresp.StatusCode);
            dataRes.errorMessage = errorMessage;
            dataRes.result = new List<result>();
            if (errorMessage == "Success") {
                dataRes.result.Add(
                    new result {
                        verify = verifyAd.ToString(),
                        fullnameSales = fullnameAd,
                        departmentSales = departmentSales
                    });
            }
            //string json = JsonConvert.SerializeObject(dataRes);
            //return json;
            //keep log
            var jsonLog = JsonConvert.SerializeObject(new {
                username = username,
                password = password,
            });
            string jsonReturn = JsonConvert.SerializeObject(dataRes);
            String lastId = _apiServerService.SaveApiResponse("AuthenSale", jsonLog, "");
            _apiServerService.UpdateApiRespone(lastId, jsonReturn.ToString());
            return Json(dataRes);
        }
    }
    //model
    public class DataRespond
    {
        public int statusCode { get; set; }
        public string errorMessage { get; set; }
        public List<result> result { get; set; }
    }
    //array list result
    public class result
    {
        public string verify { get; set; }
        public string fullnameSales { get; set; }
        public string departmentSales { get; set; }
        public string salesmanCode { get; set; }
        public int userType { get; set; }
    }
}
