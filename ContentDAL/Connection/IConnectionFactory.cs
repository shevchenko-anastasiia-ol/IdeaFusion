using System.Data;

namespace ContentDAL.Connection;

public interface IConnectionFactory
{
    IDbConnection CreateConnection();
}