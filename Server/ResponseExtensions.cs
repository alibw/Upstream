using System.Text;

namespace Server;

public static class ResponseExtensions
{
    public static async Task RestoreOriginalResponseBody(this HttpResponse response, Stream originalResponseBody,
        Stream responseCaptureStream)
    {
        responseCaptureStream.Seek(0, SeekOrigin.Begin);
        await responseCaptureStream.CopyToAsync(originalResponseBody);
        response.Body = originalResponseBody;
    }

    public static async void WriteResponseBody(this HttpResponse response, Stream originalBody, string newResponseBody)
    {
        byte[] responseBodyBytes = Encoding.UTF8.GetBytes(newResponseBody);
        MemoryStream memoryStream = new MemoryStream(responseBodyBytes);
        await memoryStream.CopyToAsync(originalBody);
        response.Body = originalBody;
    }
}