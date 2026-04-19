# Project Purpose

This project contains runtime orchestration for the `PhialeTech.YamlApp` family.
It is intended to coordinate definition loading, state transitions, and the operational lifecycle of configured UI flows.

# What belongs here

- Runtime state
- Definition loading orchestration
- Submit, cancel, next, and back lifecycle handling
- Binding coordination
- Provider orchestration
- Runtime services that sit above Core rules and below platform adapters

# What must NOT be placed here

- Stable public contract definitions
- Neutral definition-only models
- Native WPF, Avalonia, or WinUI controls
- Tooling UI and editor-only code

# Dependencies

- `PhialeTech.YamlApp.Abstractions`
- `PhialeTech.YamlApp.Definitions`
- `PhialeTech.YamlApp.Core`

# Notes for future development

- Keep runtime concerns separate from pure core transformations
- Make orchestration explicit and testable
- Avoid leaking platform-specific behavior into this project
