# Project Purpose

This project contains the neutral definition model for configurable YamlApp UI structures.
It is the place for describing documents, wizards, frames, fields, actions, layout metadata, and inheritance metadata without platform-specific behavior.

# What belongs here

- Document definitions
- Wizard definitions
- Step definitions
- Frame definitions
- Field definitions
- Action definitions
- Layout metadata
- Inheritance metadata such as `extends`
- Localization keys and other definition-time identifiers

# What must NOT be placed here

- Platform-specific rendering logic
- Runtime orchestration code
- Validation engines that execute workflow rules
- WPF, Avalonia, or WinUI controls
- Tooling UI and editor implementations

# Dependencies

- `PhialeTech.YamlApp.Abstractions`

# Notes for future development

- Keep the definition model platform-neutral
- Prefer descriptive configuration objects over behavior objects
- Use stable identifiers and localization keys instead of hardcoded captions

