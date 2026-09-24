using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Data;

/// <summary>
/// Исход upsert: Added — вставлена новая запись; UpdatedExisting — запись существовала,
/// применены заданные секции details; Skipped — запись существует, применять было нечего.
/// </summary>
public enum UpsertOutcome
{
    Added,
    UpdatedExisting,
    Skipped,
}

/// <summary>Данные транзистора, заполняемые при импорте: null означает «раздел не задан — не менять».</summary>
public sealed record TransistorDetails
{
    public TransistorAttributes? Attributes { get; init; }
    public IReadOnlyList<string>? Manufacturers { get; init; }
    public IReadOnlyList<ElectricalParameter>? Parameters { get; init; }
    public MaximumRatings? Ratings { get; init; }
}

public sealed record TransistorQuery
{
    public SemiconductorMaterial? Material { get; init; }
    public char? Subclass { get; init; }
    public bool? IsAssembly { get; init; }
    public int? Feature { get; init; }
    public int? DevelopmentNumber { get; init; }
    public string? Letters { get; init; }
    public int? Modification { get; init; }
    public int? ChipVariant { get; init; }
    public int? Limit { get; init; }
}

/// <summary>Хранилище транзисторов: обозначения, электрические параметры, предельные данные.</summary>
public interface ITransistorDatabase : IDisposable
{
    void EnsureCreated();
    UpsertOutcome Save(Transistor transistor, TransistorDetails? details);
    int? FindId(Transistor transistor);
    IReadOnlyList<Transistor> FindMaterialEquivalents(Transistor transistor);
    bool Delete(Transistor transistor);
    int CountAll();
    IReadOnlyList<Transistor> Query(TransistorQuery query);
    TransistorAttributes? GetAttributes(int transistorId);
    IReadOnlyList<ElectricalParameter> GetParameters(int transistorId);
    MaximumRatings? GetRatings(int transistorId);
    IReadOnlyList<string> GetManufacturers(int transistorId);
}
