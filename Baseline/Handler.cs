using System.Web;

namespace Baseline
{
    public class Handler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            context.Response.Write("Hello world");
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}
