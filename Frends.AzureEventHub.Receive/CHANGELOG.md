# Changelog

## [2.4.0] - 2024-11-13
### Changed
- Upgraded Azure.Messaging.EventHubs to version 5.11.5.
- Upgraded Azure.Messaging.EventHubs.Processor to version 5.11.5.

## [2.3.0] - 2024-10-30
### Fixed
- Issue #11 - Fixed Receive hanging in execution by adding a check for the MaximumWaitTime.

## [2.2.0] - 2024-08-22
### Added
- Updated Azure.Identity to the latest version 1.12.0.

## [2.1.0] - 2024-05-31
### Changed
- Replaced the List<> with a ConcurrentBag<> for the received messages.

## [2.0.1] - 2023-11-29
### Fixed
- Fixed ExceptionHandler check in catch and documentational fixes.

## [2.0.0] - 2023-10-31
### Added
- New property `Result.Errors`.
### Changed
- Improved descriptions and examples.
- Errors and exceptions will be listed in `Result.Errors` if `Options.ExceptionHandler` is set to `ExceptionHandlers.Info`.
- Renamed `Options.MaximumWaitTimeInMinutes` to `Options.MaxRunTime`.
- `Options.MaxRunTime` (previously called `Options.MaximumWaitTimeInMinutes`) is now in seconds instead of minutes.
- `Consumer.MaximumWaitTime` cannot exceed `Options.MaxRunTime` when `Options.MaxRunTime` is greater than 0.
- Renamed `Consumer.FullyQualifiedNamespace` to `Consumer.Namespace`.
- Renamed `Options.Delay` to `Options.ConsumeAttemptDelay`.
- Package updates:
	- Azure.Identity 1.8.2 to 1.10.3
	- Azure.Messaging.EventHubs 5.9.0 to 5.9.3
	- Azure.Messaging.EventHubs.Processor 5.9.0 to 5.9.3
	- System.ComponentModel.Annotations 4.7.0 to 5.0.0

## [1.0.0] - 2023-05-31
### Added
- Initial implementation