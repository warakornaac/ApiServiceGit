using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace ApiService
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // Global.asax.cs
        protected void Application_BeginRequest()
        {
            string path = Request.Path ?? "";

            // จัดการเฉพาะ /Claim/ และ /Return/ paths
            bool isCorsPath = path.IndexOf("/Claim/", StringComparison.OrdinalIgnoreCase) >= 0
                           || path.IndexOf("/Return/", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isCorsPath)
                return;

            string requestOrigin = Request.Headers["Origin"];
            if (string.IsNullOrEmpty(requestOrigin))
                return;

            string configValue =
                System.Configuration.ConfigurationManager.AppSettings["ClaimCorsOrigins"] ?? "";

            bool isAllowed = configValue
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .Any(o => o.Equals(requestOrigin, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
                return;

            Response.Headers.Set("Access-Control-Allow-Origin", requestOrigin);
            Response.Headers.Set("Access-Control-Allow-Credentials", "true");
            Response.Headers.Set("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            Response.Headers.Set("Access-Control-Allow-Headers",
                "Content-Type, Authorization, X-Requested-With");

            if (Request.HttpMethod == "OPTIONS")
            {
                Response.StatusCode = 200;
                Response.Headers.Set("Access-Control-Max-Age", "600");
                Response.End(); // ตอบ preflight แล้วหยุด — ไม่ต้องไปถึง controller
            }
        }
    }
}
