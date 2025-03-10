using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json.Nodes;
using Server;

namespace Dummy;

public class RecordMiddleware
{
    private readonly RequestDelegate _next;

    private string OutputProjectName;

    public RecordMiddleware(RequestDelegate next, string outputProjectName)
    {
        _next = next;
        OutputProjectName = outputProjectName;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        string requestBodyRead = await new StreamReader(context.Request.Body, Encoding.UTF8).ReadToEndAsync();
        context.Request.Body.Position = 0;
        var requestPath = context.Request.Path;
        var requestQueryString = context.Request.QueryString;
        var responseCaptureStream = new MemoryStream();
        var originalResponseBody = context.Response.Body;
        context.Response.Body = responseCaptureStream;
        string responseBody = "";
        bool hasError = false;
        ExceptionDispatchInfo dispatchInfo = null;
        try
        {
            await _next(context);
            responseBody = await responseCaptureStream.ReadAsString();
        }
        catch (Exception e)
        {
            responseBody = e.ToString();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            hasError = true;
            dispatchInfo = ExceptionDispatchInfo.Capture(e);
        }
        finally
        {
            var requestMethod = context.Request.Method;
            var requestBody = string.IsNullOrEmpty(requestBodyRead)
                ? ""
                : JsonNode.Parse(requestBodyRead).ToString();
            var requestHeadersKeyValuePair = context.Request.Headers.Select(kvp => $"{kvp.Key}: {kvp.Value}");
            var responseHeadersValuePair = context.Response.Headers.Select(kvp => $"{kvp.Key}: {kvp.Value}");
            string fileData = $"""
                               {requestMethod} {context.Request.Scheme}://{context.Request.Host}{requestPath}{requestQueryString}
                               {string.Join(Environment.NewLine, requestHeadersKeyValuePair!)}

                               {requestBody}

                               /*
                               {context.Request.Protocol} {context.Response.StatusCode}{(responseHeadersValuePair.Any() ? Environment.NewLine : "")}{string.Join(Environment.NewLine, responseHeadersValuePair!)}{(string.IsNullOrEmpty(responseBody) ? "" : $"{Environment.NewLine}{Environment.NewLine}")}{responseBody}
                                */
                               """;
            var path = $"{OutputProjectName}/Records";

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var dateTimeNow = $"{DateTime.Now.Date:yyyy-MM-dd} {DateTime.Now:HH-mm-ss}";
            var routeParameter = context.GetRouteValue("anything")?.ToString();
            using (var fileStream = File.Create($"{path}/{dateTimeNow}-{routeParameter}-{requestMethod}.http"))
            {
                byte[] file = new UTF8Encoding(true).GetBytes(fileData);
                await fileStream.WriteAsync(file);
            }

            await context.Response.RestoreOriginalResponseBody(originalResponseBody, responseCaptureStream);
            if (hasError)
                dispatchInfo.Throw();
        }
    }
}

    public static class RecordMiddlewareExtensions
    {
        public static IApplicationBuilder UseRecordMiddleware(
            this IApplicationBuilder builder, string outputProjectName)
        {
            return builder.UseMiddleware<RecordMiddleware>(outputProjectName);
        }
    }