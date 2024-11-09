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

#include "CallbackService.h"

using namespace System;


namespace Grumpy {

	namespace DAQmxNetApi {


		CallbackHandle^ CallbackService::RegisterDoneEvent(IntPtr taskHandle,
			DAQmxDoneCallbackDelegate^ del, Object^ data) {

			auto r = gcnew CallbackHandle(taskHandle, del, nullptr,
				EventType::Done, data, -1);

			return (r->IsRegistered() == true) ? r : nullptr;
		}

		CallbackHandle^ CallbackService::RegisterNSamplesWrittenEvent(IntPtr taskHandle,
			DAQmxEveryNSamplesCallbackDelegate^ del,
			int nSamples, Object^ data) {

			auto r = gcnew CallbackHandle(taskHandle, nullptr, del,
				EventType::EveryNSamplesTransferred, data, nSamples);
			return (r->IsRegistered() == true) ? r : nullptr;
		}

		CallbackHandle^ CallbackService::RegisterNSamplesReadEvent(IntPtr taskHandle,
			DAQmxEveryNSamplesCallbackDelegate^ del,
			int nSamples, Object^ data) {

			auto r = gcnew CallbackHandle(taskHandle, nullptr, del,
				EventType::EveryNSamplesReceived, data, nSamples);
			return (r->IsRegistered() == true) ? r : nullptr;
		}
	}
}

