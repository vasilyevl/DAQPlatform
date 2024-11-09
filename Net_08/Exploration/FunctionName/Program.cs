using System.Reflection;

namespace FunctionName
{

    delegate bool MyAction();
    internal class Program
    {
        static void Main(string[] args) {
            Console.WriteLine("Hello, World!");

            MyProcedure(MyFunction);

            MyProcedure(() => {
                return true;
            });
            MyProcedure(() => {
                return true;
            });
            MyProcedure(() => {
                return true;
            });

            MyProcedure(MyFunction);
            MyProcedure(MyFunction);
        }


        static object MyProcedure(MyAction action) {

            var info = action.GetMethodInfo();
            var name = info.Name;
            Console.WriteLine($"Function name: {name}");
            var m = action.Method;
            Console.WriteLine($"Function name: {m.Name}");

            var result = action();

            return result;
        }

        static bool MyFunction() {
            return true;
        }

    }




}
