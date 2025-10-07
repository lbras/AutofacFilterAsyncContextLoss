using Autofac.Integration.WebApi;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;

namespace AutofacFilterAsyncContextLoss.Filters
{
    public class ContinuationFilter : IAutofacContinuationActionFilter
    {
        public async Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> next)
        {
            Debug.WriteLine(
                $"{Thread.CurrentThread.ManagedThreadId}: before await; " +
                $"HttpContext.Current is null: {HttpContext.Current == null}; " +
                $"SynchronizationContext.Current is null: {SynchronizationContext.Current == null}");

            await Task.Delay(3000);

            Debug.WriteLine(
                $"{Thread.CurrentThread.ManagedThreadId}: after await; " +
                $"HttpContext.Current is null: {HttpContext.Current == null}; " +
                $"SynchronizationContext.Current is null: {SynchronizationContext.Current == null}");

            return await next();
        }
    }
}