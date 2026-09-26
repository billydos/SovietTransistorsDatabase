package importer

import (
	"strings"
	"testing"
)

// Тесты семантики чтения записей (формы записи, секции, отвержение ошибочных)
// идут напрямую на дереве value — без участия фронтендов форматов. Фронтенды
// покрываются format_jsonc_test.go и format_yaml_test.go, выбор формата по
// расширению файла — format_test.go.

// Конструкторы дерева value для тестов читателя.
func str(s string) value       { return value{kind: kindString, str: s} }
func num(text string) value    { return value{kind: kindNumber, num: text} }
func boolean(b bool) value     { return value{kind: kindBool, boolean: b} }
func null() value              { return value{kind: kindNull} }
func arr(items ...value) value { return value{kind: kindArray, items: items} }
func obj(ms ...member) value   { return value{kind: kindObject, members: ms} }

func TestParseRoot_RootProblems(t *testing.T) {
	if _, err := parseRoot(arr(num("1"), num("2"))); err == nil {
		t.Error("ожидалась ошибка: корень не объект")
	}
	if _, err := parseRoot(obj()); err == nil {
		t.Error("ожидалась ошибка: нет ключа transistors")
	}
	if _, err := parseRoot(obj(member{"transistors", obj()})); err == nil {
		t.Error("ожидалась ошибка: transistors не массив")
	}
}

func TestParseRoot_UnknownRootKey_ProducesIssue(t *testing.T) {
	result, err := parseRoot(obj(member{"extra", num("1")}, member{"transistors", arr()}))
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if !result.HasErrors() {
		t.Fatal("ожидалась проблема")
	}
	want := "неизвестный ключ корневого объекта «extra» (допустим только \"transistors\")"
	if got := result.Issues[0].Description; got != want {
		t.Errorf("текст проблемы = %q, ожидалось %q", got, want)
	}
}

func TestParseRoot_StringEntry_AndKindDescription(t *testing.T) {
	result, err := parseRoot(obj(member{"transistors", arr(str("КТ315Б"), num("42"))}))
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if len(result.Entries) != 1 || result.Entries[0].Transistor.Name() != "КТ315Б" {
		t.Fatalf("entries = %+v", result.Entries)
	}
	if len(result.Issues) != 1 {
		t.Fatalf("issues = %+v", result.Issues)
	}
	want := "запись должна быть строкой (обозначение) или объектом, получено: число"
	if got := result.Issues[0].Description; got != want {
		t.Errorf("текст проблемы = %q, ожидалось %q", got, want)
	}
}

func TestParseRoot_MixedNameAndDesignationFields(t *testing.T) {
	root := obj(
		member{"transistors", arr(
			obj(member{"name", str("КТ315Б")}, member{"letters", str("Б")}),
		)},
	)
	result, err := parseRoot(root)
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if len(result.Entries) != 0 {
		t.Error("запись со смешением форм не должна быть принята")
	}
	if len(result.Issues) != 1 {
		t.Fatalf("issues = %+v", result.Issues)
	}
	want := "нельзя смешивать \"name\" и явные поля обозначения: «letters» задано вместе с \"name\" — обозначение задаётся либо строкой \"name\", либо полями (material, subclass, assembly, feature, number, letters, modification, chip)"
	if got := result.Issues[0].Description; got != want {
		t.Errorf("текст проблемы:\n%q\nожидалось:\n%q", got, want)
	}
}

func TestParseRoot_DesignationFormEntry(t *testing.T) {
	root := obj(
		member{"transistors", arr(
			obj(
				member{"material", str("Г")}, member{"subclass", str("Т")},
				member{"assembly", boolean(false)},
				member{"feature", num("1")}, member{"number", num("15")}, member{"letters", str("А")},
				member{"ratings", obj(member{"IkMax", num("50")}, member{"tempMin", num("-40")}, member{"tempMax", num("55")})},
			),
		)},
	)
	result, err := parseRoot(root)
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if len(result.Entries) != 1 {
		t.Fatalf("entries = %+v, issues = %+v", result.Entries, result.Issues)
	}
	entry := result.Entries[0]
	if entry.Transistor.Name() != "ГТ115А" {
		t.Errorf("обозначение = %q", entry.Transistor.Name())
	}
	if entry.Ratings == nil || entry.Ratings.IkMax == nil || *entry.Ratings.IkMax != 50 {
		t.Errorf("ratings = %+v", entry.Ratings)
	}
}

func TestParseRoot_SectionError_RejectsWholeEntry(t *testing.T) {
	root := obj(
		member{"transistors", arr(
			obj(
				member{"name", str("КТ315Б")},
				member{"parameters", arr(obj(member{"parameter", str("h21e")}, member{"min", num("50")}))},
			),
		)},
	)
	result, err := parseRoot(root)
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if len(result.Entries) != 0 {
		t.Error("запись с ошибкой в секции должна быть отвергнута целиком")
	}
	if len(result.Issues) != 1 {
		t.Fatalf("issues = %+v", result.Issues)
	}
	if !contains(result.Issues[0].Description, "условия —") {
		t.Errorf("текст проблемы = %q", result.Issues[0].Description)
	}
}

func TestParseRoot_EmptyParameterArray_ClearsSection(t *testing.T) {
	root := obj(
		member{"transistors", arr(
			obj(member{"name", str("КТ315Б")}, member{"parameters", arr()}),
		)},
	)
	result, err := parseRoot(root)
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if len(result.Entries) != 1 {
		t.Fatalf("entries = %+v, issues = %+v", result.Entries, result.Issues)
	}
	parameters := result.Entries[0].Parameters
	if parameters == nil {
		t.Error("пустой массив должен давать непустую-nil пустую секцию (очистка)")
	}
	if len(parameters) != 0 {
		t.Errorf("parameters = %+v", parameters)
	}
}

func TestParseRoot_ManufacturersNull_DoesNotChange(t *testing.T) {
	root := obj(
		member{"transistors", arr(
			obj(
				member{"name", str("КТ315Б")},
				member{"attributes", obj(member{"manufacturers", null()}, member{"structure", str("npn")})},
			),
		)},
	)
	result, err := parseRoot(root)
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	entry := result.Entries[0]
	if entry.Attributes == nil || entry.Attributes.Structure == nil || *entry.Attributes.Structure != "npn" {
		t.Fatalf("attributes = %+v", entry.Attributes)
	}
	if entry.Manufacturers != nil {
		t.Errorf("manufacturers = %v, ожидалось nil", entry.Manufacturers)
	}
}

func TestParseRoot_UnknownParameterCode(t *testing.T) {
	root := obj(
		member{"transistors", arr(
			obj(
				member{"name", str("КТ315Б")},
				member{"parameters", arr(
					obj(member{"parameter", str("h21")}, member{"min", num("50")}, member{"Uke", num("10")}, member{"Ik", num("1")}),
				)},
			),
		)},
	)
	result, err := parseRoot(root)
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if len(result.Issues) != 1 {
		t.Fatalf("issues = %+v", result.Issues)
	}
	if !contains(result.Issues[0].Description, "неизвестный код «h21»") {
		t.Errorf("текст проблемы = %q", result.Issues[0].Description)
	}
}

func TestParseRoot_RatingsValidation(t *testing.T) {
	root := obj(
		member{"transistors", arr(
			obj(member{"name", str("КТ315Б")}, member{"ratings", obj(member{"IkPulseMax", num("100")})}),
		)},
	)
	result, err := parseRoot(root)
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if len(result.Issues) != 1 {
		t.Fatalf("issues = %+v", result.Issues)
	}
	if !contains(result.Issues[0].Description, "длительность импульса (pulseDuration, мкс) обязательна") {
		t.Errorf("текст проблемы = %q", result.Issues[0].Description)
	}
}

func contains(haystack, fragment string) bool {
	return strings.Contains(haystack, fragment)
}
