using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace DiffingApiTask.Controllers
{
    //[RoutePrefix("v1")]
    public class EndpointController : Controller
    {
        // quick and dirty solution : should be persisted, locked before reading, unlock after updating and retried when locked
        // left = Key, right = Value
        //private static Dictionary<int, KeyValuePair<string, string>> partialInfo = new Dictionary<int, KeyValuePair<string, string>>();

        [HttpGet]
        public ActionResult Test()
        {
            return this.Json(new { diffResultType = "Test" }, JsonRequestBehavior.AllowGet);
        }

        // v1/diff/{ID}
        //[HttpGet]
        public ActionResult Diff(int? id)
        {
            var index = id ?? int.Parse(this.Request.RequestContext.RouteData.Values["id:int"]?.ToString());

            var left = GetLeftIndex(index);
            var right = GetRightIndex(index);

            var hasData = !string.IsNullOrWhiteSpace(left) &&
                !string.IsNullOrWhiteSpace(right);

            if (!hasData)
            {
                return HttpNotFound();
            }

            // have equal lengths?
            if (left.Length != right.Length)
            {
                this.HttpContext.Response.StatusCode = 200;
                return this.Json(new { diffResultType = "SizeDoNotMatch" }, JsonRequestBehavior.AllowGet);
            }

            // diff
            if (left == right)
            {
                this.HttpContext.Response.StatusCode = 200;
                return this.Json(new { diffResultType = "Equals" }, JsonRequestBehavior.AllowGet);
            }

            var diff = new StringBuilder();
            var lastEqual = true;
            var fromIndex = 0;
            for (int i = 0; i < left.Length; i++)
            {
                var areEqual = left[i] == right[i];
                if (areEqual == lastEqual)
                {
                    continue;   // nothing to do
                }

                if (areEqual)
                {
                    // stop counting
                    diff.AppendFormat("{{ \"offset\":{0}, \"length\":{1},", fromIndex, i - fromIndex);
                }
                else
                {
                    // start counting
                    fromIndex = i;
                }

                lastEqual = areEqual;
            }

            var diffMessage = @"{ ""diffResultType"": ""ContentDoNotMatch"", ""diffs"": [" +
                diff.ToString(0, diff.Length - 1) + "]}";
            return this.Json(new { diffResultType = diffMessage }, JsonRequestBehavior.AllowGet);
        }

        //v1/diff/{ID}/side
        [HttpGet]
        //[HttpPut] // there is a debate as to why this is not working; I used PostMan and it worked
        public ActionResult DiffSide(int? id, string side, BodyContent body)
        {
            var index = id ?? int.Parse(this.Request.RequestContext.RouteData.Values["id:int"]?.ToString());

            var content = string.Empty;
            if (body != null)
            {
                content = body.data;
            }
            else if (Request.ContentLength > 0 &&
                 Request.Headers["Content-Type"] == "application/json")
            {
                using (Stream req = Request.InputStream)
                {
                    req.Seek(0, System.IO.SeekOrigin.Begin);
                    string bodyContent = new StreamReader(req).ReadToEnd();
                    content = JsonConvert.DeserializeObject<BodyContent>(bodyContent).data;
                }
            }

            if (string.Compare(side, "left", true) == 0)
            {
                SetLeftIndex(index, content);
            }
            else if (string.Compare(side, "right", true) == 0)
            {
                SetRightIndex(index, content);
            }
            else
            {
                this.HttpContext.Response.StatusCode = 404;
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            this.HttpContext.Response.StatusCode = 201;
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        private string GetRightIndex(int index) => GetIndex(index, "right");
        private string GetLeftIndex(int index) => GetIndex(index, "left");
        private string GetIndex(int index, string suffix) => TempData.ContainsKey(index + suffix) ? TempData[index + suffix] as string : string.Empty;

        private void SetRightIndex(int index, string value) => SetIndex(index, "right", value);
        private void SetLeftIndex(int index, string value) => SetIndex(index, "left", value);
        private void SetIndex(int index, string suffix, string value) => TempData[index + suffix] = value;

        public class BodyContent
        {
            public string data { get; set; }
        }
    }
}