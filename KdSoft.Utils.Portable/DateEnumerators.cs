using System;
using System.Collections.Generic;

namespace KdSoft.Utils
{
  /// <summary>
  /// Enumerates all dates in the date range - excluding the end date.
  /// </summary>
  public class DateEnumerable: IEnumerable<DateTime>
  {
    public readonly DateTime StartDate, EndDate;

    public DateEnumerable(DateTime startDate, DateTime endDate) {
      this.StartDate = startDate.Date;
      this.EndDate = endDate.Date;
      if (this.EndDate < this.StartDate)
        throw new ArgumentOutOfRangeException("endDate", "End date must not be before start date.");
    }

    public IEnumerator<DateTime> GetEnumerator() {
      return new DateEnumerator(this);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }

  public sealed class DateEnumerator: IEnumerator<DateTime>
  {
    DateEnumerable enumerable;
    DateTime? current;
    bool atEnd;

    public DateEnumerator(DateEnumerable enumerable) {
      this.enumerable = enumerable;
      current = null;
    }

    public DateTime Current {
      get {
        if (!atEnd && current.HasValue)
          return current.Value;
        else
          throw new InvalidOperationException();
      }
    }

    public void Dispose() {
      //
    }

    object System.Collections.IEnumerator.Current {
      get { return Current; }
    }

    public bool MoveNext() {
      if (atEnd)
        return false;
      if (current.HasValue) {
        var temp = current.Value.AddDays(1);
        if (temp < enumerable.EndDate) {
          current = temp;
          return true;
        }
        atEnd = true;
        return false;
      }
      else {
        current = enumerable.StartDate;
        if (current < enumerable.EndDate) {
          return true;
        }
        atEnd = true;
        return false;
      }
    }

    public void Reset() {
      current = null;
    }
  }

  /// <summary>
  /// Enumerates a number of weeks with the start date's week day interpreted as "first day of week".
  /// The DateTime values returned have the same day of week as the start date.
  /// </summary>
  public class WeekEnumerable: IEnumerable<DateTime>
  {
    public readonly DateTime StartDate;
    public readonly int DayCount;

    public WeekEnumerable(DateTime startDate, int weekCount) {
      if (weekCount < 0)
        throw new ArgumentOutOfRangeException("weekCount", "Week count must not be negative.");
      this.StartDate = startDate.Date;
      this.DayCount = weekCount * 7;
    }

    public IEnumerator<DateTime> GetEnumerator() {
      return new WeekEnumerator(this);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }

  public sealed class WeekEnumerator: IEnumerator<DateTime>
  {
    WeekEnumerable enumerable;
    DateTime? current;
    bool atEnd;

    public WeekEnumerator(WeekEnumerable enumerable) {
      this.enumerable = enumerable;
      current = null;
    }

    public DateTime Current {
      get {
        if (!atEnd && current.HasValue)
          return current.Value;
        else
          throw new InvalidOperationException();
      }
    }

    public void Dispose() {
      //
    }

    object System.Collections.IEnumerator.Current {
      get { return Current; }
    }

    public bool MoveNext() {
      if (atEnd)
        return false;
      if (current.HasValue) {
        var temp = current.Value.AddDays(7);
        if ((temp - enumerable.StartDate).Days < enumerable.DayCount) {
          current = temp;
          return true;
        }
        atEnd = true;
        return false;
      }
      else {
        current = enumerable.StartDate;
        if ((current.Value - enumerable.StartDate).Days < enumerable.DayCount) {
          return true;
        }
        atEnd = true;
        return false;
      }
    }

    public void Reset() {
      current = null;
    }
  }

  /// <summary>
  /// Enumerates all months that the given data range (excluding the end date) touches.
  /// The DateTime values returned always have "1" for the Day value.
  /// </summary>
  public class MonthEnumerable: IEnumerable<DateTime>
  {
    public readonly DateTime StartDate, EndDate;

    public MonthEnumerable(DateTime startDate, DateTime endDate) {
      this.StartDate = new DateTime(startDate.Year, startDate.Month, 1);
      this.EndDate = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1);
      if (this.EndDate < this.StartDate)
        throw new ArgumentOutOfRangeException("endDate", "End date must not be before start date.");
    }

    public IEnumerator<DateTime> GetEnumerator() {
      return new MonthEnumerator(this);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }

  // enumerates all months that are covered by the input date range
  public sealed class MonthEnumerator: IEnumerator<DateTime>
  {
    MonthEnumerable enumerable;
    DateTime? current;
    bool atEnd;

    public MonthEnumerator(MonthEnumerable enumerable) {
      this.enumerable = enumerable;
      current = null;
    }

    public DateTime Current {
      get {
        if (!atEnd && current.HasValue)
          return current.Value;
        else
          throw new InvalidOperationException();
      }
    }

    public void Dispose() {
      //
    }

    object System.Collections.IEnumerator.Current {
      get { return Current; }
    }

    public bool MoveNext() {
      if (atEnd)
        return false;
      if (current.HasValue) {
        var temp = current.Value.AddMonths(1);
        if (temp < enumerable.EndDate) {
          current = temp;
          return true;
        }
        atEnd = true;
        return false;
      }
      else {
        current = enumerable.StartDate;
        if (current.Value < enumerable.EndDate) {
          return true;
        }
        atEnd = true;
        return false;
      }
    }

    public void Reset() {
      current = null;
    }
  }
}
