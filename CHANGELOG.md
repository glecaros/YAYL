# Changelog

## [1.6.1] - 2025-05-12
- **YamlPathFieldAttribute**: Attribute now properly works with collections.

## [1.6.0] - 2025-05-11

- **Adding YamlContext**: New configuration object that can be provided to parse operations.
- **Adding YamlPathFieldAttribute**: New attribute that resolves file paths to absolute paths during parsing.

## [1.5.0] - 2025-05-09

- **Parser support for Sets:** Adding support for parsing set types (`ISet<T>`, `HashSet<T>`, and `SortedSet<T>`)

## [1.4.0] - 2025-05-05

- **YamlDerivedTypeDefaultAttribute:** Adding attribute to enable fallback type for discriminated type.

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
