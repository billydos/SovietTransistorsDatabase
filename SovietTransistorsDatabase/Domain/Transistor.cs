using System.Globalization;
using System.Text;

namespace SovietTransistors.Domain;

/// <summary>Советский транзистор по системе обозначений (ГОСТ 10862).</summary>
public sealed record Transistor
{
    /// <summary>Тип материала: Г|1, К|2, А|3, И|4 (хранится как в обозначении).</summary>
    public required char Material { get; init; }

    /// <summary>Подкласс: Т — биполярный, П — полевой.</summary>
    public required char Subclass { get; init; }

    /// <summary>Сборка (буква С в обозначении).</summary>
    public bool IsAssembly { get; init; }

    /// <summary>Характерный эксплуатационный признак: 1–9.</summary>
    public required int Feature { get; init; }

    /// <summary>Порядковый номер разработки: 1–999 (в обозначении 01–999).</summary>
    public required int DevelopmentNumber { get; init; }

    /// <summary>Классификация по параметрам: одна или две заглавные русские буквы.</summary>
    public required string Letters { get; init; }

    /// <summary>Модификация: 1–9 или отсутствует.</summary>
    public int? Modification { get; init; }

    /// <summary>Бескорпусное исполнение (через дефис): 1–6 или отсутствует.</summary>
    public int? ChipVariant { get; init; }

    /// <summary>Полное обозначение, например КТ315Б или 2Т914А-1.</summary>
    public string Name
    {
        get
        {
            var sb = new StringBuilder(10);
            sb.Append(Material);
            sb.Append(Subclass);
            if (IsAssembly) sb.Append('С');
            sb.Append(Feature);
            sb.Append(DevelopmentNumber.ToString("00", CultureInfo.InvariantCulture));
            sb.Append(Letters);
            if (Modification is int modification) sb.Append(modification);
            if (ChipVariant is int chip) sb.Append('-').Append(chip);
            return sb.ToString();
        }
    }

    public override string ToString() => Name;
}
