namespace ManagementClient.Core.Common.Models;

public class ZipcodeSearchRequest
{
    public int Radius { get; set; }
    public string ZipCode { get; set; } = string.Empty;
}
