using System.Data.Common;
using System.Globalization;
using System.Text;
using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Data;

/// <summary>
/// Переносимая реляционная реализация: DML использует только переносимые конструкции
/// (именованные параметры @имя, LIMIT, производные таблицы), совместимые с SQLite,
/// PostgreSQL и MariaDB. Булевы значения хранятся как INTEGER 0/1, отсутствие
/// необязательного значения — NULL; сравнение колонок, допускающих NULL, —
/// переносимый предикат «col = @p OR (col IS NULL AND @p IS NULL)» без сенти́нелов.
/// Многошаговые операции записи (Save, Delete) выполняются в транзакции
/// соединения: сбой в середине не оставляет частично применённую запись.
/// Соединение — одно на экземпляр: открывается лениво при первом обращении и закрывается в Dispose.
/// Для новой СУБД достаточно унаследовать класс и переопределить OpenConnection(),
/// CreateTableSql и LastInsertIdSql.
/// </summary>
public abstract class RelationalTransistorDatabase : ITransistorDatabase
{
    // Центральные спецификации чтения: из одного массива собирается и текст SELECT
    // (SelectFrom), и разрешение ординалов по именам (RowReader) — порядок колонок в
    // запросе не влияет на чтение, а рассинхронизация SELECT и материализации невозможна.
    private static readonly string[] TransistorColumns =
        ["Material", "Subclass", "Assembly", "Feature", "DevNumber", "Letters", "Modification", "ChipVariant"];

    // Список полей атрибутов — не копия, а TransistorAttributes.FieldNames (единый источник
    // вместе с chk_any_attribute и проверкой «задано хоть одно поле»).
    private static readonly string[] AttributeColumns = [.. TransistorAttributes.FieldNames];

    private static readonly string[] RatingColumns =
    [
        "UkeMax", "UkbMax", "UbeMax", "UkeoMax", "IkMax", "IbMax", "PkMax",
        "IkPulseMax", "PkPulseMax", "PulseDuration",
        "TempMin", "TempMax", "TempJunctionMax", "Rth"
    ];

    // условия измерения — из того же источника, из которого собирается DDL electrical_parameters
    private static readonly string[] ConditionColumns =
        [.. ConditionKeys.All.Select(ConditionKeys.Name), "Temp"];

    private static readonly string[] ParameterColumns =
        ["Parameter", "ValueMin", "ValueMax", .. ConditionColumns];

    private static string SelectFrom(string table, IReadOnlyList<string> columns) =>
        "SELECT " + string.Join(", ", columns) + " FROM " + table;

    private DbConnection? _connection;

    /// <summary>
    /// Единственное соединение экземпляра: создаётся при первом обращении (диалектозависимые
    /// настройки соединения — PRAGMA и т.п. — выполняются в OpenConnection один раз) и
    /// переиспользуется всеми методами до Dispose.
    /// </summary>
    protected DbConnection Connection => _connection ??= OpenConnection();

    protected abstract DbConnection OpenConnection();

    protected abstract string CreateTableSql { get; }

    public void EnsureCreated()
    {
        DbConnection connection = Connection;
        using DbCommand command = connection.CreateCommand();
        command.CommandText = CreateTableSql;
        command.ExecuteNonQuery();
    }

    public UpsertOutcome Save(Transistor transistor, TransistorDetails? details)
    {
        ValidateTransistor(transistor);
        ValidateDetails(transistor, details);

        // «секций нет» ⟺ details null либо все секции null: запись без секций либо
        // вставляется, либо пропускается — id и чистка сирот в этих путях не нужны.
        bool hasSections = details?.Attributes is not null || details?.Manufacturers is not null
            || details?.Parameters is not null || details?.Ratings is not null;

        DbConnection connection = Connection;
        using DbTransaction transaction = connection.BeginTransaction();
        int id;
        UpsertOutcome outcome;
        if (InsertTransistorIfAbsent(connection, transaction, transistor))
        {
            outcome = UpsertOutcome.Added;
            id = hasSections ? GetLastInsertId(connection, transaction) : 0;
        }
        else if (!hasSections)
        {
            outcome = UpsertOutcome.Skipped;
            id = 0;
        }
        else
        {
            id = FindIdCore(connection, transaction, transistor)
                ?? throw new InvalidOperationException($"не удалось определить id записи «{transistor.Name}»");
            outcome = UpsertOutcome.UpdatedExisting;
        }

        if (details is not null)
        {
            if (details.Attributes is { } attributes)
            {
                if (TransistorAttributes.HasAnyValue(attributes))
                {
                    SetAttributesCore(connection, transaction, id, attributes);
                }
                else if (details.Manufacturers is null)
                {
                    // секция задана пустой (без manufacturers) — очистить атрибуты;
                    // если задан только manufacturers — атрибуты не трогаем
                    ClearAttributesCore(connection, transaction, id);
                }
            }
            if (details.Manufacturers is { } manufacturers)
            {
                SetManufacturersCore(connection, transaction, id, manufacturers);
            }
            if (details.Parameters is { } parameters)
            {
                ReplaceParametersCore(connection, transaction, id, parameters);
            }
            if (details.Ratings is { } ratings)
            {
                SetRatingsCore(connection, transaction, id, ratings);
            }
        }

        if (hasSections)
        {
            // применялись секции — могла отвязаться ссылка производителя
            DeleteOrphanManufacturers(connection, transaction);
        }
        transaction.Commit();
        return outcome;
    }

    public int? FindId(Transistor transistor)
    {
        return FindIdCore(Connection, transaction: null, transistor);
    }

    public IReadOnlyList<Transistor> FindMaterialEquivalents(Transistor transistor)
    {
        using DbCommand command = Connection.CreateCommand();
        command.CommandText = SelectFrom("transistors", TransistorColumns) + " WHERE "
            + BuildMaterialCounterpartMatch(command, transistor);
        using DbDataReader reader = command.ExecuteReader();
        var row = new RowReader(reader, TransistorColumns);
        var rows = new List<Transistor>();
        while (reader.Read())
        {
            rows.Add(ReadTransistor(row));
        }
        return rows;
    }

    public bool Delete(Transistor transistor)
    {
        DbConnection connection = Connection;
        using DbTransaction transaction = connection.BeginTransaction();
        int removed;
        using (DbCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM transistors WHERE " + BuildExactMatch(command, transistor);
            removed = command.ExecuteNonQuery();
        }
        if (removed > 0)
        {
            DeleteOrphanManufacturers(connection, transaction);
        }
        transaction.Commit();
        return removed > 0;
    }

    public int CountAll()
    {
        using DbCommand command = Connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM transistors";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public IReadOnlyList<Transistor> Query(TransistorQuery query)
    {
        using DbCommand command = Connection.CreateCommand();

        var sql = new StringBuilder();
        sql.Append(SelectFrom("transistors", TransistorColumns));

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
            conditions.Add("Assembly = @assembly");
            AddParameter(command, "@assembly", assembly ? 1 : 0);
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
            conditions.Add("Modification = @modification");
            AddParameter(command, "@modification", modification);
        }
        if (query.ChipVariant is int chip)
        {
            conditions.Add("ChipVariant = @chip");
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
        var row = new RowReader(reader, TransistorColumns);
        var rows = new List<Transistor>();
        while (reader.Read())
        {
            rows.Add(ReadTransistor(row));
        }
        return rows;
    }

    public TransistorAttributes? GetAttributes(int transistorId)
    {
        using DbCommand command = Connection.CreateCommand();
        command.CommandText = SelectFrom("transistor_attributes", AttributeColumns) + " WHERE TransistorId = @id";
        AddParameter(command, "@id", transistorId);
        using DbDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }
        var row = new RowReader(reader, AttributeColumns);
        return new TransistorAttributes
        {
            Structure = row.GetStringOrNull("Structure"),
            Technology = row.GetStringOrNull("Technology"),
            Package = row.GetStringOrNull("Package"),
            PackageMaterial = row.GetStringOrNull("PackageMaterial"),
            ColorMarking = row.GetStringOrNull("ColorMarking"),
            Pinout = row.GetStringOrNull("Pinout"),
            EsdSensitive = row.GetBoolOrNull("EsdSensitive"),
            MilitaryGrade = row.GetBoolOrNull("MilitaryGrade"),
            RadiationHardened = row.GetBoolOrNull("RadiationHardened"),
            Tu = row.GetStringOrNull("Tu"),
            Notes = row.GetStringOrNull("Notes"),
            YearFrom = row.GetInt32OrNull("YearFrom"),
            YearTo = row.GetInt32OrNull("YearTo"),
            MassMax = row.GetDoubleOrNull("MassMax"),
            DatasheetUrl = row.GetStringOrNull("DatasheetUrl"),
        };
    }

    public IReadOnlyList<string> GetManufacturers(int transistorId)
    {
        using DbCommand command = Connection.CreateCommand();
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
        using DbCommand command = Connection.CreateCommand();
        command.CommandText = SelectFrom("electrical_parameters", ParameterColumns)
            + " WHERE TransistorId = @id ORDER BY Id";
        AddParameter(command, "@id", transistorId);
        using DbDataReader reader = command.ExecuteReader();
        var row = new RowReader(reader, ParameterColumns);
        var rows = new List<ElectricalParameter>();
        while (reader.Read())
        {
            string code = row.GetString("Parameter");
            if (!ElectricalParameterCatalog.TryGetByCode(code, out ParameterKind kind))
            {
                throw new InvalidOperationException($"в базе найден неизвестный код параметра «{code}»");
            }
            rows.Add(new ElectricalParameter
            {
                Kind = kind,
                ValueMin = row.GetDoubleOrNull("ValueMin"),
                ValueMax = row.GetDoubleOrNull("ValueMax"),
                Uke = row.GetDoubleOrNull("Uke"),
                Ukb = row.GetDoubleOrNull("Ukb"),
                Ueb = row.GetDoubleOrNull("Ueb"),
                Ik = row.GetDoubleOrNull("Ik"),
                Ie = row.GetDoubleOrNull("Ie"),
                Ib = row.GetDoubleOrNull("Ib"),
                Freq = row.GetDoubleOrNull("Freq"),
                Rg = row.GetDoubleOrNull("Rg"),
                Rbe = row.GetDoubleOrNull("Rbe"),
                Temp = row.GetDoubleOrNull("Temp"),
            });
        }
        return rows;
    }

    public MaximumRatings? GetRatings(int transistorId)
    {
        using DbCommand command = Connection.CreateCommand();
        command.CommandText = SelectFrom("maximum_ratings", RatingColumns) + " WHERE TransistorId = @id";
        AddParameter(command, "@id", transistorId);
        using DbDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }
        var row = new RowReader(reader, RatingColumns);
        return new MaximumRatings
        {
            UkeMax = row.GetDoubleOrNull("UkeMax"),
            UkbMax = row.GetDoubleOrNull("UkbMax"),
            UbeMax = row.GetDoubleOrNull("UbeMax"),
            UkeoMax = row.GetDoubleOrNull("UkeoMax"),
            IkMax = row.GetDoubleOrNull("IkMax"),
            IbMax = row.GetDoubleOrNull("IbMax"),
            PkMax = row.GetDoubleOrNull("PkMax"),
            IkPulseMax = row.GetDoubleOrNull("IkPulseMax"),
            PkPulseMax = row.GetDoubleOrNull("PkPulseMax"),
            PulseDuration = row.GetDoubleOrNull("PulseDuration"),
            TempMin = row.GetDoubleOrNull("TempMin"),
            TempMax = row.GetDoubleOrNull("TempMax"),
            TempJunctionMax = row.GetDoubleOrNull("TempJunctionMax"),
            Rth = row.GetDoubleOrNull("Rth"),
        };
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
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

    private int? FindIdCore(DbConnection connection, DbTransaction? transaction, Transistor transistor)
    {
        using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT Id FROM transistors WHERE " + BuildExactMatch(command, transistor);
        object? result = command.ExecuteScalar();
        return result is null || result is DBNull ? null : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Вставка «если нет точной записи»: одна команда вместо «найти, затем вставить» —
    /// без гонки на дубликат и без лишнего SELECT. Предикат тождества совпадает с
    /// UNIQUE-ограничением uq_transistor.
    /// Возвращает false, если запись уже существует (ничего не вставлено).
    /// </summary>
    private static bool InsertTransistorIfAbsent(DbConnection connection, DbTransaction? transaction, Transistor transistor)
    {
        using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        // однострочная производная таблица вместо SELECT без FROM:
        // MariaDB требует FROM при использовании WHERE
        command.CommandText = """
            INSERT INTO transistors
                (Material, Subclass, Assembly, Feature, DevNumber, Letters, Modification, ChipVariant)
            SELECT @material, @subclass, @assembly, @feature, @dev_number, @letters, @modification, @chip_variant
            FROM (SELECT 1) AS src
            WHERE NOT EXISTS (SELECT 1 FROM transistors WHERE
            """ + " " + BuildExactMatch(command, transistor) + ")";
        AddParameter(command, "@material", transistor.Material.ToString());
        AddParameter(command, "@subclass", transistor.Subclass.ToString());
        AddParameter(command, "@assembly", transistor.IsAssembly ? 1 : 0);
        AddParameter(command, "@feature", transistor.Feature);
        AddParameter(command, "@dev_number", transistor.DevelopmentNumber);
        AddParameter(command, "@letters", transistor.Letters);
        AddParameter(command, "@modification", transistor.Modification is int m ? m : DBNull.Value);
        AddParameter(command, "@chip_variant", transistor.ChipVariant is int c ? c : DBNull.Value);
        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>Возвращает id строки, вставленной в этом соединении последней (сразу после INSERT).</summary>
    private int GetLastInsertId(DbConnection connection, DbTransaction transaction)
    {
        using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = LastInsertIdSql;
        object? result = command.ExecuteScalar();
        return result is null || result is DBNull
            ? throw new InvalidOperationException("не удалось получить id вставленной записи")
            : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// SQL-запрос, возвращающий id последней вставленной в текущем соединении строки.
    /// Единственный диалектозависимый элемент DML (SQLite — last_insert_rowid(),
    /// PostgreSQL — lastval(), MariaDB — LAST_INSERT_ID()), переопределяется вместе с OpenConnection().
    /// </summary>
    protected abstract string LastInsertIdSql { get; }

    private static void ClearAttributesCore(DbConnection connection, DbTransaction transaction, int transistorId)
    {
        using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM transistor_attributes WHERE TransistorId = @id";
        AddParameter(command, "@id", transistorId);
        command.ExecuteNonQuery();
    }

    private static void SetAttributesCore(DbConnection connection, DbTransaction transaction, int transistorId, TransistorAttributes attributes)
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
            update.Transaction = transaction;
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
        insert.Transaction = transaction;
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

    private static void SetManufacturersCore(DbConnection connection, DbTransaction transaction, int transistorId, IReadOnlyList<string> manufacturers)
    {
        using (DbCommand delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
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
        if (names.Count == 0) return;

        // два подготовленных один раз запроса на каждое имя вместо трёх:
        // «создать имя, если его нет» + «связать с транзистором»
        using DbCommand ensureManufacturer = connection.CreateCommand();
        ensureManufacturer.Transaction = transaction;
        ensureManufacturer.CommandText = """
            INSERT INTO manufacturers (Name)
            SELECT @name FROM (SELECT 1) AS src
            WHERE NOT EXISTS (SELECT 1 FROM manufacturers WHERE Name = @name)
            """;
        DbParameter ensureName = AddParameter(ensureManufacturer, "@name", "");

        using DbCommand link = connection.CreateCommand();
        link.Transaction = transaction;
        link.CommandText = """
            INSERT INTO transistor_manufacturers (TransistorId, ManufacturerId)
            SELECT @id, Id FROM manufacturers WHERE Name = @name
            """;
        AddParameter(link, "@id", transistorId);
        DbParameter linkName = AddParameter(link, "@name", "");

        foreach (string name in names)
        {
            ensureName.Value = name;
            ensureManufacturer.ExecuteNonQuery();
            linkName.Value = name;
            link.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Чистка бесхозных имён производителей: связи transistor_manufacturers удаляются
    /// при замене списка производителей и каскадом при delete транзистора, а сами строки
    /// manufacturers FK-каскад не трогает — без этой чистки они копились бы бесконечно.
    /// Выполняется в конце Save/Delete внутри их транзакции. NOT IN переносим
    /// (SQLite/PostgreSQL/MariaDB); ManufacturerId NOT NULL — ловушки NULL в подзапросе нет.
    /// </summary>
    private static void DeleteOrphanManufacturers(DbConnection connection, DbTransaction transaction)
    {
        using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "DELETE FROM manufacturers WHERE Id NOT IN (SELECT ManufacturerId FROM transistor_manufacturers)";
        command.ExecuteNonQuery();
    }

    private static void SetRatingsCore(DbConnection connection, DbTransaction transaction, int transistorId, MaximumRatings ratings)
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
            update.Transaction = transaction;
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
        insert.Transaction = transaction;
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

    private static void ReplaceParametersCore(DbConnection connection, DbTransaction transaction, int transistorId, IReadOnlyList<ElectricalParameter> parameters)
    {
        using (DbCommand delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM electrical_parameters WHERE TransistorId = @id";
            AddParameter(delete, "@id", transistorId);
            delete.ExecuteNonQuery();
        }
        if (parameters.Count == 0) return;

        // команда готовится один раз и переиспользуется для всех строк
        using DbCommand insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO electrical_parameters
                (TransistorId, Parameter, ValueMin, ValueMax, Uke, Ukb, Ueb, Ik, Ie, Ib, Freq, Rg, Rbe, Temp)
            VALUES
                (@id, @parameter, @valueMin, @valueMax, @uKe, @uKb, @uEb, @iK, @iE, @iB, @freq, @rg, @rbe, @temp)
            """;
        AddParameter(insert, "@id", transistorId);
        DbParameter pParameter = AddParameter(insert, "@parameter", "");
        DbParameter pValueMin = AddParameter(insert, "@valueMin", DBNull.Value);
        DbParameter pValueMax = AddParameter(insert, "@valueMax", DBNull.Value);
        DbParameter pUke = AddParameter(insert, "@uKe", DBNull.Value);
        DbParameter pUkb = AddParameter(insert, "@uKb", DBNull.Value);
        DbParameter pUeb = AddParameter(insert, "@uEb", DBNull.Value);
        DbParameter pIk = AddParameter(insert, "@iK", DBNull.Value);
        DbParameter pIe = AddParameter(insert, "@iE", DBNull.Value);
        DbParameter pIb = AddParameter(insert, "@iB", DBNull.Value);
        DbParameter pFreq = AddParameter(insert, "@freq", DBNull.Value);
        DbParameter pRg = AddParameter(insert, "@rg", DBNull.Value);
        DbParameter pRbe = AddParameter(insert, "@rbe", DBNull.Value);
        DbParameter pTemp = AddParameter(insert, "@temp", DBNull.Value);
        foreach (ElectricalParameter parameter in parameters)
        {
            pParameter.Value = ElectricalParameterCatalog.Info(parameter.Kind).Code;
            pValueMin.Value = Box(parameter.ValueMin);
            pValueMax.Value = Box(parameter.ValueMax);
            pUke.Value = Box(parameter.Uke);
            pUkb.Value = Box(parameter.Ukb);
            pUeb.Value = Box(parameter.Ueb);
            pIk.Value = Box(parameter.Ik);
            pIe.Value = Box(parameter.Ie);
            pIb.Value = Box(parameter.Ib);
            pFreq.Value = Box(parameter.Freq);
            pRg.Value = Box(parameter.Rg);
            pRbe.Value = Box(parameter.Rbe);
            pTemp.Value = Box(parameter.Temp);
            insert.ExecuteNonQuery();
        }
    }

    private static Transistor ReadTransistor(RowReader row) => new()
    {
        Material = row.GetChar("Material"),
        Subclass = row.GetChar("Subclass"),
        IsAssembly = row.GetBool("Assembly"),
        Feature = row.GetInt32("Feature"),
        DevelopmentNumber = row.GetInt32("DevNumber"),
        Letters = row.GetString("Letters"),
        Modification = row.GetInt32OrNull("Modification"),
        ChipVariant = row.GetInt32OrNull("ChipVariant"),
    };

    /// <summary>
    /// Чтение строки по именам колонок: ординалы один раз разрешаются из той же
    /// спецификации колонок, из которой собран SELECT, поэтому порядок колонок в
    /// запросе не влияет на результат, а опечатка в имени падает громко, а не
    /// молча читает соседнюю колонку. Булевы читаются из INTEGER 0/1.
    /// </summary>
    private sealed class RowReader
    {
        private readonly DbDataReader _reader;
        private readonly Dictionary<string, int> _ordinals;

        public RowReader(DbDataReader reader, IReadOnlyList<string> columns)
        {
            _reader = reader;
            _ordinals = new Dictionary<string, int>(columns.Count, StringComparer.Ordinal);
            foreach (string column in columns)
            {
                _ordinals[column] = reader.GetOrdinal(column);
            }
        }

        private int Ordinal(string column) => _ordinals.TryGetValue(column, out int ordinal)
            ? ordinal
            : throw new InvalidOperationException($"в SELECT нет колонки «{column}»");

        public string GetString(string column) => _reader.GetString(Ordinal(column));

        public char GetChar(string column) => _reader.GetString(Ordinal(column))[0];

        public int GetInt32(string column) => _reader.GetInt32(Ordinal(column));

        public bool GetBool(string column) => _reader.GetInt32(Ordinal(column)) != 0;

        public string? GetStringOrNull(string column)
        {
            int ordinal = Ordinal(column);
            return _reader.IsDBNull(ordinal) ? null : _reader.GetString(ordinal);
        }

        public int? GetInt32OrNull(string column)
        {
            int ordinal = Ordinal(column);
            return _reader.IsDBNull(ordinal) ? null : _reader.GetInt32(ordinal);
        }

        public bool? GetBoolOrNull(string column)
        {
            int ordinal = Ordinal(column);
            return _reader.IsDBNull(ordinal) ? null : _reader.GetInt32(ordinal) != 0;
        }

        public double? GetDoubleOrNull(string column)
        {
            int ordinal = Ordinal(column);
            return _reader.IsDBNull(ordinal) ? null : _reader.GetDouble(ordinal);
        }
    }

    private static object Box(double? value) => value is double v ? v : DBNull.Value;

    private static string BuildExactMatch(DbCommand command, Transistor t)
    {
        AddParameter(command, "@eqMaterial", t.Material.ToString());
        return "Material = @eqMaterial AND " + AppendDesignationColumns(command, t, "@eq");
    }

    private static string BuildMaterialCounterpartMatch(DbCommand command, Transistor t)
    {
        (char letter, char digit) = Materials.SymbolsOf(Materials.KindOf(t.Material));
        AddParameter(command, "@cpMaterial", (t.Material == letter ? digit : letter).ToString());
        return "Material = @cpMaterial AND " + AppendDesignationColumns(command, t, "@cp");
    }

    private static string AppendDesignationColumns(DbCommand command, Transistor t, string p)
    {
        AddParameter(command, p + "Subclass", t.Subclass.ToString());
        AddParameter(command, p + "Feature", t.Feature);
        AddParameter(command, p + "DevNumber", t.DevelopmentNumber);
        AddParameter(command, p + "Letters", t.Letters);
        AddParameter(command, p + "Assembly", t.IsAssembly ? 1 : 0);
        AddParameter(command, p + "Modification", t.Modification is int m ? m : DBNull.Value);
        AddParameter(command, p + "ChipVariant", t.ChipVariant is int c ? c : DBNull.Value);
        // обязательные колонки и Assembly (NOT NULL 0/1) — простое равенство;
        // опциональные — NULL-безопасное равенство без сенти́нелов
        return $"""
            Subclass = {p}Subclass
            AND Feature = {p}Feature
            AND DevNumber = {p}DevNumber
            AND Letters = {p}Letters
            AND Assembly = {p}Assembly
            AND (Modification = {p}Modification OR (Modification IS NULL AND {p}Modification IS NULL))
            AND (ChipVariant = {p}ChipVariant OR (ChipVariant IS NULL AND {p}ChipVariant IS NULL))
            """;
    }

    private static DbParameter AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
        return parameter;
    }
}
