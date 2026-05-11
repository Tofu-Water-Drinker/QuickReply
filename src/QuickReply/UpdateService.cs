using System.Net.Http;
using System.Text.Json;

namespace QuickReply;

public class UpdateService : IDisposable
{
    public const string RepoOwner = "Tofu-Water-Drinker";
    public const string RepoName  = "QuickReply";

    public static readonly string ReleasesPageUrl =
        $"https://github.com/{RepoOwner}/{RepoName}/releases";

    private static readonly string GitHubApiLatestUrl =
        $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

    private readonly HttpClient _http;

    public UpdateService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("QuickReply-UpdateCheck/1.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public Version CurrentVersion =>
        typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0);

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync(GitHubApiLatestUrl, ct).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return UpdateCheckResult.Error(CurrentVersion,
                    "No releases have been published yet.", ReleasesPageUrl);
            }
            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.Error(CurrentVersion,
                    $"GitHub returned {(int)response.StatusCode} {response.ReasonPhrase}.",
                    ReleasesPageUrl);
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() : null;
            var url = root.TryGetProperty("html_url", out var u) ? u.GetString() : ReleasesPageUrl;

            if (string.IsNullOrWhiteSpace(tag))
            {
                return UpdateCheckResult.Error(CurrentVersion,
                    "The latest release has no tag.", url ?? ReleasesPageUrl);
            }
            if (!TryParseVersion(tag, out var latest))
            {
                return UpdateCheckResult.Error(CurrentVersion,
                    $"Could not parse version from tag \"{tag}\".", url ?? ReleasesPageUrl);
            }

            return new UpdateCheckResult(
                Success: true,
                UpdateAvailable: IsNewer(latest, CurrentVersion),
                CurrentVersion: CurrentVersion,
                LatestVersion: latest,
                ReleaseTag: tag,
                ReleaseUrl: url ?? ReleasesPageUrl,
                ErrorMessage: null);
        }
        catch (OperationCanceledException)
        {
            return UpdateCheckResult.Error(CurrentVersion,
                "The update check was cancelled or timed out.", ReleasesPageUrl);
        }
        catch (Exception ex)
        {
            return UpdateCheckResult.Error(CurrentVersion, ex.Message, ReleasesPageUrl);
        }
    }

    private static bool TryParseVersion(string tag, out Version version)
    {
        var v = tag.TrimStart('v', 'V').Trim();
        // Strip pre-release / build metadata (e.g. "1.0.0-beta", "1.0.0+sha")
        var stop = v.IndexOfAny(new[] { '-', '+' });
        if (stop > 0) v = v[..stop];
        return Version.TryParse(v, out version!);
    }

    private static bool IsNewer(Version latest, Version current)
    {
        // Normalise missing components (Version represents missing parts as -1)
        // so a 3-part tag like "v1.0.0" compares cleanly against a 4-part
        // AssemblyVersion like 1.0.0.0.
        int latMajor = Math.Max(latest.Major, 0);
        int latMinor = Math.Max(latest.Minor, 0);
        int latBuild = Math.Max(latest.Build, 0);
        int latRev   = Math.Max(latest.Revision, 0);
        int curMajor = Math.Max(current.Major, 0);
        int curMinor = Math.Max(current.Minor, 0);
        int curBuild = Math.Max(current.Build, 0);
        int curRev   = Math.Max(current.Revision, 0);

        if (latMajor != curMajor) return latMajor > curMajor;
        if (latMinor != curMinor) return latMinor > curMinor;
        if (latBuild != curBuild) return latBuild > curBuild;
        return latRev > curRev;
    }

    public void Dispose() => _http.Dispose();
}

public record UpdateCheckResult(
    bool     Success,
    bool     UpdateAvailable,
    Version? CurrentVersion,
    Version? LatestVersion,
    string?  ReleaseTag,
    string?  ReleaseUrl,
    string?  ErrorMessage)
{
    public static UpdateCheckResult Error(Version? currentVersion, string message, string fallbackUrl) =>
        new(Success: false, UpdateAvailable: false,
            CurrentVersion: currentVersion, LatestVersion: null,
            ReleaseTag: null, ReleaseUrl: fallbackUrl, ErrorMessage: message);
}
