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

#include "DAQmxCLIWrapper.h"

using namespace System;

# define ErrorBufferSize 2048

namespace Grumpy{
	
	namespace DAQmxCLIWrap {

		int DAQmxCLIWrapper::CreateTask(String^ taskName, 
			[Out] long long int% taskHandle) {

			char* taskNameChar = GenerateCString(taskName);

			TaskHandle taskHandleLocal;

			int result = DAQmxCreateTask(taskNameChar, &taskHandleLocal);
			taskHandle = (long long int)taskHandleLocal;

			FreeCString(taskNameChar);
			return result;
		};

		String^ DAQmxCLIWrapper::GetErrorDescription(int errorCode) {

			char errorString[ErrorBufferSize];
			DAQmxGetErrorString(errorCode, errorString, ErrorBufferSize);
			return gcnew String(errorString);
		};

		int DAQmxCLIWrapper::CreateAIVoltageChannel(long long int taskHandle, 
			String^ physicalChannel, String^ nameToAssignToChannel, 
			int terminalConfig, double minVal, double maxVal, 
			int units, String^ customScaleName) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			
			char* nameToAssignToChannelChar = GenerateCString(nameToAssignToChannel);
			char* customScaleNameChar = GenerateCString(customScaleName);
			char* physicalChannelChar = GenerateCString(physicalChannel);

			int result = DAQmxCreateAIVoltageChan(taskHandleLocal, 
				physicalChannelChar, nameToAssignToChannelChar, 
				terminalConfig, minVal, maxVal, units, customScaleNameChar);

			FreeCString(nameToAssignToChannelChar);
			FreeCString(customScaleNameChar);
			FreeCString(physicalChannelChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateAOVoltageChannel(long long int taskHandle, 
			String^ physicalChannel, String^ nameToAssignToChannel, 
			double minVal, double maxVal, int units, 
			String^ customScaleName) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;

			char* nameToAssignToChannelChar = 
				GenerateCString(nameToAssignToChannel);
			char* customScaleNameChar = GenerateCString(customScaleName);
			char* physicalChannelChar = GenerateCString(physicalChannel);


			int result = DAQmxCreateAOVoltageChan(taskHandleLocal, physicalChannelChar, 
				nameToAssignToChannelChar, minVal, maxVal, units, customScaleNameChar);
			
			FreeCString(nameToAssignToChannelChar);
			FreeCString(customScaleNameChar);
			FreeCString(physicalChannelChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateDOChannel(long long int taskHandle, 
			String^ lines, String^ nameToAssignToLines, 
			ChannelLineGrouping lineGrouping) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;

			char* nameToAssignToLinesChar = 
				GenerateCString(nameToAssignToLines);
			char* linesChar = GenerateCString(lines);


			int result = DAQmxCreateDOChan(taskHandleLocal, linesChar, 
				nameToAssignToLinesChar, (int) lineGrouping);
			
			FreeCString(nameToAssignToLinesChar);
			FreeCString(linesChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateDIChannel(long long int taskHandle, 
			String^ lines, String^ nameToAssignToLines, 
			ChannelLineGrouping lineGrouping) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;

			char* nameToAssignToLinesChar = GenerateCString(nameToAssignToLines);
			char* linesChar = GenerateCString(lines);

			int result = DAQmxCreateDIChan(taskHandleLocal, 
				linesChar, nameToAssignToLinesChar, (int) lineGrouping);

			FreeCString(nameToAssignToLinesChar);
			FreeCString(linesChar);

			return result;
		};

		int DAQmxCLIWrapper::ReadDigitalLines(long long int taskHandle,
			uInt32 numSamplesPerChan, double timeout,
			ChannelInterleaveMode interleaveMode,
			array<Byte>^ data, uInt32 bufferSize,
			[Out] int% sampsPerChanRead, [Out] int% bytesPerSample) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			pin_ptr<Byte> dataPtr = &data[0];
			int32 read, bytesPerSmp;

			int result = DAQmxReadDigitalLines(taskHandleLocal,
				numSamplesPerChan, timeout, (int)interleaveMode,
				dataPtr, bufferSize, &read, &bytesPerSmp, NULL);

			sampsPerChanRead = read;
			bytesPerSample = bytesPerSmp;

			return result;
		};

		int DAQmxCLIWrapper::ReadDigitalScalarU32(long long int taskHandle,
							  double timeout,[Out] UInt32% data) {

			uInt32 dataLocal;
			int result = DAQmxReadDigitalScalarU32((TaskHandle)taskHandle, 
					timeout, &dataLocal, NULL);
			data = dataLocal;
			return result;
		}

		int DAQmxCLIWrapper::ReadDigitU32(long long int taskHandle,
			int samplesPerChannel, double timeout,
			ChannelInterleaveMode interleaveMode, array<uInt32>^ data, 
			uInt32 arraySize, [Out] int% sampsPerChanRead) {

			pin_ptr<uInt32> dataPtr = &data[0];
			int32 sampsPerChanReadLocal;
			int result = DAQmxReadDigitalU32((TaskHandle)taskHandle,
				samplesPerChannel, timeout, 
				(bool32)interleaveMode,  
				dataPtr,
				arraySize, &sampsPerChanReadLocal, NULL);

			sampsPerChanRead = sampsPerChanReadLocal;
			return result;
		}

		int DAQmxCLIWrapper::ReadDigitU16(long long int taskHandle,
			int samplesPerChannel, double timeout,
			ChannelInterleaveMode interleaveMode, array<uInt16>^ data, 
			uInt32 arraySize, [Out] int% sampsPerChanRead) {

			pin_ptr<uInt16> dataPtr = &data[0];
			int32 sampsPerChanReadLocal;
			int result = DAQmxReadDigitalU16((TaskHandle)taskHandle,
				samplesPerChannel, timeout,
				(bool32)interleaveMode,
				dataPtr,
				arraySize, &sampsPerChanReadLocal, NULL);
			sampsPerChanRead = sampsPerChanReadLocal;
			return result;
		}

		int DAQmxCLIWrapper::ReadDigitU8(long long int taskHandle,
			int samplesPerChannel, double timeout,
			ChannelInterleaveMode interleaveMode, array<uInt8>^ data, 
			uInt32 arraySize, [Out] int% sampsPerChanRead) {

			pin_ptr<uInt8> dataPtr = &data[0];
			int32 sampsPerChanReadLocal;

			int result = DAQmxReadDigitalU8((TaskHandle)taskHandle,
				samplesPerChannel, timeout,
				(bool32)interleaveMode,
				dataPtr,
				arraySize, &sampsPerChanReadLocal, NULL);
			sampsPerChanRead = sampsPerChanReadLocal;
			return result;
		}






		int DAQmxCLIWrapper::WriteDigitalLines(long long int taskHandle,
			int32 numSampsPerChan, bool autoStart, double timeout,
			ChannelInterleaveMode interleaveMode,
			array<Byte>^ data,
			[Out] int% sampsPerChanWritten) {

			pin_ptr<uInt8> dataPtr = &data[0];
			int32 sampsPerChanWrittenLocal;
			int result  = DAQmxWriteDigitalLines((TaskHandle)taskHandle,
				numSampsPerChan, autoStart, timeout, (int)interleaveMode,
				dataPtr, &sampsPerChanWrittenLocal, NULL);
			sampsPerChanWritten = sampsPerChanWrittenLocal;
			return result;
		}


		int DAQmxCLIWrapper::WriteDigitalScalarU32(long long int taskHandle,
			bool autostart, double timeout, uInt32 data) {

			return DAQmxWriteDigitalScalarU32((TaskHandle)taskHandle,
				autostart, timeout, data, NULL);
		}

		int DAQmxCLIWrapper::WriteDigitalU32(long long int taskHandle, int32 numSampsPerChan,
			bool autoStart, double timeout, ChannelInterleaveMode interleaveMode,
			array<uInt32>^ data, [Out] int% samplesPerChannelWritten) {	
		
			pin_ptr<uInt32> dataPtr = &data[0];
			int32 sampsPerChanWrittenLocal;

			int result = DAQmxWriteDigitalU32((TaskHandle) taskHandle, numSampsPerChan,
				autoStart, timeout, (bool32)interleaveMode,
				dataPtr, &sampsPerChanWrittenLocal, NULL);

			samplesPerChannelWritten = sampsPerChanWrittenLocal;	
			return result;
		}


		int DAQmxCLIWrapper::WriteDigitalU16(long long int taskHandle, int32 numSampsPerChan,
			bool autoStart, double timeout, ChannelInterleaveMode interleaveMode,
			array<uInt16>^ data, [Out] int% samplesPerChannelWritten) {

			pin_ptr<uInt16> dataPtr = &data[0];
			int32 sampsPerChanWrittenLocal;

			int result = DAQmxWriteDigitalU16((TaskHandle)taskHandle, 
				numSampsPerChan, autoStart, timeout, 
				(bool32)interleaveMode, dataPtr, &sampsPerChanWrittenLocal, NULL);

			samplesPerChannelWritten = sampsPerChanWrittenLocal;
			return result;
		}


		int DAQmxCLIWrapper::WriteDigitalU8(long long int taskHandle, int32 numSampsPerChan,
			bool autoStart, double timeout, ChannelInterleaveMode interleaveMode,
			array<uInt8>^ data, [Out] int% samplesPerChannelWritten) {

			pin_ptr<uInt8> dataPtr = &data[0];
			int32 sampsPerChanWrittenLocal;

			int result = DAQmxWriteDigitalU8((TaskHandle)taskHandle,
				numSampsPerChan, autoStart, timeout,
				(bool32)interleaveMode, dataPtr, &sampsPerChanWrittenLocal, NULL);

			samplesPerChannelWritten = sampsPerChanWrittenLocal;
			return result;
		}


		int  DAQmxCLIWrapper::ConfigureTiming(long long taskHandle,
			String^ source, double rate, ActiveEdge activeEdge,
			SamplingMode sampleMode, long long sampsPerChan) {
		
			return DAQmxCfgSampClkTiming((TaskHandle)taskHandle,
				GenerateCString(source), rate, (int)activeEdge,
				(int)sampleMode, sampsPerChan);
		}


		int DAQmxCLIWrapper::ReadAnalogF64(long long int taskHandle,
			int32 sampsPerChan, double timeout,
			ChannelGroup groupMode,
			array<double>^ data, uInt32 bufferSizeInSamples,
			[Out] int% sampsPerChanRead) {

			pin_ptr<float64> dataPtr = &data[0];
			int32 sampsPerChanReadLocal;
			int result = DAQmxReadAnalogF64((TaskHandle) taskHandle, 
				 sampsPerChan, timeout, (bool32) groupMode, 
				dataPtr, bufferSizeInSamples, &sampsPerChanReadLocal, NULL);
			sampsPerChanRead = sampsPerChanReadLocal;
			return result;
		}


		int DAQmxCLIWrapper::ReadAnalogScalarF64(long long int taskHandle,
			double timeout, [Out] double% data) {

			float64 localValue;
			int result = DAQmxReadAnalogScalarF64((TaskHandle) taskHandle, 
				timeout, &localValue,NULL);
			data = localValue;
			return result;
		}

		int DAQmxCLIWrapper::ReadBinaryI16(long long int taskHandle,
			int32 sampsPerChan, double timeout,
			ChannelGroup groupMode, array<int16>^ data,
			uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead)
		{
			pin_ptr<int16> dataPtr = &data[0];
			int32 sampsPerChanReadLocal;
			int result = DAQmxReadBinaryI16((TaskHandle) taskHandle, 
				sampsPerChan, timeout, (bool32) groupMode,dataPtr, 
				bufferSizeInSamples, &sampsPerChanReadLocal, NULL);
			sampsPerChanRead = sampsPerChanReadLocal;
			return result;
		}

		int DAQmxCLIWrapper::ReadBinaryI32(long long int taskHandle,
			int32 sampsPerChan, double timeout,
			ChannelGroup groupMode, array<int32>^ dat,
			uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead)
		{
			pin_ptr<int32> dataPtr = &dat[0];
			int32 sampsPerChanReadLocal;
			int result = DAQmxReadBinaryI32((TaskHandle)taskHandle,
				sampsPerChan, timeout, (bool32)groupMode, dataPtr,
				bufferSizeInSamples, &sampsPerChanReadLocal, NULL);
			sampsPerChanRead = sampsPerChanReadLocal;
			return result;
		}

		int DAQmxCLIWrapper::ReadBinaryUI16(long long int taskHandle,
			int32 sampsPerChan, double timeout,
			ChannelGroup groupMode, array<uInt16>^ dat,
			uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead)
		{
			pin_ptr<uInt16> dataPtr = &dat[0];
			int32 sampsPerChanReadLocal;
			int result = DAQmxReadBinaryU16((TaskHandle)taskHandle,
				sampsPerChan, timeout, (bool32)groupMode, dataPtr,
				bufferSizeInSamples, &sampsPerChanReadLocal, NULL);
			sampsPerChanRead = sampsPerChanReadLocal;
			return result;
		}


		int DAQmxCLIWrapper::ReadBinaryUI32(long long int taskHandle,
			int32 sampsPerChan, double timeout,
			ChannelGroup groupMode, array<uInt32>^ dat,
			uInt32 bufferSizeInSamples, [Out] int% sampsPerChanRead)
		{
			pin_ptr<uInt32> dataPtr = &dat[0];
			int32 sampsPerChanReadLocal;
			int result = DAQmxReadBinaryU32((TaskHandle)taskHandle,
				sampsPerChan, timeout, (bool32)groupMode, dataPtr,
				bufferSizeInSamples, &sampsPerChanReadLocal, NULL);
			sampsPerChanRead = sampsPerChanReadLocal;
			return result;
		}


		int DAQmxCLIWrapper::CreateCOPulseFrequencyChannel(
			long long int taskHandle, 
			String^ counter, String^ nameToAssignToChannel, 
			int units, int idleState, double initialDelay, 
			double freq, double dutyCycle) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* nameToAssignToChannelChar = 
				GenerateCString(nameToAssignToChannel);
			char* counterChar = GenerateCString(counter);

			int result = DAQmxCreateCOPulseChanFreq(taskHandleLocal, 
				counterChar, nameToAssignToChannelChar, 
				units, idleState, 
				initialDelay, freq, dutyCycle);

			FreeCString(nameToAssignToChannelChar);
			FreeCString(counterChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateCOPulseChanTime(long long int taskHandle, 
			String^ counter, String^ nameToAssignToChannel, int units, 
			int idleState, double initialDelay, 
			double lowTime, double highTime) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* nameToAssignToChannelChar = 
				GenerateCString(nameToAssignToChannel);
			char* counterChar = GenerateCString(counter);
			int result = DAQmxCreateCOPulseChanTime(taskHandleLocal, 
				counterChar, nameToAssignToChannelChar, units, idleState, 
				initialDelay, lowTime, highTime);

			FreeCString(nameToAssignToChannelChar);
			FreeCString(counterChar);

			return result;
		};
		
		int DAQmxCLIWrapper::CreateCICountEdgesChan(long long int taskHandle, 
			String^ counter, String^ nameToAssignToChannel, int edge, 
			int initialCount, int countDirection) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* nameToAssignToChannelChar =
				GenerateCString(nameToAssignToChannel);
			char* counterChar = GenerateCString(counter);

			int result = DAQmxCreateCICountEdgesChan(taskHandleLocal, 
				counterChar, nameToAssignToChannelChar, edge, 
				initialCount, countDirection);

			FreeCString(nameToAssignToChannelChar);
			FreeCString(counterChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateCIFreqChan(long long int taskHandle, 
			String^ counter, String^ nameToAssignToChannel, double minVal, 
			double maxVal, int units, int edge, int measMethod, 
			double measTime, UInt32 divisor, String^ customScaleName) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* nameToAssignToChannelChar = 
				GenerateCString(nameToAssignToChannel);
			char* counterChar = GenerateCString(counter);
			char* customScaleNameChar = GenerateCString(customScaleName);

			int result = DAQmxCreateCIFreqChan(taskHandleLocal, counterChar, 
				nameToAssignToChannelChar, minVal, maxVal, units, edge, 
				measMethod, measTime, divisor, customScaleNameChar);

			FreeCString(nameToAssignToChannelChar);
			FreeCString(counterChar);
			FreeCString(customScaleNameChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateCIPeriodChan(long long int taskHandle, 
			String^ counter, String^ nameToAssignToChannel, double minVal, 
			double maxVal, int units, int edge, int measMethod, 
			double measTime, UInt32 divisor, String^ customScaleName) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* nameToAssignToChannelChar = 
				GenerateCString(nameToAssignToChannel);
			char* counterChar = GenerateCString(counter);
			char* customScaleNameChar = GenerateCString(customScaleName);

			int result = DAQmxCreateCIPeriodChan(taskHandleLocal, 
				counterChar, nameToAssignToChannelChar, minVal, maxVal, 
				units, edge, measMethod, measTime, divisor, 
				customScaleNameChar);
			
			FreeCString(nameToAssignToChannelChar);
			FreeCString(counterChar);
			FreeCString(customScaleNameChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateCISemiPeriodChan(long long int taskHandle, 
			String^ counter, String^ nameToAssignToChannel, double minVal, 
			double maxVal, int units, String^ customScaleName){

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* nameToAssignToChannelChar = 
				GenerateCString(nameToAssignToChannel);
			char* counterChar = GenerateCString(counter);
			char* customScaleNameChar = GenerateCString(customScaleName);

			int result = DAQmxCreateCISemiPeriodChan(taskHandleLocal, 
				counterChar, nameToAssignToChannelChar, 
				minVal, maxVal, units, customScaleNameChar);

			FreeCString(nameToAssignToChannelChar);
			FreeCString(counterChar);
			FreeCString(customScaleNameChar);

			return result;
		};

		int DAQmxCLIWrapper::LoadTask(long long int taskHandle, 
									  String^ taskName) {
			
			char* taskNameChar = GenerateCString(taskName);
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			int result = DAQmxLoadTask(taskNameChar, &taskHandleLocal);
			taskHandle = (long long int)taskHandleLocal;
			FreeCString(taskNameChar);
			return result;
		};

		int DAQmxCLIWrapper::AddGlobalChansToTask(long long int taskHandle, 
				String^ channelNames) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelNamesChar = GenerateCString(channelNames);
			int result = DAQmxAddGlobalChansToTask(taskHandleLocal, 
												   channelNamesChar);
			FreeCString(channelNamesChar);
			return result;
		};


		int DAQmxCLIWrapper::IsTaskDone(long long int taskHandle, 
										[Out] bool% isTaskDone) {
			 
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			bool32 isTaskDoneLocal;
			int result = DAQmxIsTaskDone(taskHandleLocal, &isTaskDoneLocal);
			isTaskDone = (bool) isTaskDoneLocal;
			return result;
		};
		
		int DAQmxCLIWrapper::GetNthTaskChannel(long long int taskHandle,	
									uInt32 index, [Out] String^% buffer) {
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char bufferChar[ErrorBufferSize];
			int result = DAQmxGetNthTaskChannel(taskHandleLocal,	
									index, bufferChar, ErrorBufferSize);
			buffer = gcnew String(bufferChar);
			return result;
		};

		int DAQmxCLIWrapper::GetNthTaskDevice(long long int taskHandle,
									uInt32 index, [Out] String^% buffer) {
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char bufferChar[ErrorBufferSize];
			int result = DAQmxGetNthTaskDevice(taskHandleLocal, 
								index, bufferChar, ErrorBufferSize);
			buffer = gcnew String(bufferChar);
			return result;
		};
	}
}