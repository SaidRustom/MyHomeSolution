namespace BlazorUI.Infrastructure.Configuration;

public sealed class ApiSettings
{
    public const string SectionName = "Api";

    public string BaseUrl { get; set; } = string.Empty;
}
