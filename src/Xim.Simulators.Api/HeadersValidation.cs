using System.Runtime.CompilerServices;

namespace Xim.Simulators.Api
{
    internal static class HeadersValidation
    {
        private static readonly bool[] ValidNameChars = InitializeValidNameChars();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NameContainsInvalidChar(string name, out (char Char, int Index) character)
        {
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (!IsValidNameChar(c))
                {
                    character = (c, i);
                    return true;
                }
            }
            character = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ValueContainsInvalidChar(string value, out (char Char, int Index) character)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (!IsValidValueChar(c))
                {
                    character = (c, i);
                    return true;
                }
            }
            character = default;
            return false;
        }

        private static bool[] InitializeValidNameChars()
        {
            var chars = new bool[128];
            for (var c = '0'; c <= '9'; c++)
            {
                chars[c] = true;
            }
            for (var c = 'A'; c <= 'Z'; c++)
            {
                chars[c] = true;
            }
            for (var c = 'a'; c <= 'z'; c++)
            {
                chars[c] = true;
            }
            chars['!'] = true;
            chars['#'] = true;
            chars['$'] = true;
            chars['%'] = true;
            chars['&'] = true;
            chars['\''] = true;
            chars['*'] = true;
            chars['+'] = true;
            chars['-'] = true;
            chars['.'] = true;
            chars['^'] = true;
            chars['_'] = true;
            chars['`'] = true;
            chars['|'] = true;
            chars['~'] = true;
            return chars;
        }

        private static bool IsValidNameChar(char c)
            => c < ValidNameChars.Length && ValidNameChars[c];

        private static bool IsValidValueChar(char c)
            => c >= 0x20 && c <= 0x7e;
    }
}
