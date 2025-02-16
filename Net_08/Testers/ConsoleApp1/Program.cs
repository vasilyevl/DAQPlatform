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

using Grumpy.DAQFramework.Drivers.MMTimer;
using System.Diagnostics;
using DAQFramework.Utilities;

namespace ConsoleApp1
{
    internal class Program {

        static List<double>? data;
        static uint highResolution = 1;
        static void Main(string[] args) {
          
               TestThreadingTimer(timerInterval: 33);
               TestMultimediaTimer(timerInterval: 1);

            return;
        }

        private static void TestMultimediaTimer( uint timerInterval = 10) {

            data = new List<double>();

            var timer = new HighResTimer(
                periodMs: timerInterval,
                resolutionMs: highResolution,
                userCallback: null,
                operatingMode: TimerMode.Periodic,
                autoStart: false );

            timer.TimerEvent += EventHandler!;
            
            timer.Start();

                Console.WriteLine("Click any key to stop.");
                Console.ReadKey();

                timer.Stop();

            ReportGenerator.TimingReport(data, true);          
        }

        private static void TestThreadingTimer(int timerInterval = 100) 
        {
            data = new List<double>();

            Console.WriteLine("Console Startig Threading Timer");
            Stopwatch s = new Stopwatch();
            
            using (var timer = new System.Timers.Timer()) {

                timer.Interval = timerInterval;
                
                timer.Elapsed += ((o, e) => {

                    data.Add(s.ElapsedTicks/10000.0);
                    return;
                });
                
                timer.AutoReset = true;       
                timer.Enabled = true;

                s.Start();

                Console.WriteLine("Click any key to stop.");
                Console.ReadKey();

                ReportGenerator.TimingReport(data, true);
            }
        }

        private static void EventHandler(object sender,
            TimerEventArgs e) {

            data.Add(e.Time);
        }
    } 
}
