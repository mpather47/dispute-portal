using System.Net;
using System.Text.RegularExpressions;

namespace DisputePortal.Api.Helpers;

public static partial class InputSanitizer
{
    [GeneratedRegex("<[^>]*>", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex HtmlTagPattern();

    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Decode encoded entities (e.g. &lt;script&gt;) before stripping
        var decoded = WebUtility.HtmlDecode(input);
        var stripped = HtmlTagPattern().Replace(decoded, string.Empty);
        return stripped.Trim();
    }
}
