namespace SovietTransistors.Domain;

/// <summary>Описательные атрибуты транзистора (не измеримые величины). null — не задано.</summary>
public sealed record TransistorAttributes
{
    /// <summary>Структура проводимости: npn, pnp, n-fet, p-fet…</summary>
    public string? Structure { get; init; }

    /// <summary>Технология изготовления: сплавная, сплавно-диффузионная, планарная…</summary>
    public string? Technology { get; init; }

    /// <summary>Корпус: «КТ-13», «TO-92»…</summary>
    public string? Package { get; init; }

    /// <summary>Материал корпуса: металл, металлокерамика, пластик…</summary>
    public string? PackageMaterial { get; init; }

    /// <summary>Цветовая маркировка (когда обозначения на корпусе нет): «красный», «жёлтая точка»…</summary>
    public string? ColorMarking { get; init; }

    /// <summary>Цоколёвка: «КБЭ», «1-Э 2-К 3-Б»…</summary>
    public string? Pinout { get; init; }

    /// <summary>Повышенная чувствительность к статическому напряжению.</summary>
    public bool? EsdSensitive { get; init; }

    /// <summary>Военное исполнение.</summary>
    public bool? MilitaryGrade { get; init; }

    /// <summary>Радиационная стойкость.</summary>
    public bool? RadiationHardened { get; init; }

    /// <summary>Обозначение ТУ/ОТУ: «ТУ 11.365.001-71»…</summary>
    public string? Tu { get; init; }

    /// <summary>Примечание свободным текстом.</summary>
    public string? Notes { get; init; }

    /// <summary>Год начала выпуска (>= 1949).</summary>
    public int? YearFrom { get; init; }

    /// <summary>Год окончания выпуска; null — выпускался на момент описания.</summary>
    public int? YearTo { get; init; }

    /// <summary>Масса «не более», г.</summary>
    public double? MassMax { get; init; }

    /// <summary>Ссылка на документацию (скан даташита).</summary>
    public string? DatasheetUrl { get; init; }
}

public static class TransistorAttributesValidator
{
    public const int MinYear = 1949;
    public const int MaxYear = 2100;

    public static IReadOnlyList<string> Errors(TransistorAttributes attributes)
    {
        var errors = new List<string>();
        Add(attributes.Structure, "структура", errors);
        Add(attributes.Technology, "технология", errors);
        Add(attributes.Package, "корпус", errors);
        Add(attributes.PackageMaterial, "материал корпуса", errors);
        Add(attributes.ColorMarking, "цветовая маркировка", errors);
        Add(attributes.Pinout, "цоколёвка", errors);
        Add(attributes.Tu, "ТУ", errors);
        Add(attributes.Notes, "примечание", errors);
        Add(attributes.DatasheetUrl, "ссылка на документацию", errors);

        if (attributes.YearFrom is int yearFrom && (yearFrom < MinYear || yearFrom > MaxYear))
            errors.Add($"год начала выпуска должен быть от {MinYear} до {MaxYear}, получено {yearFrom}");
        if (attributes.YearTo is int yearTo && (yearTo < MinYear || yearTo > MaxYear))
            errors.Add($"год окончания выпуска должен быть от {MinYear} до {MaxYear}, получено {yearTo}");
        if (attributes.YearFrom is int from && attributes.YearTo is int to && from >= to)
            errors.Add($"год начала выпуска ({from}) должен быть меньше года окончания ({to})");
        if (attributes.MassMax is <= 0)
            errors.Add("масса «не более» должна быть положительной");
        return errors;
    }

    private static void Add(string? value, string name, List<string> errors)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{name}: пустое значение (null — «не задано», но не пустая строка)");
        }
    }
}
