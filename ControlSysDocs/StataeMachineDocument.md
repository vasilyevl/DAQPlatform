# Blog Plan: Control Systems on Windows and State Machine Design

This document outlines a proposed blog series about building control systems on Windows, starting with the dedicated-thread state machine design implemented in this repository.

## Introduction Draft

Many data acquisition and control systems are built on standard Windows. That is not accidental: Windows has excellent hardware vendor support, mature development tools, good UI frameworks, broad driver availability, and a large .NET ecosystem. For laboratory instruments, production testers, measurement automation, motion control stations, and engineering tools, Windows is often the most practical platform.

Because these systems are built as Windows applications, they often follow standard Windows and .NET guidance. Long-running or blocking work is moved out of the UI thread. I/O is exposed through `async` methods. Work is scheduled through `Task`, `Task.Run`, timers, callbacks, and the .NET thread pool. For many normal applications, this is the correct approach. It keeps user interfaces responsive, avoids manually managing threads, and lets the runtime use system resources efficiently.

Data acquisition and control software is driven by requirements that are less common in typical business, UI, or service applications. Operations often must happen in a specific order, within a defined time window, and with a clearly understood system state before and after each step. A measurement may need to start only after motion has settled. An output may need to change only after an interlock is verified. A fault path may need to leave hardware in a known condition. Drivers may be synchronous even when wrapped in modern APIs, and external callbacks may arrive on arbitrary threads. The control architecture therefore has to make ownership, ordering, timing assumptions, and recovery behavior explicit.

Real-time requirements make this distinction sharper. In a real-time system, correctness depends on both the result and the time when that result is produced. That does not mean every Windows-based control application is hard real-time. Standard Windows normally cannot provide hard worst-case scheduling guarantees for user-mode code. If a deadline is very relaxed, Windows may be acceptable, but then reliability, persistence, restart behavior, watchdogs, and fault recovery become more important than scheduler jitter. Many Windows control systems are better described as time-sensitive and order-sensitive rather than hard real-time.


`async` deserves separate treatment because it is often presented as the modern default for non-blocking work. It is useful when the operation is naturally asynchronous and the continuation does not own critical state. That is not always the case with hardware control. Many device APIs are synchronous underneath, so wrapping them in `Task.Run` only moves the blocking call to a pool thread. It does not make the device operation cancellable, deterministic, or safer. `async` can also fragment a control sequence into continuations that resume later, possibly on different threads, after other events have changed the world. For UI and service code that is usually acceptable. For control logic, it can obscure ownership, ordering, timeout handling, and fault recovery unless there is a stricter execution model above it.

The standard task-pool approach is dynamic shared scheduling. Work is submitted to a common pool, and the runtime decides which worker thread runs it and when. This is efficient for general workloads, but it is a weak ownership model for control flow. Continuations may resume on unrelated worker threads. Independent application areas compete for the same pool. Blocking hardware calls can consume pool threads and introduce latency elsewhere. Parallelism can become accidental rather than intentional. The code may look sequential at the call site while the actual execution order is distributed across callbacks, continuations, and pooled workers.

This does not make the thread pool wrong. It means the thread pool should not be the primary owner of control-state execution. The approach used by this FSM design is to make ownership explicit. The finite state machine owns one dedicated thread. All state callbacks run sequentially on that thread. External code does not modify FSM state directly; it posts events into a FIFO queue. This makes state-processing order visible and reproducible, and it prevents unrelated thread-pool work from becoming part of the control-state scheduling model.

Synchronous device-driver pairs are handled separately. A driver/device pair can run behind its own dedicated worker, preserving per-device ordering while allowing independent devices to operate in parallel. The FSM receives device results as events and decides the next transition. This is controlled parallelism: the design allows parallel hardware activity where it makes sense, but keeps state transitions single-threaded and deterministic.

By no means is this design a claim of hard real-time behavior on standard Windows. Device calls still need bounded timeouts. Critical safety deadlines should be handled by hardware, PLCs, motion controllers, DAQ timing engines, watchdogs, or real-time components when required. The value of this FSM approach is that it attempts to reduce unnecessary uncertainty from the application layer: state changes have one owner, event order is explicit, driver calls are isolated, and failure paths can be modeled directly.

The first articles should establish this motivation before diving into code: why many control systems are built on Windows, how standard Windows application guidance maps poorly to some data acquisition and control problems, what real-time requirements actually mean, where Windows is acceptable and where it is not, and how queues, guards, device workers, and explicit failure paths form a practical control-system architecture on Windows.

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





