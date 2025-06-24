using System;

namespace AspectFix.Services
{
    public static class Utils
    {
        public static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
        
        /// <summary>
        /// Shortens a string to the specified maximum length by truncating the middle of the string
        /// and replacing it with ellipsis ("...") if the string exceeds the maximum length.
        /// If the string is shorter than or equal to the maximum length, it will be returned unchanged.
        /// </summary>
        /// <param name="input">The input string to be shortened.</param>
        /// <param name="maxLength">The maximum desired length of the output string, including ellipsis if applicable.</param>
        /// <returns>
        /// A shortened version of the input string if it exceeds the specified maximum length,
        /// or the original string if it is within the limit.
        /// </returns>
        public static string ShortenString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
                return input;

            if (maxLength < 4)
                return input.Substring(0, maxLength); // Return the start of the string if maxLength is too small

            var startLength = (maxLength - 3) / 2;
            var endLength = (maxLength - 3) - startLength;

            return input.Substring(0, startLength) + "..." + input.Substring(input.Length - endLength);
        }
    }
}
