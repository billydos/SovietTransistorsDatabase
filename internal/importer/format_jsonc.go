package importer

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"

	"github.com/tailscale/hujson"

	"soviettransistors/internal/domain"
)

// parseJsonc разбирает jsonc (JSON с комментариями и висячими запятыми):
// hujson нормализует текст до стандартного JSON (комментарии и лишние запятые
// убирает библиотека), затем потоковый декодер stdlib строит дерево value.
func parseJsonc(data []byte) (value, error) {
	standardized, err := hujson.Standardize(data)
	if err != nil {
		return value{}, err
	}
	decoder := json.NewDecoder(bytes.NewReader(standardized))
	decoder.UseNumber()
	return decodeValue(decoder)
}

// ParseJsoncText разбирает jsonc-текст. Некорректный формат — *domain.UserError.
func ParseJsoncText(text string) (*ParseResult, error) {
	root, err := parseJsonc([]byte(text))
	if err != nil {
		return nil, domain.NewUserError("файл не является корректным JSONC: %s", err.Error())
	}
	return parseRoot(root)
}

// decodeValue строит дерево value из потока json-токенов (UseNumber — числа
// как текст, без потери точности).
func decodeValue(decoder *json.Decoder) (value, error) {
	token, err := decoder.Token()
	if err != nil {
		return value{}, err
	}
	switch t := token.(type) {
	case nil:
		return value{kind: kindNull}, nil
	case bool:
		return value{kind: kindBool, boolean: t}, nil
	case string:
		return value{kind: kindString, str: t}, nil
	case json.Number:
		return value{kind: kindNumber, num: t.String()}, nil
	case json.Delim:
		switch t {
		case '{':
			object := value{kind: kindObject}
			for decoder.More() {
				nameToken, err := decoder.Token()
				if err != nil {
					return value{}, err
				}
				name, ok := nameToken.(string)
				if !ok {
					return value{}, errors.New("неверный ключ объекта")
				}
				item, err := decodeValue(decoder)
				if err != nil {
					return value{}, err
				}
				object.members = append(object.members, member{name: name, value: item})
			}
			if _, err := decoder.Token(); err != nil { // закрывающая }
				return value{}, err
			}
			return object, nil
		case '[':
			array := value{kind: kindArray}
			for decoder.More() {
				item, err := decodeValue(decoder)
				if err != nil {
					return value{}, err
				}
				array.items = append(array.items, item)
			}
			if _, err := decoder.Token(); err != nil { // закрывающая ]
				return value{}, err
			}
			return array, nil
		}
	}
	return value{}, fmt.Errorf("неожидаемый токен %v", token)
}
