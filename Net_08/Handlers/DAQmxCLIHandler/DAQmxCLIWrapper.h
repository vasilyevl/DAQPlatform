#pragma once

using namespace System;
using namespace System::Runtime::InteropServices;
namespace Grumpy{
	namespace DAQmxCLIWrap {

		#include <NIDAQmx.h>

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
				[Out] long long int% taskHandle);

			static String^ GetErrorDescription(int errorCode);

			inline static int StartTask(long long int taskHandle) {
				return DAQmxStartTask((TaskHandle)taskHandle);
			}

			static int StopTask(long long int taskHandle) {
				return DAQmxStopTask((TaskHandle)taskHandle);
			}


			inline static int ClearTask([Out] long long int% taskHandle) {

				Int32 r = DAQmxClearTask((TaskHandle)taskHandle);
				if (r == 0) {
					taskHandle = 0LL;
				}
				return r;
			}

			
			inline static int DisposeTask([Out] long long int% taskHandle) {
					return ClearTask(taskHandle);
			}

			inline static int TaskControl(long long int taskHandle, 
				int action) {
				return DAQmxTaskControl((TaskHandle)taskHandle, action);
			}

			inline static int WaitUntilTaskDone(long long int taskHandle, 
				double timeToWait){
					return DAQmxWaitUntilTaskDone((TaskHandle)taskHandle, 
						timeToWait);
			}
		
			static int CreateAIVoltageChannel(long long int taskHandle, 
				String^ physicalChannel, String^ nameToAssignToChannel, 
				int terminalConfig, double minVal, double maxVal, int units, 
				String^ customScaleName);

			static int CreateAOVoltageChannel(long long int taskHandle, 
				String^ physicalChannel, String^ nameToAssignToChannel, 
				double minVal, double maxVal, int units, 
				String^ customScaleName);

			static int CreateDIChannel(long long int taskHandle, String^ lines, 
				String^ nameToAssignToLines, int lineGrouping);
			static int CreateDOChannel(long long int taskHandle, String^ lines, 
				String^ nameToAssignToLines, int lineGrouping);

			

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
