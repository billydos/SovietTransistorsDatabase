using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Data;

/// <summary>
/// Сборка фрагментов DDL таблицы transistor_attributes из <see cref="TransistorAttributes"/>:
/// chk_any_attribute генерируется из единого списка полей записи и не дублируется вручную.
/// </summary>
public static class AttributesDdl
{
    /// <summary>Предикат «задан хотя бы один атрибут»; по три колонки на строку, как в остальном DDL.</summary>
    public static string AnyAttributeCheckSql()
    {
        var lines = new List<string>();
        IReadOnlyList<string> names = TransistorAttributes.FieldNames;
        for (int index = 0; index < names.Count; index += 3)
        {
            lines.Add(string.Join(" OR ",
                names.Skip(index).Take(3).Select(name => $"{name} IS NOT NULL")));
        }
        return string.Join("\n        OR ", lines);
    }
}