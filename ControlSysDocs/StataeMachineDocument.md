# Blog Plan: Control Systems on Windows and State Machine Design

This document outlines a proposed blog series about building control systems on Windows, starting with the dedicated-thread state machine design implemented in this repository.

## Introduction Draft

Many data acquisition and control systems are built on standard Windows. That is not accidental: Windows has excellent hardware vendor support, mature development tools, good UI frameworks, broad driver availability, and a large .NET ecosystem. For laboratory instruments, production testers, measurement automation, motion control stations, and engineering tools, Windows is often the most practical and widely accepted platform.

Because these systems are built as Windows applications, they often follow standard Windows and .NET guidance: move long-running work out of the UI thread, expose I/O through `async` methods, and schedule work through `Task`, `Task.Run`, timers, callbacks, and the .NET thread pool. That guidance is useful background, but this document is mainly about where a control-system state machine benefits from a more explicit execution model.

Data acquisition and control software is driven by requirements that are less common in typical business, UI, or service applications. Operations often must happen in a specific order, within a defined time window, and with a clearly understood system state before and after each step. A measurement may need to start only after motion has settled. An output may need to change only after an interlock is verified. A fault path may need to leave hardware in a known condition. Drivers may be synchronous even when wrapped in modern APIs, and external callbacks may arrive on arbitrary threads. The control architecture therefore has to make ownership, ordering, timing assumptions, and recovery behavior explicit.

Real-time requirements make this distinction sharper. In a real-time system, correctness depends on both the result and the time when that result is produced. That does not mean every Windows-based control application is hard real-time. Standard Windows normally cannot provide hard worst-case scheduling guarantees for user-mode code. If a deadline is very relaxed, Windows may be acceptable, but then reliability, persistence, restart behavior, watchdogs, and fault recovery become more important than scheduler jitter. Many Windows control systems are better described as time-sensitive and order-sensitive rather than hard real-time.


`async` deserves separate treatment because it is often presented as the modern default for non-blocking work. It is useful when the operation is naturally asynchronous and the continuation does not own critical state. That is not always the case with hardware control. Many device APIs are synchronous underneath, so wrapping them in `Task.Run` only moves the blocking call to a pool thread. It does not make the device operation cancellable, deterministic, or safer. `async` can also fragment a control sequence into continuations that resume later, possibly on different threads, after other events have changed the world. For UI and service code that is usually acceptable. For control logic, it can obscure ownership, ordering, timeout handling, and fault recovery unless there is a stricter execution model above it.

For a control-state machine, the main drawback of a task-pool-centered design is that execution ownership becomes indirect. The code may look sequential at the call site while the actual control flow is distributed across callbacks, continuations, and pooled workers. That can make ordering, timeout handling, and recovery paths harder to reason about.

The design here starts from a different priority: make control-state ownership explicit, keep transition order visible, and treat parallelism as a deliberate architectural choice rather than an incidental result of scheduling.

Finite state machines are a well-known design technique, and they are widely used in control systems, embedded software, device orchestration, communication protocols, and UI workflows. Their value comes from making allowed states, accepted events, and permitted transitions explicit instead of scattering control flow across callbacks and flags.

The approach used by this FSM implementation attempts to combine that familiar state-machine model with an execution model suitable for Windows-based control software. The finite state machine owns one dedicated thread. All state callbacks run sequentially on that thread. External code does not modify FSM state directly; it posts events into a FIFO queue. This makes state-processing order visible and reproducible, and it keeps unrelated thread-pool work out of the control-state execution path.

By no means is this design a claim of hard real-time behavior on standard Windows. Device calls still need bounded timeouts. Critical safety deadlines should be handled by hardware, PLCs, motion controllers, DAQ timing engines, watchdogs, or real-time components when required. The value of this FSM approach is that it attempts to reduce unnecessary uncertainty from the application layer: state changes have one owner, event order is explicit, driver calls are isolated, and failure paths can be modeled directly.

The first articles should establish this design motivation before diving into code: why many control systems are built on Windows, what real-time requirements actually mean, how this FSM owns state execution, and how queues, guards, device workers, and explicit failure paths form a practical control-system architecture.

## Grand Schema

### 1. Why Control Systems Are Different

Explain how control software differs from web, backend, and UI applications. Address ordering, repeatability, hardware latency, blocking drivers, and diagnostics. Example: start acquisition, wait for motion complete, trigger camera, validate result.
#### Key Points To Address

- Windows is a practical and widely used platform for control systems, especially where vendor drivers, engineering tools, operator UI, data storage, reporting, and integration with existing infrastructure matter.

- Control software has ordered hardware dependencies. A later operation may only be valid if an earlier operation completed successfully and the hardware is in a known state.

- Hardware state matters as much as software state. It is not enough to know that a method returned; the control logic may need to know whether motion has settled, an interlock is still valid, a trigger was accepted, a device was stopped, or an output was returned to a safe value.

- Control operations often require bounded waits, timeouts, interlocks, settling checks, and explicit recovery paths.

- Real-time terminology should be used carefully. Standard Windows applications usually should not claim hard real-time behavior. However, many Windows control systems are still time-sensitive and order-sensitive, so the application architecture should avoid adding unnecessary scheduling and ordering uncertainty.

- Standard `async`, task, timer, and callback patterns can make ownership and ordering less obvious when used as the main control-flow model.

- This section should leave the reader with the motivation for explicit control-state ownership, without explaining the full FSM implementation yet.

### 2. Why Control-State Execution Needs Explicit Ownership

Explain why control-state execution should have a clear owner. Address ordering, continuation placement, hidden parallelism, and blocking hardware calls. Example: two hardware calls accidentally overlap because both were launched as independent tasks.

### 3. Dedicated Threads as Ownership Boundaries

Introduce the idea that some parts of a control system should own their execution thread. The FSM owns one thread. Each synchronous driver/device pair may own one worker thread. Example diagram: UI/config code posts command; FSM thread processes event; driver worker executes hardware call; result returns to FSM queue.

### 4. The State Machine Core

Explain states as classes, state type as identity, Enter/Handle/Exit, StateOutcome, transition registration with the builder, and FIFO event posting. Example: Idle to Initialize to Acquire to Process to Complete or Error.

### 5. Why the FSM Is Single-Threaded

Explain why state transitions should be serialized. States should not be modified from device callbacks, UI, timers, or worker threads. Example: bad design where a callback changes state directly versus good design where callback posts a DeviceCompleted event.

### 6. Device Workers

Explain synchronous driver/device pairs and why they belong behind an explicit execution boundary. A driver/device pair can run behind its own dedicated worker, preserving per-device ordering while allowing independent devices to operate in parallel. The FSM receives device results as events and decides the next transition. This is controlled parallelism: the design allows parallel hardware activity where it makes sense, but keeps state transitions single-threaded and deterministic.

Example: motion worker moves stage, DAQ worker starts acquisition, camera worker captures frame, FSM coordinates completion events.



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

1. Why Control Systems Need Explicit State Ownership

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
## First Article Plan: Why Control Systems Are Different

Purpose: introduce the reader to the practical differences between ordinary Windows applications and data acquisition/control applications. This section should motivate the FSM design without explaining the full implementation yet.

### Proposed Section Layout

1. Start from the Windows reality

   Many control and data acquisition systems are built on Windows because it is practical: vendor drivers are available, engineering tools are mature, UI development is productive, and .NET is widely used in industrial and laboratory software.

2. Explain the standard Windows application model briefly

   Mention that typical Windows guidance encourages responsive UI design, `async` APIs, background tasks, callbacks, timers, and thread-pool scheduling. Keep this short. The goal is not to teach the thread pool, only to establish the common baseline.

3. State the key difference

   In control software, correctness often depends on ordered actions, known state, bounded waiting, and explicit recovery behavior. The system must not only complete work; it must do the right thing in the right order, under known assumptions.

4. Give a concrete measurement sequence

   Example sequence:

   ```text
   Home axis
   Move to measurement position
   Wait for motion complete
   Verify interlock
   Configure DAQ
   Start acquisition
   Trigger camera
   Read result
   Validate measurement
   Return to idle or go to fault handling
   ```

   Use this example to show that each step depends on the previous step and that errors must branch into known recovery paths.

5. Explain critical requirements

   Address these requirements explicitly:

   - operation ordering
   - hardware settling and readiness
   - interlocks and safety conditions
   - bounded waits and timeouts
   - known state before and after each operation
   - deterministic fault handling
   - diagnostics after failure

6. Clarify real-time language

   Define real-time briefly: correctness depends on both the result and when the result is produced. Then clarify that standard Windows is usually not the component that should guarantee hard real-time deadlines. For Windows control software, the application layer should reduce avoidable timing and ordering uncertainty, while critical deadlines should be handled by hardware, PLCs, motion controllers, DAQ timing engines, watchdogs, or real-time components.

7. Explain why scattered control flow hurts

   Point out that callbacks, timers, `Task.Run`, and `async` continuations can spread control logic across multiple execution contexts. This can make it harder to answer simple but important questions:

   - Who owns the current state?
   - What event caused this transition?
   - Can another operation modify the same device at the same time?
   - What happens if this call times out?
   - What state is the hardware left in after a fault?

8. Introduce the design direction

   End the section with the principle:

   ```text
   One owner changes control state.
   Everyone else posts events.
   ```

   This prepares the reader for the dedicated-thread FSM design introduced in the next section.



### Example Material To Prepare
- A small measurement sequence with motion, DAQ, and camera steps.
- A bad example where a driver callback directly changes control state.
- A good example where the callback posts an event to the FSM queue.
- A bad example where two `Task.Run` operations access the same device concurrently.
- A good example where a dedicated device worker serializes access to that device.
- A simple fault path: timeout during acquisition leads to `StopAcquisition`, `SafeState`, then `Error`.

### Suggested Narrative Flow

Start practical, not theoretical. First explain why Windows is commonly used. Then show that data acquisition and control programs have requirements that ordinary application patterns do not fully address. Only after that introduce real-time terminology and the need for explicit state ownership. The section should end by making the dedicated-thread FSM feel like a natural response to the problem, not like an arbitrary framework choice.



