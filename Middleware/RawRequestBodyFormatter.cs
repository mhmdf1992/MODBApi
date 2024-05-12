using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Threading.Tasks;
using System.IO;

namespace MODB.Api.Middleware{
    public class RawRequestBodyFormatter : InputFormatter{
        public RawRequestBodyFormatter(){
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
        }

        /// <summary>
        /// Allow text/plain, application/octet-stream and no content type to
        /// be processed
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Boolean CanRead(InputFormatterContext context){
            if (context == null) throw new ArgumentNullException(nameof(context));

            var contentType = context.HttpContext.Request.ContentType;
            if (string.IsNullOrEmpty(contentType) || contentType.StartsWith("text/plain") || contentType.StartsWith("application/json"))
                return true;

            return false;
        }

        /// <summary>
        /// Handle text/plain or no content type for string results
        /// Handle application/octet-stream for byte[] results
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context){
            var request = context.HttpContext.Request;
            var contentType = context.HttpContext.Request.ContentType;

            if (string.IsNullOrEmpty(contentType) || contentType.StartsWith("text/plain") || contentType.StartsWith("application/json"))
            {
                var ms = new MemoryStream();
                await request.BodyReader.CopyToAsync(ms);
                return await InputFormatterResult.SuccessAsync(ms);
            }

            return await InputFormatterResult.FailureAsync();
        }
    }
}