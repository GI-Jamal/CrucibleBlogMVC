using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CrucibleBlogMVC.Helpers
{
    public class StringHelper
    {
        public static string BlogPostSlug(string? title)
        {
            string? output = RemoveAccents(title).ToLower();

            output = Regex.Replace(output, @"[^A-Za-z0-9\s-]", "");

            output = Regex.Replace(output, @"\s+", " ");

            output = Regex.Replace(output, @"\s", "-");

            return output;
        }

        private static string RemoveAccents(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return title!;
            }

            title = title.Normalize(NormalizationForm.FormD);

            char[] chars = title.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();

            return new string(chars).Normalize(NormalizationForm.FormC);
        }
    }
}
