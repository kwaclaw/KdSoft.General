using System;
using System.Text;

namespace KdSoft.Utils
{
  public static class TimeSpanExtensions
  {
    /// <summary>
    /// Parses ISO 8601 duration string into a <see cref="TimeSpan"/>.
    /// Does not handle calendars, therefore months and years must be expressed as a number of weeks.
    /// </summary>
    /// <param name="duration">ISO 8601 duration string.</param>
    /// <param name="result"><see cref="TimeSpan"/> instance, if parsed successfully.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public static bool TryParseIso(string duration, out TimeSpan result) {
      bool inTimeSection = false;
      bool isValid = true;
      double weeks = 0;
      double days = 0;
      double hours = 0;
      double minutes = 0;
      double seconds = 0;

      result = default(TimeSpan);

      if (duration.Length == 0) {
        return true;
      }
      if (duration.Length < 2 || duration[0] != 'P') {
        return false;
      }

      var tokenBuilder = new StringBuilder();
      bool HandleDesignator(bool handleTime, ref double target)
      {
        if (inTimeSection != handleTime || tokenBuilder.Length == 0) {
          return false;
        }

        if (!double.TryParse(tokenBuilder.ToString(), out target)) {
          return false;
        }

        tokenBuilder.Clear();
        return true;
      }

      for (int indx = 1; indx < duration.Length; indx++) {
        if (!isValid) {
          return false;
        }

        char token = duration[indx];
        switch (token) {
          case 'Y':
            isValid = false;
            continue;
          case 'T':
            inTimeSection = true;
            continue;
          case 'W':
            isValid = HandleDesignator(false, ref weeks);
            continue;
          case 'D':
            isValid = HandleDesignator(false, ref days);
            continue;
          case 'H':
            isValid = HandleDesignator(true, ref hours);
            continue;
          case 'M':
            isValid = HandleDesignator(true, ref minutes);
            continue;
          case 'S':
            isValid = HandleDesignator(true, ref seconds);
            continue;
          default:
            tokenBuilder.Append(token);
            break;
        }
      }

      isValid &= tokenBuilder.Length == 0;
      if (isValid) {
        result = TimeSpan.FromDays(days + (weeks * 7))
            .Add(TimeSpan.FromSeconds(seconds + (minutes * 60) + (hours * 3600)));
      }
      return isValid;
    }

    /// <summary>
    /// Parses ISO 8601 duration string into a <see cref="TimeSpan"/>.
    /// Does not handle calendars, therefore months and years must be expressed as a number of weeks.
    /// </summary>
    /// <param name="duration">ISO 8601 duration string.</param>
    /// <returns><see cref="TimeSpan"/> instance.</returns>
    /// <exception cref="FormatException"></exception>
    public static TimeSpan ParseIso(string duration) {
      TimeSpan result;
      if (!TryParseIso(duration, out result)) {
        throw new FormatException("ISO 8601 duration not properly formatted.");
      }
      return result;
    }

    /// <summary>
    /// Serializes <see cref="TimeSpan"/> to an ISO 8601 duration string.
    /// Does not handle calendars, therefore months and years must be expressed as a number of weeks.
    /// </summary>
    /// <param name="timeSpan"><see cref="TimeSpan"/> to serialize.</param>
    /// <returns>ISO duration string.</returns>
    public static string ToIsoString(this TimeSpan timeSpan) {
      if (timeSpan == TimeSpan.Zero) {
        return "PT0S";
      }

      var builder = new StringBuilder("P");

      if (timeSpan.TotalDays > 7) {
        var weeks = Math.Floor(timeSpan.TotalDays / 7);
        builder.Append($"{weeks}W");

        timeSpan = timeSpan.Subtract(TimeSpan.FromDays(weeks * 7));
      }

      if (timeSpan.Days > 0) {
        builder.Append($"{timeSpan.Days}D");

        timeSpan = timeSpan.Subtract(TimeSpan.FromDays(timeSpan.Days));
      }

      if (timeSpan.TotalSeconds > 0) {
        builder.Append("T");
      }

      if (timeSpan.Hours > 0) {
        builder.Append($"{timeSpan.Hours}H");

        timeSpan = timeSpan.Subtract(TimeSpan.FromHours(timeSpan.Hours));
      }

      if (timeSpan.Minutes > 0) {
        builder.Append($"{timeSpan.Minutes}M");

        timeSpan = timeSpan.Subtract(TimeSpan.FromMinutes(timeSpan.Minutes));
      }

      if (timeSpan.TotalSeconds > 0) {
        builder.Append($"{timeSpan.TotalSeconds}S");
      }

      if (builder.Length == 1) {
        return string.Empty;
      }
      else {
        return builder.ToString();
      }
    }
  }
}
