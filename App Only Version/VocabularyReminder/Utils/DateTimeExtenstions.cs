using System;

namespace VocabularyReminder.Utils
{
    public static class DateTimeExtensions
    {
        // Converts a DateTime to UNIX time (seconds)
        public static long ToUnixTimeInSeconds(this DateTime dateTime)
        {
            DateTimeOffset dto = new DateTimeOffset(dateTime.ToUniversalTime());
            return dto.ToUnixTimeSeconds();
        }

        // Converts a DateTime to UNIX time (milliseconds)
        public static long ToUnixTimeInMilliseconds(this DateTime dateTime)
        {
            DateTimeOffset dto = new DateTimeOffset(dateTime.ToUniversalTime());
            return dto.ToUnixTimeMilliseconds();
        }
    }
}
