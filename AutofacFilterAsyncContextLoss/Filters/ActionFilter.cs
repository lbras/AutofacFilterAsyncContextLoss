using Autofac.Integration.WebApi;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace AutofacFilterAsyncContextLoss.Filters
{
    public class ActionFilter : IAutofacActionFilter
    {
        public Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
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
        }
    }
}