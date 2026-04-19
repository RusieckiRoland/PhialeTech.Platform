# PhialeTech.Yaml.Library

Passive YAML definition library for PhialeTech forms.

This project intentionally contains:

- YAML definition files embedded into the assembly
- localization YAML files embedded into the assembly
- a single marker type used to discover the assembly

This project intentionally does not contain:

- parsing logic
- namespace/import resolution logic
- `extends` resolution logic
- runtime UI generation logic

Those responsibilities belong to the YAML loader/compiler and platform renderers.

## Purpose

`PhialeTech.Yaml.Library` is the neutral source of YAML definitions shared by every renderer:

- WPF
- Angular
- Flutter
- future platforms

The library is platform-agnostic and only stores YAML resources and localization entries.

## Current structure

```text
Definitions/
  bases/
  medium/
  domain/
  application/
Localization/
  en.yaml
  pl.yaml
```

## YAML DSL rules

### 1. Module root

Every YAML module declares a namespace.

```yaml
namespace: domain.person
imports:
  - medium
```

- `namespace` is required
- `imports` is optional
- imported namespaces become visible to the current module

### 2. Definitions

Reusable building blocks live under `definitions`.

```yaml
namespace: medium
imports:
  - bases

definitions:
  limited50Text:
    extends: baseText
    maxLength: 50
```

### 3. Documents

Runtime forms live under `documents`.

```yaml
namespace: application.forms
imports:
  - domain.person

documents:
  yaml-generated-form:
    name: YAML generated form
```

### 4. Only one composition keyword

The DSL uses only one composition mechanism:

- `extends`

`extends` always means:

1. take the referenced object as the base
2. copy its properties
3. apply local overrides

This rule is used:

- between base definitions
- between domain definitions
- inside final document fields

### 5. Fields inside documents

Document fields are object instances identified by `id`.

```yaml
fields:
  - id: firstName
    extends: firstName

  - id: lastName
    extends: lastName
```

- `id` is the local field identifier
- `id` is also the key that later maps to JSON output
- `extends` points to the reusable definition used as the base

### 6. Localization

Definitions should use localization keys instead of literal UI text.

```yaml
captionKey: person.firstName.caption
placeholderKey: person.firstName.placeholder
```

Localization files map those keys to language-specific strings:

```yaml
person.firstName.caption: First name
person.firstName.placeholder: Enter first name
```

### 7. Neutral localization key naming

Localization keys should be semantic and language-neutral.

Good:

- `person.firstName.caption`
- `person.firstName.placeholder`
- `actions.ok.caption`

Avoid:

- `zasoby.name`
- `resources.name`

## Example

### Base definition

```yaml
namespace: bases

definitions:
  baseText:
    control: YamlTextBox
    valueType: string
    multiline: false
```

### Domain definition

```yaml
namespace: domain.person
imports:
  - medium

definitions:
  firstName:
    extends: limited50Text
    captionKey: person.firstName.caption
    placeholderKey: person.firstName.placeholder
    required: true
```

### Final document

```yaml
namespace: application.forms
imports:
  - domain.person

documents:
  yaml-generated-form:
    name: YAML generated form
    interactionMode: Classic
    densityMode: Normal
    fieldChromeMode: Framed
    actionStripMode: SharedFooter
    fields:
      - id: firstName
        extends: firstName
      - id: lastName
        extends: lastName
      - id: notes
        extends: notes
```

## Notes

- The current first implementation covers text-based fields.
- Future libraries will add reusable definitions for:
  - dates
  - numbers
  - currencies
  - other input kinds
