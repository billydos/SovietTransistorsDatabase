package importer

import (
	"errors"
	"strings"
	"testing"

	"soviettransistors/internal/domain"
)

func TestParseYamlText_SyntaxErrorMessage(t *testing.T) {
	_, err := ParseYamlText("transistors: [\n")
	if err == nil {
		t.Fatal("ожидалась ошибка синтаксиса")
	}
	if !strings.Contains(err.Error(), "файл не является корректным YAML: ") {
		t.Errorf("текст ошибки = %q", err.Error())
	}
	var userError *domain.UserError
	if !errors.As(err, &userError) {
		t.Error("ожидалась *domain.UserError")
	}
}

func TestParseYamlText_RootProblems(t *testing.T) {
	if _, err := ParseYamlText(""); err == nil {
		t.Error("ожидалась ошибка: пустой файл — null, не объект")
	}
	if _, err := ParseYamlText("- 1\n- 2\n"); err == nil {
		t.Error("ожидалась ошибка: корень не объект")
	}
	if _, err := ParseYamlText("{}"); err == nil {
		t.Error("ожидалась ошибка: нет ключа transistors")
	}
	if _, err := ParseYamlText("transistors: {}"); err == nil {
		t.Error("ожидалась ошибка: transistors не массив")
	}
	if _, err := ParseYamlText("transistors: []\n---\ntransistors: []\n"); err == nil {
		t.Error("ожидалась ошибка: несколько yaml-документов в одном файле")
	}
}

func TestParseYamlText_UnknownRootKey_ProducesIssue(t *testing.T) {
	result, err := ParseYamlText("# комментарий\nextra: 1\ntransistors: [] # хвостовой комментарий\n")
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

func TestParseYamlText_StringEntry_AndKindDescription(t *testing.T) {
	result, err := ParseYamlText("transistors:\n  - КТ315Б\n  - 42\n")
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if len(result.Entries) != 1 || result.Entries[0].Transistor.Name() != "КТ315Б" {
		t.Fatalf("entries = %+v", result.Entries)
	}
	want := "запись должна быть строкой (обозначение) или объектом, получено: число"
	if got := result.Issues[0].Description; got != want {
		t.Errorf("текст проблемы = %q, ожидалось %q", got, want)
	}
}

func TestParseYamlText_DesignationFormEntry(t *testing.T) {
	text := `transistors:
  - material: Г
    subclass: Т
    assembly: false
    feature: 1
    number: 15
    letters: А
    ratings: {IkMax: 50, tempMin: -40, tempMax: 55}
`
	result, err := ParseYamlText(text)
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

func TestParseYamlText_ManufacturersNull_DoesNotChange(t *testing.T) {
	text := "transistors:\n  - name: КТ315Б\n    attributes:\n      manufacturers: null\n      structure: npn\n"
	result, err := ParseYamlText(text)
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

func TestParseYamlText_EmptySections_Clear(t *testing.T) {
	result, err := ParseYamlText("transistors:\n  - name: КТ315Б\n    parameters: []\n    ratings: {}\n")
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	entry := result.Entries[0]
	if entry.Parameters == nil || len(entry.Parameters) != 0 {
		t.Errorf("parameters = %+v, ожидалась пустая непустая-nil секция", entry.Parameters)
	}
	if entry.Ratings == nil {
		t.Error("ratings = nil, ожидалась пустая непустая-nil секция")
	}
}

func TestParseYamlText_DuplicateKeys_Rejected(t *testing.T) {
	if _, err := ParseYamlText("transistors: []\ntransistors: []\n"); err == nil {
		t.Error("ожидалась ошибка повторяющегося ключа")
	}
}
