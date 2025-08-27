namespace ManagementClient.Core.Common.Models;

public class UserProfile
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
