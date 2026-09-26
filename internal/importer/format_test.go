package importer

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestParseFile_UnsupportedExtension(t *testing.T) {
	path := filepath.Join(t.TempDir(), "data.txt")
	if err := os.WriteFile(path, []byte("x"), 0o600); err != nil {
		t.Fatal(err)
	}
	_, err := ParseFile(path)
	if err == nil {
		t.Fatal("ожидалась ошибка неподдерживаемого расширения")
	}
	if !strings.Contains(err.Error(), "неподдерживаемое расширение файла «.txt»") {
		t.Errorf("текст ошибки = %q", err.Error())
	}
}

func TestParseFile_DispatchByExtension(t *testing.T) {
	path := filepath.Join(t.TempDir(), "data.yaml")
	source := "transistors:\n  - КТ315Б\n"
	if err := os.WriteFile(path, []byte(source), 0o600); err != nil {
		t.Fatal(err)
	}
	result, err := ParseFile(path)
	if err != nil {
		t.Fatalf("разбор не удался: %v", err)
	}
	if len(result.Entries) != 1 || result.Entries[0].Transistor.Name() != "КТ315Б" {
		t.Fatalf("entries = %+v", result.Entries)
	}
}
