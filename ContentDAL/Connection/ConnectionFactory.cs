using System.Data;
using Npgsql;

namespace ContentDAL.Connection;

public class ConnectionFactory : IConnectionFactory
{
    private readonly string _connectionString;

    public ConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
    
}