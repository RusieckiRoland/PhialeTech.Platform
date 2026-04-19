# Project Purpose

This project contains the stable public contracts for the `PhialeTech.YamlApp` family.
It is the cross-project API surface intended to stay portable and predictable as the product family evolves.

# What belongs here

- Public interfaces used by other YamlApp projects
- Public contract models that are intended to remain stable
- Public enums and event argument types that define the cross-project API
- Contract-level abstractions that platform adapters and runtime components depend on

# What must NOT be placed here

- YAML parsing or inheritance resolution logic
- Runtime orchestration and state handling
- Platform-specific UI code
- WPF, Avalonia, or WinUI control implementations
- Tooling-specific authoring helpers

# Dependencies

- No internal project references

# Notes for future development

- Keep the surface area small and intentional
- Prefer additive evolution over breaking contract changes
- Treat this project as the public boundary for the YamlApp family
