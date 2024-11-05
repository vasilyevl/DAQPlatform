/*
Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted,
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software
without restriction, including without limitation the rights to use, copy,
modify, merge, publish, distribute, sublicense, and/or sell copies of the
Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#pragma once

using namespace System;
using namespace System::Runtime::InteropServices;
namespace Grumpy{

	namespace DAQmxNetApi {

	#pragma unmanaged
		#include <NIDAQmx.h>

	#pragma managed
		
		public enum class EventType
		{
			Done = 0,
			EveryNSamplesReceived = 1,
			EveryNSamplesTransferred = 2
		};

		public enum class AiTermination
		{
			Default = DAQmx_Val_Cfg_Default,			//  -1  Default
			RSE = DAQmx_Val_RSE,						//  10083 Referenced Single-Ended
			NRSE = DAQmx_Val_NRSE,						//  10078 Non-Referenced Single-Ended
			Differential = DAQmx_Val_Diff,				//  10106 Differential
			PseudoDifferential = DAQmx_Val_PseudoDiff	//  12529 Pseudo-Differential
		};

		public enum class VoltageUnits
		{
			Volts = DAQmx_Val_Volts,						// 10348  Volts	
			FromCustomScale = DAQmx_Val_FromCustomScale		// 10065  From Custom Scale
		};

		public enum class ActiveEdge
		{
			Rising = DAQmx_Val_Rising,		// 10280  Rising	
			Falling = DAQmx_Val_Falling		// 10171  Falling
		};
	
		public enum class DIOLineGrouping
		{
			ChanPerLine = DAQmx_Val_ChanPerLine, 		// 10204  One Channel For Each Line
			ChanForAllLines = DAQmx_Val_ChanForAllLines	// 10205  One Channel For All Lines
		};

		public enum class ReadbacklFillMode
		{
			ByChannel = DAQmx_Val_GroupByChannel, 	// 0  Group by Channel
			ByScan = DAQmx_Val_GroupByScanNumber	// 1  Group by Scan Number
		};

		public enum class TaskAction
		{
			Start = DAQmx_Val_Task_Start,			//	0  Start
			Stop = DAQmx_Val_Task_Stop,				//	1  Stop
			Verify = DAQmx_Val_Task_Verify,			//	2  Verify
			Commit = DAQmx_Val_Task_Commit,			//	3  Commit
			Reserve = DAQmx_Val_Task_Reserve,		//	4  Reserve
			Unreserve = DAQmx_Val_Task_Unreserve,	//	5  Unreserve
			Abort = DAQmx_Val_Task_Abort            //	6  Abort                     
		};

		public enum class SamplingMode
		{
			FiniteSamples = DAQmx_Val_FiniteSamps,				// 10178  Finite Samples						
			ContineousSamples = DAQmx_Val_ContSamps,			// 10123  Continuous Samples
			HWTimedSinglePoint = DAQmx_Val_HWTimedSinglePoint   // 12522  Hardware Timed Single Point
		};

		public enum class ExportableSignal
		{
			AIConvertClock = DAQmx_Val_AIConvertClock,				// 12484 Clock that causes an analog - to - digital 
																	// conversion on an E Series or M Series device. 
																	// One conversion corresponds to a single sample from 
																	// one channel.
			RefClock10Mhz = DAQmx_Val_10MHzRefClock,				// 12536 Output of an oscillator that you can use to 
																	// synchronize multiple devices.
			RefClock20Mhz = DAQmx_Val_20MHzTimebaseClock,			// 12486 Output of an oscillator that is the onboard 
																	// source of the Master Timebase.Other timebases are 
																	// derived from this clock.
			SampleClock = DAQmx_Val_SampleClock,					// 12487 Clock the device uses to time each sample.
			AdvanceTrigger = DAQmx_Val_AdvanceTrigger,				// 12488 Trigger that moves a switch to the next 
																	// entry in a scan list.
			ReferenceTrigger = DAQmx_Val_ReferenceTrigger,			// 12490 Trigger that establishes the reference point 
																	// between pretrigger and posttrigger samples.
			StartTrigger = DAQmx_Val_StartTrigger,					// 12491 Trigger that begins a measurement or generation.
			AdvCmpltEvent = DAQmx_Val_AdvCmpltEvent,				// 12492 Signal that a switch product generates after 
																	// it both executes the command(s) in a scan list entry 
																	// and waits for the settling time to elapse.
			AIHoldCmpltEvent = DAQmx_Val_AIHoldCmpltEvent,			// 12493 Signal that an E Series or M Series device 
																	// generates when the device latches analog input 
																	// data(the ADC enters "hold" mode) and it is safe 
																	// for any external switching hardware to remove 
																	// the signal and replace it with the next signal.
																	// This event does not indicate the completion of 
																	// the actual analog - to - digital conversion.
			CounterOutputEven = DAQmx_Val_CounterOutputEvent,		// 12494 Signal that a counter generates.Each time 
																	// the counter reaches terminal count, this signal 
																	// toggles or pulses.
			ChangeDetectionEvent = DAQmx_Val_ChangeDetectionEvent,  // 12511 Signal that a static DIO device generates 
																	// when the device detects a rising or falling edge on 
																	// any of the lines or ports you selected when you 
																	// configured change detection timing.
			WDTExpiredEvent = DAQmx_Val_WDTExpiredEvent				// 12512 Signal that a static DIO device generates 
																	// when the watchdog timer expires.
		};

		public ref class DAQmxCLIWrapper
		{

		public:

			/**
			* @brief Creates a DAQmx task with the specified task name and returns the task handle.
			*
			* This function wraps the NI-DAQmx `DAQmxCreateTask` function. It creates a new task
			* with the provided name and returns a handle to the task, which can be used for further
			* operations. The task name is passed as a .NET `String^`, converted to a C-style string,
			* and passed to the DAQmx function.
			*
			* @param[in] taskName The name of the task to create, passed as a .NET `String^`.
			* @param[out] taskHandle A reference to an `IntPtr` where the handle of the created task will be stored.
			*                        This handle can be used to interact with the task in subsequent DAQmx API calls.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The task name is converted to a C-style string before being passed to the DAQmx API, and
			*       the memory is freed after the task is created.
			*
			* @see DAQmxCreateTask
			*/
			static int CreateTask(String^ taskName,
				[Out] IntPtr% taskHandle);

			/**
			* @brief Checks if an error code indicates a failure.
			*
			* This function checks if the given error code is less than zero, which typically indicates a failure in the context
			* of error handling. The function is used to determine if an operation or function call resulted in an error.
			*
			* @param[in] errorCode The error code to be checked. This is usually a return value from a function indicating success
			*                      or failure.
			*
			* @return `true` if the error code is less than zero, indicating a failure; `false` otherwise.
			*
			* @note This function is a simple inline utility for error handling, commonly used in error checking routines.
			*/
			static inline bool Failed(int errorCode)
			{ return errorCode < 0; };

			/**
			 * @brief Checks if an error code indicates success.
			 *
			 * This function checks if the given error code is greater than or equal to zero, which typically indicates a successful
			 * operation in the context of error handling. The function is used to determine if an operation or function call
			 * completed without errors.
			 *
			 * @param[in] errorCode The error code to be checked. This is usually a return value from a function indicating success
			 *                      or failure.
			 *
			 * @return `true` if the error code is greater than or equal to zero, indicating success; `false` otherwise.
			 *
			 * @note This function is a simple inline utility for error handling, commonly used in error checking routines.
			 */
			static inline bool Success(int errorCode)
			{ return errorCode >= 0; };

			/**
			* @brief Checks if an error code indicates a warning condition.
			*
			* This function checks if the given error code is greater than zero, which typically indicates a warning condition
			* rather than a critical failure. The function is used to identify non-critical issues or warnings that may need
			* attention but do not necessarily prevent the operation from continuing.
			*
			* @param[in] errorCode The error code to be checked. This is usually a return value from a function where positive
			*                      values indicate warnings.
			*
			* @return `true` if the error code is greater than zero, indicating a warning; `false` otherwise.
			*
			* @note This function is a simple inline utility for distinguishing between errors and warnings.
			*/
			static inline bool Warning(int errorCode)
			{ return errorCode > 0; };

			/**
			 * @brief Retrieves the error description string corresponding to a DAQmx error code.
			 *
			 * This function calls the NI-DAQmx `DAQmxGetErrorString` function to obtain a human-readable
			 * description of the specified error code. The error description is returned as a .NET `String^`.
			 *
			 * @param[in] errorCode The error code for which the description is required. This should be
			 *                      a valid DAQmx error code.
			 *
			 * @return A .NET `String^` containing the error description associated with the provided error code.
			 *         If the error code is valid, a descriptive string is returned. If the error code is not valid,
			 *         an appropriate error message is returned.
			 *
			 * @see DAQmxGetErrorString
			 */
			static String^ GetErrorDescription(int errorCode);

			/**
			* @brief Starts the execution of a DAQmx task.
			*
			* This function initiates the execution of the specified DAQmx task by calling the
			* `DAQmxStartTask` function. The task must be properly configured before calling this
			* function to ensure correct operation.
			*
			* @param[in] taskHandle A handle to the DAQmx task to be started. This is passed as an `IntPtr` and cast
			*                       to the NI-DAQmx `TaskHandle`.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note This function assumes that the task handle provided is valid and the task has been correctly
			*       configured. It does not perform any checks on the task's state before starting it.
			*/
			static inline int StartTask(IntPtr taskHandle) {
				return DAQmxStartTask((TaskHandle)taskHandle);
			}

			/**
			* @brief Stops the execution of a DAQmx task.
			*
			* This function halts the execution of the specified DAQmx task by calling the
			* `DAQmxStopTask` function. It is used to stop a running task and should be called
			* when the task is no longer needed or before reconfiguring the task.
			*
			* @param[in] taskHandle A handle to the DAQmx task to be stopped. This is passed as an `IntPtr` and cast
			*                       to the NI-DAQmx `TaskHandle`.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note This function assumes that the task handle provided is valid and that the task is currently
			*       running. It does not perform any checks on the task's state before stopping it.
			*/
			static int StopTask(IntPtr taskHandle) {
				return DAQmxStopTask((TaskHandle)taskHandle);
			}

			/**
			* @brief Clears and releases a DAQmx task.
			*
			* This function wraps the NI-DAQmx `DAQmxClearTask` function to clear and release a specified task.
			* If the task is successfully cleared, the task handle is set to `NULL`. The function returns
			* an error code indicating the success or failure of the operation.
			*
			* @param[in, out] taskHandle A reference to the task handle. On successful execution, this
			*                            handle is set to `NULL`.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The task handle should be valid before calling this function. After successful execution,
			*       the `taskHandle` reference will be set to `NULL`.
			*
			* @see DAQmxClearTask
			*/
			static inline int ClearTask([Out] IntPtr% taskHandle) {

				Int32 r = DAQmxClearTask((TaskHandle)taskHandle);
				if (r == 0) {
					taskHandle = (IntPtr)NULL;
				}
				return r;
			}
			
			/**
			* @brief Disposes of a DAQmx task.
			*
			* This function wraps the `ClearTask` function to dispose of a specified task. It clears the
			* task and sets the task handle to `NULL` if successful. This is useful for releasing resources
			* and ensuring that the task is properly cleaned up.
			*
			* @param[in, out] taskHandle A reference to the handle of the task to be disposed of.
			*                            After successful completion, the handle is set to `NULL`.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note This function is a convenience wrapper around the `ClearTask` function, designed
			*       to simplify task disposal.
			*
			* @see ClearTask
			*/
			static inline int DisposeTask([Out] IntPtr% taskHandle) {
					return ClearTask(taskHandle);
			}

			/**
			* @brief Controls a DAQmx task.
			*
			* This function wraps the NI-DAQmx `DAQmxTaskControl` function to perform various actions on
			* a specified task. The `TaskAction` parameter determines the action to be performed, such as
			* starting, stopping, or clearing the task.
			*
			* @param[in] taskHandle A handle to the task to be controlled.
			* @param[in] action Specifies the action to perform on the task, using the `TaskAction` enum.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `TaskAction` enum should be used to specify the desired action. Common actions include
			*       starting, stopping, and clearing the task.
			*
			* @see DAQmxTaskControl
			* @see TaskAction
			*/
			static inline int TaskControl(IntPtr taskHandle,
				TaskAction action) {
				return DAQmxTaskControl((TaskHandle)taskHandle, (int)action);
			}

			/**
			* @brief Waits until the specified task is complete.
			*
			* This function wraps the `DAQmxWaitUntilTaskDone` function to wait for a specified amount of time
			* until the given task is completed. It is useful for ensuring that a task has finished its operation
			* before proceeding with other actions.
			*
			* @param[in] taskHandle A handle to the task to monitor. This is passed as an `IntPtr` and cast to
			*                       the NI-DAQmx `TaskHandle`.
			* @param[in] timeToWait The amount of time, in seconds, to wait for the task to complete. A value
			*                       of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			*
			* @return
			* - `0` on success, indicating the task has completed within the specified time.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note If the task does not complete within the specified time, the function will return an error code.
			*
			* @see DAQmxWaitUntilTaskDone
			*/
			static inline int WaitUntilTaskDone(IntPtr taskHandle,
				double timeToWait){
					return DAQmxWaitUntilTaskDone((TaskHandle)taskHandle, 
						timeToWait);
			}
		
			/**
			 * @brief Configures the timing for a task's sample clock.
			 *
			 * This function wraps the NI-DAQmx `DAQmxCfgSampClkTiming` function to configure the sample clock timing for a
			 * specified task. It allows setting the clock source, sampling rate, active edge, sampling mode, and the number
			 * of samples per channel.
			 *
			 * @param[in] taskHandle A handle to the task whose sample clock timing is to be configured. This is passed as a `long long`
			 *                       and cast to the NI-DAQmx `TaskHandle`.
			 * @param[in] source The source of the sample clock signal, specified as a string. This is converted to a C-style string
			 *                   for use with the NI-DAQmx API.
			 * @param[in] rate The sampling rate, in samples per second, to be used for the task.
			 * @param[in] activeEdge Specifies the active edge of the sample clock signal, using the `ActiveEdge` enum.
			 * @param[in] sampleMode Specifies the sampling mode, using the `SamplingMode` enum. This determines how samples are
			 *                        collected (e.g., finite or continuous).
			 * @param[in] sampsPerChan The number of samples per channel to acquire or generate.
			 *
			 * @return
			 * - `0` on success.
			 * - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			 *
			 * @note The `source` string is converted to a C-style string and then used with the NI-DAQmx API.
			 *
			 * @see DAQmxCfgSampClkTiming
			 */
			static int ConfigureTiming(long long taskHandle,
				String^ source, double rate, ActiveEdge activeEdge,
				SamplingMode sampleMode, long long  sampsPerChan);

			/**
			* @brief Reads multiple analog samples as 64-bit floating-point numbers from a task.
			*
			* This function wraps the NI-DAQmx `DAQmxReadAnalogF64` function to read multiple analog samples from the
			* specified task. The samples are stored in a .NET managed array of 64-bit floating-point numbers (`array<double>^`).
			* It supports specifying the number of samples per channel, the timeout for the read operation, and the grouping mode.
			* The number of samples actually read per channel is returned through an output parameter.
			*
			* @param[in] taskHandle A handle to the task from which to read analog samples. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] sampsPerChan The number of samples to read per channel.
			* @param[in] timeout The amount of time, in seconds, to wait for the function to read the requested samples.
			*                    A value of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[in] groupMode Specifies whether the data is grouped by channel or interleaved, using the `ReadbacklFillMode` enum.
			* @param[out] data A managed array where the read analog data will be stored, represented as 64-bit floating-point numbers (`double`).
			* @param[out] samplsPerChanRead A reference to an integer that will store the number of samples read per channel.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `data` array is pinned to allow the DAQmx API to access the managed memory directly.
			*
			* @see DAQmxReadAnalogF64
			*/
			static int ReadAnalogF64(IntPtr taskHandle,
				int32 sampsPerChan, double timeout, 
				ReadbacklFillMode groupMode,
				array<double>^ data,
				[Out] int% samplsPerChanRead);

			/**
			* @brief Reads a single analog sample as a 64-bit floating-point number from a task.
			*
			* This function wraps the NI-DAQmx `DAQmxReadAnalogScalarF64` function to read a single analog sample from the
			* specified task. The sample is stored as a 64-bit floating-point number (`double`). It supports specifying the
			* timeout for the read operation. The value read is returned through an output parameter.
			*
			* @param[in] taskHandle A handle to the task from which to read the analog sample. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] timeout The amount of time, in seconds, to wait for the function to read the sample.
			*                    A value of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[out] data A reference to a double where the read analog sample will be stored.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `data` parameter is updated with the value read from the task.
			*
			* @see DAQmxReadAnalogScalarF64
			*/
			static int ReadAnalogScalarF64(IntPtr taskHandle,
								double timeout, [Out] double% data);

			/**
			* @brief Reads multiple binary samples as 16-bit signed integers from a task.
			*
			* This function wraps the NI-DAQmx `DAQmxReadBinaryI16` function to read multiple binary samples from the
			* specified task. The samples are stored in a .NET managed array of 16-bit signed integers (`array<int16>^`).
			* The function allows specifying the number of samples per channel, the timeout for the read operation, and the
			* grouping mode. The number of samples actually read per channel is returned through an output parameter.
			*
			* @param[in] taskHandle A handle to the task from which to read binary samples. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] sampsPerChan The number of samples to read per channel.
			* @param[in] timeout The amount of time, in seconds, to wait for the function to read the requested samples.
			*                    A value of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[in] groupMode Specifies whether the data is grouped by channel or interleaved, using the `ReadbacklFillMode` enum.
			* @param[out] data A managed array where the read binary data will be stored, represented as 16-bit signed integers (`int16`).
			* @param[in] bufferSizeInSamples The size of the `data` array, in samples.
			* @param[out] sampsPerChanRead A reference to an integer that will store the number of samples read per channel.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `data` array is pinned to allow the DAQmx API to access the managed memory directly.
			*
			* @see DAQmxReadBinaryI16
			*/
			static int ReadBinaryI16(IntPtr taskHandle,
				int32 sampsPerChan, double timeout,
				ReadbacklFillMode groupMode, array<int16>^ data,
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			/**
			* @brief Reads multiple binary samples as 16-bit unsigned integers from a task.
			*
			* This function wraps the NI-DAQmx `DAQmxReadBinaryU16` function to read multiple binary samples from the
			* specified task. The samples are stored in a .NET managed array of 16-bit unsigned integers (`array<uInt16>^`).
			* The function allows specifying the number of samples per channel, the timeout for the read operation, and the
			* grouping mode. The number of samples actually read per channel is returned through an output parameter.
			*
			* @param[in] taskHandle A handle to the task from which to read binary samples. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] sampsPerChan The number of samples to read per channel.
			* @param[in] timeout The amount of time, in seconds, to wait for the function to read the requested samples.
			*                    A value of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[in] groupMode Specifies whether the data is grouped by channel or interleaved, using the `ReadbacklFillMode` enum.
			* @param[out] dat A managed array where the read binary data will be stored, represented as 16-bit unsigned integers (`uInt16`).
			* @param[in] bufferSizeInSamples The size of the `dat` array, in samples.
			* @param[out] sampsPerChanRead A reference to an integer that will store the number of samples read per channel.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `dat` array is pinned to allow the DAQmx API to access the managed memory directly.
			*
			* @see DAQmxReadBinaryU16
			*/
			static int ReadBinaryUI16(IntPtr taskHandle,
				int32 sampsPerChan, double timeout,
				ReadbacklFillMode groupMode, array<uInt16>^ data,
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			/**
			* @brief Reads multiple binary samples as 32-bit signed integers from a task.
			*
			* This function wraps the NI-DAQmx `DAQmxReadBinaryI32` function to read multiple binary samples from the
			* specified task. The samples are stored in a .NET managed array of 32-bit signed integers (`array<int32>^`).
			* The function allows specifying the number of samples per channel, the timeout for the read operation, and the
			* grouping mode. The number of samples actually read per channel is returned through an output parameter.
			*
			* @param[in] taskHandle A handle to the task from which to read binary samples. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] sampsPerChan The number of samples to read per channel.
			* @param[in] timeout The amount of time, in seconds, to wait for the function to read the requested samples.
			*                    A value of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[in] groupMode Specifies whether the data is grouped by channel or interleaved, using the `ReadbacklFillMode` enum.
			* @param[out] dat A managed array where the read binary data will be stored, represented as 32-bit signed integers (`int32`).
			* @param[in] bufferSizeInSamples The size of the `dat` array, in samples.
			* @param[out] sampsPerChanRead A reference to an integer that will store the number of samples read per channel.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `dat` array is pinned to allow the DAQmx API to access the managed memory directly.
			*
			* @see DAQmxReadBinaryI32
			*/
			static int ReadBinaryI32(IntPtr taskHandle,
				int32 sampsPerChan, double timeout,
				ReadbacklFillMode groupMode, array<int32>^ data,
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			/**
			* @brief Reads multiple binary samples as 32-bit unsigned integers from a task.
			*
			* This function wraps the NI-DAQmx `DAQmxReadBinaryU32` function to read multiple binary samples from the
			* specified task. The samples are stored in a .NET managed array of 32-bit unsigned integers (`array<uInt32>^`).
			* The function allows specifying the number of samples per channel, the timeout for the read operation, and the
			* grouping mode. The number of samples actually read per channel is returned through an output parameter.
			*
			* @param[in] taskHandle A handle to the task from which to read binary samples. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] sampsPerChan The number of samples to read per channel.
			* @param[in] timeout The amount of time, in seconds, to wait for the function to read the requested samples.
			*                    A value of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[in] groupMode Specifies whether the data is grouped by channel or interleaved, using the `ReadbacklFillMode` enum.
			* @param[out] dat A managed array where the read binary data will be stored, represented as 32-bit unsigned integers (`uInt32`).
			* @param[in] bufferSizeInSamples The size of the `dat` array, in samples.
			* @param[out] sampsPerChanRead A reference to an integer that will store the number of samples read per channel.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `dat` array is pinned to allow the DAQmx API to access the managed memory directly.
			*
			* @see DAQmxReadBinaryU32
			*/
			static int ReadBinaryUI32(IntPtr taskHandle,
				int32 sampsPerChan, double timeout,
				ReadbacklFillMode groupMode, array<uInt32>^ dat,
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			/**
			* @brief Creates an analog input voltage channel in the specified task.
			*
			* This function wraps the NI-DAQmx `DAQmxCreateAIVoltageChan` function to create an analog input voltage
			* channel in a task. It accepts various parameters for configuring the channel, including the physical channel,
			* voltage range, and measurement units. The task handle is passed as an `IntPtr`, and the function internally
			* converts .NET `String^` parameters to C-style strings.
			*
			* @param[in] taskHandle A handle to the task to which the analog input voltage channel is added. This is passed
			*                       as an `IntPtr` and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] physicalChannel The name of the physical channel (e.g., "Dev1/ai0") to create the voltage channel on.
			*                            This is a .NET `String^`.
			* @param[in] nameToAssignToChannel The name to assign to the created channel. If `nullptr`, a default name will be used.
			*                                  This is a .NET `String^`.
			* @param[in] terminalConfig The input terminal configuration (e.g., differential, single-ended) for the channel.
			*                           This is specified as an `AiTermination` enum.
			* @param[in] minVal The minimum value, in volts, expected for the input signal.
			* @param[in] maxVal The maximum value, in volts, expected for the input signal.
			* @param[in] units The units for the voltage values, specified as a `VoltageUnits` enum (e.g., Volts, millivolts).
			* @param[in] customScaleName The name of a custom scale to apply to the input values, or `nullptr` if no custom scale is used.
			*                            This is a .NET `String^`.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The string parameters are converted to C-style strings and freed after the channel is created.
			*
			* @see DAQmxCreateAIVoltageChan
			*/
			static int CreateAIVoltageChannel(IntPtr taskHandle,
				String^ physicalChannel, String^ nameToAssignToChannel, 
				AiTermination terminalConfig, double minVal, 
				double maxVal, VoltageUnits units,
				String^ customScaleName);

			/**
			* @brief Creates an analog output voltage channel in the specified task.
			*
			* This function wraps the NI-DAQmx `DAQmxCreateAOVoltageChan` function to create an analog output voltage
			* channel in a task. It allows configuring the channel with parameters such as the physical channel, voltage
			* range, and measurement units. The task handle is passed as an `IntPtr`, and the function converts .NET
			* `String^` parameters to C-style strings for DAQmx API usage.
			*
			* @param[in] taskHandle A handle to the task to which the analog output voltage channel is added. This is passed
			*                       as an `IntPtr` and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] physicalChannel The name of the physical channel (e.g., "Dev1/ao0") to create the voltage channel on.
			*                            This is a .NET `String^`.
			* @param[in] nameToAssignToChannel The name to assign to the created channel. If `nullptr`, a default name will be used.
			*                                  This is a .NET `String^`.
			* @param[in] minVal The minimum value, in volts, expected for the output signal.
			* @param[in] maxVal The maximum value, in volts, expected for the output signal.
			* @param[in] units The units for the voltage values, specified as an integer representing a DAQmx constant.
			*                  Typically, this should be `DAQmx_Val_Volts`.
			* @param[in] customScaleName The name of a custom scale to apply to the output values, or `nullptr` if no custom scale is used.
			*                            This is a .NET `String^`.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The string parameters are converted to C-style strings and freed after the channel is created.
			*
			* @see DAQmxCreateAOVoltageChan
			*/
			static int CreateAOVoltageChannel(IntPtr taskHandle,
				String^ physicalChannel, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, 
				String^ customScaleName);

			/**
			* @brief Creates a digital input channel in the specified task.
			*
			* This function wraps the NI-DAQmx `DAQmxCreateDIChan` function to create one or more digital input
			* lines or ports in a task. It allows configuring the lines or ports, specifying their names, and defining
			* how the lines are grouped. The task handle is passed as an `IntPtr`, and .NET `String^` parameters
			* are converted to C-style strings for use in the DAQmx API.
			*
			* @param[in] taskHandle A handle to the task to which the digital input lines or ports are added. This is passed
			*                       as an `IntPtr` and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] lines The physical lines or ports (e.g., "Dev1/port0") to be added to the task. This is a .NET `String^`.
			* @param[in] nameToAssignToLines The name to assign to the created lines or ports. If `nullptr`, a default name will be used.
			*                                This is a .NET `String^`.
			* @param[in] lineGrouping Specifies how the digital lines are grouped, using the `DIOLineGrouping` enum
			*                         (e.g., `DAQmx_Val_ChanForAllLines` for one channel for all lines, or `DAQmx_Val_ChanPerLine` for one channel per line).
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The string parameters are converted to C-style strings and freed after the channel is created.
			*
			* @see DAQmxCreateDIChan
			*/
			static int CreateDIChannel(IntPtr taskHandle, String^ lines,
				String^ nameToAssignToLines, DIOLineGrouping lineGrouping);

			/**
			* @brief Creates a digital output channel in the specified task.
			*
			* This function wraps the NI-DAQmx `DAQmxCreateDOChan` function to create one or more digital output
			* lines or ports in a task. It allows configuring the lines or ports, specifying their names, and defining
			* how the lines are grouped. The task handle is passed as an `IntPtr`, and the .NET `String^` parameters
			* are converted to C-style strings for use in the DAQmx API.
			*
			* @param[in] taskHandle A handle to the task to which the digital output lines or ports are added. This is passed
			*                       as an `IntPtr` and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] lines The physical lines or ports (e.g., "Dev1/port0") to be added to the task. This is a .NET `String^`.
			* @param[in] nameToAssignToLines The name to assign to the created lines or ports. If `nullptr`, a default name will be used.
			*                                This is a .NET `String^`.
			* @param[in] lineGrouping Specifies how the digital lines are grouped, using the `DIOLineGrouping` enum
			*                         (e.g., `DAQmx_Val_ChanForAllLines` for one channel for all lines, or `DAQmx_Val_ChanPerLine` for one channel per line).
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The string parameters are converted to C-style strings and freed after the channel is created.
			*
			* @see DAQmxCreateDOChan
			*/
			static int CreateDOChannel(IntPtr taskHandle, String^ lines,
				String^ nameToAssignToLines, DIOLineGrouping lineGrouping);

			/**
			* @brief Creates a counter output pulse frequency channel.
			*
			* This function wraps the NI-DAQmx `DAQmxCreateCOPulseChanFreq` function to create a counter output channel
			* configured for generating pulse frequency signals. The function sets up the channel with the specified
			* counter, name, units, idle state, initial delay, frequency, and duty cycle.
			*
			* @param[in] taskHandle A handle to the task in which to create the counter output channel. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] counter The name of the counter to be used for generating the pulse frequency signal.
			* @param[in] nameToAssignToChannel The name to assign to the channel.
			* @param[in] units The units for the frequency measurement, represented as an integer.
			* @param[in] idleState The idle state of the output signal when not actively pulsing, represented as an integer.
			* @param[in] initialDelay The initial delay before the output signal starts, in seconds.
			* @param[in] freq The frequency of the pulse signal, in Hz.
			* @param[in] dutyCycle The duty cycle of the pulse signal, expressed as a ratio between 0 and 1.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `counter` and `nameToAssignToChannel` parameters are converted from managed `String^` to C-style strings
			*       using the `ConvertToCString` function. Memory for these strings is freed after the DAQmx function call.
			*
			* @see DAQmxCreateCOPulseChanFreq
			*/
			static int CreateCOPulseFrequencyChannel(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				int units, int idleState, double initialDelay, double freq, 
				double dutyCycle);

			/**
			* @brief Creates a counter output pulse channel with time-based parameters.
			*
			* This function wraps the NI-DAQmx `DAQmxCreateCOPulseChanTime` function to create a counter output channel
			* configured for generating pulse signals with specified time-based parameters. The function sets up the
			* channel with the specified counter, name, units, idle state, initial delay, low time, and high time.
			*
			* @param[in] taskHandle A handle to the task in which to create the counter output channel. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] counter The name of the counter to be used for generating the pulse signal.
			* @param[in] nameToAssignToChannel The name to assign to the channel.
			* @param[in] units The units for the pulse timing, represented as an integer.
			* @param[in] idleState The idle state of the output signal when not actively pulsing, represented as an integer.
			* @param[in] initialDelay The initial delay before the output signal starts, in seconds.
			* @param[in] lowTime The duration of the low state of the pulse signal, in seconds.
			* @param[in] highTime The duration of the high state of the pulse signal, in seconds.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `counter` and `nameToAssignToChannel` parameters are converted from managed `String^` to C-style strings
			*       using the `ConvertToCString` function. Memory for these strings is freed after the DAQmx function call.
			*
			* @see DAQmxCreateCOPulseChanTime
			*/
			static int CreateCOPulseChanTime(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				int units, int idleState, 
				double initialDelay, double lowTime, double highTime);
			
			/**
			* @brief Creates a counter input channel for counting edges.
			*
			* This function wraps the NI-DAQmx `DAQmxCreateCICountEdgesChan` function to create a counter input channel
			* configured for counting edges. The function sets up the channel with the specified counter, name, edge type,
			* initial count, and count direction.
			*
			* @param[in] taskHandle A handle to the task in which to create the counter input channel. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] counter The name of the counter to be used for edge counting.
			* @param[in] nameToAssignToChannel The name to assign to the channel.
			* @param[in] edge Specifies the edge to count, represented as an integer. Typically, this is `DAQmx_Val_Rising` or
			*                 `DAQmx_Val_Falling`.
			* @param[in] initialCount The initial count value to be set for the channel.
			* @param[in] countDirection The direction of counting, represented as an integer. This usually indicates whether
			*                           to count up or down.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `counter` and `nameToAssignToChannel` parameters are converted from managed `String^` to C-style strings
			*       using the `ConvertToCString` function. Memory for these strings is freed after the DAQmx function call.
			*
			* @see DAQmxCreateCICountEdgesChan
			*/
			static int CreateCICountEdgesChan(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				int edge, int initialCount, int countDirection);

			/**
			 * @brief Creates a counter input channel for measuring frequency.
			 *
			 * This function wraps the NI-DAQmx `DAQmxCreateCIFreqChan` function to set up a counter input channel
			 * for frequency measurements. The function configures the channel with the specified counter, name,
			 * frequency range, units, edge type, measurement method, measurement time, divisor, and custom scale name.
			 *
			 * @param[in] taskHandle A handle to the task in which to create the counter input channel. This is passed as an `IntPtr`
			 *                       and cast to the NI-DAQmx `TaskHandle`.
			 * @param[in] counter The name of the counter to be used for frequency measurement.
			 * @param[in] nameToAssignToChannel The name to assign to the channel.
			 * @param[in] minVal The minimum frequency value to be measured, in Hertz.
			 * @param[in] maxVal The maximum frequency value to be measured, in Hertz.
			 * @param[in] units The units for the frequency measurement, represented as an integer.
			 * @param[in] edge Specifies the edge to count, represented as an integer. This typically indicates whether to count on
			 *                 the rising or falling edge.
			 * @param[in] measMethod The method used for frequency measurement, represented as an integer.
			 * @param[in] measTime The measurement time for frequency, in seconds.
			 * @param[in] divisor The divisor used in the frequency measurement.
			 * @param[in] customScaleName The name of the custom scale to apply to the measurement, or an empty string if no custom
			 *                            scale is used.
			 *
			 * @return
			 * - `0` on success.
			 * - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			 *
			 * @note The `counter`, `nameToAssignToChannel`, and `customScaleName` parameters are converted from managed `String^`
			 *       to C-style strings using the `ConvertToCString` function. Memory for these strings is freed after the DAQmx
			 *       function call.
			 *
			 * @see DAQmxCreateCIFreqChan
			 */
			static int CreateCIFreqChan(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, int edge, 
				int measMethod, double measTime, UInt32 divisor,  
				String^ customScaleName);

			/**
		* @brief Creates a counter input channel for measuring period.
		*
		* This function wraps the NI-DAQmx `DAQmxCreateCIPeriodChan` function to configure a counter input channel
		* for period measurement. It sets up the channel with the specified counter, name, period range, units, edge type,
		* measurement method, measurement time, divisor, and custom scale name.
		*
		* @param[in] taskHandle A handle to the task in which to create the counter input channel. This is passed as an `IntPtr`
		*                       and cast to the NI-DAQmx `TaskHandle`.
		* @param[in] counter The name of the counter to be used for period measurement.
		* @param[in] nameToAssignToChannel The name to assign to the channel.
		* @param[in] minVal The minimum period value to be measured, in seconds.
		* @param[in] maxVal The maximum period value to be measured, in seconds.
		* @param[in] units The units for the period measurement, represented as an integer.
		* @param[in] edge Specifies the edge to count, represented as an integer. This typically indicates whether to count on
		*                 the rising or falling edge.
		* @param[in] measMethod The method used for period measurement, represented as an integer.
		* @param[in] measTime The measurement time for period, in seconds.
		* @param[in] divisor The divisor used in the period measurement.
		* @param[in] customScaleName The name of the custom scale to apply to the measurement, or an empty string if no custom
		*                            scale is used.
		*
		* @return
		* - `0` on success.
		* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
		*
		* @note The `counter`, `nameToAssignToChannel`, and `customScaleName` parameters are converted from managed `String^`
		*       to C-style strings using the `ConvertToCString` function. Memory for these strings is freed after the DAQmx
		*       function call.
		*
		* @see DAQmxCreateCIPeriodChan
		*/
			static int CreateCIPeriodChan(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, int edge, 
				int measMethod, double measTime, UInt32 divisor, 
				String^ customScaleName);

			/**
			* @brief Creates a counter input channel for measuring semi-period.
			*
			* This function wraps the NI-DAQmx `DAQmxCreateCISemiPeriodChan` function to configure a counter input channel
			* for semi-period measurement. It sets up the channel with the specified counter, name, semi-period range, units,
			* and custom scale name.
			*
			* @param[in] taskHandle A handle to the task in which to create the counter input channel. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] counter The name of the counter to be used for semi-period measurement.
			* @param[in] nameToAssignToChannel The name to assign to the channel.
			* @param[in] minVal The minimum semi-period value to be measured, in seconds.
			* @param[in] maxVal The maximum semi-period value to be measured, in seconds.
			* @param[in] units The units for the semi-period measurement, represented as an integer.
			* @param[in] customScaleName The name of the custom scale to apply to the measurement, or an empty string if no custom
			*                            scale is used.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `counter`, `nameToAssignToChannel`, and `customScaleName` parameters are converted from managed `String^`
			*       to C-style strings using the `ConvertToCString` function. Memory for these strings is freed after the DAQmx
			*       function call.
			*
			* @see DAQmxCreateCISemiPeriodChan
			*/
			static int CreateCISemiPeriodChan(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, 
				String^ customScaleName);

			/**
			* @brief Loads a task from the specified task name.
			*
			* This function wraps the NI-DAQmx `DAQmxLoadTask` function to load a task by its name. It assigns the loaded task
			* handle to the provided `taskHandle` parameter.
			*
			* @param[in] taskHandle A handle to the task to be loaded. This is passed as an `IntPtr` and cast to the NI-DAQmx
			*                       `TaskHandle`. This parameter is updated with the handle of the loaded task.
			* @param[in] taskName The name of the task to be loaded.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `taskName` parameter is converted from a managed `String^` to a C-style string using the `ConvertToCString`
			*       function. Memory for this string is freed after the DAQmx function call.
			*
			* @see DAQmxLoadTask
			*/
			static int LoadTask(IntPtr taskHandle, String^ taskName);
			
			/**
			* @brief Adds global channels to a task.
			*
			* This function wraps the NI-DAQmx `DAQmxAddGlobalChansToTask` function to add global channels to the specified task.
			* The global channels are identified by their names, which are provided as a comma-separated list.
			*
			* @param[in] taskHandle A handle to the task to which the global channels will be added. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] channelNames A comma-separated list of global channel names to be added to the task.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `channelNames` parameter is converted from a managed `String^` to a C-style string using the `ConvertToCString`
			*       function. Memory for this string is freed after the DAQmx function call.
			*
			* @see DAQmxAddGlobalChansToTask
			*/
			static int AddGlobalChansToTask(IntPtr taskHandle, 
				String^ channelNames);
			
			/**
			* @brief Checks if a task is complete.
			*
			* This function wraps the NI-DAQmx `DAQmxIsTaskDone` function to determine whether the specified task has completed its
			* execution. The result is provided via an output parameter.
			*
			* @param[in] taskHandle A handle to the task whose completion status is to be checked. This is passed as an `IntPtr` and
			*                       cast to the NI-DAQmx `TaskHandle`.
			* @param[out] isTaskDone A reference to a boolean that will be set to `true` if the task is complete, and `false` otherwise.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `isTaskDone` parameter is updated with the completion status of the task. The status is represented as a `bool`,
			*       which is converted from the `bool32` value returned by the DAQmx function.
			*
			* @see DAQmxIsTaskDone
			*/
			static int IsTaskDone(IntPtr taskHandle, [Out] bool% isTaskDone);
			
			/**
			* @brief Retrieves the name of the nth channel in a task.
			*
			* This function wraps the NI-DAQmx `DAQmxGetNthTaskChannel` function to obtain the name of the nth channel in the specified
			* task. The channel name is returned as a managed .NET `String^`.
			*
			* @param[in] taskHandle A handle to the task from which the channel name will be retrieved. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] index The zero-based index of the channel whose name is to be retrieved.
			* @param[out] buffer A reference to a `String^` that will be set to the name of the nth channel.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `buffer` parameter is updated with the channel name as a managed `String^`. The name is initially retrieved
			*       as a C-style string (`char` array) and then converted to a .NET `String^`.
			*
			* @see DAQmxGetNthTaskChannel
			*/
			static int GetNthTaskChannel(IntPtr taskHandle, uInt32 index, 
				[Out] String^% buffer);
			
			/**
			* @brief Retrieves the name of the nth device associated with a task.
			*
			* This function wraps the NI-DAQmx `DAQmxGetNthTaskDevice` function to obtain the name of the nth device associated with
			* the specified task. The device name is returned as a managed .NET `String^`.
			*
			* @param[in] taskHandle A handle to the task from which the device name will be retrieved. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] index The zero-based index of the device whose name is to be retrieved.
			* @param[out] buffer A reference to a `String^` that will be set to the name of the nth device.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `buffer` parameter is updated with the device name as a managed `String^`. The name is initially retrieved
			*       as a C-style string (`char` array) and then converted to a .NET `String^`.
			*
			* @see DAQmxGetNthTaskDevice
			*/
			static int GetNthTaskDevice(IntPtr taskHandle, uInt32 index, 
				[Out] String^% buffer);

			/**
			* @brief Reads digital lines from a task into the specified buffer.
			*
			* This function wraps the NI-DAQmx `DAQmxReadDigitalLines` function to read digital signals from the specified task
			* into a managed .NET array. It supports reading multiple samples per channel and handles data interleaving modes.
			* The function returns the number of samples read per channel and the number of bytes per sample via output parameters.
			*
			* @param[in] taskHandle A handle to the task from which to read digital lines. This is passed as an `IntPtr` and
			*                       cast to the NI-DAQmx `TaskHandle`.
			* @param[in] numSamplesPerChan The number of samples to read per channel.
			* @param[in] timeout The amount of time, in seconds, to wait for the function to read the requested samples.
			*                    A value of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[in] interleaveMode Specifies whether the data is interleaved or grouped by channel, using the `ReadbacklFillMode` enum.
			* @param[out] data A managed array of bytes where the read digital data will be stored.
			* @param[in] bufferSize The size of the data buffer, in bytes.
			* @param[out] sampsPerChanRead A reference to an integer that will store the number of samples read per channel.
			* @param[out] bytesPerSample A reference to an integer that will store the number of bytes per sample.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `data` parameter is passed as a pinned pointer to allow the DAQmx API to access the managed memory directly.
			*
			* @see DAQmxReadDigitalLines
			*/
			static int ReadDigitalLines(IntPtr taskHandle,
								uInt32 numSampsPerChan, double timeout,
									ReadbacklFillMode interleaveMode,
									array<Byte>^ data,
									uInt32 bufferSize,
									[Out] int% sampsPerChanRead,
									[Out] int% bytesPerSample);

			/**
			* @brief Reads a single sample from a digital input channel as an unsigned 32-bit integer.
			*
			* This function wraps the NI-DAQmx `DAQmxReadDigitalScalarU32` function to read a single sample from a digital
			* input channel. The sample is returned as a 32-bit unsigned integer (`UInt32`), with a timeout for the read operation.
			*
			* @param[in] taskHandle A handle to the task from which to read the digital sample. This is passed as an `IntPtr` and
			*                       cast to the NI-DAQmx `TaskHandle`.
			* @param[in] timeout The amount of time, in seconds, to wait for the function to read the requested sample.
			*                    A value of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[out] data A reference to a `UInt32` variable where the read digital sample will be stored.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The digital sample is read as a single 32-bit unsigned integer.
			*
			* @see DAQmxReadDigitalScalarU32
			*/
			static int ReadDigitalScalarU32(IntPtr taskHandle,
								double timeout, [Out] UInt32% data);

			/**
			* @brief Reads multiple digital samples as unsigned 32-bit integers from a task.
			*
			* This function wraps the NI-DAQmx `DAQmxReadDigitalU32` function to read multiple digital samples from the
			* specified task. The samples are stored in a managed .NET array as 32-bit unsigned integers (`uInt32`).
			* The number of samples per channel and the interleaving mode can be specified. The number of samples
			* actually read is returned via an output parameter.
			*
			* @param[in] taskHandle A handle to the task from which to read digital samples. This is passed as an `IntPtr` and
			*                       cast to the NI-DAQmx `TaskHandle`.
			* @param[in] samplesPerChannel The number of samples to read per channel.
			* @param[in] timeout The amount of time, in seconds, to wait for the function to read the requested samples.
			*                    A value of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[in] interleaveMode Specifies whether the data is interleaved or grouped by channel, using the `ReadbacklFillMode` enum.
			* @param[out] data A managed array where the read digital data will be stored, represented as `uInt32`.
			* @param[in] arraySize The size of the `data` array, in elements.
			* @param[out] sampsPerChanRead A reference to an integer that will store the number of samples read per channel.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `data` array is pinned to allow the DAQmx API to access the managed memory directly.
			*
			* @see DAQmxReadDigitalU32
			*/
			static int ReadDigitU32(IntPtr taskHandle,
				int samplesPerChannel, double timeout, 
				ReadbacklFillMode interleaveMode,
				array<uInt32> ^data, uInt32 arraySize,
				[Out] int% sampsPerChanRead);

			/**
			 * @brief Reads multiple digital samples as unsigned 16-bit integers from a task.
			 *
			 * This function wraps the NI-DAQmx `DAQmxReadDigitalU16` function to read multiple digital samples from
			 * the specified task. The samples are stored in a managed .NET array as 16-bit unsigned integers (`uInt16`).
			 * The number of samples per channel and the interleaving mode can be specified. The number of samples
			 * actually read is returned via an output parameter.
			 *
			 * @param[in] taskHandle A handle to the task from which to read digital samples. This is passed as an
			 *                       `IntPtr` and cast to the NI-DAQmx `TaskHandle`.
			 * @param[in] samplesPerChannel The number of samples to read per channel.
			 * @param[in] timeout The amount of time, in seconds, to wait for the function to read the requested samples.
			 *                    A value of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			 * @param[in] interleaveMode Specifies whether the data is interleaved or grouped by channel, using the
			 *                           `ReadbacklFillMode` enum.
			 * @param[out] data A managed array where the read digital data will be stored, represented as `uInt16`.
			 * @param[in] arraySize The size of the `data` array, in elements.
			 * @param[out] sampsPerChanRead A reference to an integer that will store the number of samples read per channel.
			 *
			 * @return
			 * - `0` on success.
			 * - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			 *
			 * @note The `data` array is pinned to allow the DAQmx API to access the managed memory directly.
			 *
			 * @see DAQmxReadDigitalU16
			 */
			static int ReadDigitU16(IntPtr taskHandle,
				int samplesPerChannel, double timeout,
				ReadbacklFillMode interleaveMode,
				array<uInt16>^ data, uInt32 arraySize,
				[Out] int% sampsPerChanRead);

			/**
			* @brief Reads multiple digital samples as unsigned 8-bit integers from a task.
			*
			* This function wraps the NI-DAQmx `DAQmxReadDigitalU8` function to read multiple digital samples from
			* the specified task. The samples are stored in a managed .NET array as 8-bit unsigned integers (`uInt8`).
			* The number of samples per channel and the interleaving mode can be specified. The number of samples
			* actually read is returned via an output parameter.
			*
			* @param[in] taskHandle A handle to the task from which to read digital samples. This is passed as an
			*                       `IntPtr` and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] samplesPerChannel The number of samples to read per channel.
			* @param[in] timeout The amount of time, in seconds, to wait for the function to read the requested samples.
			*                    A value of `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[in] interleaveMode Specifies whether the data is interleaved or grouped by channel, using the
			*                           `ReadbacklFillMode` enum.
			* @param[out] data A managed array where the read digital data will be stored, represented as `uInt8`.
			* @param[in] arraySize The size of the `data` array, in elements.
			* @param[out] sampsPerChanRead A reference to an integer that will store the number of samples read per channel.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `data` array is pinned to allow the DAQmx API to access the managed memory directly.
			*
			* @see DAQmxReadDigitalU8
			*/
			static int ReadDigitU8(IntPtr taskHandle,
				int samplesPerChannel, double timeout,
				ReadbacklFillMode interleaveMode,
				array<uInt8>^ data, uInt32 arraySize,
				[Out] int% sampsPerChanRead);

			/**
			* @brief Writes multiple digital samples to a task.
			*
			* This function wraps the NI-DAQmx `DAQmxWriteDigitalLines` function to write multiple digital samples to a specified task.
			* The samples are taken from a .NET managed array of bytes (`array<Byte>^`). The number of samples to write and the
			* interleaving mode can be specified. The function also supports auto-starting the task and provides the number of
			* samples successfully written via an output parameter.
			*
			* @param[in] taskHandle A handle to the task to which digital samples will be written. This is passed as an `IntPtr` and
			*                       cast to the NI-DAQmx `TaskHandle`.
			* @param[in] numSampsPerChan The number of samples to write per channel.
			* @param[in] autoStart A boolean indicating whether to automatically start the task after writing the samples.
			*                      `true` to start automatically, `false` otherwise.
			* @param[in] timeout The time, in seconds, to wait for the function to complete the write operation. A value of
			*                    `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[in] interleaveMode Specifies whether the data is interleaved or organized by channel, using the `ReadbacklFillMode` enum.
			* @param[in] data A managed .NET array where the digital data to be written is stored, represented as bytes (`Byte`).
			* @param[out] sampsPerChanWritten A reference to an integer that will store the number of samples written per channel.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `data` array is pinned to allow the DAQmx API to access the managed memory directly.
			*
			* @see DAQmxWriteDigitalLines
			*/
			static int WriteDigitalLines(IntPtr taskHandle,
				int32 numSampsPerChan, bool autoStart, double timeout,
				ReadbacklFillMode interleaveMode,
				array<Byte>^ data,
				[Out] int% sampsPerChanWritten);

			/**
			* @brief Writes a single digital sample as an unsigned 32-bit integer to a task.
			*
			* This function wraps the NI-DAQmx `DAQmxWriteDigitalScalarU32` function to write a single digital sample
			* to a specified task. The sample is provided as a 32-bit unsigned integer (`uInt32`). The function allows
			* for automatic task starting and specifies a timeout for the write operation.
			*
			* @param[in] taskHandle A handle to the task to which the digital sample will be written. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] autostart A boolean indicating whether to automatically start the task after writing the sample.
			*                      `true` to start automatically, `false` otherwise.
			* @param[in] timeout The time, in seconds, to wait for the function to complete the write operation. A value of
			*                    `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[in] data The digital sample to be written, represented as a 32-bit unsigned integer (`uInt32`).
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @see DAQmxWriteDigitalScalarU32
			*/
			static int WriteDigitalScalarU32(IntPtr taskHandle,
				bool autostart, double timeout, uInt32 data);

			/**
			* @brief Writes multiple digital samples as 8-bit unsigned integers to a task.
			*
			* This function wraps the NI-DAQmx `DAQmxWriteDigitalU8` function to write multiple digital samples to the
			* specified task. The samples are provided in a .NET managed array of 8-bit unsigned integers (`array<uInt8>^`).
			* The function allows specifying the number of samples per channel, automatic task starting, and a timeout for
			* the write operation. The number of samples successfully written per channel is returned through an output parameter.
			*
			* @param[in] taskHandle A handle to the task to which digital samples will be written. This is passed as an `IntPtr`
			*                       and cast to the NI-DAQmx `TaskHandle`.
			* @param[in] numSampsPerChan The number of samples to write per channel.
			* @param[in] autoStart A boolean indicating whether to automatically start the task after writing the samples.
			*                      `true` to start automatically, `false` otherwise.
			* @param[in] timeout The time, in seconds, to wait for the function to complete the write operation. A value of
			*                    `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			* @param[in] interleaveMode Specifies whether the data is interleaved or organized by channel, using the `ReadbacklFillMode` enum.
			* @param[in] data A managed .NET array containing the digital data to be written, represented as 8-bit unsigned integers (`uInt8`).
			* @param[out] samplesPerChannelWritten A reference to an integer that will store the number of samples written per channel.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `data` array is pinned to ensure that the DAQmx API can access the managed memory directly.
			*
			* @see DAQmxWriteDigitalU8
			*/
			static int WriteDigitalU8(IntPtr taskHandle, int32 numSampsPerChan,
				bool autoStart, double timeout, 
				ReadbacklFillMode interleaveMode,
				array<uInt8>^ data, [Out] int% samplesPerChannelWritten);

			/**
			 * @brief Writes multiple digital samples as 16-bit unsigned integers to a task.
			 *
			 * This function wraps the NI-DAQmx `DAQmxWriteDigitalU16` function to write multiple digital samples to the
			 * specified task. The samples are provided in a .NET managed array of 16-bit unsigned integers (`array<uInt16>^`).
			 * It supports specifying the number of samples per channel, automatic task starting, and the timeout for the operation.
			 * The number of samples successfully written per channel is returned through an output parameter.
			 *
			 * @param[in] taskHandle A handle to the task to which digital samples will be written. This is passed as an `IntPtr`
			 *                       and cast to the NI-DAQmx `TaskHandle`.
			 * @param[in] numSampsPerChan The number of samples to write per channel.
			 * @param[in] autoStart A boolean indicating whether to automatically start the task after writing the samples.
			 *                      `true` to start automatically, `false` otherwise.
			 * @param[in] timeout The time, in seconds, to wait for the function to complete the write operation. A value of
			 *                    `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			 * @param[in] interleaveMode Specifies whether the data is interleaved or organized by channel, using the `ReadbacklFillMode` enum.
			 * @param[in] data A managed .NET array containing the digital data to be written, represented as 16-bit unsigned integers (`uInt16`).
			 * @param[out] samplesPerChannelWritten A reference to an integer that will store the number of samples written per channel.
			 *
			 * @return
			 * - `0` on success.
			 * - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			 *
			 * @note The `data` array is pinned to ensure that the DAQmx API can access the managed memory directly.
			 *
			 * @see DAQmxWriteDigitalU16
			 */
			static int WriteDigitalU16(IntPtr taskHandle, int32 numSampsPerChan,
				bool autoStart, double timeout, 
				ReadbacklFillMode interleaveMode,
				array<uInt16>^ data, [Out] int% samplesPerChannelWritten);

			/**
			 * @brief Writes multiple digital samples as 32-bit unsigned integers to a task.
			 *
			 * This function wraps the NI-DAQmx `DAQmxWriteDigitalU32` function to write multiple digital samples to the
			 * specified task. The samples are provided in a .NET managed array of 32-bit unsigned integers (`array<uInt32>^`).
			 * It supports specifying the number of samples per channel, automatic task starting, and the timeout for the operation.
			 * The number of samples successfully written per channel is returned through an output parameter.
			 *
			 * @param[in] taskHandle A handle to the task to which digital samples will be written. This is passed as an `IntPtr`
			 *                       and cast to the NI-DAQmx `TaskHandle`.
			 * @param[in] numSampsPerChan The number of samples to write per channel.
			 * @param[in] autoStart A boolean indicating whether to automatically start the task after writing the samples.
			 *                      `true` to start automatically, `false` otherwise.
			 * @param[in] timeout The time, in seconds, to wait for the function to complete the write operation. A value of
			 *                    `DAQmx_Val_WaitInfinitely` can be used to wait indefinitely.
			 * @param[in] interleaveMode Specifies whether the data is interleaved or organized by channel, using the `ReadbacklFillMode` enum.
			 * @param[in] data A managed .NET array containing the digital data to be written, represented as 32-bit unsigned integers (`uInt32`).
			 * @param[out] samplesPerChannelWritten A reference to an integer that will store the number of samples written per channel.
			 *
			 * @return
			 * - `0` on success.
			 * - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			 *
			 * @note The `data` array is pinned to ensure that the DAQmx API can access the managed memory directly.
			 *
			 * @see DAQmxWriteDigitalU32
			 */
			static int WriteDigitalU32(IntPtr taskHandle, int32 numSampsPerChan,
				bool autostart, double timeout, 
				ReadbacklFillMode interleaveMode, 
				array<uInt32>^ data, [Out] int% samplesPerChannelWritten);

			/**
			* @brief Configures the task to export a specific signal to an external terminal.
			*
			* This function wraps the NI-DAQmx `DAQmxExportSignal` function to configure the task to export a specified signal to
			* an external terminal. The signal to be exported and the output terminal are specified as parameters.
			*
			* @param[in] taskHandle A handle to the task from which the signal will be exported. This is passed as an `IntPtr` and
			*                       cast to the NI-DAQmx `TaskHandle`.
			* @param[in] signal The signal to be exported, specified using the `ExportableSignal` enum.
			* @param[in] outputTerminal The name of the output terminal to which the signal will be exported. This is passed as a
			*                           managed .NET `String^` and converted to a C-style string.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `outputTerminal` parameter is converted from a managed `String^` to a C-style string (`char*`) before being
			*       passed to the underlying NI-DAQmx function.
			*
			* @see DAQmxExportSignal
			*/
			static int ExportSignal(IntPtr taskHandle, ExportableSignal signal, 
				String^ outputTerminal);

			/**
			* @brief Retrieves the total number of samples generated by the task.
			*
			* This function wraps the NI-DAQmx `DAQmxGetWriteTotalSampPerChanGenerated` function to obtain the total number of samples
			* that have been generated by the specified task. This value is output through a parameter.
			*
			* @param[in] taskHandle A handle to the task from which to retrieve the total number of samples generated. This is passed
			*                       as an `IntPtr` and cast to the NI-DAQmx `TaskHandle`.
			* @param[out] data A reference to an unsigned 64-bit integer (`UInt64`) that will store the total number of samples generated.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `data` parameter is an output parameter that will be updated with the total number of samples generated by the task.
			*
			* @see DAQmxGetWriteTotalSampPerChanGenerated
			*/
			static int TotalSamplesGenerated(IntPtr taskHandle, 
				[Out] UInt64 data);

			/**
			* @brief Retrieves the total number of samples read from the task.
			*
			* This function wraps the NI-DAQmx `DAQmxGetReadTotalSampPerChanAcquired` function to obtain the total number of samples
			* that have been read from the specified task. This value is output through a parameter.
			*
			* @param[in] taskHandle A handle to the task from which to retrieve the total number of samples read. This is passed
			*                       as an `IntPtr` and cast to the NI-DAQmx `TaskHandle`.
			* @param[out] data A reference to an unsigned 64-bit integer (`UInt64`) that will store the total number of samples read.
			*
			* @return
			* - `0` on success.
			* - Non-zero error code on failure. The error code corresponds to DAQmx status codes.
			*
			* @note The `data` parameter is an output parameter that will be updated with the total number of samples read from the task.
			*
			* @see DAQmxGetReadTotalSampPerChanAcquired
			*/
			static int TotalSamplesRead(IntPtr taskHandle,
				[Out] UInt64 data);

		protected:
			/**
			* @brief Converts a .NET `String^` to a C-style string.
			*
			* This function converts a managed .NET `String^` to a C-style string (null-terminated ASCII string)
			* for use with native code. If the input string is `nullptr`, the function returns `NULL`.
			*
			* @param[in] inputString A managed .NET `String^` to be converted.
			*
			* @return A pointer to a null-terminated ASCII string in unmanaged memory.
			*         Returns `NULL` if `inputString` is `nullptr`.
			*
			* @note The returned C-style string must be freed by the caller using `Marshal::FreeHGlobal`
			*		or FreeCString to avoid memory leaks.
			*/
			static inline char* ConvertToCString(String^ inputString) {
			
				return (inputString != nullptr) ?
					(char*)(void*)Marshal::StringToHGlobalAnsi(inputString) :
					NULL;
			}

			/**
			* @brief Frees memory allocated for a C-style string.
			*
			* This function releases the unmanaged memory allocated for a C-style string that was previously
			* obtained using `Marshal::StringToHGlobalAnsi`. If the provided pointer is `NULL`, no action is taken.
			*
			* @param[in] cString A pointer to the C-style string that needs to be freed. This pointer should be
			*                    obtained from `ConvertToCString` or similar functions.
			*
			* @note The function does not perform any operation if `cString` is `NULL`.
			*       The caller is responsible for ensuring that `cString` was allocated using `Marshal::StringToHGlobalAnsi`
			*       before calling this function to avoid undefined behavior.
			*/
			static inline void FreeCString(char* cString) {
				if (cString != NULL) {
					Marshal::FreeHGlobal((IntPtr)cString);
				}
			}
		};	
	};
}
