using DTE_ATTACHER.Config;
using DTE_ATTACHER.DTE;
using DTE_ATTACHER.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DTE_ATTACHER
{
    class Program
    {
        #region --- Win32 API ---
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", EntryPoint = "GetWindowPos")]
        public static extern UInt32 GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        #endregion --- Win32 API ---

        static void Main(string[] args)
        {
            Console.WriteLine($"\r\n==========================================================================================");
            Console.WriteLine($"{Assembly.GetEntryAssembly().GetName().Name} - Version {Assembly.GetEntryAssembly().GetName().Version}");
            Console.WriteLine($"==========================================================================================\r\n");

            AppConfig configuration = ConfigurationLoad();

            // Restore window position
            RestoreWindowPosition(configuration);

            ClearFileContents(configuration.Application.ClearLogFile);

            ConsoleKeyInfo keyPressed = new ConsoleKeyInfo();

            do
            {
                //Task<bool> taskResult = LoadDebuggerAutomationAsTask(processesList);
                bool escapeKeyPressed = LoadDebuggerAutomationAsMethod(configuration.Processes);

                //if (taskResult.Result == false)
                if (!escapeKeyPressed)
                {
                    Console.WriteLine("\r\nPRESS <ENTER> to RERUN\r\nPRESS <ESC> to QUIT\r\n");
                    keyPressed = Console.ReadKey(true);
                }
                else
                {
                    Console.WriteLine("\r\n\r\nUSER ABORTED PROCESS!");
                    break;
                }

            } while (keyPressed.Key != ConsoleKey.Escape);

            // save window position
            SaveWindowPosition(configuration);
        }

        static void ClearFileContents(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                // filename: logYYYYMMDD.txt
                DateTime dt = DateTime.Now;
                string timestamp = dt.ToString("yyyyMMdd");
                string file = Path.Combine(path, $"log{timestamp}.txt");

                try
                {
                    //using (FileStream fs = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    //{
                    //    lock (fs)
                    //    {
                    //        fs.SetLength(0);
                    //    }
                    //}
                    File.WriteAllText(file, string.Empty);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception clearing log: {ex.Message}");
                }
            }
        }

        #region --- AS METHODS ---

        static bool LoadDebuggerAutomationAsMethod(List<ProcessesList> processesList)
        {
            bool escapeKeyPressed = false;

            foreach (ProcessesList process in processesList)
            {
                escapeKeyPressed = AttacherAsMethod(process.Name, process.MSDelay);
                if (escapeKeyPressed)
                {
                    break;
                }
            }

            return escapeKeyPressed;
        }

        static bool AttacherAsMethod(string targetProcess, int delay)
        {
            bool escapeKeyPressed = false;

            //Console.Write($"Waiting for process {targetProcess} ...");
            Console.Write($"{Utils.FormatStringAsRequired(string.Format("Waiting for process {0} ", targetProcess), Utils.DeviceLogKeyValueLength, Utils.DeviceLogKeyValuePaddingCharacter)}... ");

            while (!DTEAttacher.Attach(targetProcess))
            {
                System.Threading.Thread.Sleep(delay);
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyPressed = Console.ReadKey(true);
                    if (keyPressed.Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine("");
                        escapeKeyPressed = true;
                        break;
                    }
                }
            }

            return escapeKeyPressed;
        }

        #endregion --- AS METHODS ---

        #region --- AS TASKS ---
        static async Task<bool> AttacherAsTask(string targetProcess)
        {
            bool escapeKeyPressed = false;

            await Task.Run(() =>
            {
                Console.Write($"Waiting for process {targetProcess} ...");

                while (!DTEAttacher.Attach(targetProcess))
                {
                    System.Threading.Thread.Sleep(100);
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keyPressed = Console.ReadKey(true);
                        if (keyPressed.Key == ConsoleKey.Escape)
                        {
                            Console.WriteLine("");
                            escapeKeyPressed = true;
                            break;
                        }
                    }
                }

                if (!escapeKeyPressed)
                {
                    Console.WriteLine(" attached!");
                }
            });

            return escapeKeyPressed;
        }

        static async Task<bool> LoadDebuggerAutomationAsTask(List<string> processesList)
        {
            bool escapeKeyPressed = false;

            foreach (string targetProcess in processesList)
            {
                escapeKeyPressed = await AttacherAsTask(targetProcess);
                if (escapeKeyPressed)
                {
                    break;
                }
            }

            return escapeKeyPressed;
        }
        #endregion --- AS TASKS ---

        static AppConfig ConfigurationLoad()
        {
            string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            // Get appsettings.json config.
            AppConfig configuration = new ConfigurationBuilder()
                .AddJsonFile(appSettingsPath, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build()
                .Get<AppConfig>();

            return configuration;
        }

        static void RestoreWindowPosition(AppConfig configuration)
        {
            IntPtr ptr = GetConsoleWindow();

            Rect parentWindowRectangle = new Rect()
            {
                Top = Convert.ToInt16(configuration.Application.WindowPosition.Top),
                Left = Convert.ToInt16(configuration.Application.WindowPosition.Left),
                Right = Convert.ToInt16(configuration.Application.WindowPosition.Width),
                Bottom = Convert.ToInt16(configuration.Application.WindowPosition.Height),
            };

            // int X, int Y, int nWidth, int nHeight
            MoveWindow(ptr, 
                       parentWindowRectangle.Left, parentWindowRectangle.Top, 
                       parentWindowRectangle.Right, parentWindowRectangle.Bottom,
                       true);
        }

        static void SaveWindowPosition(AppConfig configuration)
        {
            IntPtr ptr = GetConsoleWindow();
            Rect parentWindowRectangle = new Rect();
            GetWindowRect(ptr, ref parentWindowRectangle);

            configuration.Application.WindowPosition.Top = Convert.ToString(parentWindowRectangle.Top);
            configuration.Application.WindowPosition.Left = Convert.ToString(parentWindowRectangle.Left);
            configuration.Application.WindowPosition.Height = Convert.ToString(parentWindowRectangle.Bottom -parentWindowRectangle.Top);
            configuration.Application.WindowPosition.Width = Convert.ToString(parentWindowRectangle.Right - parentWindowRectangle.Left);

            AppSettingsUpdate(configuration);
        }

        static void AppSettingsUpdate(AppConfig configuration)
        {
            try
            {
                var jsonWriteOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };
                jsonWriteOptions.Converters.Add(new JsonStringEnumConverter());

                string newJson = JsonSerializer.Serialize(configuration, jsonWriteOptions);
                Debug.WriteLine($"{newJson}");

                string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                File.WriteAllText(appSettingsPath, newJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in saving settings: {ex}");
            }
        }
    }
}
