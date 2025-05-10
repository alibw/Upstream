using System.Text.Json.Nodes;
using Dummy;
using Server;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseMiddleware<RecordMiddleware>("Server");

var lastResponse = string.Empty;

var connectionString = "Server=NUC;Database=Northwind;Integrated Security=True;TrustServerCertificate=true;";

app.MapPost("/Insert", (JsonNode requestBody, HttpContext context) =>
{
    var dbRepository = new DbRepository(connectionString);
    return dbRepository.Insert( requestBody["type"].ToString(), requestBody["payload"]);
});

app.MapPost("/Products", () =>
{
    var dataFileText = JsonNode.Parse(File.ReadAllText("C:\\Users\\Ali\\RiderProjects\\Upstream\\Server\\Data.json")).AsArray();
    var products = dataFileText.First()["Products"].AsArray();
    return products;
});

app.MapPost("/Store", (HttpContext context) =>
{
    var requestBody = context.Request.Body.ReadAsString().Result;
    lastResponse = string.IsNullOrEmpty(requestBody) ? string.Empty : requestBody;
    return Results.Ok("Response stored successfully.");
});

app.MapGet("/Retrieve", () =>
{
    var response = lastResponse;
    lastResponse = string.Empty;
    return Results.Content(response, "application/json");
});

app.MapPost("/StoreAndRetrieve", (HttpContext context) =>
{
    var requestBody = context.Request.Body.ReadAsString().Result;
    return Results.Content(requestBody, "application/json");
});

app.MapPost("/AwaitAndRetrieve", (HttpContext context) =>
{ 
    Thread.Sleep(1000);
    lastResponse = string.IsNullOrEmpty(context.Request.Body.ReadAsString().Result) ? string.Empty : lastResponse;
    return Results.Ok(lastResponse);
});

app.Run();
