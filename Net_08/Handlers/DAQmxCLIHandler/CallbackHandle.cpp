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
#include "CallbackHandle.h"

namespace Grumpy {

	namespace DAQmxCLIWrap {


		CallbackHandle::CallbackHandle(IntPtr taskHandle, DAQmxDoneCallbackDelegate^ del,
			DAQmxEveryNSamplesCallbackDelegate^ evryNSamplesDel,
			EventType evetType, Object^ data, int nSamples) {

			_taskHandle = taskHandle;
			_managedDoneDelegate = del;
			_managedEveryNSamplesDelegate = evryNSamplesDel;

			_managedDataPointer = data;
			_nSamples = nSamples;

			_eventType = evetType;

			_lastError = String::Empty;

			try {
				_gcCallbackHandle = GCHandle::Alloc(_managedDoneDelegate);
				if (data != nullptr) {
					_managedDataPointer = data;
					_gcDataHandle = GCHandle::Alloc(_managedDataPointer);
				}
				int r = -1;
				switch (_eventType)
				{
				case EventType::Done:
					r = _RegisterDoneEvent();
					break;
				case EventType::EveryNSamplesReceived:
					r = _RegisterEveryNSamplesEvent(true);
					break;
				case EventType::EveryNSamplesTransferred:
					r = _RegisterEveryNSamplesEvent(false);
					break;
				default:
					break;
				}

				if (r == 0) {
					_registered = true;
				}
				else {
					_FreeResources();
					_registered = false;
				}
			}
			catch (Exception^ ex) {
				_FreeResources();
				_lastError = gcnew String(ex->Message);
				delete(ex);
				_registered = false;
			}
		}


		CallbackHandle::~CallbackHandle() {
			_FreeResources();
		}

		bool CallbackHandle::IsRegistered() {
			return _registered;
		}


		int CallbackHandle::_RegisterDoneEvent() {

			if (!_registered) {

				int r = 0;

				r = DAQmxRegisterDoneEvent((TaskHandle)_taskHandle,
					0,
					static_cast<DAQmxDoneEventCallbackPtr>(_GetFunctionPointer()),
					_managedDataPointer != nullptr ?
					GCHandle::ToIntPtr(_gcDataHandle).ToPointer() :
					NULL);


				if (r != 0)
				{
					_FreeResources();
				}
				_registered = (r == 0);
				return r;
			}

			return 0;
		}


		int CallbackHandle::_RegisterEveryNSamplesEvent(bool read) {

			if (!_registered) {

				int r = 0;

				r = DAQmxRegisterEveryNSamplesEvent(
					(TaskHandle)_taskHandle,
					read ? DAQmx_Val_Acquired_Into_Buffer : DAQmx_Val_Transferred_From_Buffer,
					_nSamples,
					0,
					static_cast<DAQmxEveryNSamplesEventCallbackPtr>(_GetFunctionPointer()),
					_managedDataPointer != nullptr ?
					GCHandle::ToIntPtr(_gcDataHandle).ToPointer() :
					NULL);


				if (r != 0) {
					_FreeResources();
				}
				_registered = (r == 0);
				return r;
			}

			return 0;
		}


		void* CallbackHandle::_GetFunctionPointer()
		{
			if ((_eventType == EventType::Done && _managedDoneDelegate == nullptr)
				|| (_eventType != EventType::Done && _managedEveryNSamplesDelegate == nullptr)) {
				return NULL;
			}

			if (_eventType == EventType::Done)
			{
				return Marshal::GetFunctionPointerForDelegate(_managedDoneDelegate).ToPointer();
			}
			else
			{
				return Marshal::GetFunctionPointerForDelegate(_managedEveryNSamplesDelegate).ToPointer();
			}

		}


		void CallbackHandle::_FreeResources() {
			_managedDoneDelegate = nullptr;
			_managedDoneDelegate = nullptr;
			_lastError = nullptr;

			if (_gcCallbackHandle.IsAllocated) {
				_gcCallbackHandle.Free();
			}
			if (_gcDataHandle.IsAllocated) {
				_gcDataHandle.Free();
			}
			GC::Collect();
		}
	}
}
