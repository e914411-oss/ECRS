using System.Net;
using System.Reflection;

namespace ECRS_WEB.Helpers
{
    public static class QueryStringSecurityHelper
    {
        public static string UrlEncode(object? value)
        {
            var encodedValue = WebUtility.UrlEncode(value?.ToString() ?? string.Empty);
            return WebUtility.UrlEncode(encodedValue);
        }

        public static string UrlDecode(string? value)
        {
            return WebUtility.UrlDecode(value ?? string.Empty).Trim();
        }

        public static void UrlDecodeStringProperties(object? value)
        {
            if (value is null)
            {
                return;
            }

            foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType != typeof(string) || !property.CanRead || !property.CanWrite)
                {
                    continue;
                }

                var raw = (string?)property.GetValue(value);
                property.SetValue(value, UrlDecode(raw));
            }
        }
    }
}
