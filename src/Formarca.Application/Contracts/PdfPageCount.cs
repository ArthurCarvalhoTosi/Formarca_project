using System.Text;

namespace Formarca.Application.Contracts;

internal static class PdfPageCount
{
    public static int FromPdf(byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        if (pdf.Length == 0)
        {
            return 1;
        }

        var text = Encoding.ASCII.GetString(pdf);
        var count = 0;
        var index = 0;
        const string marker = "/Type /Page";
        while (true)
        {
            var found = text.IndexOf(marker, index, StringComparison.Ordinal);
            if (found < 0)
            {
                break;
            }

            var after = found + marker.Length;
            if (after < text.Length && text[after] == 's')
            {
                index = after + 1;
                continue;
            }

            count++;
            index = after;
        }

        return Math.Max(count, 1);
    }
}
