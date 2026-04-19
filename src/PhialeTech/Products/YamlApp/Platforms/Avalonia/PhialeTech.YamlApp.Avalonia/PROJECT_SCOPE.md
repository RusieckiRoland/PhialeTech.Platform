# Project Purpose

This project contains the Avalonia adapter for the `PhialeTech.YamlApp` family.
It is responsible for mapping the neutral YamlApp model to Avalonia controls and hosting constructs.

# What belongs here

- Mapping from the neutral YamlApp model to Avalonia controls
- Avalonia-specific rendering adapter responsibilities
- Avalonia composition helpers for YamlApp surfaces

# What must NOT be placed here

- Core validation and canonical model logic
- Neutral YAML definition models
- Runtime orchestration that is not Avalonia-specific
- WPF or WinUI implementations

# Dependencies

- `PhialeTech.YamlApp.Abstractions`
- `PhialeTech.YamlApp.Definitions`
- `PhialeTech.YamlApp.Core`
- `PhialeTech.YamlApp.Runtime`
- `UniversalInput.Contracts`

# Notes for future development

- Keep this adapter thin and focused on Avalonia concerns
- Reuse shared runtime behavior instead of duplicating orchestration
- Preserve compatibility with the repository's cross-platform direction
