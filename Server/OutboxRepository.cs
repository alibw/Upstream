using System.Data;
using System.Text.Json.Nodes;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Server;

public class OutboxRepository
{
    private string _connectionString;

    private IDbConnection GetConnection() => new SqlConnection(_connectionString);
    
    public OutboxRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public int Insert(JsonNode input)
    {
        using var connection = GetConnection();
        var outboxRecord = new
        {
            Url = input["url"],
            Payload = input["payload"],
            Request = input["request"],
            Prefix = input["prefix"]
        };
        var insertScript = """
                     INSERT INTO [dbo].[outbox]
                                ([url]
                                ,[payload]
                                ,[request]
                                ,[request_status]
                                ,[response]
                                ,[is_handled]
                                ,[request_date]
                                ,[ignored]
                                ,[is_processing]
                                ,[prefix])
                          VALUES
                          (@Url, @Payload, @Request, null, null, false, null, false, false, @Prefix)
                     """;
        return connection.Execute(insertScript, outboxRecord);
    }
}