using Grumpy.SDAQFramework.MathUtilities;

using System.Text;
using System.Runtime.CompilerServices;

namespace Grumpy.SDAQFramework.Utilities.Testing
{
    public static class TestUtilities
    {
        public static bool AssertOrNotify(bool condition, 
            string testName,
            bool accepableFalse = false, 
            string message = "") {
            
            if (condition) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [PASS] {testName}");
            }
            else {
                if (accepableFalse) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  [Info] {testName}");
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [ERROR] {testName} " +
                        $"{(string.IsNullOrWhiteSpace(message) ? "" : $"- {message}")}");
                }
            }

            Console.ResetColor();
            return condition;
        }
    

       // Overload for the new signature requested earlier
        public static bool AssertOrNotify( bool condition,
            string? testName = null,
            string? successMessage = null,
            string? failureMessage = null,
            bool accepableFalse = false)
           {

            if (string.IsNullOrEmpty(failureMessage) && !string.IsNullOrEmpty(successMessage)) {
                failureMessage = successMessage;
            }

            string prefix = string.IsNullOrWhiteSpace(testName)
                ? ""
                : $"[{testName}] ";

            if (condition) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [PASS] {prefix}{successMessage ?? ""}");
            }
            else {
                if (accepableFalse) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  [Info] {prefix} {failureMessage?? ""}");
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [ERROR] {prefix} {failureMessage ?? ""}");
                }
            }
            Console.ResetColor();
            return condition;
        }

        public static bool AssertOrNotify(
            Action testMethod,
            string? successMessage = null,
            string? failureMessage = null,
            bool accepableFalse = false) {
            string testName = testMethod.Method.Name;
            try {
                if (string.IsNullOrEmpty(failureMessage) && !string.IsNullOrEmpty(successMessage)) {
                    failureMessage = successMessage;
                }

                testMethod();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [PASS] [{testName}] {successMessage ?? ""}");
                Console.ResetColor();
                return true;
            }
            catch (Exception ex) {
                if (accepableFalse) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  [Info] [{testName}] {failureMessage ?? ex.Message}");
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [ERROR] [{testName}] {failureMessage ?? ex.Message}");
                }
                Console.ResetColor();
                return false;
            }
        }

        public static bool AssertOrNotify<T>(
            Func<T> testMethod,
            Func<T, bool> condition,
            string? successMessage = null,
            string? failureMessage = null,
            bool accepableFalse = false) {
            string testName = testMethod.Method.Name;
            try {
                if (string.IsNullOrEmpty(failureMessage) && !string.IsNullOrEmpty(successMessage)) {
                    failureMessage = successMessage;
                }

                T result = testMethod();
                if (condition(result)) {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  [PASS] [{testName}] {successMessage ?? ""}");
                    Console.ResetColor();
                    return true;
                }
                else {
                    if (accepableFalse) {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  [Info] [{testName}] {failureMessage ?? ""}");
                    }
                    else {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  [ERROR] [{testName}] {failureMessage ?? ""}");
                    }
                    Console.ResetColor();
                    return false;
                }
            }
            catch (Exception ex) {
                if (accepableFalse) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  [Info] [{testName}] {failureMessage ?? ex.Message}");
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [ERROR] [{testName}] {failureMessage ?? ex.Message}");
                }
                Console.ResetColor();
                return false;
            }
        }

        public static void Print(string message,
    int offset = 0,
    ConsoleColor color = ConsoleColor.White)
        {
            Console.WriteLine(new string(' ', offset) + message);
            Console.ResetColor(); ;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintInfo(string message, int offset = 0) =>
            Print(message, offset, ConsoleColor.Yellow);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintError<T>(string message, int offset = 0) =>
            Print(message, offset, ConsoleColor.Red);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintSuccess<T>(string message, int offset = 0) =>
            Print(message, offset, ConsoleColor.Green);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintComment(string message, int offset = 0) =>
            Print(message, offset, ConsoleColor.White);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintDebug(string message, int offset = 0) =>
            Print(message, offset, ConsoleColor.Cyan);
    }


    public static class ReportGenerator
    {
        public static void TimingReport(List<double> data,
            bool outputData = false, string? filePathName = null)
        {

            TimingReport(data.ToArray(), outputData);
        }

        private static void TimingReport(double[] data,
            bool outputData = false, string? filePathName = null)
        {

            double ave = 0;
            double mx = double.MinValue;
            double mn = double.MaxValue;
            int tmMax = 0;
            int tmMin = 0;

            if ( data.Count() == 0) {
                if (filePathName == null) {
                    Console.WriteLine("No data to report.");
                    return;
                }
                else {
                    throw new ArgumentException("No data to report.");
                }
            }


            double[] recalculatedData = new double[data.Count()];

            for (int i = 0; i < data.Count(); i++) {

                recalculatedData[i] = i == 0 ? data[i] : data[i] - data[i - 1];
                mx = System.Math.Max(mx, recalculatedData[i]);
                mn = System.Math.Min(mn, recalculatedData[i]);
                ave += recalculatedData[i];
                tmMax = (mx == recalculatedData[i]) ? i : tmMax;
                tmMin = (mn == recalculatedData[i]) ? i : tmMin;
            }

            StringBuilder output = new StringBuilder();

            output.Append($"Timing Report\n" +
                $"Samples: {data.Count()}\n" +
                $"Ave [ms]: {(ave / data.Count()).ToString("F3")}\n" +
                $"STD [ms]: {Regression.StDev(recalculatedData).ToString("F3")}\n" +
                $"Max [ms]: {mx.ToString("F3")}, " +
                $"Index max: {tmMax.ToString("D4")}.\n" +
                $"Min [ms]: {mn.ToString("F3")}, " +
                $"Index min: {tmMin.ToString("D4")}.");

            if (outputData) {

                output.Append($"\nIndex\tDelta [ms]\n");

                for (int i = 0; i < recalculatedData.Length; i++) {

                    output.Append($"{i.ToString("D4")}\t" +
                        $"{recalculatedData[i].ToString("F4")}\n");
                }
            }

            if (filePathName != null) {

                if (!FileUtilities.SaveTextFile(output.ToString(), "", filePathName)) {
                    throw new Exception(FileUtilities.LastError);
                }
            }
            else {

                Console.WriteLine(output.ToString());
            }
        }
    }

}
