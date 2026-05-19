namespace IdentityBLL.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? UserName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? FullName { get; set; }
    public string? Specialization { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public DateTime CreatedAt { get; set; }
}