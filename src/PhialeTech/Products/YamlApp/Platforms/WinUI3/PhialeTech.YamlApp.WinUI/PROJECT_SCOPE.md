# Project Purpose

This project contains the WinUI 3 adapter for the `PhialeTech.YamlApp` family.
It is responsible for mapping the neutral YamlApp model to WinUI 3 controls and platform services.

# What belongs here

- Mapping from the neutral YamlApp model to WinUI 3 controls
- WinUI-specific rendering adapter responsibilities
- WinUI composition helpers for YamlApp surfaces

# What must NOT be placed here

- Core validation and canonical model logic
- Neutral YAML definition models
- Runtime orchestration that is not WinUI-specific
- WPF or Avalonia implementations

# Dependencies

- `PhialeTech.YamlApp.Abstractions`
- `PhialeTech.YamlApp.Definitions`
- `PhialeTech.YamlApp.Core`
- `PhialeTech.YamlApp.Runtime`
- `UniversalInput.Contracts`

# Notes for future development

- Keep WinUI-specific concerns isolated here
- Reuse shared runtime and contracts wherever possible
- Avoid introducing direct dependencies from Core to WinUI types
