# Project Purpose

This project contains the WPF adapter for the `PhialeTech.YamlApp` family.
It is responsible for mapping the neutral YamlApp model to WPF controls while staying aligned with the shared WPF styling infrastructure.

# What belongs here

- Mapping from the neutral YamlApp model to WPF controls
- WPF rendering adapter responsibilities
- Cooperation with `PhialeTech.Styles.Wpf`
- WPF-specific composition helpers for YamlApp surfaces

# What must NOT be placed here

- Core validation and canonical model logic
- Neutral YAML definition models
- Runtime orchestration that is not WPF-specific
- Avalonia or WinUI implementations

# Dependencies

- `PhialeTech.YamlApp.Abstractions`
- `PhialeTech.YamlApp.Definitions`
- `PhialeTech.YamlApp.Core`
- `PhialeTech.YamlApp.Runtime`
- `UniversalInput.Contracts`
- `PhialeTech.Styles.Wpf`

# Notes for future development

- Keep visual consistency centralized through the shared styles project
- Avoid pushing WPF-only assumptions back into Core or Runtime
- Treat this project as a thin adapter over the shared YamlApp logic
