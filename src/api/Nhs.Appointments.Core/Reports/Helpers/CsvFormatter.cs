namespace Nhs.Appointments.Core.Reports.Helpers;

public static class CsvFormatter
{
    private static readonly char[] FormulaTriggers = { '=', '+', '-', '@' };

    /// <summary>
    /// Neutralizes CSV Injection (Formula Injection) and handles standard CSV escaping.
    /// </summary>
    public static string FormatValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        if (value.Trim().IndexOfAny(FormulaTriggers) == 0)
        {
            value = "'" + value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
