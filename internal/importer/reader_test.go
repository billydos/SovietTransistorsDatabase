package importer

import (
	"strings"
	"testing"
)

func TestJsoncToJSON_CommentsAndTrailingCommas(t *testing.T) {
	source := `{
		// строкный комментарий
		"a": "значение // не комментарий", /* блочный
		комментарий */
		"b": [1, 2, 3,],
		"c": {"d": "e",},
	}`
	value, err := parseJsoncToValue(source)
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if value.kind != jsonObject {
		t.Fatalf("корень не объект: %v", value.kind)
	}
	a, ok := value.has("a")
	if !ok || a.str != "значение // не комментарий" {
		t.Errorf("a = %v, ok = %v", a, ok)
	}
	b, _ := value.has("b")
	if len(b.items) != 3 {
		t.Errorf("b.items = %d, ожидалось 3", len(b.items))
	}
	c, _ := value.has("c")
	if _, ok := c.has("d"); !ok {
		t.Error("c.d отсутствует")
	}
}

func TestParseJsoncText_RootProblems(t *testing.T) {
	if _, err := ParseJsoncText("{ не json"); err == nil {
		t.Error("ожидалась ошибка некорректного JSONC")
	}
	if _, err := ParseJsoncText("[1, 2]"); err == nil {
		t.Error("ожидалась ошибка: корень не объект")
	}
	if _, err := ParseJsoncText("{}"); err == nil {
		t.Error("ожидалась ошибка: нет ключа transistors")
	}
	if _, err := ParseJsoncText("{\"transistors\": {}}"); err == nil {
		t.Error("ожидалась ошибка: transistors не массив")
	}
}

func TestParseJsoncText_UnknownRootKey_ProducesIssue(t *testing.T) {
	result, err := ParseJsoncText("{\"extra\": 1, \"transistors\": []}")
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if !result.HasErrors() {
		t.Fatal("ожидалась проблема")
	}
	if got := result.Issues[0].Description; got != "неизвестный ключ корневого объекта «extra» (допустим только \"transistors\")" {
		t.Errorf("текст проблемы = %q", got)
	}
}

func TestParseJsoncText_StringEntry_AndKeyOrder(t *testing.T) {
	result, err := ParseJsoncText("{\"transistors\": [\"КТ315Б\", 42]}")
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

func TestParseJsoncText_MixedNameAndDesignationFields(t *testing.T) {
	text := `{"transistors": [{"name": "КТ315Б", "letters": "Б"}]}`
	result, err := ParseJsoncText(text)
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

func TestParseJsoncText_DesignationFormEntry(t *testing.T) {
	text := `{"transistors": [{
		"material": "Г", "subclass": "Т", "assembly": false,
		"feature": 1, "number": 15, "letters": "А",
		"ratings": {"IkMax": 50, "tempMin": -40, "tempMax": 55}
	}]}`
	result, err := ParseJsoncText(text)
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

func TestParseJsoncText_SectionError_RejectsWholeEntry(t *testing.T) {
	text := `{"transistors": [{"name": "КТ315Б", "parameters": [{"parameter": "h21e", "min": 50}]}]}`
	result, err := ParseJsoncText(text)
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

func TestParseJsoncText_EmptyParameterArray_ClearsSection(t *testing.T) {
	text := `{"transistors": [{"name": "КТ315Б", "parameters": []}]}`
	result, err := ParseJsoncText(text)
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

func TestParseJsoncText_ManufacturersNull_DoesNotChange(t *testing.T) {
	text := `{"transistors": [{"name": "КТ315Б", "attributes": {"manufacturers": null, "structure": "npn"}}]}`
	result, err := ParseJsoncText(text)
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

func TestParseJsoncText_UnknownParameterCode(t *testing.T) {
	text := `{"transistors": [{"name": "КТ315Б", "parameters": [{"parameter": "h21", "min": 50, "Uke": 10, "Ik": 1}]}]}`
	result, err := ParseJsoncText(text)
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

func TestParseJsoncText_RatingsValidation(t *testing.T) {
	text := `{"transistors": [{"name": "КТ315Б", "ratings": {"IkPulseMax": 100}}]}`
	result, err := ParseJsoncText(text)
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
