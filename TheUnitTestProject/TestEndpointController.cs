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

        [DataTestMethod]
        [DataRow("AAAAAA==", "AAAAAA==", "{ diffResultType = Equals }")]
        [DataRow("AAAAAA==", "AQABAQ==", "{ diffResultType = ContentDoNotMatch, diffs = [{ \"offset\":0, \"length\":1},{ \"offset\":2, \"length\":2}] }")]
        public void MultipleBodies(string left,string right,string diffResult)
        {
            var index = new Random().Next(1000); 

            var target = GetTarget();

            var result = target.DiffSide(index, "left", body: new EndpointController.BodyContent { data = left });
            Debug.WriteLine("diff1:[" + GetProperty((result as JsonResult).Data, "diffResultType") + "]"); // = []
            Assert.AreEqual(target.HttpContext.Response.StatusCode, 201);

            result = target.DiffSide(index, "right", body: new EndpointController.BodyContent { data = right });
            Debug.WriteLine("diff2:[" + GetProperty((result as JsonResult).Data, "diffResultType") + "]"); // = []
            Assert.AreEqual(target.HttpContext.Response.StatusCode, 201);

            result = target.Diff(index);
            Debug.WriteLine("diff3:[" + GetProperty((result as JsonResult).Data, "diffResultType") + "]"); // = []
            Debug.WriteLine("3rd result.Data.diffs:[" + GetProperty((result as JsonResult).Data, "diffs") + "]"); // = []
            Assert.AreEqual(diffResult, (result as JsonResult).Data.ToString());
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void IntermingledIndexes(bool sameInstance)
        {
            var rnd = new Random();
            var index1 = rnd.Next(1000);
            var index2 = rnd.Next(1000);
            if (index1 == index2) index2++;

            var target1 = GetTarget();
            var target2 = sameInstance ? target1 : GetTarget();

            var result1 = target1.DiffSide(index1, "left", body: new EndpointController.BodyContent { data = "AAAAAA==" });
            Assert.AreEqual(target1.HttpContext.Response.StatusCode, 201);

            var result2 = target2.DiffSide(index2, "left", body: new EndpointController.BodyContent { data = "AAAAAA==" });
            Assert.AreEqual(target2.HttpContext.Response.StatusCode, 201);

            result1 = target1.DiffSide(index1, "right", body: new EndpointController.BodyContent { data = "AAAAAA==" });
            Assert.AreEqual(target1.HttpContext.Response.StatusCode, 201);

            result2 = target2.DiffSide(index2, "right", body: new EndpointController.BodyContent { data = "AQABAQ==" });
            Assert.AreEqual(target2.HttpContext.Response.StatusCode, 201);

            result1 = target1.Diff(index1);
            Assert.AreEqual("{ diffResultType = Equals }", (result1 as JsonResult).Data.ToString());

            result2 = target2.Diff(index2);
            Assert.AreEqual("{ diffResultType = ContentDoNotMatch, diffs = [{ \"offset\":0, \"length\":1},{ \"offset\":2, \"length\":2}] }", (result2 as JsonResult).Data.ToString());
        }

        private static string GetProperty(object target, string propertyName) =>
            target?.GetType().GetProperty(propertyName)?.GetValue(target)?.ToString();        
    }
}
