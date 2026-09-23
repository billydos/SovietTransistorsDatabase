namespace SovietTransistors.Domain;

/// <summary>Разбор обозначения транзистора, например КТ315Б или 2Т914А-1.</summary>
public static class TransistorNameParser
{
    public static Transistor Parse(string text)
    {
        if (TryParse(text, out var transistor, out var error)) return transistor!;
        throw new FormatException(error);
    }

    public static bool TryParse(string? text, out Transistor? transistor, out string error)
    {
        transistor = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "обозначение пустое";
            return false;
        }

        string s = Normalize(text);
        int i = 0;

        char material = s[0];
        if (!Materials.IsValidSymbol(material))
        {
            error = $"позиция 1: ожидался тип материала — Г или 1 (германий), К или 2 (кремний), А или 3 (арсенид галлия), И или 4 (индий), получено «{Show(s[0])}»";
            return false;
        }
        i = 1;

        char subclass = CharAt(s, i);
        if (subclass is not ('Т' or 'П'))
        {
            error = $"позиция {i + 1}: ожидался подкласс — Т (биполярный) или П (полевой), получено «{Show(subclass)}»";
            return false;
        }
        i++;

        bool isAssembly = CharAt(s, i) == 'С';
        if (isAssembly) i++;

        char featureChar = CharAt(s, i);
        if (!IsAsciiDigit(featureChar) || featureChar == '0')
        {
            error = $"позиция {i + 1}: ожидался характерный эксплуатационный признак — цифра от 1 до 9, получено «{Show(featureChar)}»";
            return false;
        }
        int feature = featureChar - '0';
        i++;

        int numberStart = i;
        while (i < s.Length && IsAsciiDigit(s[i])) i++;
        int numberLength = i - numberStart;
        if (numberLength is < 2 or > 3)
        {
            string got = numberLength == 0 ? "цифры отсутствуют" : s[numberStart..i];
            error = $"позиция {numberStart + 1}: порядковый номер разработки — от 2 до 3 цифр (01–999), {got}";
            return false;
        }
        if (numberLength == 3 && s[numberStart] == '0')
        {
            error = $"позиция {numberStart + 1}: номер разработки из трёх цифр не может начинаться с нуля";
            return false;
        }
        int number = int.Parse(s[numberStart..i], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        if (number == 0)
        {
            error = $"позиция {numberStart + 1}: номер разработки не может быть нулём";
            return false;
        }

        int lettersStart = i;
        while (i < s.Length && Cyrillic.IsUpperLetter(s[i]) && i - lettersStart < 2) i++;
        int lettersLength = i - lettersStart;
        if (lettersLength == 0)
        {
            error = i < s.Length
                ? $"позиция {i + 1}: ожидалась буква классификации по параметрам (заглавная русская буква), получено «{Show(s[i])}»"
                : "отсутствует буква классификации по параметрам (одна или две заглавные русские буквы после номера разработки)";
            return false;
        }
        string letters = s[lettersStart..i];
        if (i < s.Length && Cyrillic.IsUpperLetter(s[i]))
        {
            error = $"позиция {i + 1}: не более двух букв классификации по параметрам";
            return false;
        }

        int? modification = null;
        if (i < s.Length && IsAsciiDigit(s[i]))
        {
            if (s[i] == '0')
            {
                error = $"позиция {i + 1}: модификация — цифра от 1 до 9, получено «0»";
                return false;
            }
            modification = s[i] - '0';
            i++;
        }

        int? chipVariant = null;
        if (i < s.Length)
        {
            if (s[i] != '-')
            {
                error = $"позиция {i + 1}: неожидаемый символ «{Show(s[i])}» — после букв допускаются только модификация (цифра 1–9) или бескорпусное исполнение (дефис и цифра 1–6)";
                return false;
            }
            i++;
            char chipChar = CharAt(s, i);
            if (chipChar is (< '1' or > '6') or '\0')
            {
                error = $"позиция {i + 1}: бескорпусное исполнение — дефис и цифра от 1 до 6, получено «{Show(chipChar)}»";
                return false;
            }
            chipVariant = chipChar - '0';
            i++;
            if (i < s.Length)
            {
                error = $"позиция {i + 1}: лишние символы после бескорпусного исполнения: «{s[i..]}»";
                return false;
            }
        }

        if (i < s.Length)
        {
            error = $"позиция {i + 1}: лишние символы: «{s[i..]}»";
            return false;
        }

        transistor = new Transistor
        {
            Material = material,
            Subclass = subclass,
            IsAssembly = isAssembly,
            Feature = feature,
            DevelopmentNumber = number,
            Letters = letters,
            Modification = modification,
            ChipVariant = chipVariant,
        };
        error = string.Empty;
        return true;
    }

    private static string Normalize(string text) =>
        text.Trim().ToUpperInvariant().Replace('−', '-').Replace('–', '-');

    private static char CharAt(string s, int index) => index < s.Length ? s[index] : '\0';

    private static bool IsAsciiDigit(char c) => c is >= '0' and <= '9';

    private static string Show(char c) => c == '\0' ? "конец обозначения" : c.ToString();
}
