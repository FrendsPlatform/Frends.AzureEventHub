# Changelog

## [1.3.0] - 2026-01-22

### Changed

- Set FailIfPartitionMissing default to false and clarified its documentation, including how skipped partitions are treated and how it interacts with ThrowErrorOnFailure

## [1.2.0] - 2026-01-16

### Changed

- Update Azure packages to latest versions.
    - Azure.Storage.Blobs to 12.27.0

## [1.1.0] - 2025-11-20

### Changed

- Fixed checkpoint implementation to use Azure Event Hubs metadata format and removed unsupported timestamp adjustment
  feature.

## [1.0.0] - 2025-07-23

### Changed

- Initial implementation
