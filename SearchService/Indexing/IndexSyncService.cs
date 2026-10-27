using System.Diagnostics;
using Npgsql;
using SearchService.Data;
using SearchService.Feed;
using SearchService.Personal;

namespace SearchService.Indexing;

public sealed class SearchIndexOptions
{
    /// <summary>How often the sources are re-read. Each cycle is a full snapshot, so deletions are picked up too.</summary>
    public int SyncIntervalSeconds { get; set; } = 60;
}

/// <summary>
/// Keeps searchdb in step with the source databases. The services publish no change events,
/// so the index is rebuilt from read-only snapshots: every cycle reads each source in full and
/// writes only the differences. A failing source keeps its previous documents.
/// </summary>
public sealed class IndexSyncService(
    NpgsqlDataSource indexDb,
    IEnumerable<ISearchSource> sources,
    SearchIndexWriter writer,
    IndexSyncTrigger trigger,
    TagCatalogCache catalogCache,
    SubscriptionMatcher subscriptions,
    IInteractionSource interactions,
    InterestProfileWriter profiles,
    Microsoft.Extensions.Options.IOptions<SearchIndexOptions> options,
    ILogger<IndexSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await MigrateWithRetryAsync(stoppingToken);

        var interval = TimeSpan.FromSeconds(Math.Max(5, options.Value.SyncIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncOnceAsync(stoppingToken);
            await trigger.WaitAsync(interval, stoppingToken);
        }
    }

    private async Task MigrateWithRetryAsync(CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await SearchMigrations.ApplyAsync(indexDb, logger, ct);
                trigger.MarkSchemaReady();
                return;
            }
            catch (Exception ex) when (ex is NpgsqlException or TimeoutException && !ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "searchdb is not reachable yet (attempt {Attempt})", attempt);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, 2 * attempt)), ct);
            }
        }
    }

    public async Task SyncOnceAsync(CancellationToken ct)
    {
        var started = Stopwatch.StartNew();
        var changed = 0;
        TagCatalog catalog;
        try
        {
            catalog = await writer.LoadTagCatalogAsync(ct);
            catalogCache.Set(catalog);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Cannot read the tag catalog; search index sync skipped");
            return;
        }

        foreach (var source in sources)
        {
            try
            {
                var docs = await source.ReadAllAsync(ct);
                var rows = docs.Select(d => SearchIndexWriter.BuildRow(d, catalog)).ToList();
                var n = await writer.SyncKindAsync(source.Kind, rows, ct);
                changed += n;
                if (n > 0) logger.LogInformation("Search index: {Kind} {Changed} changed of {Total}", source.Kind, n, rows.Count);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Search index: source {Kind} failed, its previous documents are kept", source.Kind);
                try { await writer.RecordFailureAsync(source.Kind, ex.Message, ct); }
                catch (Exception inner) { logger.LogDebug(inner, "Cannot record the failure of {Kind}", source.Kind); }
            }
        }

        if (changed > 0 || !trigger.TermsBuilt)
        {
            try
            {
                await writer.RebuildTermsAsync(ct);
                await writer.RebuildTagsAsync(catalog, ct);
                await writer.RebuildRolesAsync(catalog, ct);
                trigger.TermsBuilt = true;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Search index: vocabulary, tag or role rebuild failed");
            }
        }

        // Interactions change without the documents changing, so the interest profiles are rebuilt every cycle.
        try
        {
            await profiles.SyncAsync(await interactions.ReadAllAsync(ct), ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Search feed: interactions could not be read, the previous profiles are kept");
            try { await writer.RecordFailureAsync("interaction", ex.Message, ct); }
            catch (Exception inner) { logger.LogDebug(inner, "Cannot record the failure of interactions"); }
        }

        // New documents may match saved searches with notify on.
        if (changed > 0)
        {
            try
            {
                var notified = await subscriptions.RunAsync(ct);
                if (notified > 0) logger.LogInformation("Search subscriptions: {Count} notifications", notified);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Search subscriptions could not be matched");
            }
        }

        trigger.LastSyncDuration = started.Elapsed;
    }
}

/// <summary>Lets the API start a sync cycle now instead of waiting for the interval.</summary>
public sealed class IndexSyncTrigger
{
    private readonly SemaphoreSlim _signal = new(0, 1);
    private readonly TaskCompletionSource _schemaReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool TermsBuilt { get; set; }
    public TimeSpan? LastSyncDuration { get; set; }

    /// <summary>Completes once the searchdb schema exists; queries wait for it after a cold start.</summary>
    public Task SchemaReady => _schemaReady.Task;

    public void MarkSchemaReady() => _schemaReady.TrySetResult();

    public void Request()
    {
        try { _signal.Release(); }
        catch (SemaphoreFullException) { /* a cycle is already requested */ }
    }

    public async Task WaitAsync(TimeSpan timeout, CancellationToken ct)
    {
        try { await _signal.WaitAsync(timeout, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }
}
