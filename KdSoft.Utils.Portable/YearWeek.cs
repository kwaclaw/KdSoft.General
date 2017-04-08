using System;
using System.Globalization;

namespace KdSoft.Utils
{
  public struct YearWeek: IComparable, IComparable<YearWeek>, IEquatable<YearWeek>
  {
    static Calendar cal = CultureInfo.InvariantCulture.Calendar;

    public static CalendarWeekRule WeekRule = CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule;
    public static DayOfWeek FirstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;

    public static void InitializeRules(CalendarWeekRule weekRule, DayOfWeek firstDayOfWeek) {
      WeekRule = weekRule;
      FirstDayOfWeek = firstDayOfWeek;
    }

    public static void InitializeRules(CultureInfo cultureInfo) {
      var dtInfo = cultureInfo.DateTimeFormat;
      InitializeRules(dtInfo.CalendarWeekRule, dtInfo.FirstDayOfWeek);
    }

    int year;
    int week;

    /// <summary>
    /// Basic constructor. Only basic input checking performed..
    /// </summary>
    /// <param name="year"></param>
    /// <param name="week"></param>
    public YearWeek(int year, int week) {
      if (year < 1 || year > 9999)
        throw new ArgumentOutOfRangeException("year");
      if (week < 1 || week > 53)
        throw new ArgumentOutOfRangeException("week");
      this.year = year;
      this.week = week;
    }

    public YearWeek(DateTime day, CalendarWeekRule weekRule, DayOfWeek firstDayOfWeek) {
      year = day.Year;
      week = cal.GetWeekOfYear(day, weekRule, firstDayOfWeek);
    }

    public YearWeek(DateTime day) {
      year = day.Year;
      week = cal.GetWeekOfYear(day, WeekRule, FirstDayOfWeek);
    }

    // The very first week has some invalid days, so we take the second week
    public static YearWeek MinValue {
      get { return new YearWeek(1, 2); }
    }

    // The very last week has some invalid days, so we take the next to last week
    public static YearWeek MaxValue {
      get { return new YearWeek(9999, 52); }
    }

    /// <summary>
    /// Returns ISO 8601 conformat week of year. That is, weeks start with Monday. Week 1 is the 1st week of the year with a Thursday in it.
    /// This is the only way to get a week instance that stays the same even if the days cross a year boundary.
    /// </summary>
    /// <param name="date">Date for which the ISO 8601 conforming <see cref="YearWeek"/> instance should be created.</param>
    /// <returns>ISO 8601 conformant <see cref="YearWeek"/> instance.</returns>
    public static YearWeek GetIso8601Week(DateTime date) {
      // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll be the same week#
      // as whatever Thursday, Friday or Saturday are, and we always get those right
      DayOfWeek day = cal.GetDayOfWeek(date);
      if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday) {
        date = date.AddDays(3);
      }

      int week = cal.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
      return new YearWeek(date.Year, week);
    }

    public int Year { get { return year; } }
    public int Week { get { return week; } }

    public DateTime GetFirstDay(CalendarWeekRule weekRule, DayOfWeek firstDayOfWeek) {
      return DateTimeExtensions.FirstDateOfWeek(year, week, weekRule, firstDayOfWeek);
    }

    public DateTime FirstDay {
      get { return DateTimeExtensions.FirstDateOfWeek(year, week, WeekRule, FirstDayOfWeek); }
    }

    public YearWeek AddWeeks(int weekCount, CalendarWeekRule weekRule, DayOfWeek firstDayOfWeek) {
      var firstDay = GetFirstDay(weekRule, firstDayOfWeek);
      var nextDay = firstDay.AddDays(weekCount * 7);
      return new YearWeek(nextDay, weekRule, firstDayOfWeek);
    }

    public YearWeek AddWeeks(int weekCount) {
      var nextDay = FirstDay.AddDays(weekCount * 7);
      return new YearWeek(nextDay);
    }

    public override bool Equals(object obj) {
      if (obj is YearWeek) {
        return Equals((YearWeek)obj);
      }
      return false;
    }

    public bool Equals(YearWeek other) {
      return year == other.year && week == other.week;
    }

    public override int GetHashCode() {
      return year.GetHashCode() ^ week.GetHashCode();
    }

    public override string ToString() {
      return ToString("{0}-W{1}");
    }

    public string ToString(string format) {
      return string.Format(format, year, week);
    }

    public int CompareTo(object obj) {
      return CompareTo((YearWeek)obj);
    }

    public int CompareTo(YearWeek other) {
      int result = year.CompareTo(other.year);
      if (result == 0)
        result = week.CompareTo(other.week);
      return result;
    }

    public static bool operator ==(YearWeek w1, YearWeek w2) {
      return w1.Equals(w2);
    }

    public static bool operator !=(YearWeek w1, YearWeek w2) {
      return !(w1 == w2);
    }

    public static bool operator <(YearWeek w1, YearWeek w2) {
      return w1.Year < w2.Year || (w1.Year == w2.Year && w1.Week < w2.Week);
    }

    public static bool operator >=(YearWeek w1, YearWeek w2) {
      return !(w1 < w2);
    }

    public static bool operator >(YearWeek w1, YearWeek w2) {
      return w1.Year > w2.Year || (w1.Year == w2.Year && w1.Week > w2.Week);
    }

    public static bool operator <=(YearWeek w1, YearWeek w2) {
      return !(w1 > w2);
    }
  }
}
