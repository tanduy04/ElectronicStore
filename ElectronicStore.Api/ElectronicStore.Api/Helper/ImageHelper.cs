namespace ElectronicStore.Api.Helper;


using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class ImageHelper
{

    public static string NormalizeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "file";

        // To lower
        var s = input.ToLowerInvariant();

        // Remove diacritics
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        s = sb.ToString().Normalize(NormalizationForm.FormC);

        // replace spaces by underscore
        s = Regex.Replace(s, @"\s+", "_");

        // remove invalid chars (keep a-z,0-9, underscore and dot)
        s = Regex.Replace(s, @"[^a-z0-9._\-]", "");

        // trim underscores/dots at ends
        s = s.Trim('_', '.');

        if (string.IsNullOrWhiteSpace(s)) s = "file";
        return s;
    }

    public static void DeleteFileIfExists(string path)
    {
        try
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
        catch
        {
            // optionally log
        }
    }
}

