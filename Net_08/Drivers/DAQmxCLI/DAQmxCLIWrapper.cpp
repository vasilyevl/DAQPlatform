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
	
	namespace DAQmxNetApi {

		int DAQmxCLIWrapper::CreateTask(String^ taskName, 
			[Out] IntPtr% taskHandle) {

			char* taskNameChar = StringToCharz(taskName);

			TaskHandle taskHandleLocal;

			int result = DAQmxCreateTask(taskNameChar, &taskHandleLocal);
			taskHandle = (IntPtr)taskHandleLocal;

			FreeCharz(taskNameChar);
			return result;
		};

		String^ DAQmxCLIWrapper::GetErrorDescription(int errorCode) {

			char errorString[ErrorBufferSize];
			DAQmxGetErrorString(errorCode, errorString, ErrorBufferSize);
			return gcnew String(errorString);
		};

		int DAQmxCLIWrapper::CreateAIVoltageChannel(IntPtr taskHandle, 
			String^ physicalChannel, String^ nameToAssignToChannel, 
			AiTermination terminalConfig, double minVal, double maxVal,
			VoltageUnits units, String^ customScaleName) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			
			char* nameToAssignToChannelChar = StringToCharz(nameToAssignToChannel);
			char* customScaleNameChar = StringToCharz(customScaleName);
			char* physicalChannelChar = StringToCharz(physicalChannel);

			int result = DAQmxCreateAIVoltageChan(taskHandleLocal, 
				physicalChannelChar, nameToAssignToChannelChar, 
				(int) terminalConfig, minVal, maxVal,
				(int)units, customScaleNameChar);

			FreeCharz(nameToAssignToChannelChar);
			FreeCharz(customScaleNameChar);
			FreeCharz(physicalChannelChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateAOVoltageChannel(IntPtr taskHandle, 
			String^ physicalChannel, String^ nameToAssignToChannel, 
			double minVal, double maxVal, VoltageUnits units,
			String^ customScaleName) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;

			char* nameToAssignToChannelChar = 
				StringToCharz(nameToAssignToChannel);
			char* customScaleNameChar = StringToCharz(customScaleName);
			char* physicalChannelChar = StringToCharz(physicalChannel);


			int result = DAQmxCreateAOVoltageChan(taskHandleLocal, physicalChannelChar, 
				nameToAssignToChannelChar, minVal, maxVal, (int) units, customScaleNameChar);
			
			FreeCharz(nameToAssignToChannelChar);
			FreeCharz(customScaleNameChar);
			FreeCharz(physicalChannelChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateDOChannel(IntPtr taskHandle, 
			String^ lines, String^ nameToAssignToLines, 
			DIOLineGrouping lineGrouping) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;

			char* nameToAssignToLinesChar = 
				StringToCharz(nameToAssignToLines);
			char* linesChar = StringToCharz(lines);


			int result = DAQmxCreateDOChan(taskHandleLocal, linesChar, 
				nameToAssignToLinesChar, (int) lineGrouping);
			
			FreeCharz(nameToAssignToLinesChar);
			FreeCharz(linesChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateDIChannel(IntPtr taskHandle, 
			String^ lines, String^ nameToAssignToLines, 
			DIOLineGrouping lineGrouping) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;

			char* nameToAssignToLinesChar = StringToCharz(nameToAssignToLines);
			char* linesChar = StringToCharz(lines);

			int result = DAQmxCreateDIChan(taskHandleLocal, 
				linesChar, nameToAssignToLinesChar, (int) lineGrouping);

			FreeCharz(nameToAssignToLinesChar);
			FreeCharz(linesChar);

			return result;
		};

		int DAQmxCLIWrapper::ReadDigitalLines(IntPtr taskHandle,
			uInt32 numSamplesPerChan, double timeout,
			ReadWriteFillMode interleaveMode,
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

		int DAQmxCLIWrapper::ReadDigitalScalarU32(IntPtr taskHandle,
							  double timeout,[Out] UInt32% data) {

			uInt32 dataLocal;
			int result = DAQmxReadDigitalScalarU32((TaskHandle)taskHandle, 
					timeout, &dataLocal, NULL);
			data = dataLocal;
			return result;
		}

		int DAQmxCLIWrapper::ReadDigitU32(IntPtr taskHandle,
			int samplesPerChannel, double timeout,
			ReadWriteFillMode interleaveMode, array<uInt32>^ data, 
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

		int DAQmxCLIWrapper::ReadDigitU16(IntPtr taskHandle,
			int samplesPerChannel, double timeout,
			ReadWriteFillMode interleaveMode, array<uInt16>^ data, 
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

		int DAQmxCLIWrapper::ReadDigitU8(IntPtr taskHandle,
			int samplesPerChannel, double timeout,
			ReadWriteFillMode interleaveMode, array<uInt8>^ data, 
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

		int DAQmxCLIWrapper::WriteDigitalLines(IntPtr taskHandle,
			int32 numSampsPerChan, 
			bool autoStart, 
			double timeout,
			ReadWriteFillMode interleaveMode,
			array<Byte>^ data,
			[Out] int% sampsPerChanWritten) {

			pin_ptr<uInt8> dataPtr = &data[0];
			int32 sampsPerChanWrittenLocal;
			int result  = DAQmxWriteDigitalLines(
				(TaskHandle)taskHandle,
				numSampsPerChan, 
				autoStart, 
				timeout, 
				(int)interleaveMode,
				dataPtr, 
				&sampsPerChanWrittenLocal, 
				NULL);

			sampsPerChanWritten = sampsPerChanWrittenLocal;
			return result;
		}

		int DAQmxCLIWrapper::WriteDigitalScalarU32(IntPtr taskHandle,
			bool autostart, double timeout, uInt32 data) {

			return DAQmxWriteDigitalScalarU32((TaskHandle)taskHandle,
				autostart, timeout, data, NULL);
		}

		int DAQmxCLIWrapper::WriteDigitalU32(IntPtr taskHandle, int32 numSampsPerChan,
			bool autoStart, double timeout, ReadWriteFillMode interleaveMode,
			array<uInt32>^ data, [Out] int% samplesPerChannelWritten) {	
		
			pin_ptr<uInt32> dataPtr = &data[0];
			int32 sampsPerChanWrittenLocal;

			int result = DAQmxWriteDigitalU32((TaskHandle) taskHandle, numSampsPerChan,
				autoStart, timeout, (bool32)interleaveMode,
				dataPtr, &sampsPerChanWrittenLocal, NULL);

			samplesPerChannelWritten = sampsPerChanWrittenLocal;	
			return result;
		}

		int DAQmxCLIWrapper::WriteDigitalU16(IntPtr taskHandle, int32 numSampsPerChan,
			bool autoStart, double timeout, ReadWriteFillMode interleaveMode,
			array<uInt16>^ data, [Out] int% samplesPerChannelWritten) {

			pin_ptr<uInt16> dataPtr = &data[0];
			int32 sampsPerChanWrittenLocal;

			int result = DAQmxWriteDigitalU16((TaskHandle)taskHandle, 
				numSampsPerChan, autoStart, timeout, 
				(bool32)interleaveMode, dataPtr, &sampsPerChanWrittenLocal, NULL);

			samplesPerChannelWritten = sampsPerChanWrittenLocal;
			return result;
		}

		int DAQmxCLIWrapper::WriteDigitalU8(IntPtr taskHandle, int32 numSampsPerChan,
			bool autoStart, double timeout, ReadWriteFillMode interleaveMode,
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

			char* sourceChar = StringToCharz(source);

			int result = DAQmxCfgSampClkTiming((TaskHandle)taskHandle,
				sourceChar, rate, (int)activeEdge,
				(int)sampleMode, sampsPerChan);

			FreeCharz(sourceChar);
			return result;
		}

		int DAQmxCLIWrapper::ReadAnalogLines(IntPtr taskHandle,
			int32 sampsPerChan, double timeout,
			ReadWriteFillMode groupMode,
			array<double>^ data,
			[Out] int% samplsPerChanRead) {

			pin_ptr<float64> dataPtr = &data[0];
			int32 sampsPerChanReadLocal;
			int result = DAQmxReadAnalogF64((TaskHandle) taskHandle, 
				 sampsPerChan, timeout, (bool32)groupMode, 
				dataPtr, data->Length, &sampsPerChanReadLocal, NULL);
			samplsPerChanRead = sampsPerChanReadLocal;
			return result;
		}

		int DAQmxCLIWrapper::WriteAnalogF64(IntPtr taskHandle,
			int32 sampsPerChan, bool autoStart, double timeout,
			ReadWriteFillMode groupMode, array<double>^ data,
			[Out] int% sampsPerChanWritten) {

			pin_ptr<float64> dataPtr = &data[0];
			int32 sampsPerChanWrittenLocal;
			int result = DAQmxWriteAnalogF64((TaskHandle)taskHandle,
				sampsPerChan, autoStart, timeout, (bool32) groupMode,
				dataPtr, &sampsPerChanWrittenLocal, NULL);
			sampsPerChanWritten = sampsPerChanWrittenLocal;
			return result;
		}


		int DAQmxCLIWrapper::ReadAnalogScalarF64(IntPtr taskHandle,
			double timeout, [Out] double% data) {

			float64 localValue;
			int result = DAQmxReadAnalogScalarF64((TaskHandle) taskHandle, 
				timeout, &localValue,NULL);
			data = localValue;
			return result;
		}


		int DAQmxCLIWrapper::WriteAnalogScalarF64(IntPtr taskHandle,
			bool autoStart, double timeout, double data) {

			return DAQmxWriteAnalogScalarF64((TaskHandle)taskHandle,
				autoStart, timeout, data, NULL);
		}

		int DAQmxCLIWrapper::ReadBinaryI16(IntPtr taskHandle,
			int32 sampsPerChan, double timeout,
			ReadWriteFillMode groupMode, array<int16>^ data,
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

		int DAQmxCLIWrapper::WriteBinaryI16(IntPtr taskHandle,
			int32 sampsPerChan, bool autoStart, double timeout,
			ReadWriteFillMode groupMode, array<int16>^ data,
			[Out] int% sampsPerChanWritten)
		{
			pin_ptr<int16> dataPtr = &data[0];
			int32 sampsPerChanWrittenLocal;
			int result = DAQmxWriteBinaryI16((TaskHandle)taskHandle,
				sampsPerChan, autoStart, timeout, (bool32)groupMode,
				dataPtr, &sampsPerChanWrittenLocal, NULL);
			sampsPerChanWritten = sampsPerChanWrittenLocal;
			return result;
		}

		int DAQmxCLIWrapper::ReadBinaryI32(IntPtr taskHandle,
			int32 sampsPerChan, double timeout,
			ReadWriteFillMode groupMode, array<int32>^ dat,
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

		int DAQmxCLIWrapper::WriteBinaryI32(IntPtr taskHandle,
			int32 sampsPerChan, bool autoStart, double timeout,
			ReadWriteFillMode groupMode, array<int32>^ dat,
			[Out] int% sampsPerChanWritten)
		{
			pin_ptr<int32> dataPtr = &dat[0];
			int32 sampsPerChanWrittenLocal;
			int result = DAQmxWriteBinaryI32((TaskHandle)taskHandle,
				sampsPerChan, autoStart, timeout, (bool32)groupMode,
				dataPtr, &sampsPerChanWrittenLocal, NULL);
			sampsPerChanWritten = sampsPerChanWrittenLocal;
			return result;
		}

		int DAQmxCLIWrapper::ReadBinaryUI16(IntPtr taskHandle,
			int32 sampsPerChan, double timeout,
			ReadWriteFillMode groupMode, array<uInt16>^ dat,
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

		int DAQmxCLIWrapper::WriteBinaryUI16(IntPtr taskHandle,
			int32 sampsPerChan, bool autoStart, double timeout,
			ReadWriteFillMode groupMode, array<uInt16>^ dat,
			[Out] int% sampsPerChanWritten)
		{
			pin_ptr<uInt16> dataPtr = &dat[0];
			int32 sampsPerChanWrittenLocal;
			int result = DAQmxWriteBinaryU16((TaskHandle)taskHandle,
				sampsPerChan, autoStart, timeout, (bool32)groupMode,
				dataPtr, &sampsPerChanWrittenLocal, NULL);
			sampsPerChanWritten = sampsPerChanWrittenLocal;
			return result;
		}

		int DAQmxCLIWrapper::ReadBinaryUI32(IntPtr taskHandle,
			int32 sampsPerChan, double timeout,
			ReadWriteFillMode groupMode, array<uInt32>^ dat,
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




		int DAQmxCLIWrapper::WriteBinaryUI32(IntPtr taskHandle,
			int32 sampsPerChan, bool autoStart, double timeout,
			ReadWriteFillMode groupMode, array<uInt32>^ dat,
			[Out] int% sampsPerChanWritten)
		{
			pin_ptr<uInt32> dataPtr = &dat[0];
			int32 sampsPerChanWrittenLocal;
			int result = DAQmxWriteBinaryU32((TaskHandle)taskHandle,
				sampsPerChan, autoStart, timeout, (bool32)groupMode,
				dataPtr, &sampsPerChanWrittenLocal, NULL);
			sampsPerChanWritten = sampsPerChanWrittenLocal;
			return result;
		}
		int DAQmxCLIWrapper::CreateCOPulseFrequencyChannel(
			IntPtr taskHandle, 
			String^ counter, 
			String^ nameToAssignToChannel, 
			int units, 
			int idleState, 
			double initialDelay, 
			double freq, 
			double dutyCycle) {

			char* nameToAssignToChannelChar = 
				StringToCharz(nameToAssignToChannel);
			char* counterChar = StringToCharz(counter);

			int result = DAQmxCreateCOPulseChanFreq(
				(TaskHandle)taskHandle,
				counterChar, 
				nameToAssignToChannelChar, 
				units, 
				idleState, 
				initialDelay, 
				freq, 
				dutyCycle);

			FreeCharz(nameToAssignToChannelChar);
			FreeCharz(counterChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateCOPulseChanTime(IntPtr taskHandle, 
			String^ counter, 
			String^ nameToAssignToChannel, 
			TimeUnits units,
			DioState idleState,
			double initialDelay, 
			double lowTime, 
			double highTime) {

			char* nameToAssignToChannelChar = 
				StringToCharz(nameToAssignToChannel);
			char* counterChar = StringToCharz(counter);
			int result = DAQmxCreateCOPulseChanTime((TaskHandle)taskHandle,
				counterChar, 
				nameToAssignToChannelChar, 
				(int) units, 
				(int) idleState, 
				initialDelay, 
				lowTime, 
				highTime);

			FreeCharz(nameToAssignToChannelChar);
			FreeCharz(counterChar);

			return result;
		};
		
		int DAQmxCLIWrapper::CreateCICountEdgesChan(IntPtr taskHandle, 
			String^ counter, 
			String^ nameToAssignToChannel, 
			int edge, 
			int initialCount, 
			int countDirection) {

			char* nameToAssignToChannelChar =
				StringToCharz(nameToAssignToChannel);
			char* counterChar = StringToCharz(counter);

			int result = DAQmxCreateCICountEdgesChan((TaskHandle)taskHandle,
				counterChar, 
				nameToAssignToChannelChar, 
				edge, 
				initialCount, 
				countDirection);

			FreeCharz(nameToAssignToChannelChar);
			FreeCharz(counterChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateCIFreqChan(IntPtr taskHandle, 
			String^ counter, String^ nameToAssignToChannel, double minVal, 
			double maxVal, int units, int edge, int measMethod, 
			double measTime, UInt32 divisor, String^ customScaleName) {

			char* nameToAssignToChannelChar = 
				StringToCharz(nameToAssignToChannel);
			char* counterChar = StringToCharz(counter);
			char* customScaleNameChar = StringToCharz(customScaleName);

			int result = DAQmxCreateCIFreqChan((TaskHandle)taskHandle, counterChar,
				nameToAssignToChannelChar, minVal, maxVal, units, edge, 
				measMethod, measTime, divisor, customScaleNameChar);

			FreeCharz(nameToAssignToChannelChar);
			FreeCharz(counterChar);
			FreeCharz(customScaleNameChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateCIPeriodChan(IntPtr taskHandle, 
			String^ counter, String^ nameToAssignToChannel, double minVal, 
			double maxVal, int units, int edge, int measMethod, 
			double measTime, UInt32 divisor, String^ customScaleName) {

			char* nameToAssignToChannelChar = 
				StringToCharz(nameToAssignToChannel);
			char* counterChar = StringToCharz(counter);
			char* customScaleNameChar = StringToCharz(customScaleName);

			int result = DAQmxCreateCIPeriodChan((TaskHandle)taskHandle,
				counterChar, nameToAssignToChannelChar, minVal, maxVal, 
				units, edge, measMethod, measTime, divisor, 
				customScaleNameChar);
			
			FreeCharz(nameToAssignToChannelChar);
			FreeCharz(counterChar);
			FreeCharz(customScaleNameChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateCISemiPeriodChan(IntPtr taskHandle, 
			String^ counter, String^ nameToAssignToChannel, double minVal, 
			double maxVal, int units, String^ customScaleName){

			char* nameToAssignToChannelChar = 
				StringToCharz(nameToAssignToChannel);
			char* counterChar = StringToCharz(counter);
			char* customScaleNameChar = StringToCharz(customScaleName);

			int result = DAQmxCreateCISemiPeriodChan((TaskHandle)taskHandle,
				counterChar, nameToAssignToChannelChar, 
				minVal, maxVal, units, customScaleNameChar);

			FreeCharz(nameToAssignToChannelChar);
			FreeCharz(counterChar);
			FreeCharz(customScaleNameChar);

			return result;
		};

		int DAQmxCLIWrapper::LoadTask([Out] IntPtr% taskHandle, 
									  String^ taskName) {
			
			char* taskNameChar = StringToCharz(taskName);
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			int result = DAQmxLoadTask(taskNameChar, &taskHandleLocal);
			taskHandle = (IntPtr)taskHandleLocal;
			FreeCharz(taskNameChar);
			return result;
		};

		int DAQmxCLIWrapper::AddGlobalChansToTask(IntPtr taskHandle, 
				String^ channelNames) {

			char* channelNamesChar = StringToCharz(channelNames);
			int result = DAQmxAddGlobalChansToTask((TaskHandle)taskHandle,
												   channelNamesChar);
			FreeCharz(channelNamesChar);
			return result;
		};

		int DAQmxCLIWrapper::IsTaskDone(IntPtr taskHandle, 
										[Out] bool% isTaskDone) {
			 
			bool32 isTaskDoneLocal;
			int result = DAQmxIsTaskDone((TaskHandle)taskHandle, &isTaskDoneLocal);
			isTaskDone = (bool) isTaskDoneLocal;
			return result;
		};
		
		int DAQmxCLIWrapper::GetNthTaskChannel(IntPtr taskHandle,	
									uInt32 index, [Out] String^% buffer) {

			char bufferChar[ErrorBufferSize];
			int result = DAQmxGetNthTaskChannel((TaskHandle)taskHandle,
									index, bufferChar, ErrorBufferSize);
			buffer = gcnew String(bufferChar);
			return result;
		};

		int DAQmxCLIWrapper::GetNthTaskDevice(IntPtr taskHandle,
									uInt32 index, [Out] String^% buffer) {
	
			char bufferChar[ErrorBufferSize];
			int result = DAQmxGetNthTaskDevice((TaskHandle)taskHandle,
								index, bufferChar, ErrorBufferSize);
			buffer = gcnew String(bufferChar);
			return result;
		};

		int DAQmxCLIWrapper::ExportSignal(IntPtr taskHandle, 
			ExportableSignal signal, String^ outputTerminal) {

			char * cString = StringToCharz(outputTerminal);
			int result = DAQmxExportSignal((TaskHandle) taskHandle, 
				(int)signal, cString);
			FreeCharz(cString);
			return result;
		}

		int DAQmxCLIWrapper::TotalSamplesGenerated(IntPtr taskHandle,
												   [Out] UInt64% data) {

			uInt64 dataLocal;
			int result = DAQmxGetWriteTotalSampPerChanGenerated(
									    (TaskHandle)taskHandle, &dataLocal);
			data = dataLocal;
			return result;
		}

		int DAQmxCLIWrapper::TotalSamplesRead(IntPtr taskHandle, 
											  [Out] UInt64% data) {

			uInt64 dataLocal;
			int result = DAQmxGetReadTotalSampPerChanAcquired(
										(TaskHandle)taskHandle, &dataLocal);
			data = dataLocal;
			return result;
		}	


		int DAQmxCLIWrapper::GetCOPulseTerm(IntPtr taskHandle, String^ channel, 
			[Out] String^% data, uInt32 bufferSize) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);
			char* dataBuffer = new char[bufferSize];

			int result = DAQmxGetCOPulseTerm(taskHandleLocal, channelChar, 
				dataBuffer, bufferSize);

			data = gcnew String(dataBuffer);

			FreeCharz(channelChar);
			delete[] dataBuffer;

			return result;
		}

		int DAQmxCLIWrapper::SetCOPulseTerm(IntPtr taskHandle, 
			String^ channel, String^ data) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);
			char* dataChar = StringToCharz(data);

			int result = DAQmxSetCOPulseTerm(taskHandleLocal, 
				channelChar, dataChar);

			FreeCharz(channelChar);
			FreeCharz(dataChar);

			return result;
		}

		int DAQmxCLIWrapper::ResetCOPulseTerm(IntPtr taskHandle, String^ channel) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			int result = DAQmxResetCOPulseTerm(taskHandleLocal, channelChar);

			FreeCharz(channelChar);

			return result;
		}


		int DAQmxCLIWrapper::GetCOPulseDone(IntPtr taskHandle, 
			String^ channel, [Out] bool% data)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			// Prepare the output parameter
			bool32 dataUnmanaged;

			// Call the DAQmx function
			int32 result = DAQmxGetCOPulseDone(taskHandleLocal, 
				channelChar, &dataUnmanaged);

			// Convert the unmanaged bool32 to managed bool
			data = dataUnmanaged != 0;

			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::GetCIPrescaler(IntPtr taskHandle, 
			String^ channel, [Out] uInt32% data)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);
			uInt32 dataUnmanaged;

			int32 result = DAQmxGetCIPrescaler(taskHandleLocal, 
				channelChar, &dataUnmanaged);
			data = dataUnmanaged;
			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::SetCIPrescaler(IntPtr taskHandle, 
			String^ channel, uInt32 data)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			int32 result =  DAQmxSetCIPrescaler(taskHandleLocal, 
				channelChar, data);

			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::ResetCIPrescaler(IntPtr taskHandle, 
			String^ channel)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			int32 result = DAQmxResetCIPrescaler(taskHandleLocal, channelChar);

			FreeCharz(channelChar);

			return result;
		}

		int DAQmxCLIWrapper::GetCICount(IntPtr taskHandle, String^ channel, 
			[Out] uInt32% data)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			uInt32 dataUnmanaged;

			int32 result = DAQmxGetCICount(taskHandleLocal, 
				channelChar, &dataUnmanaged);
			data = dataUnmanaged;
			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::GetCIOutputState(IntPtr taskHandle, 
			String^ channel, [Out] int32% data)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);
			int32 dataUnmanaged;

			int32 result = DAQmxGetCIOutputState(taskHandleLocal, 
				channelChar, &dataUnmanaged);
			data = dataUnmanaged;
			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::GetCITCReached(IntPtr taskHandle, 
			String^ channel, [Out] bool% data)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);
			bool32 dataUnmanaged;

			int32 result = DAQmxGetCITCReached(taskHandleLocal, 
				channelChar, &dataUnmanaged);
			data = dataUnmanaged != 0;

			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::GetCICtrTimebaseMasterTimebaseDiv(IntPtr taskHandle, 
			String^ channel, [Out] uInt32% data)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);
			uInt32 dataUnmanaged;

			int32 result = DAQmxGetCICtrTimebaseMasterTimebaseDiv(taskHandleLocal, 
				channelChar, &dataUnmanaged);
			data = dataUnmanaged;

			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::SetCICtrTimebaseMasterTimebaseDiv(IntPtr taskHandle, 
			String^ channel, uInt32 data)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			int32 result = DAQmxSetCICtrTimebaseMasterTimebaseDiv(taskHandleLocal, 
				channelChar, data);
			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::ResetCICtrTimebaseMasterTimebaseDiv(IntPtr taskHandle, 
			String^ channel)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			int32 result = DAQmxResetCICtrTimebaseMasterTimebaseDiv(taskHandleLocal, 
				channelChar);
			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::GetCIPulseTimeTerm(IntPtr taskHandle, String^ channel, 
			[Out] String^% data, uInt32 bufferSize)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);
			char* dataUnmanaged = new char[bufferSize];

			int32 result = DAQmxGetCIPulseTimeTerm(taskHandleLocal, 
				channelChar, dataUnmanaged, bufferSize);
			data = gcnew String(dataUnmanaged);

			FreeCharz(channelChar);
			delete[] dataUnmanaged;
			return result;
		}

		int DAQmxCLIWrapper::SetCIPulseTimeTerm(IntPtr taskHandle, 
			String^ channel, String^ terminal)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			char* terminalStr = StringToCharz(terminal);

			int32 result =  DAQmxSetCIPulseTimeTerm(taskHandleLocal, 
				channelChar, terminalStr);

			FreeCharz(channelChar);
			FreeCharz(terminalStr);

			return result;
		}

		int DAQmxCLIWrapper::ResetCIPulseTimeTerm(IntPtr taskHandle, 
			String^ channel)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			int32 result =  DAQmxResetCIPulseTimeTerm(taskHandleLocal, 
				channelChar);

			FreeCharz(channelChar);

			return result;
		}

		int DAQmxCLIWrapper::GetCIPulseTimeTermCfg(IntPtr taskHandle, 
			String^ channel, [Out] int32% data)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);
			int32 dataUnmanaged;

			int32 result = DAQmxGetCIPulseTimeTermCfg(taskHandleLocal, 
				channelChar, &dataUnmanaged);
			data = dataUnmanaged;
			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::SetCIPulseTimeTermCfg(IntPtr taskHandle, 
			String^ channel, int32 data)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			int32 result = DAQmxSetCIPulseTimeTermCfg(taskHandleLocal, 
				channelChar, data);
			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::ResetCIPulseTimeTermCfg(IntPtr taskHandle, 
			String^ channel)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			int32 result = DAQmxResetCIPulseTimeTermCfg(taskHandleLocal, channelChar);
			FreeCharz(channelChar);
			return result;
		}

		int DAQmxCLIWrapper::GetCIPulseFreqTerm(IntPtr taskHandle, 
			String^ channel, [Out] String^% data, uInt32 bufferSize)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);
			char* dataUnmanaged = new char[bufferSize];

			int32 result = DAQmxGetCIPulseFreqTerm(taskHandleLocal, 
				channelChar, dataUnmanaged, bufferSize);
			data = gcnew String(dataUnmanaged);

			FreeCharz(channelChar);
			delete[] dataUnmanaged;
			return result;
		}

		int DAQmxCLIWrapper::SetCIPulseFreqTerm(IntPtr taskHandle, 
			String^ channel, String^ terminal)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);
			char* terminalChar = StringToCharz(terminal);

			int32 result = DAQmxSetCIPulseFreqTerm(taskHandleLocal, 
				channelChar, terminalChar);

			FreeCharz(channelChar);
			FreeCharz(terminalChar);
			return result;
		}

		int DAQmxCLIWrapper::ResetCIPulseFreqTerm(IntPtr taskHandle, 
			String^ channel)
		{
			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;
			char* channelChar = StringToCharz(channel);

			int32 result = DAQmxResetCIPulseFreqTerm(taskHandleLocal, 
				channelChar);

			FreeCharz(channelChar);

			return result;
		}



		int DAQmxCLIWrapper::GetSystemInfoAttribute(int attribute, 
			[Out] IntPtr% value) {

			void* valueUnmanaged;
			int32 result = DAQmxGetSystemInfoAttribute(attribute, 
				&valueUnmanaged);

			value = IntPtr(valueUnmanaged);
			return result;
		}

		int DAQmxCLIWrapper::SetDigitalPowerUpStates(String^ deviceName, 
			String^ channelNames, int state) {

			char* deviceNameChar = StringToCharz(deviceName);
			char* channelNamesChar = StringToCharz(channelNames);

			int result = DAQmxSetDigitalPowerUpStates(deviceNameChar, 
				channelNamesChar, state);

			FreeCharz(deviceNameChar);
			FreeCharz(channelNamesChar);
			return result;
		}

		int DAQmxCLIWrapper::GetDigitalPowerUpStates(String^ deviceName, 
			String^ channelName, [Out] int% state) {

			char* deviceNameChar = StringToCharz(deviceName);
			char* channelNameChar = StringToCharz(channelName);
			int32 stateUnmanaged;

			int32 result = DAQmxGetDigitalPowerUpStates(deviceNameChar, 
				channelNameChar, &stateUnmanaged);

			state = stateUnmanaged;
			FreeCharz(deviceNameChar);
			FreeCharz(channelNameChar);
			return result;
		}

		int DAQmxCLIWrapper::SetDigitalPullUpPullDownStates(String^ deviceName, 
			String^ channelName, int state) {

			char* deviceNameChar = StringToCharz(deviceName);
			char* channelNameChar = StringToCharz(channelName);

			int result = DAQmxSetDigitalPullUpPullDownStates(deviceNameChar, 
				channelNameChar, state);

			FreeCharz(deviceNameChar);
			FreeCharz(channelNameChar);
			return result;
		}

		int DAQmxCLIWrapper::GetDigitalPullUpPullDownStates(String^ deviceName, 
			String^ channelName, [Out] int% state) {

			char* deviceNameChar = StringToCharz(deviceName);
			char* channelNameChar = StringToCharz(channelName);
			int32 stateUnmanaged;

			int32 result = DAQmxGetDigitalPullUpPullDownStates(deviceNameChar, 
				channelNameChar, &stateUnmanaged);

			state = stateUnmanaged;
			FreeCharz(deviceNameChar);
			FreeCharz(channelNameChar);
			return result;
		}

		int DAQmxCLIWrapper::SetAnalogPowerUpStates(String^ deviceName, 
			String^ channelNames, 
			double state, 
			int channelType) {

			char* deviceNameChar = StringToCharz(deviceName);
			char* channelNamesChar = StringToCharz(channelNames);

			int result = DAQmxSetAnalogPowerUpStates(deviceNameChar, 
				channelNamesChar, state, channelType);
			
			FreeCharz(deviceNameChar);
			FreeCharz(channelNamesChar);
			
			return result;
		}

		int DAQmxCLIWrapper::SetAnalogPowerUpStatesWithOutputType(
			String^ channelNames, 
			array<double>^ stateArray, 
			array<AnalogChannelType>^ channelTypeArray) {

			uInt32 arraySize = stateArray->Length;

			array <int32>^ channelTypeArrayInt32 = gcnew array<int32>(arraySize);

			for (int i = 0; i < channelTypeArray->Length; i++) {

				channelTypeArrayInt32[i] = (int32)channelTypeArray[i];
			}

			char* channelNamesChar = StringToCharz(channelNames);

			pin_ptr<double> stateArrayPtr = &stateArray[0];
			pin_ptr<int32> channelTypeArrayPtr = &channelTypeArrayInt32[0];

			int result = DAQmxSetAnalogPowerUpStatesWithOutputType(
				channelNamesChar, 
				stateArrayPtr, 
				channelTypeArrayPtr, 
				arraySize);

			FreeCharz(channelNamesChar);
			return result;
		}

		int DAQmxCLIWrapper::GetAnalogPowerUpStates(String^ deviceName, 
			String^ channelName, [Out] double% state, int channelType) {
			char* deviceNameChar = StringToCharz(deviceName);
			char* channelNameChar = StringToCharz(channelName);
			float64 stateUnmanaged;
			int32 result = DAQmxGetAnalogPowerUpStates(deviceNameChar, 
				channelNameChar, &stateUnmanaged, channelType);
			state = stateUnmanaged;
			FreeCharz(deviceNameChar);
			FreeCharz(channelNameChar);
			return result;
		}

		int DAQmxCLIWrapper::GetAnalogPowerUpStatesWithOutputType(String^ channelNames, 
			array<double>^ stateArray, 
			array<AnalogChannelType>^ channelTypeArray,
			[Out] uInt32% arraySize) {

			char* channelNamesChar = StringToCharz(channelNames);

			pin_ptr<double> stateArrayPtr = &stateArray[0];
			auto v = (int32)channelTypeArray[0];
			pin_ptr<int32> channelTypeArrayPtr = &v;

			uInt32 arraySizeUnmanaged;
			int32 result = DAQmxGetAnalogPowerUpStatesWithOutputType(channelNamesChar, 
				stateArrayPtr, &v, &arraySizeUnmanaged);

			arraySize = arraySizeUnmanaged;

			FreeCharz(channelNamesChar);
			return result;
		}

		int DAQmxCLIWrapper::SetDigitalLogicFamilyPowerUpState(String^ deviceName, int logicFamily) {
			char* deviceNameChar = StringToCharz(deviceName);
			int result = DAQmxSetDigitalLogicFamilyPowerUpState(deviceNameChar, logicFamily);
			FreeCharz(deviceNameChar);
			return result;
		}

		int DAQmxCLIWrapper::GetDigitalLogicFamilyPowerUpState(String^ deviceName, [Out] int% logicFamily) {
			char* deviceNameChar = StringToCharz(deviceName);
			int32 logicFamilyUnmanaged;
			int32 result = DAQmxGetDigitalLogicFamilyPowerUpState(deviceNameChar, &logicFamilyUnmanaged);
			logicFamily = logicFamilyUnmanaged;
			FreeCharz(deviceNameChar);
			return result;
		}

		int DAQmxCLIWrapper::AddNetworkDevice(String^ IPAddress, String^ deviceName, bool attemptReservation, double timeout, [Out] String^% deviceNameOut, uInt32 deviceNameOutBufferSize) {
			char* IPAddressChar = StringToCharz(IPAddress);
			char* deviceNameChar = StringToCharz(deviceName);
			char* deviceNameOutUnmanaged = new char[deviceNameOutBufferSize];

			int32 result = DAQmxAddNetworkDevice(IPAddressChar, deviceNameChar, attemptReservation, timeout, deviceNameOutUnmanaged, deviceNameOutBufferSize);
			deviceNameOut = gcnew String(deviceNameOutUnmanaged);

			delete[] deviceNameOutUnmanaged;
			FreeCharz(IPAddressChar);
			FreeCharz(deviceNameChar);
			return result;
		}

		int DAQmxCLIWrapper::DeleteNetworkDevice(String^ deviceName) {
			char* deviceNameChar = StringToCharz(deviceName);
			int result = DAQmxDeleteNetworkDevice(deviceNameChar);
			FreeCharz(deviceNameChar);
			return result;
		}

		int DAQmxCLIWrapper::ReserveNetworkDevice(String^ deviceName, bool overrideReservation) {
			char* deviceNameChar = StringToCharz(deviceName);
			int result = DAQmxReserveNetworkDevice(deviceNameChar, overrideReservation);
			FreeCharz(deviceNameChar);
			return result;
		}

		int DAQmxCLIWrapper::UnreserveNetworkDevice(String^ deviceName) {
			char* deviceNameChar = StringToCharz(deviceName);
			int result = DAQmxUnreserveNetworkDevice(deviceNameChar);
			FreeCharz(deviceNameChar);
			return result;
		}


		int DAQmxCLIWrapper::WaitUntilTaskDone(IntPtr taskHandle,
			double timeToWait) {
			return DAQmxWaitUntilTaskDone((TaskHandle)taskHandle,
				timeToWait);
		}


		int DAQmxCLIWrapper::ConfigureStartTrigger(IntPtr taskHandle,
			String^ triggerSource, ActiveEdge activeEdge) {

			char* triggerSourceChar = StringToCharz(triggerSource);

			int result = DAQmxCfgDigEdgeStartTrig((TaskHandle)taskHandle,
				triggerSourceChar, (int)activeEdge);
			FreeCharz(triggerSourceChar);
			return result;
		}
	}
}
