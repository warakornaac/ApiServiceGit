using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Http;
using StructureMap;

namespace ApiService
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config) {
            // ← เพิ่มบล็อกนี้ก่อน MapHttpAttributeRoutes
            // NOTE: to enable the block below, first install the NuGet package
            // Microsoft.AspNet.WebApi.Cors and re-add `using System.Web.Http.Cors;`.
            // The package is not referenced by this project, so the using directive
            // alone did not compile (CS0234).
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