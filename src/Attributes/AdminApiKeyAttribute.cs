using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MO.MODBApi.DataModels.Sys;
using System;
using System.Threading.Tasks;

namespace MO.MODBApi.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private const string APIKEYNAME = "ApiKey";
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
            {
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.Unauthorized, System.Net.HttpStatusCode.Unauthorized.ToString(), "Api key not provided in header");
            }

            var appSettings = context.HttpContext.RequestServices.GetRequiredService<SystemSettings>();

            if (!appSettings.ApiKey.Equals(extractedApiKey))
            {
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.Unauthorized, System.Net.HttpStatusCode.Unauthorized.ToString(), "Api key not valid");
            }

            await next();
        }
    }
}