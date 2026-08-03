# Blog Plan: Control Systems on Windows and State Machine Design

This document outlines a proposed blog series about building control systems on Windows, starting with the dedicated-thread state machine design implemented in this repository.

## Introduction Draft

Windows and .NET provide a capable task-pool model. For many applications, this is the right abstraction: submit work, let the runtime schedule it, and avoid manually managing threads. Web services, UI background tasks, file processing, and many general-purpose applications benefit from this approach.

Control systems have a different failure profile. The important question is often not how much work can run in parallel, but whether the software can explain exactly what happens next and whether required actions happen before their deadlines. In a real-time system, correctness depends on both the result and the time when that result is produced. That does not mean every control application on Windows is hard real-time. Standard Windows normally cannot provide hard worst-case scheduling guarantees for user-mode code. If a deadline is very relaxed, Windows may be fully acceptable, but the dominant risks become reliability, persistence, restart behavior, watchdogs, and fault recovery rather than scheduler jitter.

This distinction matters because many Windows-based control applications are not hard real-time, but they are still time-sensitive and order-sensitive. Hardware operations have ordering requirements. Drivers may be synchronous. Some calls may block longer than expected. State transitions must be reproducible, diagnosable, and safe to reason about after a failure. The architecture should therefore reduce avoidable scheduling uncertainty even when it cannot turn Windows into a hard real-time operating system.

The standard thread-pool approach is best understood as dynamic shared scheduling. Work is submitted to a common pool, and the runtime decides which worker thread runs it and when. This is efficient for many workloads, but it is a weak ownership model for control flow. Continuations may resume on unrelated worker threads. Independent application areas compete for the same pool. Blocking hardware calls can consume pool threads and introduce latency elsewhere. Parallelism can become accidental rather than intentional.

The FSM design described in this series takes the opposite position for control state: state ownership is explicit. The finite state machine owns one dedicated thread. All state callbacks run sequentially on that thread. External code does not modify FSM state directly; it posts events into a FIFO queue. This makes the order of state processing visible and reproducible. The state machine is not competing with unrelated thread-pool work for its control-flow execution context.

Synchronous device-driver pairs are handled separately. A driver/device pair can run behind its own dedicated worker, preserving per-device ordering while allowing independent devices to operate in parallel. The FSM receives device results as events and decides the next state transition. This is controlled parallelism: the design allows parallel hardware activity where it makes sense, but keeps control-state transitions single-threaded and deterministic.

By no means thia design is a claim of hard real-time behavior on standard Windows. Device calls still need bounded timeouts. Critical safety deadlines should be handled by hardware, PLCs, motion controllers, DAQ timing engines, watchdogs, or real-time components when required. The value of this FSM approach is that it attempts to reduce unnecessary uncertainty from the application layer: state changes have one owner, event order is explicit, driver calls are isolated, and failure paths can be modeled directly.

The first articles should establish this motivation before diving into code: what real-time requirements actually mean, where Windows is acceptable and where it is not, why thread-pool scheduling is useful but insufficient as the main control-flow model, and how queues, guards, device workers, and explicit failure paths form a practical control-system architecture on Windows.

## Grand Schema

### 1. Why Control Systems Are Different

Explain how control software differs from web, backend, and UI applications. Address ordering, repeatability, hardware latency, blocking drivers, and diagnostics. Example: start acquisition, wait for motion complete, trigger camera, validate result.

### 2. The Thread Pool Is Good, But Not for Everything

Explain what the .NET task pool is good at, then explain why it becomes risky as the main control-flow mechanism. Address scheduling variability, continuation placement, hidden parallelism, and starvation from blocking calls. Example: two hardware calls accidentally overlap because both were launched as tasks.

### 3. Dedicated Threads as Ownership Boundaries

Introduce the idea that some parts of a control system should own their execution thread. The FSM owns one thread. Each synchronous driver/device pair may own one worker thread. Example diagram: UI/config code posts command; FSM thread processes event; driver worker executes hardware call; result returns to FSM queue.

### 4. The State Machine Core

Explain states as classes, state type as identity, Enter/Handle/Exit, StateOutcome, transition registration with the builder, and FIFO event posting. Example: Idle to Initialize to Acquire to Process to Complete or Error.

### 5. Why the FSM Is Single-Threaded

Explain why state transitions should be serialized. States should not be modified from device callbacks, UI, timers, or worker threads. Example: bad design where a callback changes state directly versus good design where callback posts a DeviceCompleted event.

### 6. Device Workers

Explain synchronous driver/device pairs. Each worker serializes calls to one device. Workers can run in parallel for independent devices. Example: motion worker moves stage, DAQ worker starts acquisition, camera worker captures frame, FSM coordinates completion events.

### 7. Guards and Decision Logic

Explain guards as transition filters. Use guards when one state outcome can lead to different next states. Address deterministic rule: exactly one guard must match. Example: ValidateResult to Retry if retry count remains, Fail if retry limit exceeded, Complete if measurement is valid.

### 8. Macros and Sequences of States

Present macros as a future direction. A macro is not a separate thread; it is a planned sequence of state transitions. Dynamic macros can be config-defined, but should still execute through the FSM queue and guard rules. Example: calibration macro with home axis, move to reference, acquire baseline, store correction, return to idle.

### 9. Error Handling and Faults

Explain why control systems need explicit failure paths. Separate expected hardware errors from FSM faults. Example: device timeout returns event/result; missing transition or ambiguous guard faults the FSM.

### 10. Testing Strategy

Unit-test state transitions without hardware. Use fake device workers. Mark real hardware or simulator tests separately. Example: given Idle, when StartCommand, expect Initialize; given DAQ timeout event, expect Error.

### 11. DAQmx and ClickPLC as Real Driver Examples

Show how low-level wrappers are kept separate from FSM. ClickPLC is a synchronous driver behind a dedicated worker. DAQmx is a low-level C++/CLI wrapper consumed by DAQmxDeviceServer, not directly by FSM. Example: DAQ callback posts event to device server, which posts result event to FSM.

### 12. Practical Limitations

This is not hard real-time. Windows scheduling still applies. Blocking driver calls still need timeouts. Dedicated threads are not free; allocate them intentionally. Example: one thread per active driver/device pair, not one thread per tiny operation.

### 13. Final Design Rules

FSM state changes happen only on the FSM thread. External producers post events. Driver/device calls are serialized per device. Independent devices may run in parallel. All blocking operations must have timeouts. Guards must be deterministic. Hardware code stays below the device-server/driver boundary.

## Suggested Article Sequence

1. Why Thread Pools Are Not Enough for Control Systems

2. A Dedicated-Thread FSM for Windows Control Software

3. Events, Queues, and State Ownership

4. Synchronous Drivers Without Chaos

5. Transitions, Guards, and Failure Paths

6. Composing Actions with State Macros

7. Testing Control Logic Without Hardware

8. Integrating Real Drivers: PLC and DAQmx

## Examples To Prepare

Simple FSM: Idle, Initialize, Acquire, Complete, Error.

Guard example: Validate, Retry, Fail, Complete.

Worker example: DeviceCommand, DeviceResult, dedicated receiver thread.

Bad versus good callback example: direct context mutation versus queued DataEvent<DeviceResult>.

Multi-device example: motion, DAQ, and camera workers running independently while the FSM coordinates completion events.

Macro example: calibration sequence from config.

Testing example: fake device result posted into FSM queue, then assert final state or transition sequence.

