using Autofac;
using Autofac.Integration.WebApi;
using AutofacFilterAsyncContextLoss.Controllers;
using AutofacFilterAsyncContextLoss.Filters;
using System.Reflection;
using System.Web.Http;

namespace AutofacFilterAsyncContextLoss
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Autofac container builder for dependency injection
            var builder = new ContainerBuilder();

            // Register controllers and filter provider
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            builder.RegisterWebApiFilterProvider(config);

            // Register action filters
            builder
                .RegisterType<ActionFilter>()
                .AsWebApiActionFilterFor<ReproController>()
                .InstancePerRequest();
            builder
                .RegisterType<ContinuationFilter>()
                .AsWebApiActionFilterFor<ContinuationController>()
                .InstancePerRequest();

            // Build the Autofac dependency resolver
            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            // Web API routes
            config.MapHttpAttributeRoutes();
        }
    }
}
