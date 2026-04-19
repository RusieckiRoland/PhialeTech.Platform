# Project Purpose

This project contains tooling support for the `PhialeTech.YamlApp` family.
It is the place for validation, authoring, schema support, and development-time utilities around YamlApp definitions.

# What belongs here

- Validation tooling
- Authoring support
- Schema-related support
- Development tooling around YamlApp definitions
- Optional helpers that support authoring workflows and diagnostics

# What must NOT be placed here

- Production runtime orchestration
- Platform-specific UI adapters
- Stable public contracts that belong in `Abstractions`
- Neutral definition-only models that belong in `Definitions`

# Dependencies

- `PhialeTech.YamlApp.Abstractions`
- `PhialeTech.YamlApp.Definitions`
- `PhialeTech.YamlApp.Core`

# Notes for future development

- Keep tooling concerns clearly separated from production runtime concerns
- Prefer reusable validation and schema services over one-off scripts
- Treat this project as the home for developer-facing support around YamlApp definitions
