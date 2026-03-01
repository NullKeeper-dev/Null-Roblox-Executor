using System;
using System.Reflection;
using System.Linq;

namespace Inspector
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var assembly = Assembly.LoadFrom(args[0]);
                Console.WriteLine($"Assembly: {assembly.FullName}");
                foreach (var type in assembly.GetExportedTypes())
                {
                    Console.WriteLine($"Type: {type.FullName}");
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        Console.WriteLine($"  Method: {method.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
        }
    }
}
