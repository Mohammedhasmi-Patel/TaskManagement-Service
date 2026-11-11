namespace TaskManagement.Application.Common.Options;

public class AppOptions
{
    public const string SectionName = "App";

    public string BackendUrl { get; set; } = string.Empty;

    public string? BuildFileUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        if (Uri.TryCreate(relativePath, UriKind.Absolute, out _))
        {
            return relativePath;
        }

        var baseUrl = BackendUrl.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return relativePath;
        }

        return $"{baseUrl}/{relativePath.TrimStart('/')}";
    }
}

