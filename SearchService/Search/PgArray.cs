using System.Data;
using Dapper;
using Npgsql;

namespace SearchService.Search;

/// <summary>
/// Passes an array to Postgres as one array parameter. Dapper otherwise expands any list into
/// "(@p1, @p2, ...)", which is right for IN but not for unnest(@p) or tags &amp;&amp; @p.
/// </summary>
internal sealed class PgArray<T>(T[] values) : SqlMapper.ICustomQueryParameter
{
    public void AddParameter(IDbCommand command, string name) =>
        command.Parameters.Add(new NpgsqlParameter(name, values));
}

internal static class PgArray
{
    public static PgArray<T> Of<T>(IEnumerable<T> values) => new(values.ToArray());
}
