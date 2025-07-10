# Changelog

## [1.8.0] - 2025-07-09

- **YamlVariantTypeDefaultAttribute:** Added attribute that allows adding a catch all to variant declarations.
- **Improved exception messages:** Added more information to exceptions to make errors easier to debug.
- **Fixed**: Fixed issue with variants when the node is null or not present.


## [1.7.3] - 2025-07-08

- **Fixed:**: Fixed issue that was causing the discriminator field in discriminated unions to show in the field marked with `[YamlExtra]`.

## [1.7.2] - 2025-07-08

- **Fixed:**: Fixed issue that was causing some mapped fields to show up in the field marked with `[YamlExtra]`.

## [1.7.1] - 2025-07-07

- **Updated library to target .net 8:** Updated to point to latest LTS version.

## [1.7.0] - 2025-07-04

- **YamlExtraAttribute:** Adding attribute that allows storing unhandled fields to be stored in a `Dictionary<string, object>`.
- **YamlVariantAttribute:** Adding attribute to allow parsing `object` based variants, this also includes the `YamlVariantTypeObjectAttribute` and `YamlVariantTypeScalarAttribute`.

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
