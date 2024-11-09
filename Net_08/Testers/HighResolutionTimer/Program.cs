using Haukcode.HighResolutionTimer;


namespace HighResolutionTimer
{
    internal class Program
    {
        static void Main(string[] args) {

            Console.WriteLine("Hello, World!");

            // Create a new instance of the HighResolutionTimer class
            Haukcode.HighResolutionTimer.HighResolutionTimer timer = 
                new Haukcode.HighResolutionTimer.HighResolutionTimer();

            timer.SetPeriod(100); // Set the period to 100ms (0.1 second)

            // Start the timer
            DateTime start = DateTime.Now;
            DateTime trigger = DateTime.Now;
            timer.Start();

            while (true) {
                // Wait for the timer to elapse
                timer.WaitForTrigger();
                trigger = DateTime.Now;
                // Print the current time
                Console.WriteLine($"Trigger received: " +
                    $"{(trigger - start).TotalMilliseconds}");
                start = trigger;
            }

        }
    }
}
