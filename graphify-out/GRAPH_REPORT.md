# Graph Report - SovietTransistorsDatabase  (2026-09-25)

## Corpus Check
- Corpus is ~26,618 words - fits in a single context window. You may not need a graph.

## Summary
- 576 nodes · 1231 edges · 19 communities (15 shown, 4 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 79 edges (avg confidence: 0.85)
- Token cost: 27,000 input · 9,500 output

## Community Hubs (Navigation)
- Parameter Catalog
- Relational DML Core
- DDL & Validation
- Database Interface & SQLite
- Project Rules & Docs
- jsonc Import
- Project Structure
- Designation Alphabet & Materials
- Descriptive Attributes
- Maximum Ratings & Details
- Parameter Values & Conditions
- Designation Parser & Tests
- Record Validator & Tests
- Build & Packages
- Sample Data & Storage
- Canonical Units
- CLI Commands
- DB Value Encoding
- Field Naming

## God Nodes (most connected - your core abstractions)
1. `RelationalTransistorDatabase` - 39 edges
2. `ElectricalParameter` - 38 edges
3. `ParameterKind` - 34 edges
4. `TransistorAttributes` - 34 edges
5. `Transistor` - 33 edges
6. `MaximumRatings` - 28 edges
7. `Program` - 27 edges
8. `ConditionKey` - 26 edges
9. `ElectricalParameterValidatorTests` - 24 edges
10. `SovietTransistorsDatabase.Domain` - 23 edges

## Surprising Connections (you probably didn't know these)
- `Формат jsonc (README)` --semantically_similar_to--> `jsonc-формат наполнения (корень transistors, три формы записи)`  [INFERRED] [semantically similar]
  README.md → .kilo/skills/soviet-transistors-database/SKILL.md
- `ГОСТ 10862-64 (система обозначений)` --semantically_similar_to--> `Формат обозначения по ГОСТ 10862-64 (материал/подкласс/сборка/признак/номер/буквы/модификация/бескорпусное)`  [INFERRED] [semantically similar]
  README.md → .kilo/skills/soviet-transistors-database/SKILL.md
- `Команды CLI (README)` --semantically_similar_to--> `Команды CLI (init/import/add/parse/list/info/find/delete/count)`  [INFERRED] [semantically similar]
  README.md → .kilo/skills/soviet-transistors-database/SKILL.md
- `Правило 6: обозначение — точный ключ записи; материалы равнозначны только физически` --semantically_similar_to--> `Равнозначность материалов Г/1, К/2, А/3, И/4 (не ключ записи)`  [INFERRED] [semantically similar]
  AGENTS.md → .kilo/skills/soviet-transistors-database/SKILL.md
- `Правило 3: канонические единицы, перевод до записи` --semantically_similar_to--> `Канонические единицы (В, мА, мкА, МГц, Ом, пФ, пс, нс, дБ, Вт, %, °C, мВт, мкс, г, °C/Вт)`  [INFERRED] [semantically similar]
  AGENTS.md → .kilo/skills/soviet-transistors-database/SKILL.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Single-source parameter catalog pipeline (validator, help, jsonc fields, SQL CHECK)** — agents_electricalparametercatalog, agents_conditionspecs, agents_electricalparametersddl, agents_sqlitedatabase, analysis_c6_declarative_catalog, _kilo_skills_soviet_transistors_database_skill_parameters_section [EXTRACTED 1.00]
- **Material equivalence vs exact designation key (Г/1, К/2, А/3, И/4)** — agents_exact_designation_key, _kilo_skills_soviet_transistors_database_skill_material_equivalence, analysis_c13_material_equivalence, agents_findmaterialequivalents [EXTRACTED 1.00]
- **Transactional write path redesign around RelationalTransistorDatabase** — analysis_c1_transactions, analysis_c2_orphan_manufacturers, analysis_c3_redundant_queries, analysis_c4_single_connection, agents_relationaltransistordatabase [INFERRED 0.85]

## Communities (19 total, 4 thin omitted)

### Community 0 - "Parameter Catalog"
Cohesion: 0.06
Nodes (38): IReadOnlyDictionary, IReadOnlyList, BoundDirection, AtLeast, AtLeastOrRange, AtMost, ElectricalParameterCatalog, CodesList (+30 more)

### Community 1 - "Relational DML Core"
Cohesion: 0.09
Nodes (23): DbCommand, DbDataReader, DbParameter, DbTransaction, RowReader, DbConnection, Dictionary, IReadOnlyList (+15 more)

### Community 2 - "DDL & Validation"
Cohesion: 0.05
Nodes (36): IEnumerable, IReadOnlySet, ElectricalParametersDdl, ElectricalParameterValidator, IEnumerable, IReadOnlyList, IReadOnlySet, ConditionKey (+28 more)

### Community 3 - "Database Interface & SQLite"
Cohesion: 0.09
Nodes (15): Failures, IDisposable, IReadOnlyList, ITransistorDatabase, DbConnection, SqliteDatabase, CreateTableSql, LastInsertIdSql (+7 more)

### Community 4 - "Project Rules & Docs"
Cohesion: 0.06
Nodes (47): Секция jsonc attributes (описательные атрибуты 1:1), Формат обозначения по ГОСТ 10862-64 (материал/подкласс/сборка/признак/номер/буквы/модификация/бескорпусное), SKILL.md — регламент наполнения soviet-transistors-database, Равнозначность материалов Г/1, К/2, А/3, И/4 (не ключ записи), Секция jsonc parameters (20 кодов электрических параметров с условиями), Секция jsonc ratings (предельные эксплуатационные данные), Характерные сообщения валидации (позиция N, «условия — …; задано: …», границы, positivity), AttributesDdl (генерируемый chk_any_attribute) (+39 more)

### Community 5 - "jsonc Import"
Cohesion: 0.13
Nodes (21): FrozenSet, Func, JsonDocumentOptions, JsonElement, JsonValueKind, List, Transistor, JsoncIssue (+13 more)

### Community 6 - "Project Structure"
Cohesion: 0.07
Nodes (24): SovietTransistorsDatabase.Import, SovietTransistorsDatabase, SovietTransistorsDatabase.Tests, SovietTransistorsDatabase.Data, SovietTransistorsDatabase.Domain, microsoft_data_sqlite, AttributesDdl, TransistorQuery (+16 more)

### Community 7 - "Designation Alphabet & Materials"
Cohesion: 0.10
Nodes (20): ArgumentException, Digit, Letter, Cyrillic, Dictionary, IReadOnlyDictionary, Materials, SemiconductorMaterial (+12 more)

### Community 8 - "Descriptive Attributes"
Cohesion: 0.09
Nodes (25): PropertyInfo, IReadOnlyList, List, TransistorAttributes, ColorMarking, DatasheetUrl, EsdSensitive, MassMax (+17 more)

### Community 9 - "Maximum Ratings & Details"
Cohesion: 0.09
Nodes (25): TransistorDetails, Attributes, Manufacturers, Parameters, Ratings, IReadOnlyList, List, MaximumRatings (+17 more)

### Community 10 - "Parameter Values & Conditions"
Cohesion: 0.12
Nodes (19): ElectricalParameter, Freq, Ib, Ie, Ik, Kind, Rbe, Rg (+11 more)

### Community 11 - "Designation Parser & Tests"
Cohesion: 0.15
Nodes (10): FormatException, TransistorNameParser, Fact, InlineData, MemberData, Theory, TheoryData, TransistorNameParserTests (+2 more)

### Community 12 - "Record Validator & Tests"
Cohesion: 0.31
Nodes (6): IReadOnlyList, TransistorValidator, Fact, InlineData, Theory, TransistorValidatorTests

### Community 13 - "Build & Packages"
Cohesion: 0.18
Nodes (9): coverlet.collector (6.0.4), Microsoft.Data.Sqlite (10.0.12), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0, Microsoft.NET.Sdk, net10.0 (+1 more)

### Community 14 - "Sample Data & Storage"
Cohesion: 0.33
Nodes (6): jsonc-формат наполнения (корень transistors, три формы записи), Правило 5: миграции БД не поддерживаются намеренно, база пересоздаётся импортом, sample-data.jsonc (пример наполнения, eandc.ru), transistors.db (рабочая база, генерируется импортом), Формат jsonc (README), sample-data.jsonc (README)

## Knowledge Gaps
- **162 isolated node(s):** `KindVariantPairs`, `EquivalentSymbols`, `net10.0`, `coverlet.collector (6.0.4)`, `Microsoft.NET.Test.Sdk (17.14.1)` (+157 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 212 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `SovietTransistorsDatabase.Domain` connect `Project Structure` to `Parameter Catalog`, `DDL & Validation`, `jsonc Import`, `Designation Alphabet & Materials`, `Descriptive Attributes`, `Maximum Ratings & Details`, `Designation Parser & Tests`, `Record Validator & Tests`?**
  _High betweenness centrality (0.161) - this node is a cross-community bridge._
- **Why does `ElectricalParameter` connect `Parameter Values & Conditions` to `Parameter Catalog`, `Relational DML Core`, `DDL & Validation`, `Database Interface & SQLite`, `jsonc Import`, `Maximum Ratings & Details`?**
  _High betweenness centrality (0.139) - this node is a cross-community bridge._
- **Why does `Transistor` connect `Relational DML Core` to `Database Interface & SQLite`, `Project Structure`, `Maximum Ratings & Details`, `Designation Parser & Tests`, `Record Validator & Tests`?**
  _High betweenness centrality (0.091) - this node is a cross-community bridge._
- **Are the 5 inferred relationships involving `ElectricalParameter` (e.g. with `.Conditions_AllSpecified_InCanonicalOrder()` and `.Conditions_WithoutConditions()`) actually correct?**
  _`ElectricalParameter` has 5 INFERRED edges - model-reasoned connections that need verification._
- **Are the 8 inferred relationships involving `TransistorAttributes` (e.g. with `.HasAnyValue_AllFieldsEmpty_IsFalse()` and `.HasAnyValue_AnySingleFieldSet_IsTrue()`) actually correct?**
  _`TransistorAttributes` has 8 INFERRED edges - model-reasoned connections that need verification._
- **What connects `KindVariantPairs`, `EquivalentSymbols`, `net10.0` to the rest of the system?**
  _162 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Parameter Catalog` be split into smaller, more focused modules?**
  _Cohesion score 0.06057945566286216 - nodes in this community are weakly interconnected._