# AGENTS.md

## Project Overview

This project is a Windows-based instrumentation and data acquisition framework written in C# and .NET 8.

The framework is used for:

* Data acquisition
* Motion control
* Hardware integration
* Measurement automation
* Scientific data processing

Reliability and maintainability are more important than micro-optimizations.

## Technologies

* C# 12
* C++/CLI
* .NET 8
* Microsoft.Extensions.Logging
* xUnit
* Newtonsoft.Json
* WPF (MVVM)

## General Coding Rules

* Prefer clarity over cleverness.
* Avoid unnecessary abstractions.
* Use dependency injection whenever practical.
* Use async/await for I/O-bound operations.
* Avoid async void except for UI event handlers.
* Use nullable reference types.
* Enable warnings as errors.

## Logging

* Use Microsoft.Extensions.Logging.
* Do not use Console.WriteLine.
* Log exceptions with context information.
* Use structured logging.

Example:

_logger.LogInformation(
"Task {TaskName} completed in {DurationMs} ms",
taskName,
durationMs);

## Error Handling

* Throw specific exceptions.
* Avoid swallowing exceptions.
* Add meaningful messages.
* Preserve inner exceptions.

## Testing

* Use xUnit.
* Add tests for all new public APIs.
* Prefer deterministic tests.
* Avoid timing-sensitive tests.

## Performance

* Avoid unnecessary allocations in hot paths.
* Use Span<T> only when measurable benefit exists.
* Benchmark before optimizing.

## Documentation

* Add XML comments for all public APIs.
* Keep examples compileable.
* Update documentation when behavior changes.

## Architecture Rules

* UI must not directly access hardware.
* Hardware communication belongs in driver layers.
* Business logic must not depend on WPF.
* Avoid static mutable state.

## Before Finishing Any Task

1. Build the solution.
2. Run all tests.
3. Fix warnings introduced by the change.
4. Verify documentation is updated.
5. Summarize all modifications.
