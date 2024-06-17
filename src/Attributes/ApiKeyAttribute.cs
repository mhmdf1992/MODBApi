using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using MO.MODB;
using System;
using System.Threading.Tasks;

namespace MO.MODBApi.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private const string APIKEYNAME = "ApiKey";
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
            {
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.Unauthorized, System.Net.HttpStatusCode.Unauthorized.ToString(), "Api key not provided in header");
            }

            var sysDBCollection = context.HttpContext.RequestServices.GetRequiredService<IDBCollection>();
            try{
                if (!sysDBCollection.Get("users").Exists(extractedApiKey.ToString()))
                {
                    throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.Unauthorized, System.Net.HttpStatusCode.Unauthorized.ToString(), "Api key not valid");
                }
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, context.HttpContext.TraceIdentifier);
            }
            await next();
        }
    }
}