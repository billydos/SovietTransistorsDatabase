# AGENTS.md

Справочник советских транзисторов: консольная утилита .NET 10 (C#) + SQLite (UTF-8). Разбирает обозначения по ГОСТ 10862-64, хранит электрические параметры с условиями измерения, предельные данные и описательные атрибуты. Схема БД спроектирована переносимой на PostgreSQL/MariaDB.

## Команды

```shell
# сборка (должна быть без предупреждений)
dotnet build SovietTransistorsDatabase/SovietTransistorsDatabase.csproj -c Release

# запуск
dotnet run --project SovietTransistorsDatabase -- <команда> [аргументы] [--db <путь>] [--dry-run]

# восстановление рабочей базы из данных репозитория
dotnet run --project SovietTransistorsDatabase -- import SovietTransistorsDatabase/sample-data.jsonc

# проверка jsonc без записи в базу
dotnet run --project SovietTransistorsDatabase -- import <файл.jsonc> --dry-run

# unit-тесты Domain (xUnit)
dotnet test SovietTransistorsDatabase.Tests/SovietTransistorsDatabase.Tests.csproj -c Release
```

Команды CLI: `init`, `import`, `add`, `parse`, `list`, `info`, `find`, `delete`, `count` (полный справочник — `help` и README.md). Unit-тесты Domain (xUnit, `SovietTransistorsDatabase.Tests/`) покрывают парсер обозначений, валидаторы (все варианты условий каждого параметра) и каталог параметров — прогонять при правках `Domain/`. Сценарные прогоны остаются QA для CLI и БД (импорт + негативные случаи, `info`, эквивалентность материалов, каскадное удаление, exit-коды).

## Структура

```
SovietTransistorsDatabase/
  Program.cs          CLI: разбор аргументов, вывод
  Domain/             обозначения, каталог параметров, ratings, атрибуты, валидация — без зависимостей от БД
  Data/               RelationalTransistorDatabase (переносимое ядро, DML) + SqliteDatabase (DDL)
  Import/             чтение jsonc
  sample-data.jsonc   пример наполнения (достоверные характеристики, eandc.ru)

SovietTransistorsDatabase.Tests/
  *.cs                unit-тесты Domain (xUnit): парсер, валидаторы, каталог параметров
```

## Ключевые правила

1. **Валидация электрических параметров декларативна**: правила (границы, потолок значения, варианты условий) заданы в каталоге `Domain/ElectricalParameters.cs` (`ElectricalParameterCatalog`) и `Domain/ParameterConditions.cs` (именованные спеки `ConditionSpecs`). Из каталога генерируются: C#-валидатор, раздел параметров `help`, список полей секции jsonc и CHECK-ограничения `electrical_parameters` (`Data/ElectricalParametersDdl.CreateTableSql`, компонуемый в `Data/SqliteDatabase.cs`). Новый код параметра — правка каталога плюс SKILL.md. Валидация ratings и атрибутов по-прежнему дублируется в двух местах: `Domain/*Validator` (сообщения по-русски) и CHECK в `Data/SqliteDatabase.cs` — обновляй оба места.
2. **Именование полей** единое в jsonc, C#-свойствах и колонках БД (CamelCase): символ физической величины — первая заглавная, остальные строчные (`Uke`, `Ik`, `Freq`, `Rg`); слова-модификаторы — с заглавной (`Max`, `Nas`, `Gran`, `Pulse`); описательные поля — строчными (`parameter`, `min`, `max`, `colorMarking`); индексы ГОСТ — строчными (`Ikbo`, `Ikeo`, `Tauk`); h-параметры — всегда строчные (`h21e`, `h11`). Исключения: условия `freq`/`temp` — полные слова.
3. **Единицы канонические** (фиксированы типом параметра): В, мА (токи и токовые условия), мкА (обратные токи), МГц, дБ, Вт, %, Ом, пс, нс, пФ, °C, мВт, мкс, г, °C/Вт. Переводить значения даташита до записи; 1 кГц = 0.001 МГц.
4. **Семантика секций jsonc** (`attributes`/`parameters`/`ratings`): отсутствует — не менять; задана — заменить целиком; ошибка хотя бы в одном значении — секция не применяется вовсе (защита от частичного стирания); `[]`/`{}` — очистить. Импорт идемпотентен.
5. **Миграции БД не поддерживаются намеренно** (проект в разработке): при изменении схемы рабочая база пересоздаётся импортом. Не добавляй код миграций.
6. **Материалы попарно равнозначны**: Г/1, К/2, А/3, И/4 — дубликаты, `find`/`delete`/фильтры обязаны это учитывать.
7. **Данные sample-data.jsonc** — правдоподобные, но непроверенные значения — только в рабочую базу, не в репозиторий.

## Не трогать

- `transistors.db` — в `.gitignore`, генерируется импортом; не коммитить.
- `.kilo/skills/soviet-transistors/SKILL.md` — регламент наполнения; синхронно обновлять при изменении формата jsonc (полный справочник полей и типичных ошибок там, дублировать в AGENTS.md не нужно).
- DML в `RelationalTransistorDatabase` — только переносимые конструкции (`@`-параметры, `COALESCE`, `LIMIT`, производные таблицы, `INSERT ... SELECT`); запись — в транзакции соединения. Диалектозависимые вещи — только в `CreateTableSql`/`OpenConnection`/`LastInsertIdSql` переопределениях.
- Соединение БД — одно на экземпляр `RelationalTransistorDatabase`: открывается лениво при первом обращении, закрывается в `Dispose` (CLI — `using` на время команды). `EnsureCreated` выполняют только команды записи (`init`/`add`/`import`, один раз за запуск); команды чтения и `delete` обходятся без DDL.
