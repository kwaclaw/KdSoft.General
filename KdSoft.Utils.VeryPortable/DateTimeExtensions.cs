using System;
using System.Globalization;

namespace KdSoft.Utils
{
  public static class DateTimeExtensions
  {
    public static readonly DateTime UnixDateOrigin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static string ToIsoString(this DateTime dateTime) {
      var dti = CultureInfo.InvariantCulture.DateTimeFormat;
      return dateTime.ToString(dti.SortableDateTimePattern);
    }

    public static string ToIsoDateString(string isoDateTimeString) {
      int sepIndex = isoDateTimeString.IndexOf('T');
      return isoDateTimeString.Substring(0, sepIndex);
    }

    public static string ToIsoTimeString(string isoDateTimeString) {
      int sepIndex = isoDateTimeString.IndexOf('T');
      return isoDateTimeString.Substring(sepIndex + 1);
    }

    public static long ToUnixTimestamp(this DateTime dateTime) {
      TimeSpan diff = dateTime.ToUniversalTime() - UnixDateOrigin;
      return (long)Math.Floor(diff.TotalSeconds);
    }

    public static DateTime FirstDateOfWeek(int year, int weekOfYear, CalendarWeekRule weekRule, DayOfWeek firstDayOfWeek) {
      DateTime jan1 = new DateTime(year, 1, 1);

      // get number of days between jan1 and next first day of week
      int daysOffset = (int)firstDayOfWeek - (int)jan1.DayOfWeek;

      switch (weekRule) {
        case CalendarWeekRule.FirstDay:
          if (daysOffset > 0)
            daysOffset -= 7;
          break;
        case CalendarWeekRule.FirstFullWeek:
          if (daysOffset < 0)
            daysOffset += 7;
          break;
        case CalendarWeekRule.FirstFourDayWeek:
          if (daysOffset < 0)
            daysOffset += 7;
          if (daysOffset < 4)
            daysOffset += 7;
          break;
      }

      return jan1.AddDays(daysOffset + ((weekOfYear - 1) * 7));
    }
  }
}