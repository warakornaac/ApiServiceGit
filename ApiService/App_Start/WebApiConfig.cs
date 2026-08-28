using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;  // ← เพิ่ม using นี้
using StructureMap;

namespace ApiService
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // ← เพิ่มบล็อกนี้ก่อน MapHttpAttributeRoutes
            //var cors = new EnableCorsAttribute(
            //    origins: "*",
            //    headers: "*",
            //    methods: "*"
            //);
            //config.EnableCors(cors);

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}