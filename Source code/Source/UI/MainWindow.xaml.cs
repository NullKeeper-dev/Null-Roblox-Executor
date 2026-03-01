using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using Null.Core;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System.Collections.Generic;

namespace Null.UI
{
    public class LuaCompletionData : ICompletionData
    {
        private static readonly HashSet<string> FunctionsWithQuotes = new HashSet<string> {
            "print", "warn", "error", "assert", "loadstring", "require", "dofile", "loadfile"
        };

        private static readonly HashSet<string> FunctionsWithParens = new HashSet<string> {
            "collectgarbage", "getfenv", "getmetatable", "ipairs", "load", "next", "pairs", 
            "pcall", "rawequal", "rawget", "rawset", "select", "setfenv", "setmetatable", 
            "tonumber", "tostring", "type", "unpack", "xpcall", "wait", "delay", "spawn", "tick", "task"
        };

        public LuaCompletionData(string text)
        {
            this.RawText = text;
            if (FunctionsWithQuotes.Contains(text))
                this.DisplayText = text + "(\"\")";
            else if (FunctionsWithParens.Contains(text))
                this.DisplayText = text + "()";
            else
                this.DisplayText = text;
        }

        public System.Windows.Media.ImageSource Image => null;

        public string RawText { get; private set; }
        public string Text => this.RawText;

        public string DisplayText { get; private set; }

        public object Content => this.DisplayText;

        public object Description => "Lua built-in or keyword";

        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            int offset = textArea.Caret.Offset;
            int start = offset;
            while (start > 0 && char.IsLetterOrDigit(textArea.Document.GetCharAt(start - 1)))
            {
                start--;
            }

            string completionText = this.RawText;
            int caretOffsetAfter = 0;

            if (FunctionsWithQuotes.Contains(this.RawText))
            {
                completionText = this.RawText + "(\"\")";
                caretOffsetAfter = -2; // Inside quotes
            }
            else if (FunctionsWithParens.Contains(this.RawText))
            {
                completionText = this.RawText + "()";
                caretOffsetAfter = -1; // Inside parens
            }

            textArea.Document.Replace(start, offset - start, completionText);
            
            if (caretOffsetAfter != 0)
            {
                textArea.Caret.Offset = start + completionText.Length + caretOffsetAfter;
            }
        }
    }

    public partial class MainWindow : Window
    {
        private VelocityAPI.VelAPI Velocity = new VelocityAPI.VelAPI();
        private AutoexecManager _autoexecManager;
        private CompletionWindow? completionWindow;

        private static readonly string[] LuaSuggestions = new string[] {
            "print", "warn", "error", "assert", "collectgarbage", "dofile", "getfenv", "getmetatable", 
            "ipairs", "load", "loadfile", "loadstring", "next", "pairs", "pcall", "rawequal", "rawget", 
            "rawset", "select", "setfenv", "setmetatable", "tonumber", "tostring", "type", "unpack", 
            "xpcall", "wait", "delay", "spawn", "tick", "local", "function", "end", "if", "then", 
            "else", "elseif", "for", "while", "repeat", "until", "do", "break", "return", "true", 
            "false", "nil", "and", "or", "not", "game", "workspace", "script", "Instance", "Vector3",
            "CFrame", "Color3", "Enum", "task", "math", "table", "string", "debug", "coroutine"
        };

        public MainWindow()
        {
            Console.WriteLine("[DEBUG] Initializing MainWindow...");
            try
            {
                InitializeComponent();
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Console.WriteLine("[DEBUG] InitializeComponent successful.");
                
                Velocity.StartCommunication();
                Console.WriteLine("[DEBUG] Velocity communication started.");
                
                _autoexecManager = new AutoexecManager(Velocity);
                Console.WriteLine("[DEBUG] AutoexecManager initialized.");
                
                ScriptEditor.Text = "print(\"Hello from NullKeeper\")";
                Console.WriteLine("[DEBUG] Default script set.");

                this.Loaded += (s, e) => {
                    LoadLuaHighlighting();
                    ScriptEditor.TextArea.TextView.Redraw();
                };
                
                ScriptEditor.TextArea.TextEntering += ScriptEditor_TextArea_TextEntering;
                ScriptEditor.TextArea.TextEntered += ScriptEditor_TextArea_TextEntered;

                StartStatusCheck();
                Console.WriteLine("[DEBUG] MainWindow initialization complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Critical error during MainWindow initialization: {ex}");
                MessageBox.Show($"Critical Init Error: {ex.Message}\n\nCheck console for details.", "Null Debug", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ScriptEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0)
            {
                char lastChar = e.Text[0];
                if (char.IsLetter(lastChar))
                {
                    ShowCompletion();
                }
            }
        }

        private void ScriptEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter/digit is typed, insert the selected item
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
        }

        private void ShowCompletion()
        {
            if (completionWindow != null) return;

            completionWindow = new CompletionWindow(ScriptEditor.TextArea);
            completionWindow.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            completionWindow.Foreground = System.Windows.Media.Brushes.White;
            completionWindow.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 63, 63));
            
            // Remove the tooltip/description window
            completionWindow.Width = 150;
            
            IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
            var sortedSuggestions = LuaSuggestions.OrderBy(s => s);
            foreach (var suggestion in sortedSuggestions)
            {
                data.Add(new LuaCompletionData(suggestion));
            }

            completionWindow.Show();
            
            // Auto-filter based on current word
            string currentWord = GetWordAtCaret();
            if (!string.IsNullOrEmpty(currentWord))
            {
                completionWindow.CompletionList.SelectItem(currentWord);
            }

            completionWindow.Closed += delegate {
                completionWindow = null;
            };
        }

        private string GetWordAtCaret()
        {
            int offset = ScriptEditor.CaretOffset;
            int start = offset;
            while (start > 0 && char.IsLetterOrDigit(ScriptEditor.Document.GetCharAt(start - 1)))
            {
                start--;
            }
            return ScriptEditor.Document.GetText(start, offset - start);
        }

        private int GetRobloxPid()
        {
            try
            {
                var process = Process.GetProcessesByName("RobloxPlayerBeta").FirstOrDefault();
                return process?.Id ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async void StartStatusCheck()
        {
            bool lastAttached = false;
            while (true)
            {
                try
                {
                    int pid = GetRobloxPid();
                    bool isAttached = pid != 0 && Velocity.IsAttached(pid);
                    
                    if (isAttached != lastAttached)
                    {
                        Dispatcher.Invoke(() => {
                            UpdateStatus(isAttached);
                        });

                        if (isAttached)
                        {
                            await _autoexecManager.RunAutoexec();
                        }
                        lastAttached = isAttached;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Status check error: {ex.Message}");
                }
                await Task.Delay(1000);
            }
        }

        private void LoadLuaHighlighting()
        {
            try
            {
                Console.WriteLine("[DEBUG] Loading Lua highlighting...");
                
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string[] resourceNames = assembly.GetManifestResourceNames();
                Console.WriteLine("[DEBUG] Available resources: " + string.Join(", ", resourceNames));

                string? resourceName = resourceNames.FirstOrDefault(r => r.EndsWith("Lua.xshd"));
                
                if (resourceName != null)
                {
                    using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            using (XmlReader reader = XmlReader.Create(stream))
                            {
                                ScriptEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                            }
                            Console.WriteLine("[DEBUG] Lua highlighting loaded from embedded resource: " + resourceName);
                            
                            // Force redraw to ensure highlighting applies
                            ScriptEditor.TextArea.TextView.Redraw();
                            return;
                        }
                    }
                }
                
                Console.WriteLine("[DEBUG] Resource not found or stream null, trying file fallback...");

                string xshdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lua.xshd");
                if (File.Exists(xshdPath))
                {
                    using (XmlReader reader = XmlReader.Create(xshdPath))
                    {
                        ScriptEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                    Console.WriteLine("[DEBUG] Lua highlighting loaded from file.");
                    ScriptEditor.TextArea.TextView.Redraw();
                }
                else
                {
                    // Last resort: standard built-in if available
                    var luaDef = HighlightingManager.Instance.GetDefinition("Lua");
                    if (luaDef != null)
                    {
                        ScriptEditor.SyntaxHighlighting = luaDef;
                        Console.WriteLine("[DEBUG] Loaded default AvalonEdit Lua definition.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Highlighting load error: {ex}");
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void Execute_Click(object sender, RoutedEventArgs e)
        {
            string script = ScriptEditor.Text;
            if (string.IsNullOrWhiteSpace(script)) return;

            try
            {
                int pid = GetRobloxPid();
                if (pid == 0)
                {
                    MessageBox.Show("Roblox process not found! Please open Roblox first.", "Null", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                Velocity.Execute(script);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Execution error: {ex.Message}", "Null", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            string scriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
            if (!Directory.Exists(scriptsPath))
            {
                Directory.CreateDirectory(scriptsPath);
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Lua files (*.lua)|*.lua|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                InitialDirectory = scriptsPath
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ScriptEditor.Text = File.ReadAllText(openFileDialog.FileName);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ScriptEditor.Clear();
        }

        private void SaveScript_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string scriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
                if (!Directory.Exists(scriptsPath))
                {
                    Directory.CreateDirectory(scriptsPath);
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Lua Script (*.lua)|*.lua|Text File (*.txt)|*.txt|All files (*.*)|*.*",
                    InitialDirectory = scriptsPath
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, ScriptEditor.Text);
                    MessageBox.Show("Script saved successfully!", "Null", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving script: {ex.Message}", "Null", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Attach_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int pid = GetRobloxPid();
                if (pid == 0)
                {
                    MessageBox.Show("Roblox process not found! Please open Roblox first.", "Null", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Velocity.Attach(pid);
                UpdateStatus(Velocity.IsAttached(pid));
                
                // Send a test script to verify execution
                string testScript = "print('[Null Executor] Successfully injected and executed!')";
                Velocity.Execute(testScript);
                Console.WriteLine($"[DEBUG] Sent test script to Roblox PID: {pid}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Attachment error: {ex.Message}", "Null", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatus(bool isAttached)
        {
            StatusText.Text = isAttached ? "Attached" : "Not Attached";
            StatusText.Foreground = isAttached ? 
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0)) : 
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 75, 75));
        }
    }
}
