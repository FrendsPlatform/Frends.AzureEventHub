# Changelog

## [2.0.0] - 2026-09-11

### Changed

- **Breaking:** Unified checkpoint targeting into a single `Input.Targets` list. Each `PartitionTarget` now has a `Mode` (`RelativeRollback`, `AbsoluteSequenceNumber`, `AbsoluteEnqueuedTime`) that selects which of `RollbackEvents`, `TargetSequenceNumber`, or `TargetEnqueuedTime` is used.
- **Breaking:** Removed `Input.PartitionIds` and `Input.RollbackEvents`. Relative rollback is now configured per-partition via a `PartitionTarget` with `Mode = RelativeRollback` and `RollbackEvents` set.
- `Options.FailIfPartitionMissing` is now respected when a RelativeRollback target has no existing checkpoint, setting it to true stops processing of any remaining partitions (already-applied changes are kept), while false continues processing the rest as before.
- Checkpoints are now written through the supported `BlobCheckpointStore.UpdateCheckpointAsync` API instead of direct blob metadata manipulation.
- Invalid partition IDs and out-of-range sequence numbers now produce clear, per-partition errors.
- 
### Added

- Absolute per-partition targeting via `Input.Targets`, allowing operators to set a specific `TargetSequenceNumber` or `TargetEnqueuedTime` per partition without manual arithmetic.
- Event Hub connection parameters (`EventHubNamespace`, `EventHubAuthMethod`, `EventHubConnectionString`, `EventHubSasToken`) so the Task can resolve partition ranges via `GetPartitionPropertiesAsync`.
- `FailIfPartitionOwned` option to guard against rewinding partitions currently owned by a running consumer.
- `Result.AppliedTargets` audit trail recording partition, previous sequence number and new sequence number for each applied change.

## [1.4.0] - 2026-08-13

### Changed

- Added documentation link and error property to comply with Frends task standards.

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
