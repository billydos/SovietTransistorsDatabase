namespace SovietTransistors.Domain;

public static class Cyrillic
{
    /// <summary>Заглавная буква русского алфавита (А–Я, Ё).</summary>
    public static bool IsUpperLetter(char c) => (c >= 'А' && c <= 'Я') || c == 'Ё';

    /// <summary>Строка из одной или двух заглавных русских букв.</summary>
    public static bool IsUpperLetters(string? s) =>
        !string.IsNullOrEmpty(s) && s.Length <= 2 && s.All(IsUpperLetter);
}
