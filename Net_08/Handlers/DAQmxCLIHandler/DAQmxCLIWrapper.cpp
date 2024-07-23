
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
			double minVal, double maxVal, int units, String^ customScaleName) {

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
			String^ lines, String^ nameToAssignToLines, int lineGrouping) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;

			char* nameToAssignToLinesChar = 
				GenerateCString(nameToAssignToLines);
			char* linesChar = GenerateCString(lines);


			int result = DAQmxCreateDOChan(taskHandleLocal, linesChar, 
				nameToAssignToLinesChar, lineGrouping);
			
			FreeCString(nameToAssignToLinesChar);
			FreeCString(linesChar);

			return result;
		};

		int DAQmxCLIWrapper::CreateDIChannel(long long int taskHandle, 
			String^ lines, String^ nameToAssignToLines, int lineGrouping) {

			TaskHandle taskHandleLocal = (TaskHandle)taskHandle;

			char* nameToAssignToLinesChar = GenerateCString(nameToAssignToLines);
			char* linesChar = GenerateCString(lines);

			int result = DAQmxCreateDIChan(taskHandleLocal, 
				linesChar, nameToAssignToLinesChar, lineGrouping);

			FreeCString(nameToAssignToLinesChar);
			FreeCString(linesChar);

			return result;
		};

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


	}



}