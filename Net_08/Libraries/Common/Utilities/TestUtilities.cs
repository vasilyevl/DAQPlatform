using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.Common.Utilities.Testing
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
    }

}
