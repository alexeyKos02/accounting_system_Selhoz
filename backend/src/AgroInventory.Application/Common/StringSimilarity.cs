namespace AgroInventory.Application.Common;

/// <summary>
/// Простая похожесть строк для поиска дублей химии (ТЗ §18.1): lower-case, частичные
/// совпадения, опечатки. Позже можно заменить на GPT/embeddings.
/// </summary>
public static class StringSimilarity
{
    /// <summary>Похожи ли строки: частичное вхождение или расстояние Левенштейна ниже порога.</summary>
    public static bool AreSimilar(string a, string b)
    {
        var x = Normalize(a);
        var y = Normalize(b);
        if (x.Length == 0 || y.Length == 0) return false;
        if (x == y) return true;
        if (x.Contains(y) || y.Contains(x)) return true;

        var distance = Levenshtein(x, y);
        var maxLen = Math.Max(x.Length, y.Length);
        // Считаем похожими при совпадении > ~70%.
        return 1.0 - (double)distance / maxLen >= 0.7;
    }

    /// <summary>Нижний регистр + транслит кириллицы в латиницу, чтобы сближать ru/en варианты
    /// (ТЗ §18.1: Раундап ↔ Roundup).</summary>
    public static string Normalize(string s) => Transliterate((s ?? string.Empty).Trim().ToLowerInvariant());

    private static readonly Dictionary<char, string> Cyrillic = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "e",
        ['ж'] = "zh", ['з'] = "z", ['и'] = "i", ['й'] = "i", ['к'] = "k", ['л'] = "l", ['м'] = "m",
        ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
        ['ф'] = "f", ['х'] = "h", ['ц'] = "c", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "sch",
        ['ъ'] = "", ['ы'] = "y", ['ь'] = "", ['э'] = "e", ['ю'] = "yu", ['я'] = "ya",
    };

    private static string Transliterate(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var ch in s)
            sb.Append(Cyrillic.TryGetValue(ch, out var latin) ? latin : ch.ToString());
        return sb.ToString();
    }

    public static int Levenshtein(string s, string t)
    {
        var d = new int[s.Length + 1, t.Length + 1];
        for (var i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= t.Length; j++) d[0, j] = j;

        for (var i = 1; i <= s.Length; i++)
        for (var j = 1; j <= t.Length; j++)
        {
            var cost = s[i - 1] == t[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
        }

        return d[s.Length, t.Length];
    }
}
