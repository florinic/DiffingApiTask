using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Web;
using System.Web.Mvc;
using DiffingApiTask.Controllers;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Diagnostics;
using HttpRequest = System.Web.HttpRequest;
using HttpResponse = System.Web.HttpResponse;
using HttpContext = System.Web.HttpContext;

namespace TheUnitTestProject
{
    [TestClass]
    public class TestEndpointController
    {
        private EndpointController GetTarget()
        {
            var target = new EndpointController();

            HttpRequest httpRequest = new HttpRequest("", "http://someUrl", "");
            StringWriter stringWriter = new StringWriter();
            HttpResponse httpResponse = new HttpResponse(stringWriter);
            HttpContext httpContextMock = new HttpContext(httpRequest, httpResponse);
            target.ControllerContext = new ControllerContext(new HttpContextWrapper(httpContextMock), new System.Web.Routing.RouteData(), target);

            return target;
        }

        [TestMethod]
        public void SameBodies()
        {
            const int index = 1;

            var target = GetTarget();
            var result = target.DiffSide(index, "left", body: new EndpointController.BodyContent { data = "AAAAAA==" });
            Assert.AreEqual(target.HttpContext.Response.StatusCode, 201);

            result = target.DiffSide(index, "right", body: new EndpointController.BodyContent { data = "AAAAAA==" });
            Assert.AreEqual(target.HttpContext.Response.StatusCode, 201);

            result = target.Diff(index);
            Assert.AreEqual((result as JsonResult).Data.ToString(), "{ diffResultType = Equals }");
        }

        [TestMethod]
        public void DiferentBodies()
        {
            const int index = 1;

            var target = GetTarget();
            var result = target.DiffSide(index, "left", body: new EndpointController.BodyContent { data = "AAAAAA==" });
            Assert.AreEqual(target.HttpContext.Response.StatusCode, 201);

            result = target.DiffSide(index, "right", body: new EndpointController.BodyContent { data = "AQABAQ==" });
            Assert.AreEqual(target.HttpContext.Response.StatusCode, 201);

            result = target.Diff(index);
            Assert.AreEqual((result as JsonResult).Data.ToString(),
              "{ diffResultType = { \"diffResultType\": \"ContentDoNotMatch\", \"diffs\": [{ \"offset\":1, \"length\":1,{ \"offset\":3, \"length\":1,{ \"offset\":5, \"length\":1]} }");
        }
    }
}
