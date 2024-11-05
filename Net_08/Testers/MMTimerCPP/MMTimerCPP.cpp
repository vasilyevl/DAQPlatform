// MMTimerCPP.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <Windows.h>
#include <mmsystem.h>
#include <chrono>

#pragma comment(lib, "winmm.lib")
static int counter = 0; 
static int samples = 5000;
static int* arr;

void CALLBACK TimerProc(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, 
    DWORD_PTR dw1, DWORD_PTR dw2) {

    auto now = std::chrono::high_resolution_clock::now();
    auto since_epoch = now.time_since_epoch();
    auto millis = std::chrono::
        duration_cast<std::chrono::microseconds>(since_epoch);

    if (counter < (samples)) {

		arr[counter] = (int)millis.count() ;
		counter++;
	}
}


int main()
{
    arr = new int[samples];
    counter = 0;

    UINT timerId;
    UINT resolution = 1; // 1ms resolution

    if (timeBeginPeriod(resolution) == TIMERR_NOERROR) {
        timerId = timeSetEvent(1, resolution, TimerProc, 
            0, TIME_PERIODIC); // 1000ms interval

        if (timerId == 0) {
            std::cerr << "Failed to create timer." << std::endl;
        }
        else {
            std::cout << "Timer started." << std::endl;
            while (counter < (samples -1))
            {
                Sleep(100);
            }

            timeKillEvent(timerId);
            timeEndPeriod(resolution);
        }
    }
    else {
        std::cerr << "Failed to set timer resolution." << std::endl;
    }

	for (int i = 1; i < samples; i++)
	{
		std::cout << i+1 << "\t" << (arr[i] -arr[i-1])/1000.0 << std::endl;
	}

    return 0;
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
