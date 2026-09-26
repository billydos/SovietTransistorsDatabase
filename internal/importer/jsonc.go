// Package importer читает справочник из jsonc-файла (JSON с комментариями
// и висячими запятыми). Комментарии и запятые убираются собственным стриппером,
// затем текст разбирается потоковым декодером с сохранением порядка ключей
// (порядок нужен для дословных сообщений об ошибках).
package importer

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
)

type jsonKind int

const (
	jsonNull jsonKind = iota
	jsonBool
	jsonNumber
	jsonString
	jsonArray
	jsonObject
)

type jsonMember struct {
	name  string
	value jsonValue
}

type jsonValue struct {
	kind    jsonKind
	str     string
	num     string
	boolean bool
	members []jsonMember
	items   []jsonValue
}

// has возвращает элемент объекта по имени (второе значение — наличие ключа).
func (v jsonValue) has(name string) (jsonValue, bool) {
	for _, m := range v.members {
		if m.name == name {
			return m.value, true
		}
	}
	return jsonValue{}, false
}

// parseJsoncToValue превращает jsonc-текст в дерево значений с порядком ключей.
func parseJsoncToValue(text string) (jsonValue, error) {
	decoder := json.NewDecoder(bytes.NewReader(jsoncToJSON([]byte(text))))
	decoder.UseNumber()
	root, err := decodeJsonValue(decoder)
	if err != nil {
		return jsonValue{}, err
	}
	return root, nil
}

func decodeJsonValue(decoder *json.Decoder) (jsonValue, error) {
	token, err := decoder.Token()
	if err != nil {
		return jsonValue{}, err
	}
	switch t := token.(type) {
	case nil:
		return jsonValue{kind: jsonNull}, nil
	case bool:
		return jsonValue{kind: jsonBool, boolean: t}, nil
	case string:
		return jsonValue{kind: jsonString, str: t}, nil
	case json.Number:
		return jsonValue{kind: jsonNumber, num: t.String()}, nil
	case json.Delim:
		switch t {
		case '{':
			value := jsonValue{kind: jsonObject}
			for decoder.More() {
				nameToken, err := decoder.Token()
				if err != nil {
					return jsonValue{}, err
				}
				name, ok := nameToken.(string)
				if !ok {
					return jsonValue{}, errors.New("неверный ключ объекта")
				}
				item, err := decodeJsonValue(decoder)
				if err != nil {
					return jsonValue{}, err
				}
				value.members = append(value.members, jsonMember{name: name, value: item})
			}
			if _, err := decoder.Token(); err != nil { // закрывающая }
				return jsonValue{}, err
			}
			return value, nil
		case '[':
			value := jsonValue{kind: jsonArray}
			for decoder.More() {
				item, err := decodeJsonValue(decoder)
				if err != nil {
					return jsonValue{}, err
				}
				value.items = append(value.items, item)
			}
			if _, err := decoder.Token(); err != nil { // закрывающая ]
				return jsonValue{}, err
			}
			return value, nil
		}
	}
	return jsonValue{}, fmt.Errorf("неожидаемый токен %v", token)
}

// jsoncToJSON удаляет комментарии и висячие запятые вне строковых литералов.
func jsoncToJSON(source []byte) []byte {
	return stripTrailingCommas(stripComments(source))
}

func stripComments(source []byte) []byte {
	var out bytes.Buffer
	inString := false
	escaped := false
	for i := 0; i < len(source); {
		c := source[i]
		switch {
		case inString:
			out.WriteByte(c)
			if escaped {
				escaped = false
			} else if c == '\\' {
				escaped = true
			} else if c == '"' {
				inString = false
			}
			i++
		case c == '"':
			inString = true
			out.WriteByte(c)
			i++
		case c == '/' && i+1 < len(source) && source[i+1] == '/':
			for i < len(source) && source[i] != '\n' {
				i++
			}
		case c == '/' && i+1 < len(source) && source[i+1] == '*':
			i += 2
			for i+1 < len(source) && !(source[i] == '*' && source[i+1] == '/') {
				i++
			}
			if i+1 < len(source) {
				i += 2
			} else {
				i = len(source)
			}
			out.WriteByte(' ')
		default:
			out.WriteByte(c)
			i++
		}
	}
	return out.Bytes()
}

func stripTrailingCommas(source []byte) []byte {
	var out bytes.Buffer
	inString := false
	escaped := false
	for i := 0; i < len(source); i++ {
		c := source[i]
		switch {
		case inString:
			out.WriteByte(c)
			if escaped {
				escaped = false
			} else if c == '\\' {
				escaped = true
			} else if c == '"' {
				inString = false
			}
		case c == '"':
			inString = true
			out.WriteByte(c)
		case c == ',':
			j := i + 1
			for j < len(source) && isJSONSpace(source[j]) {
				j++
			}
			if j < len(source) && (source[j] == '}' || source[j] == ']') {
				continue
			}
			out.WriteByte(c)
		default:
			out.WriteByte(c)
		}
	}
	return out.Bytes()
}

func isJSONSpace(c byte) bool {
	return c == ' ' || c == '\t' || c == '\n' || c == '\r'
}
