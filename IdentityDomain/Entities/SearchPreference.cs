namespace IdentityServiceDomain.Entities;

public class SearchPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<string> SearchHistory { get; set; } = new();
    public List<SavedFilter> SavedFilters { get; set; } = new();
}

public class SavedFilter
{
    public string Name { get; set; } = null!;
    public string QueryJson { get; set; } = null!;
}
