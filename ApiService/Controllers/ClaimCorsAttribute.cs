using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http.Filters;

namespace ApiService
{
    /// <summary>
    /// ใส่ CORS headers โดยตรง — ไม่ต้องการ EnableCors() ใน WebApiConfig
    /// ใช้กับ Controller ที่ต้องการ CORS เฉพาะตัว
    /// </summary>
    public class ClaimCorsAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext context)
        {
            if (context.Response == null)
            {
                base.OnActionExecuted(context);
                return;
            }

            IEnumerable<string> originValues;
            bool hasOrigin = context.Request.Headers
                .TryGetValues("Origin", out originValues);

            if (!hasOrigin)
            {
                base.OnActionExecuted(context);
                return;
            }

            string requestOrigin = originValues.FirstOrDefault() ?? "";

            string configValue =
                ConfigurationManager.AppSettings["ClaimCorsOrigins"] ?? "";

            bool isAllowed = configValue
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .Any(o => o.Equals(requestOrigin, StringComparison.OrdinalIgnoreCase));

            if (isAllowed)
            {
                var headers = context.Response.Headers;
                headers.Remove("Access-Control-Allow-Origin");
                headers.Add("Access-Control-Allow-Origin", requestOrigin);
                headers.Add("Access-Control-Allow-Credentials", "true");
            }

            base.OnActionExecuted(context);
        }
    }
}