using System;
using System.Reflection;

namespace Inspector
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: Inspector.exe <path-to-dll>");
                return;
            }

            try
            {
                string path = args[0];
                Console.WriteLine($"Inspecting: {path}");
                Assembly asm = Assembly.LoadFrom(path);
                Console.WriteLine($"Assembly Full Name: {asm.FullName}");
                
                foreach (Type type in asm.GetExportedTypes())
                {
                    Console.WriteLine($"Type: {type.FullName}");
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        var parameters = method.GetParameters();
                        string paramStr = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        Console.WriteLine($"  Method: {method.Name}({paramStr})");
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
