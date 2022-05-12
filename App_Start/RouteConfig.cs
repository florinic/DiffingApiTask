using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DiffingApiTask
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            /*
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );/* */
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" }
            );
            
            //routes.MapRoute(
            //    name: "v1Test",
            //    url: "v1/test",
            //    defaults: new { controller = "Endpoint", action = "Test" });
            
            routes.MapRoute(
                name: "v1",
                url: "v1/diff/{id:int}",
                defaults: new { controller = "Endpoint", action = "Diff" }
            );
            /* */
            routes.MapRoute(
                name: "v1side",
                url: "v1/diff/{id:int}/{side}",
                defaults: new { controller = "Endpoint", action = "DiffSide", side = UrlParameter.Optional }
            );
        }
    }
}
