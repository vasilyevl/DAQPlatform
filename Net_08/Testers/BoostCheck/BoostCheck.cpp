// BoostCheck.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <boost/version.hpp>
#include <boost/asio.hpp>
#include <iostream>
#include <chrono>



int main()
{
	int milliseconds = 100;    
    std::cout << "Hello World!\n";

    std::cout << "Boost version: " << BOOST_LIB_VERSION << std::endl;

    boost::asio::io_context* _ioContext;  // Correct usage is io_context, not io_service
    boost::asio::steady_timer* _timer;
    std::chrono::steady_clock::time_point _startTime;
    std::chrono::steady_clock::time_point _stopTime;

	std::chrono::high_resolution_clock::time_point t1;
	std::chrono::high_resolution_clock::time_point t2;

    _ioContext = new boost::asio::io_context();  // io_context is the correct type to use in modern Boost
    _timer = new boost::asio::steady_timer(*_ioContext);

    std::cout << "High resolution timer created... " << std::endl;
	for (int i = 0; i < 10; i++)
	{
		std::cout << "Timer set to expire in " << milliseconds << " milliseconds... " << std::endl;
		t1 = std::chrono::steady_clock::now();
		_timer->expires_from_now(std::chrono::microseconds(milliseconds*1000));
		_timer->wait();
		t2 = std::chrono::steady_clock::now();

		std::cout << "Timer expired in " << ((float)std::chrono::duration_cast<std::chrono::microseconds>(t2 - t1).count()) / 1000.0 << " ms." << std::endl;
	}

	delete _timer;
	delete _ioContext;

	std::cout << "Timer deleted... " << std::endl;

	std::cout << "Press any key to exit... " << std::endl;
	std::cin.get();

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
