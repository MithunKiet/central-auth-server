namespace AuthServer.Models;

public class ConsentViewModel
{
    public string ApplicationName { get; set; } = string.Empty;
    public IEnumerable<string> Scopes { get; set; } = [];
}
