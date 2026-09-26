package importer

import (
	"errors"
	"fmt"

	"github.com/goccy/go-yaml/ast"
	"github.com/goccy/go-yaml/parser"

	"soviettransistors/internal/domain"
)

// parseYaml разбирает yaml в дерево value через AST goccy/go-yaml: узлы
// сохраняют порядок ключей документа. Комментарии — родной синтаксис yaml;
// повторяющиеся ключи библиотека отвергает при разборе — так же, как jsonc
// (decodeValue), поэтому имена ключей в дереве value уникальны.
func parseYaml(data []byte) (value, error) {
	file, err := parser.ParseBytes(data, 0)
	if err != nil {
		return value{}, err
	}
	switch len(file.Docs) {
	case 0:
		return value{kind: kindNull}, nil // пустой файл — null-документ
	case 1:
		if file.Docs[0].Body == nil {
			return value{kind: kindNull}, nil
		}
		return yamlToValue(file.Docs[0].Body)
	}
	return value{}, errors.New("файл содержит несколько yaml-документов, ожидается один")
}

// ParseYamlText разбирает yaml-текст. Некорректный формат — *domain.UserError.
func ParseYamlText(text string) (*ParseResult, error) {
	root, err := parseYaml([]byte(text))
	if err != nil {
		return nil, domain.NewUserError("файл не является корректным YAML: %s", err.Error())
	}
	return parseRoot(root)
}

// yamlToValue конвертирует узел yaml-AST в value; якоря и алиасы раскрываются
// в значения, теги и литеральные блоки разворачиваются в обёрнутое значение.
func yamlToValue(node ast.Node) (value, error) {
	switch n := node.(type) {
	case *ast.NullNode:
		return value{kind: kindNull}, nil
	case *ast.BoolNode:
		return value{kind: kindBool, boolean: n.Value}, nil
	case *ast.IntegerNode:
		return value{kind: kindNumber, num: n.GetToken().Value}, nil
	case *ast.FloatNode:
		return value{kind: kindNumber, num: n.GetToken().Value}, nil
	case *ast.InfinityNode:
		return value{kind: kindNumber, num: n.GetToken().Value}, nil
	case *ast.NanNode:
		return value{kind: kindNumber, num: n.GetToken().Value}, nil
	case *ast.StringNode:
		return value{kind: kindString, str: n.Value}, nil
	case *ast.LiteralNode:
		return value{kind: kindString, str: n.Value.Value}, nil
	case *ast.AnchorNode:
		return yamlToValue(n.Value)
	case *ast.AliasNode:
		return yamlToValue(n.Value)
	case *ast.TagNode:
		return yamlToValue(n.Value)
	case *ast.MappingNode:
		object := value{kind: kindObject}
		for _, item := range n.Values {
			name, err := yamlKeyName(item.Key)
			if err != nil {
				return value{}, err
			}
			converted, err := yamlToValue(item.Value)
			if err != nil {
				return value{}, err
			}
			object.members = append(object.members, member{name: name, value: converted})
		}
		return object, nil
	case *ast.SequenceNode:
		array := value{kind: kindArray}
		for _, item := range n.Values {
			converted, err := yamlToValue(item)
			if err != nil {
				return value{}, err
			}
			array.items = append(array.items, converted)
		}
		return array, nil
	}
	return value{}, fmt.Errorf("неподдерживаемый элемент yaml: %T", node)
}

// yamlKeyName — текст ключа отображения: обычные ключи и ключи явной формы
// «? ключ»; прочие (числовые, merge-ключ «<<») — ошибка, как в jsonc.
func yamlKeyName(key ast.MapKeyNode) (string, error) {
	switch k := key.(type) {
	case *ast.StringNode:
		return k.Value, nil
	case *ast.MappingKeyNode:
		if s, ok := k.Value.(*ast.StringNode); ok {
			return s.Value, nil
		}
	}
	return "", errors.New("неверный ключ объекта")
}
