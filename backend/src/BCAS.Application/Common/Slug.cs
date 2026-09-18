using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BCAS.Application.Common;

public static partial class Slug
{
    /// <summary>Turns "BCAS Enrollment is Open!" into "bcas-enrollment-is-open".</summary>
    public static string From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalised = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalised.Length);

        foreach (var c in normalised)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        var slug = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        slug = NonSlugCharacters().Replace(slug, "-");
        slug = RepeatedDashes().Replace(slug, "-");

        return slug.Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharacters();

    [GeneratedRegex("-{2,}")]
    private static partial Regex RepeatedDashes();
}
