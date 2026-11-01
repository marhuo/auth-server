using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AuthServer.Web.Shared;

public static partial class Slug
{
    public static string From(string name)
    {
        var text = name.Trim().ToLowerInvariant()
            .Replace("ä", "a")
            .Replace("ö", "o")
            .Replace("å", "a")
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return NonAlphanumeric().Replace(sb.ToString(), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
