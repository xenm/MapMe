using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MapMe.Utils;

public static class Normalization
{
    public static IReadOnlyList<string> ToNorm(params string[]? values) => ToNorm((IEnumerable<string>?)values);

    public static IReadOnlyList<string> ToNorm(IEnumerable<string>? values)
    {
        if (values is null) return Array.Empty<string>();
        return values
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(Normalize)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static string Normalize(string value)
    {
        var s = value.Trim().ToLowerInvariant();
        s = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c)) continue;
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
