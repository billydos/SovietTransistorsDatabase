# Graph Report - SovietTransistorsDatabase  (2026-09-23)

## Corpus Check
- Corpus is ~16,327 words - fits in a single context window. You may not need a graph.

## Summary
- 347 nodes · 740 edges · 12 communities
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 39 edges (avg confidence: 0.86)
- Token cost: 4,600 input · 9,800 output

## Community Hubs (Navigation)
- Relational Database Core
- Electrical Parameter Catalog
- JSONC Import Reader
- CLI Commands & Entry Point
- Transistor Attributes & Validation
- Project Docs & Conventions
- Namespaces & Designation Model
- Parameter Values & Conditions
- Maximum Ratings
- Semiconductor Materials
- GOST Name Parsing
- Project Configuration

## God Nodes (most connected - your core abstractions)
1. `RelationalTransistorDatabase` - 36 edges
2. `Transistor` - 33 edges
3. `ParameterKind` - 27 edges
4. `ElectricalParameter` - 26 edges
5. `TransistorAttributes` - 24 edges
6. `Program` - 23 edges
7. `MaximumRatings` - 22 edges
8. `TransistorJsoncReader` - 21 edges
9. `AGENTS.md (правила проекта SovietTransistorsDatabase)` - 20 edges
10. `ITransistorDatabase` - 16 edges

## Surprising Connections (you probably didn't know these)
- `Семантика секций jsonc: отсутствует — не менять, задана — заменить целиком, ошибка — секция игнорируется (защита от частичного стирания, идемпотентность)` --semantically_similar_to--> `Дублирование валидации: Domain/*Validator (сообщения по-русски) + CHECK-ограничения в Data/SqliteDatabase.cs`  [INFERRED] [semantically similar]
  .kilo/skills/soviet-transistors/SKILL.md → AGENTS.md
- `Формат jsonc-файла импорта (корень transistors, три формы записи)` --conceptually_related_to--> `Import/ (чтение jsonc)`  [INFERRED]
  .kilo/skills/soviet-transistors/SKILL.md → AGENTS.md
- `Команды CLI: init, import, add, parse, list, info, find, delete, count (+ опции --db, --dry-run)` --conceptually_related_to--> `Program.cs (CLI: разбор аргументов, вывод)`  [INFERRED]
  .kilo/skills/soviet-transistors/SKILL.md → AGENTS.md
- `README.md (справочник советских транзисторов)` --references--> `SQLite (UTF-8) — хранилище базы`  [EXTRACTED]
  README.md → AGENTS.md
- `Domain/ (обозначения, каталог параметров, ratings, атрибуты, валидация — без зависимостей от БД)` --conceptually_related_to--> `Формат обозначения транзистора (материал/подкласс/сборка/признак/номер/буквы/модификация/бескорп.)`  [INFERRED]
  AGENTS.md → .kilo/skills/soviet-transistors/SKILL.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Слоистая архитектура утилиты (Program.cs + Domain/ + Data/ + Import/)** — agents_program_cs, agents_domain_layer, agents_data_layer, agents_import_layer [EXTRACTED 1.00]
- **Конвейер импорта jsonc (формат → Import/ → CLI import → схема БД; sample-data как источник восстановления)** — kilo_skills_soviet_transistors_skill_jsonc_format, agents_import_layer, kilo_skills_soviet_transistors_skill_cli, kilo_skills_soviet_transistors_skill_db_schema, agents_sample_data [INFERRED 0.85]
- **Двухуровневая валидация (Domain/*Validator в приложении + CHECK/FK в БД)** — agents_validation_duplication, agents_domain_layer, agents_data_layer, kilo_skills_soviet_transistors_skill_db_schema [EXTRACTED 1.00]

## Communities (12 total, 0 thin omitted)

### Community 0 - "Relational Database Core"
Cohesion: 0.07
Nodes (25): DbCommand, DbDataReader, IDisposable, IReadOnlyList, InsertOutcome, Added, DuplicateExists, ITransistorDatabase (+17 more)

### Community 1 - "Electrical Parameter Catalog"
Cohesion: 0.05
Nodes (43): IReadOnlyCollection, IReadOnlyDictionary, BoundDirection, AtLeast, AtLeastOrRange, AtMost, ConditionRule, ExactlyOneCurrent (+35 more)

### Community 2 - "JSONC Import Reader"
Cohesion: 0.15
Nodes (19): Func, JsonDocumentOptions, JsonElement, JsonValueKind, List, Transistor, JsoncIssue, JsoncParseResult (+11 more)

### Community 3 - "CLI Commands & Entry Point"
Cohesion: 0.16
Nodes (7): IReadOnlySet, DbConnection, SqliteDatabase, CreateTableSql, Dictionary, List, Program

### Community 4 - "Transistor Attributes & Validation"
Cohesion: 0.08
Nodes (24): TransistorDetails, Attributes, Manufacturers, Parameters, Ratings, IReadOnlyList, List, TransistorAttributes (+16 more)

### Community 5 - "Project Docs & Conventions"
Cohesion: 0.19
Nodes (28): AGENTS.md (правила проекта SovietTransistorsDatabase), Data/ (RelationalTransistorDatabase — переносимое ядро DML + SqliteDatabase — DDL), Переносимость схемы на PostgreSQL/MariaDB (только переносимые DML: @-параметры, COALESCE, LIMIT; диалекты — в CreateTableSql/OpenConnection), Domain/ (обозначения, каталог параметров, ratings, атрибуты, валидация — без зависимостей от БД), eandc.ru (источник достоверных справочных характеристик), ГОСТ 10862-64 (система обозначений транзисторов), Import/ (чтение jsonc), Единое CamelCase-именование полей в jsonc, C#-свойствах и колонках БД (Uke, Ik, h21e, colorMarking; исключения freq/temp) (+20 more)

### Community 6 - "Namespaces & Designation Model"
Cohesion: 0.11
Nodes (18): SovietTransistorsDatabase.Import, SovietTransistorsDatabase, SovietTransistorsDatabase.Data, SovietTransistorsDatabase.Domain, microsoft_data_sqlite, TransistorQuery, ChipVariant, DevelopmentNumber (+10 more)

### Community 7 - "Parameter Values & Conditions"
Cohesion: 0.11
Nodes (17): IReadOnlyList, ElectricalParameter, Freq, Ib, Ie, Ik, Kind, Rbe (+9 more)

### Community 8 - "Maximum Ratings"
Cohesion: 0.10
Nodes (18): IReadOnlyList, List, MaximumRatings, IbMax, IkMax, IkPulseMax, PkMax, PkPulseMax (+10 more)

### Community 9 - "Semiconductor Materials"
Cohesion: 0.16
Nodes (12): Digit, Letter, Dictionary, IReadOnlyDictionary, Materials, SemiconductorMaterial, GalliumArsenide, Germanium (+4 more)

### Community 10 - "GOST Name Parsing"
Cohesion: 0.16
Nodes (4): Cyrillic, TransistorNameParser, IReadOnlyList, TransistorValidator

### Community 11 - "Project Configuration"
Cohesion: 0.50
Nodes (3): net10.0, Microsoft.Data.Sqlite (10.0.12), Microsoft.NET.Sdk

## Knowledge Gaps
- **122 isolated node(s):** `Added`, `DuplicateExists`, `Added`, `UpdatedExisting`, `Attributes` (+117 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 143 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ElectricalParameter` connect `Parameter Values & Conditions` to `Relational Database Core`, `Electrical Parameter Catalog`, `JSONC Import Reader`, `Transistor Attributes & Validation`?**
  _High betweenness centrality (0.161) - this node is a cross-community bridge._
- **Why does `ParameterKind` connect `Electrical Parameter Catalog` to `Transistor Attributes & Validation`, `Parameter Values & Conditions`?**
  _High betweenness centrality (0.106) - this node is a cross-community bridge._
- **Why does `Transistor` connect `Relational Database Core` to `GOST Name Parsing`, `Transistor Attributes & Validation`, `Namespaces & Designation Model`?**
  _High betweenness centrality (0.106) - this node is a cross-community bridge._
- **What connects `Added`, `DuplicateExists`, `Added` to the rest of the system?**
  _122 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Relational Database Core` be split into smaller, more focused modules?**
  _Cohesion score 0.07287093942054433 - nodes in this community are weakly interconnected._
- **Should `Electrical Parameter Catalog` be split into smaller, more focused modules?**
  _Cohesion score 0.050241545893719805 - nodes in this community are weakly interconnected._
- **Should `JSONC Import Reader` be split into smaller, more focused modules?**
  _Cohesion score 0.145748987854251 - nodes in this community are weakly interconnected._