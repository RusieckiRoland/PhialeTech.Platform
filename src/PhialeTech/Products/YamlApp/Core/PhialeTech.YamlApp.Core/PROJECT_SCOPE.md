# Project Purpose

This project contains platform-agnostic logic for the `PhialeTech.YamlApp` mechanism.
It should host the rules that transform neutral definitions into canonical models and evaluate shared behavior without depending on a specific UI framework.

# What belongs here

- Inheritance and `extends` resolution
- Namespace resolution
- Validation of YAML definitions
- Canonical model building
- Core rules for forms, wizards, and frames
- Shared logic that interprets universal input abstractions

# What must NOT be placed here

- WPF, Avalonia, or WinUI control code
- Native platform event conversion code
- Application bootstrapping and runtime orchestration
- Tooling-only authoring workflows

# Dependencies

- `PhialeTech.YamlApp.Abstractions`
- `PhialeTech.YamlApp.Definitions`
- `UniversalInput.Contracts`

# Notes for future development

- Keep this assembly portable and deterministic
- Prefer explicit transformation and validation services over hidden side effects
- Treat YAML as configuration and contract, not as a programming language
