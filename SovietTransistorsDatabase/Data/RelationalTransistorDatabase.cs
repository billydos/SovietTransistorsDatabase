using System.Data.Common;
using System.Globalization;
using System.Text;
using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Data;

/// <summary>
/// Переносимая реляционная реализация: DML использует только стандартные конструкции
/// (именованные параметры @имя, COALESCE, LIMIT), совместимые с SQLite, PostgreSQL и MariaDB.
/// Для новой СУБД достаточно унаследовать класс и переопределить OpenConnection() и CreateTableSql.
/// </summary>
public abstract class RelationalTransistorDatabase : ITransistorDatabase
{
    private const string TransistorColumns =
        "Id, Material, Subclass, Assembly, Feature, DevNumber, Letters, Modification, ChipVariant";

    protected abstract DbConnection OpenConnection();

    protected abstract string CreateTableSql { get; }

    public void EnsureCreated()
    {
        using DbConnection connection = OpenConnection();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = CreateTableSql;
        command.ExecuteNonQuery();
    }

    public InsertOutcome Add(Transistor transistor)
    {
        ValidateTransistor(transistor);
        using DbConnection connection = OpenConnection();
        if (FindIdCore(connection, transistor) is not null)
        {
            return InsertOutcome.DuplicateExists;
        }
        InsertTransistorCore(connection, transistor);
        return InsertOutcome.Added;
    }

    public UpsertOutcome Save(Transistor transistor, TransistorDetails? details)
    {
        ValidateTransistor(transistor);
        ValidateDetails(transistor, details);

        using DbConnection connection = OpenConnection();
        int? id = FindIdCore(connection, transistor);
        UpsertOutcome outcome;
        if (id is null)
        {
            InsertTransistorCore(connection, transistor);
            id = FindIdCore(connection, transistor)
                ?? throw new InvalidOperationException($"не удалось определить id записи «{transistor.Name}»");
            outcome = UpsertOutcome.Added;
        }
        else
        {
            outcome = UpsertOutcome.UpdatedExisting;
        }

        if (details is null)
        {
            return outcome;
        }
        if (details.Attributes is { } attributes)
        {
            if (HasAnyAttributeField(attributes))
            {
                SetAttributesCore(connection, id.Value, attributes);
            }
            else if (details.Manufacturers is null)
            {
                // секция задана пустой (без manufacturers) — очистить атрибуты;
                // если задан только manufacturers — атрибуты не трогаем
                ClearAttributesCore(connection, id.Value);
            }
        }
        if (details.Manufacturers is { } manufacturers)
        {
            SetManufacturersCore(connection, id.Value, manufacturers);
        }
        if (details.Parameters is { } parameters)
        {
            ReplaceParametersCore(connection, id.Value, parameters);
        }
        if (details.Ratings is { } ratings)
        {
            SetRatingsCore(connection, id.Value, ratings);
        }
        return outcome;
    }

    public Transistor? FindEquivalent(Transistor transistor)
    {
        using DbConnection connection = OpenConnection();
        return FindEquivalentCore(connection, transistor);
    }

    public int? FindId(Transistor transistor)
    {
        using DbConnection connection = OpenConnection();
        return FindIdCore(connection, transistor);
    }

    public bool Delete(Transistor transistor)
    {
        using DbConnection connection = OpenConnection();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM transistors WHERE " + BuildEquivalence(command, transistor);
        return command.ExecuteNonQuery() > 0;
    }

    public int CountAll()
    {
        using DbConnection connection = OpenConnection();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM transistors";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public IReadOnlyList<Transistor> Query(TransistorQuery query)
    {
        using DbConnection connection = OpenConnection();
        using DbCommand command = connection.CreateCommand();

        var sql = new StringBuilder();
        sql.Append("SELECT ").Append(TransistorColumns).Append(" FROM transistors");

        var conditions = new List<string>();
        if (query.Material is SemiconductorMaterial material)
        {
            (char letter, char digit) = Materials.SymbolsOf(material);
            conditions.Add("Material IN (@matLetter, @matDigit)");
            AddParameter(command, "@matLetter", letter.ToString());
            AddParameter(command, "@matDigit", digit.ToString());
        }
        if (query.Subclass is char subclass)
        {
            conditions.Add("Subclass = @subclass");
            AddParameter(command, "@subclass", subclass.ToString());
        }
        if (query.IsAssembly is bool assembly)
        {
            conditions.Add("COALESCE(Assembly, '') = COALESCE(@assembly, '')");
            AddParameter(command, "@assembly", assembly ? "С" : DBNull.Value);
        }
        if (query.Feature is int feature)
        {
            conditions.Add("Feature = @feature");
            AddParameter(command, "@feature", feature);
        }
        if (query.DevelopmentNumber is int number)
        {
            conditions.Add("DevNumber = @dev_number");
            AddParameter(command, "@dev_number", number);
        }
        if (!string.IsNullOrEmpty(query.Letters))
        {
            conditions.Add("Letters = @letters");
            AddParameter(command, "@letters", query.Letters);
        }
        if (query.Modification is int modification)
        {
            conditions.Add("COALESCE(Modification, -1) = COALESCE(@modification, -1)");
            AddParameter(command, "@modification", modification);
        }
        if (query.ChipVariant is int chip)
        {
            conditions.Add("COALESCE(ChipVariant, -1) = COALESCE(@chip, -1)");
            AddParameter(command, "@chip", chip);
        }
        if (conditions.Count > 0)
        {
            sql.Append(" WHERE ").Append(string.Join(" AND ", conditions));
        }

        sql.Append(" ORDER BY Subclass, Feature, DevNumber, Letters, Material, Modification, ChipVariant");

        if (query.Limit is int limit and > 0)
        {
            sql.Append(" LIMIT @limit");
            AddParameter(command, "@limit", limit);
        }

        command.CommandText = sql.ToString();
        using DbDataReader reader = command.ExecuteReader();
        var rows = new List<Transistor>();
        while (reader.Read())
        {
            rows.Add(ReadTransistor(reader));
        }
        return rows;
    }

    public TransistorAttributes? GetAttributes(int transistorId)
    {
        using DbConnection connection = OpenConnection();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Structure, Technology, Package, PackageMaterial, ColorMarking, Pinout,
                   EsdSensitive, MilitaryGrade, RadiationHardened, Tu, Notes,
                   YearFrom, YearTo, MassMax, DatasheetUrl
            FROM transistor_attributes
            WHERE TransistorId = @id
            """;
        AddParameter(command, "@id", transistorId);
        using DbDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }
        return new TransistorAttributes
        {
            Structure = GetStringOrNull(reader, 0),
            Technology = GetStringOrNull(reader, 1),
            Package = GetStringOrNull(reader, 2),
            PackageMaterial = GetStringOrNull(reader, 3),
            ColorMarking = GetStringOrNull(reader, 4),
            Pinout = GetStringOrNull(reader, 5),
            EsdSensitive = GetBoolOrNull(reader, 6),
            MilitaryGrade = GetBoolOrNull(reader, 7),
            RadiationHardened = GetBoolOrNull(reader, 8),
            Tu = GetStringOrNull(reader, 9),
            Notes = GetStringOrNull(reader, 10),
            YearFrom = reader.IsDBNull(11) ? null : reader.GetInt32(11),
            YearTo = reader.IsDBNull(12) ? null : reader.GetInt32(12),
            MassMax = GetNullableDouble(reader, 13),
            DatasheetUrl = GetStringOrNull(reader, 14),
        };
    }

    public IReadOnlyList<string> GetManufacturers(int transistorId)
    {
        using DbConnection connection = OpenConnection();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.Name
            FROM manufacturers m
            JOIN transistor_manufacturers tm ON tm.ManufacturerId = m.Id
            WHERE tm.TransistorId = @id
            ORDER BY tm.ManufacturerId
            """;
        AddParameter(command, "@id", transistorId);
        using DbDataReader reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }
        return names;
    }

    public IReadOnlyList<ElectricalParameter> GetParameters(int transistorId)
    {
        using DbConnection connection = OpenConnection();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Parameter, ValueMin, ValueMax, Uke, Ukb, Ueb, Ik, Ie, Ib, Freq, Rg, Rbe, Temp
            FROM electrical_parameters
            WHERE TransistorId = @id
            ORDER BY Id
            """;
        AddParameter(command, "@id", transistorId);
        using DbDataReader reader = command.ExecuteReader();
        var rows = new List<ElectricalParameter>();
        while (reader.Read())
        {
            string code = reader.GetString(0);
            if (!ElectricalParameterCatalog.TryGetByCode(code, out ParameterKind kind))
            {
                throw new InvalidOperationException($"в базе найден неизвестный код параметра «{code}»");
            }
            rows.Add(new ElectricalParameter
            {
                Kind = kind,
                ValueMin = GetNullableDouble(reader, 1),
                ValueMax = GetNullableDouble(reader, 2),
                Uke = GetNullableDouble(reader, 3),
                Ukb = GetNullableDouble(reader, 4),
                Ueb = GetNullableDouble(reader, 5),
                Ik = GetNullableDouble(reader, 6),
                Ie = GetNullableDouble(reader, 7),
                Ib = GetNullableDouble(reader, 8),
                Freq = GetNullableDouble(reader, 9),
                Rg = GetNullableDouble(reader, 10),
                Rbe = GetNullableDouble(reader, 11),
                Temp = GetNullableDouble(reader, 12),
            });
        }
        return rows;
    }

    public MaximumRatings? GetRatings(int transistorId)
    {
        using DbConnection connection = OpenConnection();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT UkeMax, UkbMax, UbeMax, UkeoMax, IkMax, IbMax, PkMax,
                   IkPulseMax, PkPulseMax, PulseDuration,
                   TempMin, TempMax, TempJunctionMax, Rth
            FROM maximum_ratings
            WHERE TransistorId = @id
            """;
        AddParameter(command, "@id", transistorId);
        using DbDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }
        return new MaximumRatings
        {
            UkeMax = GetNullableDouble(reader, 0),
            UkbMax = GetNullableDouble(reader, 1),
            UbeMax = GetNullableDouble(reader, 2),
            UkeoMax = GetNullableDouble(reader, 3),
            IkMax = GetNullableDouble(reader, 4),
            IbMax = GetNullableDouble(reader, 5),
            PkMax = GetNullableDouble(reader, 6),
            IkPulseMax = GetNullableDouble(reader, 7),
            PkPulseMax = GetNullableDouble(reader, 8),
            PulseDuration = GetNullableDouble(reader, 9),
            TempMin = GetNullableDouble(reader, 10),
            TempMax = GetNullableDouble(reader, 11),
            TempJunctionMax = GetNullableDouble(reader, 12),
            Rth = GetNullableDouble(reader, 13),
        };
    }

    public void Dispose()
    {
    }

    private static void ValidateTransistor(Transistor transistor)
    {
        var problems = TransistorValidator.Errors(transistor);
        if (problems.Count > 0)
        {
            throw new ArgumentException($"некорректный транзистор «{transistor.Name}»: {string.Join("; ", problems)}");
        }
    }

    private static void ValidateDetails(Transistor transistor, TransistorDetails? details)
    {
        if (details is null) return;
        if (details.Attributes is { } attributes)
        {
            var errors = TransistorAttributesValidator.Errors(attributes);
            if (errors.Count > 0)
            {
                throw new ArgumentException($"«{transistor.Name}», атрибуты: {string.Join("; ", errors)}");
            }
        }
        if (details.Manufacturers is { } manufacturers)
        {
            foreach (string manufacturer in manufacturers)
            {
                if (string.IsNullOrWhiteSpace(manufacturer))
                {
                    throw new ArgumentException($"«{transistor.Name}»: название производителя не может быть пустым");
                }
            }
        }
        if (details.Parameters is { } parameters)
        {
            foreach (ElectricalParameter parameter in parameters)
            {
                var errors = ElectricalParameterValidator.Errors(parameter);
                if (errors.Count > 0)
                {
                    string code = ElectricalParameterCatalog.Info(parameter.Kind).Code;
                    throw new ArgumentException($"«{transistor.Name}», параметр {code}: {string.Join("; ", errors)}");
                }
            }
        }
        if (details.Ratings is { } ratings)
        {
            var errors = MaximumRatingsValidator.Errors(ratings);
            if (errors.Count > 0)
            {
                throw new ArgumentException($"«{transistor.Name}», предельные данные: {string.Join("; ", errors)}");
            }
        }
    }

    private int? FindIdCore(DbConnection connection, Transistor transistor)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM transistors WHERE " + BuildEquivalence(command, transistor) + " LIMIT 1";
        object? result = command.ExecuteScalar();
        return result is null || result is DBNull ? null : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private Transistor? FindEquivalentCore(DbConnection connection, Transistor transistor)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT " + TransistorColumns + " FROM transistors WHERE " + BuildEquivalence(command, transistor) + " LIMIT 1";
        using DbDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadTransistor(reader) : null;
    }

    private static void InsertTransistorCore(DbConnection connection, Transistor transistor)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO transistors
                (Material, Subclass, Assembly, Feature, DevNumber, Letters, Modification, ChipVariant)
            VALUES
                (@material, @subclass, @assembly, @feature, @dev_number, @letters, @modification, @chip_variant)
            """;
        AddParameter(command, "@material", transistor.Material.ToString());
        AddParameter(command, "@subclass", transistor.Subclass.ToString());
        AddParameter(command, "@assembly", transistor.IsAssembly ? "С" : DBNull.Value);
        AddParameter(command, "@feature", transistor.Feature);
        AddParameter(command, "@dev_number", transistor.DevelopmentNumber);
        AddParameter(command, "@letters", transistor.Letters);
        AddParameter(command, "@modification", transistor.Modification is int m ? m : DBNull.Value);
        AddParameter(command, "@chip_variant", transistor.ChipVariant is int c ? c : DBNull.Value);
        command.ExecuteNonQuery();
    }

    private static bool HasAnyAttributeField(TransistorAttributes a) =>
        a.Structure is not null || a.Technology is not null || a.Package is not null
        || a.PackageMaterial is not null || a.ColorMarking is not null || a.Pinout is not null
        || a.EsdSensitive is not null || a.MilitaryGrade is not null || a.RadiationHardened is not null
        || a.Tu is not null || a.Notes is not null || a.YearFrom is not null || a.YearTo is not null
        || a.MassMax is not null || a.DatasheetUrl is not null;

    private static void ClearAttributesCore(DbConnection connection, int transistorId)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM transistor_attributes WHERE TransistorId = @id";
        AddParameter(command, "@id", transistorId);
        command.ExecuteNonQuery();
    }

    private static void SetAttributesCore(DbConnection connection, int transistorId, TransistorAttributes attributes)
    {
        void AddValues(DbCommand command)
        {
            AddParameter(command, "@structure", attributes.Structure is string s ? s.Trim() : DBNull.Value);
            AddParameter(command, "@technology", attributes.Technology is string t ? t.Trim() : DBNull.Value);
            AddParameter(command, "@package", attributes.Package is string p ? p.Trim() : DBNull.Value);
            AddParameter(command, "@packageMaterial", attributes.PackageMaterial is string pm ? pm.Trim() : DBNull.Value);
            AddParameter(command, "@color", attributes.ColorMarking is string c ? c.Trim() : DBNull.Value);
            AddParameter(command, "@pinout", attributes.Pinout is string po ? po.Trim() : DBNull.Value);
            AddParameter(command, "@esd", attributes.EsdSensitive is bool esd ? (esd ? 1 : 0) : DBNull.Value);
            AddParameter(command, "@military", attributes.MilitaryGrade is bool military ? (military ? 1 : 0) : DBNull.Value);
            AddParameter(command, "@radiation", attributes.RadiationHardened is bool rad ? (rad ? 1 : 0) : DBNull.Value);
            AddParameter(command, "@tu", attributes.Tu is string tu ? tu.Trim() : DBNull.Value);
            AddParameter(command, "@notes", attributes.Notes is string n ? n.Trim() : DBNull.Value);
            AddParameter(command, "@yearFrom", attributes.YearFrom is int yf ? yf : DBNull.Value);
            AddParameter(command, "@yearTo", attributes.YearTo is int yt ? yt : DBNull.Value);
            AddParameter(command, "@massMax", attributes.MassMax is double mass ? mass : DBNull.Value);
            AddParameter(command, "@url", attributes.DatasheetUrl is string url ? url.Trim() : DBNull.Value);
        }

        using (DbCommand update = connection.CreateCommand())
        {
            update.CommandText = """
                UPDATE transistor_attributes
                SET Structure = @structure, Technology = @technology, Package = @package,
                    PackageMaterial = @packageMaterial, ColorMarking = @color, Pinout = @pinout,
                    EsdSensitive = @esd, MilitaryGrade = @military, RadiationHardened = @radiation,
                    Tu = @tu, Notes = @notes, YearFrom = @yearFrom, YearTo = @yearTo,
                    MassMax = @massMax, DatasheetUrl = @url
                WHERE TransistorId = @id
                """;
            AddParameter(update, "@id", transistorId);
            AddValues(update);
            if (update.ExecuteNonQuery() > 0) return;
        }
        using DbCommand insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO transistor_attributes
                (TransistorId, Structure, Technology, Package, PackageMaterial, ColorMarking, Pinout,
                 EsdSensitive, MilitaryGrade, RadiationHardened, Tu, Notes,
                 YearFrom, YearTo, MassMax, DatasheetUrl)
            VALUES
                (@id, @structure, @technology, @package, @packageMaterial, @color, @pinout,
                 @esd, @military, @radiation, @tu, @notes, @yearFrom, @yearTo, @massMax, @url)
            """;
        AddParameter(insert, "@id", transistorId);
        AddValues(insert);
        insert.ExecuteNonQuery();
    }

    private static void SetManufacturersCore(DbConnection connection, int transistorId, IReadOnlyList<string> manufacturers)
    {
        using (DbCommand delete = connection.CreateCommand())
        {
            delete.CommandText = "DELETE FROM transistor_manufacturers WHERE TransistorId = @id";
            AddParameter(delete, "@id", transistorId);
            delete.ExecuteNonQuery();
        }
        // дубликаты в списке — одно и то же имя (множество)
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string rawName in manufacturers)
        {
            string name = rawName.Trim();
            if (seen.Add(name))
            {
                names.Add(name);
            }
        }
        foreach (string name in names)
        {
            using (DbCommand find = connection.CreateCommand())
            {
                find.CommandText = "SELECT Id FROM manufacturers WHERE Name = @name";
                AddParameter(find, "@name", name);
                object? found = find.ExecuteScalar();
                if (found is not null && found is not DBNull)
                {
                    continue;
                }
            }
            using DbCommand create = connection.CreateCommand();
            create.CommandText = "INSERT INTO manufacturers (Name) VALUES (@name)";
            AddParameter(create, "@name", name);
            create.ExecuteNonQuery();
        }
        foreach (string name in names)
        {
            using DbCommand link = connection.CreateCommand();
            link.CommandText = """
                INSERT INTO transistor_manufacturers (TransistorId, ManufacturerId)
                SELECT @id, Id FROM manufacturers WHERE Name = @name
                """;
            AddParameter(link, "@id", transistorId);
            AddParameter(link, "@name", name);
            link.ExecuteNonQuery();
        }
    }

    private static void SetRatingsCore(DbConnection connection, int transistorId, MaximumRatings ratings)
    {
        void AddValues(DbCommand command)
        {
            AddParameter(command, "@uKe", Box(ratings.UkeMax));
            AddParameter(command, "@uKb", Box(ratings.UkbMax));
            AddParameter(command, "@uBe", Box(ratings.UbeMax));
            AddParameter(command, "@uKeo", Box(ratings.UkeoMax));
            AddParameter(command, "@iK", Box(ratings.IkMax));
            AddParameter(command, "@iB", Box(ratings.IbMax));
            AddParameter(command, "@pK", Box(ratings.PkMax));
            AddParameter(command, "@iKP", Box(ratings.IkPulseMax));
            AddParameter(command, "@pKP", Box(ratings.PkPulseMax));
            AddParameter(command, "@dur", Box(ratings.PulseDuration));
            AddParameter(command, "@tMin", Box(ratings.TempMin));
            AddParameter(command, "@tMax", Box(ratings.TempMax));
            AddParameter(command, "@tJ", Box(ratings.TempJunctionMax));
            AddParameter(command, "@rTh", Box(ratings.Rth));
        }

        using (DbCommand update = connection.CreateCommand())
        {
            update.CommandText = """
                UPDATE maximum_ratings
                SET UkeMax = @uKe, UkbMax = @uKb, UbeMax = @uBe, UkeoMax = @uKeo,
                    IkMax = @iK, IbMax = @iB, PkMax = @pK,
                    IkPulseMax = @iKP, PkPulseMax = @pKP, PulseDuration = @dur,
                    TempMin = @tMin, TempMax = @tMax, TempJunctionMax = @tJ, Rth = @rTh
                WHERE TransistorId = @id
                """;
            AddParameter(update, "@id", transistorId);
            AddValues(update);
            if (update.ExecuteNonQuery() > 0) return;
        }
        using DbCommand insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO maximum_ratings
                (TransistorId, UkeMax, UkbMax, UbeMax, UkeoMax, IkMax, IbMax, PkMax,
                 IkPulseMax, PkPulseMax, PulseDuration, TempMin, TempMax, TempJunctionMax, Rth)
            VALUES
                (@id, @uKe, @uKb, @uBe, @uKeo, @iK, @iB, @pK, @iKP, @pKP, @dur, @tMin, @tMax, @tJ, @rTh)
            """;
        AddParameter(insert, "@id", transistorId);
        AddValues(insert);
        insert.ExecuteNonQuery();
    }

    private static void ReplaceParametersCore(DbConnection connection, int transistorId, IReadOnlyList<ElectricalParameter> parameters)
    {
        using (DbCommand delete = connection.CreateCommand())
        {
            delete.CommandText = "DELETE FROM electrical_parameters WHERE TransistorId = @id";
            AddParameter(delete, "@id", transistorId);
            delete.ExecuteNonQuery();
        }
        foreach (ElectricalParameter parameter in parameters)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO electrical_parameters
                    (TransistorId, Parameter, ValueMin, ValueMax, Uke, Ukb, Ueb, Ik, Ie, Ib, Freq, Rg, Rbe, Temp)
                VALUES
                    (@id, @parameter, @valueMin, @valueMax, @uKe, @uKb, @uEb, @iK, @iE, @iB, @freq, @rg, @rbe, @temp)
                """;
            AddParameter(command, "@id", transistorId);
            AddParameter(command, "@parameter", ElectricalParameterCatalog.Info(parameter.Kind).Code);
            AddParameter(command, "@valueMin", Box(parameter.ValueMin));
            AddParameter(command, "@valueMax", Box(parameter.ValueMax));
            AddParameter(command, "@uKe", Box(parameter.Uke));
            AddParameter(command, "@uKb", Box(parameter.Ukb));
            AddParameter(command, "@uEb", Box(parameter.Ueb));
            AddParameter(command, "@iK", Box(parameter.Ik));
            AddParameter(command, "@iE", Box(parameter.Ie));
            AddParameter(command, "@iB", Box(parameter.Ib));
            AddParameter(command, "@freq", Box(parameter.Freq));
            AddParameter(command, "@rg", Box(parameter.Rg));
            AddParameter(command, "@rbe", Box(parameter.Rbe));
            AddParameter(command, "@temp", Box(parameter.Temp));
            command.ExecuteNonQuery();
        }
    }

    private static Transistor ReadTransistor(DbDataReader reader) => new()
    {
        Material = reader.GetString(1)[0],
        Subclass = reader.GetString(2)[0],
        IsAssembly = !reader.IsDBNull(3) && reader.GetString(3) == "С",
        Feature = reader.GetInt32(4),
        DevelopmentNumber = reader.GetInt32(5),
        Letters = reader.GetString(6),
        Modification = reader.IsDBNull(7) ? null : reader.GetInt32(7),
        ChipVariant = reader.IsDBNull(8) ? null : reader.GetInt32(8),
    };

    private static double? GetNullableDouble(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);

    private static string? GetStringOrNull(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static bool? GetBoolOrNull(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToBoolean(reader.GetInt32(ordinal), CultureInfo.InvariantCulture);

    private static object Box(double? value) => value is double v ? v : DBNull.Value;

    private static string BuildEquivalence(DbCommand command, Transistor t)
    {
        (char letter, char digit) = Materials.SymbolsOf(Materials.KindOf(t.Material));
        AddParameter(command, "@eqMatLetter", letter.ToString());
        AddParameter(command, "@eqMatDigit", digit.ToString());
        AddParameter(command, "@eqSubclass", t.Subclass.ToString());
        AddParameter(command, "@eqFeature", t.Feature);
        AddParameter(command, "@eqDevNumber", t.DevelopmentNumber);
        AddParameter(command, "@eqLetters", t.Letters);
        AddParameter(command, "@eqAssembly", t.IsAssembly ? "С" : DBNull.Value);
        AddParameter(command, "@eqModification", t.Modification is int m ? m : DBNull.Value);
        AddParameter(command, "@eqChipVariant", t.ChipVariant is int c ? c : DBNull.Value);
        return """
            Material IN (@eqMatLetter, @eqMatDigit)
            AND Subclass = @eqSubclass
            AND Feature = @eqFeature
            AND DevNumber = @eqDevNumber
            AND Letters = @eqLetters
            AND COALESCE(Assembly, '') = COALESCE(@eqAssembly, '')
            AND COALESCE(Modification, -1) = COALESCE(@eqModification, -1)
            AND COALESCE(ChipVariant, -1) = COALESCE(@eqChipVariant, -1)
            """;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
