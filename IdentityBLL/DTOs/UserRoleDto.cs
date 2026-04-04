namespace IdentityBLL.DTOs;

public class UserRoleDto
{
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = null!;
}