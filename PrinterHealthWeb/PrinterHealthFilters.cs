using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

namespace PrinterHealthWeb
{
    public static class PrinterHealthFilters
    {
        internal static readonly Regex SoftLineBreakBeforeRegex = new Regex("[-_.]+");

        [Pure]
        public static string SoftBreaks(string s)
        {
            return SoftLineBreakBeforeRegex.Replace(s, match => "<wbr/>" + match.Value);
        }
    }
}
