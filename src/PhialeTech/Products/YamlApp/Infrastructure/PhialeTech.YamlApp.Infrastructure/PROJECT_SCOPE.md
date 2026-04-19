# Project Purpose

This project contains infrastructure concerns for the `PhialeTech.YamlApp` family.
It should host technical integrations such as YAML loading, serialization details, filesystem-backed definition sources, and other external implementation details that support the Core model.

# What belongs here

- YAML parser integration
- Mapping from YAML documents to neutral definition models
- Filesystem-backed definition loading
- Infrastructure adapters that implement Core-facing definition source contracts
- Serialization and deserialization details that should stay outside Core

# What must NOT be placed here

- Neutral definition-only models
- Core resolution and business rules
- Runtime state orchestration
- WPF, Avalonia, or WinUI control code
- Tooling-only editor workflows

# Dependencies

- `PhialeTech.YamlApp.Abstractions`
- `PhialeTech.YamlApp.Definitions`
- `PhialeTech.YamlApp.Core`

# Notes for future development

- Treat YAML libraries and file IO as replaceable implementation details
- Keep Core dependent on abstractions and contracts, not on parser-specific APIs
- Prefer small adapters that translate external representations into definition models
