using Grpc.Core;
using IdeaFusion.Grpc.Teams;

namespace ContentAPI.CopyDetection;

/// <summary>
/// Composite authorship (coursework chapter 1, section 1.1.4): a match with the uploader's own
/// work, or with a work of any team the uploader belongs to, is not plagiarism. The ML service
/// already skips the uploader and the post's own team; this filter drops matches with other
/// teams the uploader is a member of, which only CollaborationService knows.
/// </summary>
public interface IAuthorshipFilter
{
    Task<CopyCheckResult> ExcludeOwnWorksAsync(CopyCheckResult result, string uploaderId, CancellationToken ct = default);
}

public sealed class AuthorshipFilter : IAuthorshipFilter
{
    private readonly TeamGrpcService.TeamGrpcServiceClient _teams;
    private readonly ILogger<AuthorshipFilter> _logger;

    public AuthorshipFilter(TeamGrpcService.TeamGrpcServiceClient teams, ILogger<AuthorshipFilter> logger)
    {
        _teams = teams;
        _logger = logger;
    }

    public async Task<CopyCheckResult> ExcludeOwnWorksAsync(CopyCheckResult result, string uploaderId, CancellationToken ct = default)
    {
        if (result.Matches.Count == 0)
            return result;

        var membership = new Dictionary<string, bool>();
        var kept = new List<CopyMatch>();
        foreach (var match in result.Matches)
        {
            if (string.Equals(match.OwnerUserId, uploaderId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (match.TeamId is { Length: > 0 } teamId)
            {
                if (!membership.TryGetValue(teamId, out var isMember))
                    membership[teamId] = isMember = await IsMemberAsync(teamId, uploaderId, ct);
                if (isMember)
                    continue;
            }

            kept.Add(match);
        }

        return kept.Count == result.Matches.Count ? result : result.WithMatches(kept);
    }

    private async Task<bool> IsMemberAsync(string teamId, string userId, CancellationToken ct)
    {
        try
        {
            var response = await _teams.CheckUserIsMemberAsync(
                new CheckUserIsMemberRequest { TeamId = teamId, UserId = userId }, cancellationToken: ct);
            return response.IsMember;
        }
        catch (RpcException ex)
        {
            // Unknown membership keeps the match: the moderator sees it and decides, which is
            // safer than silently hiding a possible copy.
            _logger.LogWarning(ex, "Could not check membership of {UserId} in team {TeamId}", userId, teamId);
            return false;
        }
    }
}
