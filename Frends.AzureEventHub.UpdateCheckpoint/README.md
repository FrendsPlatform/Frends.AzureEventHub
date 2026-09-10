# Frends.AzureEventHub.UpdateCheckpoint

Task to update checkpoints in an Azure Storage container for a specified Event Hub consumer group.

[![UpdateCheckpoint_build](https://github.com/FrendsPlatform/Frends.AzureEventHub/actions/workflows/UpdateCheckpoint_build_and_test_on_main.yml/badge.svg)](https://github.com/FrendsPlatform/Frends.AzureEventHub/actions/workflows/UpdateCheckpoint_build_and_test_on_main.yml)
![Coverage](https://app-github-custom-badges.azurewebsites.net/Badge?key=FrendsPlatform/Frends.AzureEventHub/Frends.AzureEventHub.UpdateCheckpoint|main)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

## Installing

You can install the Task via frends UI Task View.

## Usage

The Task supports two targeting modes:

- **Relative rewind (default):** provide `PartitionIds` and `RollbackEvents`. Each partition's checkpoint is moved back by the given number of events, floored at the partition's beginning sequence number.
- **Absolute targeting:** provide `Targets`, a collection of partition/position pairs. For each entry set either `TargetSequenceNumber` (an exact sequence number) or `TargetEnqueuedTime` (the first event enqueued at or after the given time). No manual arithmetic is required.

The Task resolves each partition's valid range via `GetPartitionPropertiesAsync` and rejects out-of-range sequence numbers and unknown partition IDs with clear per-partition errors. Checkpoints are written using the supported `BlobCheckpointStore.UpdateCheckpointAsync` API.

`Result.AppliedTargets` provides an audit trail (partition, previous sequence number, new sequence number) for each applied change.

### Concurrency

Rewinding a checkpoint while a consumer is actively processing the partition is unsafe: the running consumer may overwrite the checkpoint. Stop the consuming Process before rewinding. When `FailIfPartitionOwned` is enabled (default), the Task fails for partitions that appear to be actively owned rather than silently competing with a running consumer.

### Secrets

Provide storage and Event Hub connection strings, SAS tokens and OAuth secrets via frends Environment Variables (`#env`) so credentials are not stored in Task parameters or written to logs.

## Building

### Clone a copy of the repository

`git clone https://github.com/FrendsPlatform/Frends.AzureEventHub.git`

### Build the project

`dotnet build`

### Run tests

Run the tests

`dotnet test`

### Create a NuGet package

`dotnet pack --configuration Release`

### Third party licenses

StyleCop.Analyzer version (unmodified version 1.1.118) used to analyze code uses Apache-2.0 license, full text and
source code can be found at https://github.com/DotNetAnalyzers/StyleCopAnalyzers
