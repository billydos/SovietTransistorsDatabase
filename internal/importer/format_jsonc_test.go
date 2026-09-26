package importer

import (
	"errors"
	"strings"
	"testing"

	"soviettransistors/internal/domain"
)

func TestParseJsonc_CommentsAndTrailingCommas(t *testing.T) {
	source := `{
		// строкный комментарий
		"a": "значение // не комментарий", /* блочный
		комментарий */
		"b": [1, 2, 3,],
		"c": {"d": "e",},
	}`
	parsed, err := parseJsonc([]byte(source))
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if parsed.kind != kindObject {
		t.Fatalf("корень не объект: %v", parsed.kind)
	}
	a, ok := parsed.has("a")
	if !ok || a.str != "значение // не комментарий" {
		t.Errorf("a = %v, ok = %v", a, ok)
	}
	b, _ := parsed.has("b")
	if len(b.items) != 3 {
		t.Errorf("b.items = %d, ожидалось 3", len(b.items))
	}
	c, _ := parsed.has("c")
	if _, ok := c.has("d"); !ok {
		t.Error("c.d отсутствует")
	}
}

// Стандартный JSON без комментариев и висячих запятых — частный случай jsonc.
func TestParseJsonc_PlainJSON(t *testing.T) {
	parsed, err := parseJsonc([]byte(`{"a": [1, 2], "b": null, "c": true}`))
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	a, _ := parsed.has("a")
	if a.kind != kindArray || len(a.items) != 2 {
		t.Errorf("a = %+v", a)
	}
	b, _ := parsed.has("b")
	if b.kind != kindNull {
		t.Errorf("b = %+v, ожидался null", b)
	}
	c, _ := parsed.has("c")
	if c.kind != kindBool || !c.boolean {
		t.Errorf("c = %+v", c)
	}
}

func TestParseJsoncText_SyntaxErrorMessage(t *testing.T) {
	_, err := ParseJsoncText("{ не json")
	if err == nil {
		t.Fatal("ожидалась ошибка синтаксиса")
	}
	if !strings.Contains(err.Error(), "файл не является корректным JSONC: ") {
		t.Errorf("текст ошибки = %q", err.Error())
	}
	var userError *domain.UserError
	if !errors.As(err, &userError) {
		t.Error("ожидалась *domain.UserError")
	}
}
