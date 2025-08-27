using System;
using Windows.System;

namespace ManagementClient.Core.Common.Models;

public class LoginResponse
{
    public UserProfile? User { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsSuccess => User != null && !string.IsNullOrEmpty(Message) &&
                       Message.Contains("successful", StringComparison.OrdinalIgnoreCase);
}
