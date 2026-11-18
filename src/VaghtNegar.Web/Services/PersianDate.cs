using System.Globalization;

namespace VaghtNegar.Web.Services;

public static class PersianDate
{
    public static string Today
    {
        get
        {
            var calendar = new PersianCalendar();
            var now = DateTime.Now;
            var date = $"{calendar.GetYear(now):0000}/{calendar.GetMonth(now):00}/{calendar.GetDayOfMonth(now):00}";
            var day = now.ToString("dddd", CultureInfo.GetCultureInfo("fa-IR"));
            return $"{day}، {ToPersianDigits(date)}";
        }
    }

    private static string ToPersianDigits(string value) =>
        value.Replace('0', '۰').Replace('1', '۱').Replace('2', '۲')
            .Replace('3', '۳').Replace('4', '۴').Replace('5', '۵')
            .Replace('6', '۶').Replace('7', '۷').Replace('8', '۸').Replace('9', '۹');
}
