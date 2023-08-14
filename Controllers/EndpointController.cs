using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Web.Mvc;

namespace DiffingApiTask.Controllers
{
    //[RoutePrefix("v1")]
    public class EndpointController : Controller
    {
        // quick and dirty solution for persistence: TempData;
        // should be persisted, locked before reading, unlock after updating and retried when locked

        private const string Right = "right";
        private const string Left = "left";

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
                return StatusAndInfo(200, "SizeDoNotMatch");
            }

            // diff
            if (left == right)
            {
                // if removed, see end of GetDiffInfo()
                return StatusAndInfo(200, "Equals");
            }

            // convert from base64
            left = Encoding.UTF8.GetString(Convert.FromBase64String(left));
            right = Encoding.UTF8.GetString(Convert.FromBase64String(right));

            // actual diff
            var diffMessage = GetDiffInfo(left, right);
            return this.Json(new { diffResultType = "ContentDoNotMatch", diffs = diffMessage }, JsonRequestBehavior.AllowGet);
        }

        private static string GetDiffInfo(string left, string right)
        {
            var diff = new StringBuilder();     // offset/length array
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
                    diff.AppendFormat("{{ \"offset\":{0}, \"length\":{1}}},", fromIndex, i - fromIndex);
                }
                else
                {
                    // start counting
                    fromIndex = i;
                }

                lastEqual = areEqual;
            }

            if (!lastEqual)
            {
                // trailing open difference
                diff.AppendFormat("{{ \"offset\":{0}, \"length\":{1}}},", fromIndex, left.Length - fromIndex);
            }

            // checking for diff.Length == 0 is superfluous, since it is checked for earlier
            return "[" + diff.ToString(0, diff.Length - 1) + "]";
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

            if (string.Compare(side, Left, true) == 0)
            {
                SetLeftIndex(index, content);
            }
            else if (string.Compare(side, Right, true) == 0)
            {
                SetRightIndex(index, content);
            }
            else
            {
                return HttpNotFound();
            }

            return StatusAndInfo(201, string.Empty);
        }

        /// <summary>
        /// set status and return a JSON object containing the info 
        /// </summary>
        /// <param name="StatusCode"></param>
        /// <param name="info"></param>
        /// <returns>JSON object</returns>
        private ActionResult StatusAndInfo(int StatusCode, string info)
        {
            this.HttpContext.Response.StatusCode = StatusCode;
            return this.Json(new { diffResultType = info }, JsonRequestBehavior.AllowGet);
        }

        #region persistence access 
        private string GetRightIndex(int index) => GetIndex(index, Right);
        private string GetLeftIndex(int index) => GetIndex(index, Left);
        private string GetIndex(int index, string suffix) => TempData.ContainsKey(index + suffix) ? TempData[index + suffix] as string : string.Empty;

        private void SetRightIndex(int index, string value) => SetIndex(index, Right, value);
        private void SetLeftIndex(int index, string value) => SetIndex(index, Left, value);
        private void SetIndex(int index, string suffix, string value) => TempData[index + suffix] = value;
        #endregion

        public class BodyContent
        {
            public string data { get; set; }
        }
    }
}