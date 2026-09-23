namespace SovietTransistors.Domain;

/// <summary>Тип материала полупроводника. Буква и цифра равнозначны.</summary>
public enum SemiconductorMaterial
{
    Germanium,
    Silicon,
    GalliumArsenide,
    Indium,
}

public static class Materials
{
    /// <summary>Символы материала: Г|1 — германий, К|2 — кремний, А|3 — арсенид галлия, И|4 — индий.</summary>
    public static readonly IReadOnlyDictionary<char, SemiconductorMaterial> BySymbol =
        new Dictionary<char, SemiconductorMaterial>
        {
            ['Г'] = SemiconductorMaterial.Germanium,
            ['1'] = SemiconductorMaterial.Germanium,
            ['К'] = SemiconductorMaterial.Silicon,
            ['2'] = SemiconductorMaterial.Silicon,
            ['А'] = SemiconductorMaterial.GalliumArsenide,
            ['3'] = SemiconductorMaterial.GalliumArsenide,
            ['И'] = SemiconductorMaterial.Indium,
            ['4'] = SemiconductorMaterial.Indium,
        };

    private static readonly Dictionary<SemiconductorMaterial, (char Letter, char Digit)> SymbolPairs = new()
    {
        [SemiconductorMaterial.Germanium] = ('Г', '1'),
        [SemiconductorMaterial.Silicon] = ('К', '2'),
        [SemiconductorMaterial.GalliumArsenide] = ('А', '3'),
        [SemiconductorMaterial.Indium] = ('И', '4'),
    };

    public static bool IsValidSymbol(char symbol) => BySymbol.ContainsKey(symbol);

    public static bool TryGetKind(char symbol, out SemiconductorMaterial kind) => BySymbol.TryGetValue(symbol, out kind!);

    public static SemiconductorMaterial KindOf(char symbol) =>
        TryGetKind(symbol, out var kind)
            ? kind
            : throw new ArgumentException($"«{symbol}» не является обозначением материала (Г/1, К/2, А/3, И/4)");

    /// <summary>Оба равнозначных символа материала, например Г и 1.</summary>
    public static (char Letter, char Digit) SymbolsOf(SemiconductorMaterial kind) => SymbolPairs[kind];

    public static string ShortName(SemiconductorMaterial kind) => kind switch
    {
        SemiconductorMaterial.Germanium => "германий",
        SemiconductorMaterial.Silicon => "кремний",
        SemiconductorMaterial.GalliumArsenide => "арсенид галлия",
        SemiconductorMaterial.Indium => "индий",
        _ => kind.ToString(),
    };

    public static string DisplayName(SemiconductorMaterial kind) => kind switch
    {
        SemiconductorMaterial.Germanium => "германий (Г/1)",
        SemiconductorMaterial.Silicon => "кремний (К/2)",
        SemiconductorMaterial.GalliumArsenide => "арсенид галлия (А/3)",
        SemiconductorMaterial.Indium => "индий (И/4)",
        _ => kind.ToString(),
    };
}
