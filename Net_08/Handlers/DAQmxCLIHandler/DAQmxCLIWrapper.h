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

		public enum class AiTerminalConfiguration
		{
			Default = DAQmx_Val_Cfg_Default,
			RSE = DAQmx_Val_RSE,
			NRSE = DAQmx_Val_NRSE,
			Diff = DAQmx_Val_Diff,
			PseudoDiff = DAQmx_Val_PseudoDiff
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

		public enum class ChannelLineGrouping
		{
			ChanPerLine = DAQmx_Val_ChanPerLine,
			ChanForAllLines = DAQmx_Val_ChanForAllLines
		};

		public enum class ChannelGroup
		{
			NonInterleaved = DAQmx_Val_GroupByChannel,
			Interleaved = DAQmx_Val_GroupByScanNumber
		};

		public enum class ChannelInterleaveMode
		{
			NonInterleaved = DAQmx_Val_GroupByChannel,
			Interleaved = DAQmx_Val_GroupByScanNumber
		};

		public enum class TaskAction
		{
			Start = DAQmx_Val_Task_Start,
			Stop = DAQmx_Val_Task_Stop,
			Verify = DAQmx_Val_Task_Verify
		};

		public enum class SamplingMode
		{
			FiniteSamples = DAQmx_Val_FiniteSamps,
			ContineousSamples = DAQmx_Val_ContSamps,
			HWTimedSinglePoint = DAQmx_Val_HWTimedSinglePoint,
		};


		static public ref class DAQmxCLIWrapper
		{
		public:

			static inline bool Failed(int errorCode)
			{ return errorCode < 0; };

			static inline bool Success(int errorCode)
			{ return errorCode >= 0; };

			static inline bool Warning(int errorCode)
			{ return errorCode > 0; };

			static int CreateTask(String^ taskName,
				[Out] long long int% taskHandle);

			static String^ GetErrorDescription(int errorCode);

			static inline int StartTask(long long int taskHandle) {
				return DAQmxStartTask((TaskHandle)taskHandle);
			}

			static int StopTask(long long int taskHandle) {
				return DAQmxStopTask((TaskHandle)taskHandle);
			}

			static inline int ClearTask([Out] long long int% taskHandle) {

				Int32 r = DAQmxClearTask((TaskHandle)taskHandle);
				if (r == 0) {
					taskHandle = 0LL;
				}
				return r;
			}
			
			static inline int DisposeTask([Out] long long int% taskHandle) {
					return ClearTask(taskHandle);
			}

			static inline int TaskControl(long long int taskHandle,
				int action) {
				return DAQmxTaskControl((TaskHandle)taskHandle, action);
			}

			static inline int WaitUntilTaskDone(long long int taskHandle,
				double timeToWait){
					return DAQmxWaitUntilTaskDone((TaskHandle)taskHandle, 
						timeToWait);
			}
		

			static int ConfigureTiming(long long taskHandle,
				String^ source, double rate, ActiveEdge activeEdge,
				SamplingMode sampleMode, long long  sampsPerChan);



			static int ReadAnalogF64(long long int taskHandle,
				int32 sampsPerChan, double timeout, 
				ChannelGroup groupMode,
				array<double>^ dat, uInt32 bufferSizeInSamples, 
				[Out] int% sampsPerChanRead);

			static int ReadAnalogScalarF64(long long int taskHandle,
								double timeout, [Out] double% data);


			static int ReadBinaryI16(long long int taskHandle,
				int32 sampsPerChan, double timeout,
				ChannelGroup groupMode, array<int16>^ data, 
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			static int ReadBinaryUI16(long long int taskHandle,
				int32 sampsPerChan, double timeout,
				ChannelGroup groupMode, array<uInt16>^ data,
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			static int ReadBinaryI32(long long int taskHandle,
				int32 sampsPerChan, double timeout,
				ChannelGroup groupMode, array<int32>^ data,
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			static int ReadBinaryUI32(long long int taskHandle,
				int32 sampsPerChan, double timeout,
				ChannelGroup groupMode, array<uInt32>^ dat,
				uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead);

			static int CreateAIVoltageChannel(long long int taskHandle,
				String^ physicalChannel, String^ nameToAssignToChannel, 
				int terminalConfig, double minVal, double maxVal, int units, 
				String^ customScaleName);

			static int CreateAOVoltageChannel(long long int taskHandle,
				String^ physicalChannel, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, 
				String^ customScaleName);

			static int CreateDIChannel(long long int taskHandle, String^ lines,
				String^ nameToAssignToLines, ChannelLineGrouping lineGrouping);

			static int CreateDOChannel(long long int taskHandle, String^ lines,
				String^ nameToAssignToLines, ChannelLineGrouping lineGrouping);

			static int CreateCOPulseFrequencyChannel(long long int taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				int units, int idleState, double initialDelay, double freq, 
				double dutyCycle);

			static int CreateCOPulseChanTime(long long int taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				int units, int idleState, 
				double initialDelay, double lowTime, double highTime);

			static int CreateCICountEdgesChan(long long int taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				int edge, int initialCount, int countDirection);

			static int CreateCIFreqChan(long long int taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, int edge, 
				int measMethod, double measTime, UInt32 divisor,  
				String^ customScaleName);

			static int CreateCIPeriodChan(long long int taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, int edge, 
				int measMethod, double measTime, UInt32 divisor, 
				String^ customScaleName);

			static int CreateCISemiPeriodChan(long long int taskHandle,
				String^ counter, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, 
				String^ customScaleName);

			static int LoadTask(long long int taskHandle, String^ taskName);
			
			static int AddGlobalChansToTask(long long int taskHandle, String^ channelNames);
			
			static int IsTaskDone(long long int taskHandle, [Out] bool% isTaskDone);
			
			static int GetNthTaskChannel(long long int taskHandle, uInt32 index, [Out] String^% buffer);
			
			static int GetNthTaskDevice(long long int taskHandle, uInt32 index, [Out] String^% buffer);

			static int ReadDigitalLines(long long int taskHandle,
								uInt32 numSampsPerChan, double timeout,
									ChannelInterleaveMode interleaveMode,
									array<Byte>^ data,
									uInt32 bufferSize,
									[Out] int% sampsPerChanRead,
									[Out] int% bytesPerSample);

			static int ReadDigitalScalarU32(long long int taskHandle,
								double timeout, [Out] UInt32% data);

			static int ReadDigitU32(long long int taskHandle,
				int samplesPerChannel, double timeout, 
				ChannelInterleaveMode interleaveMode,
				array<uInt32> ^data, uInt32 arraySize,
				[Out] int% sampsPerChanRead);

			static int ReadDigitU16(long long int taskHandle,
				int samplesPerChannel, double timeout,
				ChannelInterleaveMode interleaveMode,
				array<uInt16>^ data, uInt32 arraySize,
				[Out] int% sampsPerChanRead);

			static int ReadDigitU8(long long int taskHandle,
				int samplesPerChannel, double timeout,
				ChannelInterleaveMode interleaveMode,
				array<uInt8>^ data, uInt32 arraySize,
				[Out] int% sampsPerChanRead);

			static int WriteDigitalLines(long long int taskHandle,
				int32 numSampsPerChan, bool autoStart, double timeout,
				ChannelInterleaveMode interleaveMode,
				array<Byte>^ data,
				[Out] int% sampsPerChanWritten);

			static int WriteDigitalScalarU32(long long int taskHandle,
				bool autostart, double timeout, uInt32 data);

			static int WriteDigitalU8(long long int taskHandle, int32 numSampsPerChan,
				bool autoStart, double timeout, ChannelInterleaveMode interleaveMode,
				array<uInt8>^ data, [Out] int% samplesPerChannelWritten);

			static int WriteDigitalU16(long long int taskHandle, int32 numSampsPerChan,
				bool autoStart, double timeout, ChannelInterleaveMode interleaveMode,
				array<uInt16>^ data, [Out] int% samplesPerChannelWritten);

			static int WriteDigitalU32(long long int taskHandle, int32 numSampsPerChan,
				bool autostart, double timeout, ChannelInterleaveMode interleaveMode, 
				array<uInt32>^ data, [Out] int% samplesPerChannelWritten);







private:

			static inline char* GenerateCString(String^ inputString) {

				char* cString = NULL;

				if (inputString != nullptr) {
					cString = (char*)(void*)Marshal::StringToHGlobalAnsi(inputString);
				}

				return cString;
			}

			static inline void FreeCString(char* cString) {
				if (cString != NULL) {
					Marshal::FreeHGlobal((IntPtr)cString);
				}
			}
		};
	};
}
