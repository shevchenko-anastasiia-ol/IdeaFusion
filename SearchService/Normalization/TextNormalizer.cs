using System.Globalization;
using System.Text;

namespace SearchService.Normalization;

/// <summary>
/// Text forms the index and the queries are compared in. The same code builds both sides,
/// so a query typed in any script or keyboard layout meets the document in a common form:
/// <list type="bullet">
/// <item><see cref="Normalize"/>: lower case, no Latin diacritics, no apostrophes, words split by single spaces;</item>
/// <item><see cref="Transliterate"/>: Ukrainian Cyrillic to Latin by the KMU-2010 standard ("тест" → "test");</item>
/// <item><see cref="SwapLayout"/>: text typed with the wrong keyboard layout ("еуые" → "test", "ntcn" → "тест").</item>
/// </list>
/// </summary>
public static class TextNormalizer
{
    // KMU-2010 (Cabinet of Ministers resolution No. 55). The second value is used at the start of a word.
    private static readonly Dictionary<char, (string Inner, string Initial)> Translit = new()
    {
        ['а'] = ("a", "a"), ['б'] = ("b", "b"), ['в'] = ("v", "v"), ['г'] = ("h", "h"), ['ґ'] = ("g", "g"),
        ['д'] = ("d", "d"), ['е'] = ("e", "e"), ['є'] = ("ie", "ye"), ['ж'] = ("zh", "zh"), ['з'] = ("z", "z"),
        ['и'] = ("y", "y"), ['і'] = ("i", "i"), ['ї'] = ("i", "yi"), ['й'] = ("i", "y"), ['к'] = ("k", "k"),
        ['л'] = ("l", "l"), ['м'] = ("m", "m"), ['н'] = ("n", "n"), ['о'] = ("o", "o"), ['п'] = ("p", "p"),
        ['р'] = ("r", "r"), ['с'] = ("s", "s"), ['т'] = ("t", "t"), ['у'] = ("u", "u"), ['ф'] = ("f", "f"),
        ['х'] = ("kh", "kh"), ['ц'] = ("ts", "ts"), ['ч'] = ("ch", "ch"), ['ш'] = ("sh", "sh"),
        ['щ'] = ("shch", "shch"), ['ь'] = ("", ""), ['ю'] = ("iu", "yu"), ['я'] = ("ia", "ya"),
        // Russian letters people still type; mapped to the nearest Ukrainian reading.
        ['ё'] = ("io", "yo"), ['ы'] = ("y", "y"), ['э'] = ("e", "e"), ['ъ'] = ("", ""),
    };

    // Physical keys: QWERTY character → Ukrainian ЙЦУКЕН character on the same key.
    private const string LatinKeys = "qwertyuiop[]asdfghjkl;'zxcvbnm,.`";
    private const string CyrillicKeys = "йцукенгшщзхїфівапролджєячсмитьбю'";

    private static readonly Dictionary<char, char> LatinToCyrillic = BuildLayout(LatinKeys, CyrillicKeys);

    private static readonly Dictionary<char, char> CyrillicToLatin = BuildCyrillicToLatin();

    private static Dictionary<char, char> BuildLayout(string from, string to)
    {
        var map = new Dictionary<char, char>();
        for (var i = 0; i < from.Length; i++) map[from[i]] = to[i];
        return map;
    }

    private static Dictionary<char, char> BuildCyrillicToLatin()
    {
        var map = BuildLayout(CyrillicKeys, LatinKeys);
        map.Remove('\''); // the apostrophe is a letter-joiner in Ukrainian words, not a key slip
        // Russian layout keys that differ from the Ukrainian one.
        map['ы'] = 's';
        map['ъ'] = ']';
        map['э'] = '\'';
        map['ё'] = '`';
        return map;
    }

    /// <summary>Lower case, Latin diacritics removed, apostrophes dropped, everything except letters and digits becomes a word break.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var sb = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var raw in text.ToLowerInvariant())
        {
            if (IsApostrophe(raw)) continue;

            var c = StripLatinDiacritic(raw);
            if (char.IsLetterOrDigit(c))
            {
                if (pendingSpace && sb.Length > 0) sb.Append(' ');
                pendingSpace = false;
                sb.Append(c);
            }
            else
            {
                pendingSpace = true;
            }
        }
        return sb.ToString();
    }

    /// <summary>Normalized words of the text.</summary>
    public static string[] Tokenize(string? text) =>
        Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// KMU-2010 transliteration of already normalized text. Latin letters and digits pass through,
    /// so the result of a mixed text is entirely Latin.
    /// </summary>
    public static string Transliterate(string normalized)
    {
        var sb = new StringBuilder(normalized.Length + 8);
        var wordStart = true;
        for (var i = 0; i < normalized.Length; i++)
        {
            var c = normalized[i];
            if (c == ' ')
            {
                sb.Append(' ');
                wordStart = true;
                continue;
            }

            if (Translit.TryGetValue(c, out var t))
            {
                // "зг" is written "zgh" so that it is not read as "ж".
                if (c == 'г' && i > 0 && normalized[i - 1] == 'з') sb.Append("gh");
                else sb.Append(wordStart ? t.Initial : t.Inner);
            }
            else
            {
                sb.Append(c);
            }
            wordStart = false;
        }
        return sb.ToString();
    }

    /// <summary>
    /// The same keystrokes in the other layout: Latin text is retyped as Cyrillic, Cyrillic as Latin.
    /// Works on raw (not normalized) text because punctuation keys carry letters in the other layout
    /// ("[" is "х", ";" is "ж"). Returns null when the text has no letters to swap.
    /// </summary>
    public static string? SwapLayout(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var lower = raw.ToLowerInvariant();

        var latin = 0;
        var cyrillic = 0;
        foreach (var c in lower)
        {
            if (c is >= 'a' and <= 'z') latin++;
            else if (IsCyrillic(c)) cyrillic++;
        }
        if (latin == 0 && cyrillic == 0) return null;

        var map = latin >= cyrillic ? LatinToCyrillic : CyrillicToLatin;
        var sb = new StringBuilder(lower.Length);
        foreach (var c in lower)
            sb.Append(map.TryGetValue(c, out var swapped) ? swapped : c);
        return sb.ToString();
    }

    /// <summary>True when the text contains Cyrillic letters.</summary>
    public static bool HasCyrillic(string text)
    {
        foreach (var c in text)
            if (IsCyrillic(c)) return true;
        return false;
    }

    private static bool IsCyrillic(char c) => c is >= 'Ѐ' and <= 'ӿ';

    private static bool IsApostrophe(char c) => c is '\'' or '’' or 'ʼ' or '`' or '‘';

    private static char StripLatinDiacritic(char c)
    {
        if (c < 'À' || IsCyrillic(c)) return c; // й and ї are letters of their own, keep them
        var decomposed = c.ToString().Normalize(NormalizationForm.FormD);
        var baseChar = decomposed[0];
        return decomposed.Length > 1
               && baseChar is >= 'a' and <= 'z'
               && CharUnicodeInfo.GetUnicodeCategory(decomposed[1]) == UnicodeCategory.NonSpacingMark
            ? baseChar
            : c;
    }
}
