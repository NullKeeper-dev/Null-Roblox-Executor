using System;
using System.Reflection;

namespace Inspector
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) return;
            try
            {
                var asm = Assembly.LoadFrom(args[0]);
                Console.WriteLine($"Assembly: {asm.FullName}");
                foreach (var type in asm.GetExportedTypes())
                {
                    Console.WriteLine($"Type: {type.FullName}");
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                    {
                        Console.WriteLine($"  Method: {method.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
