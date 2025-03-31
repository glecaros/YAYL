# Changelog

## [1.3.0] - 2025-03-31

### Added

- **Serializer:** Added serializer object.

## [1.2.1] - 2025-03-21

### Fixed

- **File not found exception:** Parse properly throws a `YamlParseException` when the file received does not exist.

## [1.2.0] - 2025-02-28

### Added

- **Nested Polymorphic Types:** Added support for hierarchical type resolution with multiple levels of polymorphic types

### Fixed

- **Fixing issue with streams:** Fixed the way streams are handled.

## [1.1.0] - 2025-02-20

### Added
- **Variable Resolution:** Support for registering custom variable resolvers which substitute placeholder values (e.g. `${name}`) during parsing.
- **Async Parsing APIs:** New asynchronous versions of `Parse`, `ParseFile`, and related methods to support non-blocking I/O operations.
