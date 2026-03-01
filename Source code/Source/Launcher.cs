using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Null.Core;
using Null.UI;
using System.Reflection;

namespace Null
{
    public class Launcher
    {
        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string assemblyName = new AssemblyName(args.Name).Name;
                if (assemblyName == "VelocityAPI")
                {
                    string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VelocityAPI_net_8.0.dll");
                    if (File.Exists(dllPath))
                    {
                        return Assembly.LoadFrom(dllPath);
                    }
                }
                return null;
            };

            try
            {
                Console.WriteLine("--- Null Executor Debug Console ---");
                Console.WriteLine("[DEBUG] Starting Launcher...");

                // Pre-load the assembly to ensure it's found
                string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VelocityAPI_net_8.0.dll");
                if (File.Exists(dllPath))
                {
                    Console.WriteLine($"[DEBUG] Found dependency at: {dllPath}");
                    try {
                        Assembly.LoadFrom(dllPath);
                        Console.WriteLine("[DEBUG] Dependency assembly loaded manually.");
                    } catch (Exception ex) {
                        Console.WriteLine($"[WARNING] Manual assembly load failed: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[WARNING] Dependency not found at expected path: {dllPath}");
                }
                
                App app = new App();
                
                Console.WriteLine("[DEBUG] Creating MainWindow...");
                MainWindow window = new MainWindow();
                
                Console.WriteLine("[DEBUG] Running Application...");
                app.Run(window);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[CRITICAL ERROR] Application failed to start:");
                Console.WriteLine(ex.ToString());
                if (ex.InnerException != null)
                {
                    Console.WriteLine("\nInner Exception:");
                    Console.WriteLine(ex.InnerException.ToString());
                }
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
