using System.Diagnostics;
using System.Threading;
using System.Web;
using System.Web.Http;

namespace AutofacFilterAsyncContextLoss.Controllers
{
    public class ContinuationController : ApiController
    {
        [HttpGet, Route("continuation")]
        public IHttpActionResult Get()
        {
            Debug.WriteLine(
                $"{Thread.CurrentThread.ManagedThreadId}: at action; " +
                $"HttpContext.Current is null: {HttpContext.Current == null}; " +
                $"SynchronizationContext.Current is null: {SynchronizationContext.Current == null}");

            return Ok();
        }
    }
}
