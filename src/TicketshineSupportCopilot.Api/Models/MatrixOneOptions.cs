using MySqlConnector;

namespace TicketshineSupportCopilot.Api.Models;

public sealed record MatrixOneOptions(string Host, int Port, string User, string Password, string Database)
{
    public static MatrixOneOptions FromConfiguration(IConfiguration configuration)
    {
        return new MatrixOneOptions(
            Host: configuration["MATRIXONE_HOST"] ?? "127.0.0.1",
            Port: int.TryParse(configuration["MATRIXONE_PORT"], out var port) ? port : 6001,
            User: configuration["MATRIXONE_USER"] ?? "root",
            Password: configuration["MATRIXONE_PASSWORD"] ?? "111",
            Database: configuration["MATRIXONE_DATABASE"] ?? "ticketshine_rag");
    }

    public string CreateConnectionString(bool includeDatabase = true)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = Host,
            Port = (uint)Port,
            UserID = User,
            Password = Password,
            AllowUserVariables = true
        };

        if (includeDatabase)
        {
            builder.Database = Database;
        }

        return builder.ConnectionString;
    }
}
