using System.Globalization;
using System.Text;
using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Data;

/// <summary>
/// Сборка DDL таблицы electrical_parameters из каталога: список кодов, правила границ и комбинации
/// условий генерируются из <see cref="ElectricalParameterCatalog"/> и не дублируются вручную.
/// </summary>
public static class ElectricalParametersDdl
{
    public static string CreateTableSql()
    {
        var sql = new StringBuilder();
        sql.AppendLine("CREATE TABLE IF NOT EXISTS electrical_parameters (");
        sql.AppendLine("    Id            INTEGER PRIMARY KEY AUTOINCREMENT,");
        sql.AppendLine("    TransistorId  INTEGER NOT NULL REFERENCES transistors(Id) ON DELETE CASCADE,");
        sql.AppendLine($"    Parameter     TEXT    NOT NULL,   -- {string.Join(" | ", OrderedCodes())}");
        sql.AppendLine("    ValueMin      REAL        NULL,   -- «не менее» / нижняя граница (единица — из каталога параметров)");
        sql.AppendLine("    ValueMax      REAL        NULL,   -- «не более» / верхняя граница");
        foreach (ConditionKey key in ConditionKeys.All)
            sql.AppendLine($"    {ConditionKeys.Name(key),-14}{"REAL",-12}NULL,   -- условие: {ConditionKeys.Description(key)}");
        sql.AppendLine("    Temp          REAL        NULL,   -- условие: температура среды, °C (допустима у любого параметра)");
        sql.AppendLine($"    CONSTRAINT chk_param_code CHECK (Parameter IN ({string.Join(", ", OrderedCodes().Select(code => $"'{code}'"))})),");
        AppendValueBounds(sql);
        AppendConditions(sql);
        sql.Append("    );");
        return sql.ToString();
    }

    private static void AppendValueBounds(StringBuilder sql)
    {
        sql.AppendLine("    CONSTRAINT chk_param_value CHECK (");
        sql.AppendLine("        CASE Parameter");
        foreach (ParameterInfo info in OrderedParameters())
        {
            string bounds = info.Direction switch
            {
                BoundDirection.AtLeast => "ValueMin IS NOT NULL AND ValueMax IS NULL",
                BoundDirection.AtMost => "ValueMax IS NOT NULL AND ValueMin IS NULL",
                BoundDirection.AtLeastOrRange => "ValueMin IS NOT NULL AND (ValueMax IS NULL OR ValueMin <= ValueMax)",
                _ => throw new InvalidOperationException($"неизвестное правило границ: {info.Direction}"),
            };
            if (info.ValueCeiling is double ceiling)
                bounds += $" AND ValueMin <= {ceiling.ToString(CultureInfo.InvariantCulture)}";
            sql.AppendLine($"            WHEN '{info.Code}' THEN CASE WHEN {bounds} THEN 1 ELSE 0 END");
        }
        sql.AppendLine("            ELSE 0");
        sql.AppendLine("        END = 1");
        sql.AppendLine("    ),");
    }

    private static void AppendConditions(StringBuilder sql)
    {
        sql.AppendLine("    CONSTRAINT chk_param_conditions CHECK (");
        sql.AppendLine("        CASE Parameter");
        foreach (ParameterInfo info in OrderedParameters())
        {
            if (info.Conditions.Variants.Count == 1)
            {
                sql.AppendLine($"            WHEN '{info.Code}' THEN CASE WHEN {VariantSql(info.Conditions.Variants[0])} THEN 1 ELSE 0 END");
                continue;
            }
            sql.AppendLine($"            WHEN '{info.Code}' THEN CASE WHEN");
            for (int index = 0; index < info.Conditions.Variants.Count; index++)
            {
                string expression = VariantSql(info.Conditions.Variants[index]);
                sql.AppendLine(index == 0 ? $"                {expression}" : $"             OR {expression}");
            }
            sql.AppendLine("                THEN 1 ELSE 0 END");
        }
        sql.AppendLine("            ELSE 0");
        sql.AppendLine("        END = 1");
        sql.AppendLine("    )");
    }

    private static string VariantSql(ConditionVariant variant)
    {
        var parts = new List<string>();
        parts.AddRange(Ordered(variant.Required).Select(key => $"{ConditionKeys.Name(key)} IS NOT NULL"));
        parts.AddRange(Ordered(Forbidden(variant)).Select(key => $"{ConditionKeys.Name(key)} IS NULL"));
        return string.Join(" AND ", parts);
    }

    private static IReadOnlySet<ConditionKey> Forbidden(ConditionVariant variant) =>
        ConditionKeys.All.Where(key => !variant.Required.Contains(key) && !variant.Optional.Contains(key)).ToHashSet();

    private static IEnumerable<ConditionKey> Ordered(IReadOnlySet<ConditionKey> keys) =>
        ConditionKeys.All.Where(keys.Contains);

    private static IEnumerable<ParameterInfo> OrderedParameters() =>
        ElectricalParameterCatalog.All.Values.OrderBy(info => info.Kind);

    private static IEnumerable<string> OrderedCodes() =>
        OrderedParameters().Select(info => info.Code);
}
