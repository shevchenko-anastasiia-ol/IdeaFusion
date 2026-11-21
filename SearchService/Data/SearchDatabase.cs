using System.Reflection;
using System.Text.RegularExpressions;
using Npgsql;

namespace SearchService.Data;

/// <summary>Connections of the service: its own index database and the read-only source databases.</summary>
public sealed class SearchDatabase
{
    public SearchDatabase(NpgsqlDataSource index) => Index = index;

    /// <summary>searchdb, the only database this service writes to.</summary>
    public NpgsqlDataSource Index { get; }

    /// <summary>
    /// A source database opened read-only: every transaction of the session is read-only,
    /// so the indexer cannot change the data of another service even by mistake.
    /// </summary>
    public static NpgsqlDataSource ReadOnly(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Options = "-c default_transaction_read_only=on",
            ApplicationName = "searchservice-indexer",
        };
        return NpgsqlDataSource.Create(builder);
    }

    /// <summary>Connections kept open to searchdb, each with the Ukrainian dictionary already loaded.</summary>
    public const int WarmConnections = 4;

    /// <summary>
    /// searchdb with a minimum pool: Postgres loads the hunspell dictionary (~25 MB) into every new session,
    /// which costs about 0.4 s on its first full-text query. Kept-open, warmed connections spare searches that.
    /// </summary>
    public static NpgsqlDataSource IndexSource(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.MinPoolSize < WarmConnections) builder.MinPoolSize = WarmConnections;
        return NpgsqlDataSource.Create(builder);
    }

    /// <summary>Opens the minimum pool at once and loads the dictionary into each of its sessions.</summary>
    public static async Task WarmUpAsync(NpgsqlDataSource index, CancellationToken ct)
    {
        var connections = new List<NpgsqlConnection>();
        try
        {
            for (var i = 0; i < WarmConnections; i++) connections.Add(await index.OpenConnectionAsync(ct));
            await Task.WhenAll(connections.Select(async c =>
            {
                await using var cmd = new NpgsqlCommand("SELECT to_tsvector('ideafusion', 'прогрів пошуку')", c);
                await cmd.ExecuteScalarAsync(ct);
            }));
        }
        finally
        {
            foreach (var c in connections) await c.DisposeAsync();
        }
    }
}

/// <summary>Applies Data/Migrations/NNN_*.sql in order, each once, under an advisory lock.</summary>
public static partial class SearchMigrations
{
    private const long LockId = 0x5EA2C4;

    [GeneratedRegex(@"\.(\d{3})_[^.]+\.sql$")]
    private static partial Regex ScriptName();

    public static async Task ApplyAsync(NpgsqlDataSource db, ILogger logger, CancellationToken ct)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        await using (var cmd = new NpgsqlCommand("SELECT pg_advisory_lock(@id)", conn))
        {
            cmd.Parameters.AddWithValue("id", LockId);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        try
        {
            await using (var cmd = new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS schema_migrations (version int PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())", conn))
                await cmd.ExecuteNonQueryAsync(ct);

            var applied = new HashSet<int>();
            await using (var cmd = new NpgsqlCommand("SELECT version FROM schema_migrations", conn))
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
                while (await reader.ReadAsync(ct)) applied.Add(reader.GetInt32(0));

            var assembly = Assembly.GetExecutingAssembly();
            var scripts = assembly.GetManifestResourceNames()
                .Select(name => (name, match: ScriptName().Match(name)))
                .Where(s => s.match.Success)
                .Select(s => (s.name, version: int.Parse(s.match.Groups[1].Value)))
                .OrderBy(s => s.version);

            foreach (var (name, version) in scripts)
            {
                if (applied.Contains(version)) continue;

                string sql;
                await using (var stream = assembly.GetManifestResourceStream(name)!)
                using (var text = new StreamReader(stream))
                    sql = await text.ReadToEndAsync(ct);

                await using var tx = await conn.BeginTransactionAsync(ct);
                await using (var cmd = new NpgsqlCommand(sql, conn, tx))
                    await cmd.ExecuteNonQueryAsync(ct);
                await using (var cmd = new NpgsqlCommand("INSERT INTO schema_migrations (version) VALUES (@v)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("v", version);
                    await cmd.ExecuteNonQueryAsync(ct);
                }
                await tx.CommitAsync(ct);
                logger.LogInformation("Applied search schema migration {Version}", version);
            }
        }
        finally
        {
            await using var cmd = new NpgsqlCommand("SELECT pg_advisory_unlock(@id)", conn);
            cmd.Parameters.AddWithValue("id", LockId);
            await cmd.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }
}
