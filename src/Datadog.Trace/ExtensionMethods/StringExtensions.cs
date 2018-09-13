using System;
using System.Text;

namespace Datadog.Trace.ExtensionMethods
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Changes the first character of the string to
        /// upper case and all other characters to lower case.
        /// </summary>
        /// <param name="value">The string to change to sentence case.</param>
        /// <returns>
        /// A new string with the first character changed to
        /// upper case and all other characters to lower case.
        /// </returns>
        public static string ToSentenceCaseInvariant(this string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value == string.Empty)
            {
                return value;
            }

            var sb = new StringBuilder(value);
            sb[0] = char.ToUpperInvariant(sb[0]);

            for (int i = 1; i < sb.Length; i++)
            {
                sb[i] = char.ToLowerInvariant(sb[i]);
            }

            return sb.ToString();
        }
    }
}
