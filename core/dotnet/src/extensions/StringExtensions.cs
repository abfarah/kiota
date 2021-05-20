using System.Linq;

namespace KiotaCore.Extensions {
    public static class StringExtensions {
        public static string ToFirstCharacterLowerCase(this string input)
            => string.IsNullOrEmpty(input) ? input : $"{char.ToLowerInvariant(input[0])}{input[1..]}";
    }
}
