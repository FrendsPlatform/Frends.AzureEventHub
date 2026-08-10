# Changelog

## [1.4.0] - 2026-07-31

### Added

- Added `ThrowErrorOnFailure` option: when set to `true` (default), the Task throws an exception on failure; when `false`, it returns a result with the `Error` property populated instead of throwing.
- Added `ErrorMessageOnFailure` option: an optional custom message to include in the error when the Task fails.
- Added `Error` property to the result, containing error details when the Task fails and `ThrowErrorOnFailure` is set to `false`.

### Changed

- Upgraded target framework from .NET 6 to .NET 8.

## [1.3.0] - 2026-01-16

### Changed

- Update Azure packages to latest versions.
    - Azure.Identity to 1.17.1
    - Azure.Messaging.EventHubs to 5.12.2

## [1.2.0] - 2024-11-13

### Changed

- Upgraded Azure.Messaging.EventHubs to version 5.11.5.

## [1.1.0] - 2024-08-22

### Added

- Updated Azure.Identity to the latest version 1.12.0.

## [1.0.0] - 2023-01-25

### Added

- Initial implementation