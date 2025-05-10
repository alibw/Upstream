using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Server;

public class DbRepository
{
    private string _connectionString;

    private IDbConnection GetConnection() => new SqlConnection(_connectionString);
    
    public DbRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public int Insert(string tableName,JsonNode input)
    {
        using var connection = GetConnection();

        var properties = input.AsObject().ToDictionary(
            kv => kv.Key,
            kv => kv.Value.GetValueKind() == JsonValueKind.String ? kv.Value.GetValue<string>() : kv.Value.ToJsonString()).ToList();

        var sb = new StringBuilder();
        var valuesSb = new StringBuilder();

        var parameters = new DynamicParameters();
        for (int i = 0; i < properties.Count; i++)
        {
            var propertyName = properties[i].Key;
            parameters.Add(propertyName, properties[i].Value);

            if (properties.Count == 1)
            {
                sb.AppendLine($"([{propertyName}])");
                valuesSb.AppendLine($"(@{propertyName})");
            }
            else
            {
                if (i == 0)
                {
                    sb.AppendLine($"([{propertyName}],");
                    valuesSb.Append($"(@{propertyName}, ");
                }
                else if (i == properties.Count - 1)
                {
                    sb.AppendLine($" [{propertyName}])");
                    valuesSb.Append($"@{propertyName})");
                }
                else
                {
                    sb.AppendLine($" [{propertyName}],");
                    valuesSb.Append($"@{propertyName}, ");
                }
            }
        }
        
        var insertScript = $"""
                     INSERT INTO [dbo].[{tableName}]
                     {sb}
                     VALUES
                     {valuesSb}
                     """;
        
        return connection.Execute(insertScript,parameters);
    }
}