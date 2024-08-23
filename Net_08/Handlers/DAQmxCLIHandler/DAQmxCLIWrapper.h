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

	namespace DAQmxCLIWrap {
		
		#include <NIDAQmx.h>

		public enum class AiTermination
		{
			Default = DAQmx_Val_Cfg_Default,
			RSE = DAQmx_Val_RSE,
			NRSE = DAQmx_Val_NRSE,
			Differential = DAQmx_Val_Diff,
			PseudoDifferential = DAQmx_Val_PseudoDiff
		};

		public enum class VoltageUnits
		{
			Volts = DAQmx_Val_Volts,
			FromCustomScale = DAQmx_Val_FromCustomScale
		};

		public enum class ActiveEdge
		{
			Rising = DAQmx_Val_Rising,
			Falling = DAQmx_Val_Falling
		};

		public enum class DIOLineGrouping
		{
			ChanPerLine = DAQmx_Val_ChanPerLine,
			ChanForAllLines = DAQmx_Val_ChanForAllLines
		};

		public enum class ReadbacklFillMode
		{
			ByChannel = DAQmx_Val_GroupByChannel,
			ByScan = DAQmx_Val_GroupByScanNumber
		};

		public enum class TaskAction
		{
			Start = DAQmx_Val_Task_Start,
			Stop = DAQmx_Val_Task_Stop,
			Verify = DAQmx_Val_Task_Verify,
			Commit = DAQmx_Val_Task_Commit,
			Reserve = DAQmx_Val_Task_Reserve,
			Unreserve = DAQmx_Val_Task_Unreserve,
			Abort = DAQmx_Val_Task_Abort                                         
		};

		public enum class SamplingMode
		{
			FiniteSamples = DAQmx_Val_FiniteSamps,					
			ContineousSamples = DAQmx_Val_ContSamps,				
			HWTimedSinglePoint = DAQmx_Val_HWTimedSinglePoint,		
		};

		public enum class ExportableSignal
		{
			AIConvertClock = DAQmx_Val_AIConvertClock,		// Clock that causes an analog - to - digital conversion on an E Series or M Series device.One conversion corresponds to a single sample from one channel.
			RefClock10Mhz = DAQmx_Val_10MHzRefClock,		// Output of an oscillator that you can use to synchronize multiple devices.
			RefClock20Mhz = DAQmx_Val_20MHzTimebaseClock,   //Output of an oscillator that is the onboard source of the Master Timebase.Other timebases are derived from this clock.
			SampleClock = DAQmx_Val_SampleClock,			//Clock the device uses to time each sample.
			AdvanceTrigger = DAQmx_Val_AdvanceTrigger,		//Trigger that moves a switch to the next entry in a scan list.
			ReferenceTrigger = DAQmx_Val_ReferenceTrigger,  //Trigger that establishes the reference point between pretrigger and posttrigger samples.
			StartTrigger = DAQmx_Val_StartTrigger,			//Trigger that begins a measurement or generation.
			AdvCmpltEvent = DAQmx_Val_AdvCmpltEvent,		//Signal that a switch product generates after it both executes the command(s) in a scan list entry and waits for the settling time to elapse.
			AIHoldCmpltEvent = DAQmx_Val_AIHoldCmpltEvent,  //Signal that an E Series or M Series device generates when the device latches analog input data(the ADC enters "hold" mode) and it is safe for any external switching hardware to remove the signal and replace it with the next signal.This event does not indicate the completion of the actual analog - to - digital conversion.
			CounterOutputEven = DAQmx_Val_CounterOutputEvent,  //Signal that a counter generates.Each time the counter reaches terminal count, this signal toggles or pulses.
			ChangeDetectionEvent = DAQmx_Val_ChangeDetectionEvent,  //Signal that a static DIO device generates when the device detects a rising or falling edge on any of the lines or ports you selected when you configured change detection timing.
			WDTExpiredEvent = DAQmx_Val_WDTExpiredEvent     // Signal that a static DIO device generates when the watchdog timer expires.
		};

		public ref class DAQmxCLIWrapper
		{
		public:

			static inline bool Failed(int errorCode)
			{ return errorCode < 0; };

			static inline bool Success(int errorCode)
			{ return errorCode >= 0; };

			static inline bool Warning(int errorCode)
			{ return errorCode > 0; };

			static int CreateTask(String^ taskName,
				[Out] IntPtr% taskHandle);

			static String^ GetErrorDescription(int errorCode);

			static inline int StartTask(IntPtr taskHandle) {
				return DAQmxStartTask((TaskHandle)taskHandle);
			}

			static int StopTask(IntPtr taskHandle) {
				return DAQmxStopTask((TaskHandle)taskHandle);
			}

			static inline int ClearTask([Out] IntPtr% taskHandle) {

				Int32 r = DAQmxClearTask((TaskHandle)taskHandle);
				if (r == 0) {
					taskHandle = (IntPtr)NULL;
				}
				return r;
			}
			
			static inline int DisposeTask([Out] IntPtr% taskHandle) {
					return ClearTask(taskHandle);
			}

			static inline int TaskControl(IntPtr taskHandle,
				TaskAction action) {
				return DAQmxTaskControl((TaskHandle)taskHandle, (int)action);
			}

			static inline int WaitUntilTaskDone(IntPtr taskHandle,
				double timeToWait){
					return DAQmxWaitUntilTaskDone((TaskHandle)taskHandle, 
						timeToWait);
			}
		
			static int ConfigureTiming(long long taskHandle,
				String^ source, double rate, ActiveEdge activeEdge,
				SamplingMode sampleMode, long long  sampsPerChan);

			static int ReadAnalogF64(IntPtr taskHandle,
				int32 sampsPerChan, double timeout, 
				ReadbacklFillMode groupMode,
				array<double>^ data,
				[Out] int% samplsPerChanRead);

			static int ReadAnalogScalarF64(IntPtr taskHandle,
								double timeout, [Out] double% data);

			static int ReadBinaryI16(IntPtr taskHandle,
				int32 sampsPerChan, double timeout,
				ReadbacklFillMode groupMode, array<int16>^ data,
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			static int ReadBinaryUI16(IntPtr taskHandle,
				int32 sampsPerChan, double timeout,
				ReadbacklFillMode groupMode, array<uInt16>^ data,
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			static int ReadBinaryI32(IntPtr taskHandle,
				int32 sampsPerChan, double timeout,
				ReadbacklFillMode groupMode, array<int32>^ data,
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			static int ReadBinaryUI32(IntPtr taskHandle,
				int32 sampsPerChan, double timeout,
				ReadbacklFillMode groupMode, array<uInt32>^ dat,
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			static int CreateAIVoltageChannel(IntPtr taskHandle,
				String^ physicalChannel, String^ nameToAssignToChannel, 
				AiTermination terminalConfig, double minVal, 
				double maxVal, VoltageUnits units,
				String^ customScaleName);

			static int CreateAOVoltageChannel(IntPtr taskHandle,
				String^ physicalChannel, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, 
				String^ customScaleName);

			static int CreateDIChannel(IntPtr taskHandle, String^ lines,
				String^ nameToAssignToLines, DIOLineGrouping lineGrouping);

			static int CreateDOChannel(IntPtr taskHandle, String^ lines,
				String^ nameToAssignToLines, DIOLineGrouping lineGrouping);

			static int CreateCOPulseFrequencyChannel(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				int units, int idleState, double initialDelay, double freq, 
				double dutyCycle);

			static int CreateCOPulseChanTime(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				int units, int idleState, 
				double initialDelay, double lowTime, double highTime);

			static int CreateCICountEdgesChan(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				int edge, int initialCount, int countDirection);

			static int CreateCIFreqChan(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, int edge, 
				int measMethod, double measTime, UInt32 divisor,  
				String^ customScaleName);

			static int CreateCIPeriodChan(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, int edge, 
				int measMethod, double measTime, UInt32 divisor, 
				String^ customScaleName);

			static int CreateCISemiPeriodChan(IntPtr taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, 
				String^ customScaleName);

			static int LoadTask(IntPtr taskHandle, String^ taskName);
			
			static int AddGlobalChansToTask(IntPtr taskHandle, 
				String^ channelNames);
			
			static int IsTaskDone(IntPtr taskHandle, [Out] bool% isTaskDone);
			
			static int GetNthTaskChannel(IntPtr taskHandle, uInt32 index, 
				[Out] String^% buffer);
			
			static int GetNthTaskDevice(IntPtr taskHandle, uInt32 index, 
				[Out] String^% buffer);

			static int ReadDigitalLines(IntPtr taskHandle,
								uInt32 numSampsPerChan, double timeout,
									ReadbacklFillMode interleaveMode,
									array<Byte>^ data,
									uInt32 bufferSize,
									[Out] int% sampsPerChanRead,
									[Out] int% bytesPerSample);

			static int ReadDigitalScalarU32(IntPtr taskHandle,
								double timeout, [Out] UInt32% data);

			static int ReadDigitU32(IntPtr taskHandle,
				int samplesPerChannel, double timeout, 
				ReadbacklFillMode interleaveMode,
				array<uInt32> ^data, uInt32 arraySize,
				[Out] int% sampsPerChanRead);

			static int ReadDigitU16(IntPtr taskHandle,
				int samplesPerChannel, double timeout,
				ReadbacklFillMode interleaveMode,
				array<uInt16>^ data, uInt32 arraySize,
				[Out] int% sampsPerChanRead);

			static int ReadDigitU8(IntPtr taskHandle,
				int samplesPerChannel, double timeout,
				ReadbacklFillMode interleaveMode,
				array<uInt8>^ data, uInt32 arraySize,
				[Out] int% sampsPerChanRead);

			static int WriteDigitalLines(IntPtr taskHandle,
				int32 numSampsPerChan, bool autoStart, double timeout,
				ReadbacklFillMode interleaveMode,
				array<Byte>^ data,
				[Out] int% sampsPerChanWritten);

			static int WriteDigitalScalarU32(IntPtr taskHandle,
				bool autostart, double timeout, uInt32 data);

			static int WriteDigitalU8(IntPtr taskHandle, int32 numSampsPerChan,
				bool autoStart, double timeout, 
				ReadbacklFillMode interleaveMode,
				array<uInt8>^ data, [Out] int% samplesPerChannelWritten);

			static int WriteDigitalU16(IntPtr taskHandle, int32 numSampsPerChan,
				bool autoStart, double timeout, 
				ReadbacklFillMode interleaveMode,
				array<uInt16>^ data, [Out] int% samplesPerChannelWritten);

			static int WriteDigitalU32(IntPtr taskHandle, int32 numSampsPerChan,
				bool autostart, double timeout, 
				ReadbacklFillMode interleaveMode, 
				array<uInt32>^ data, [Out] int% samplesPerChannelWritten);

			static int ExportSignal(IntPtr taskHandle, ExportableSignal signal, 
				String^ outputTerminal);

protected:

			static inline char* ConvertToCString(String^ inputString) {
			
				return (inputString != nullptr) ?
					(char*)(void*)Marshal::StringToHGlobalAnsi(inputString) :
					NULL;
			}

			static inline void FreeCString(char* cString) {
				if (cString != NULL) {
					Marshal::FreeHGlobal((IntPtr)cString);
				}
			}
		};
	};
}
