using DTE_ATTACHER.DTE;
using DTE_ATTACHER.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DTE_ATTACHER
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"\r\n==========================================================================================");
            Console.WriteLine($"{Assembly.GetEntryAssembly().GetName().Name} - Version {Assembly.GetEntryAssembly().GetName().Version}");
            Console.WriteLine($"==========================================================================================\r\n");

            IConfiguration configuration = ConfigurationLoad();

            ClearFileContents(LoadApplicationClearLog(configuration));
            IEnumerable<Tuple<string, int>> processesList = LoadProcessesGroup(configuration, 0);

            ConsoleKeyInfo keyPressed = new ConsoleKeyInfo();

            do
            {
                //Task<bool> taskResult = LoadDebuggerAutomationAsTask(processesList);
                bool escapeKeyPressed = LoadDebuggerAutomationAsMethod(processesList);

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

        static bool LoadDebuggerAutomationAsMethod(IEnumerable<Tuple<string, int>> processesList)
        {
            bool escapeKeyPressed = false;

            foreach ((string targetProcess, int delay) in processesList)
            {
                escapeKeyPressed = AttacherAsMethod(targetProcess, delay);
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
                Thread.Sleep(delay);
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
                    Thread.Sleep(100);
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

        static IConfiguration ConfigurationLoad()
        {
            // Get appsettings.json config.
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return configuration;
        }

        static string LoadApplicationClearLog(IConfiguration configuration)
        {
            return configuration.GetValue<string>("Application:ClearLogFile");
        }

        static IEnumerable<Tuple<string, int>> LoadProcessesGroup(IConfiguration configuration, int index)
        {
            var processesPayload = configuration.GetSection("Processes")
                    .GetChildren()
                    .ToList()
                    .Select(x => new
                    {
                        ProcessName = x.GetValue<string>("Name"),
                        MsDelay = x.GetValue<int>("MsDelay"),
                    });

            // Is there a matching item?
            List<string> processInPayload = new List<string>();
            List<int> delaysInPayload = new List<int>();

            if (processesPayload.Count() > index)
            {
                processInPayload.AddRange(from value in processesPayload
                                          select processesPayload.ElementAt(index++).ProcessName);
            }

            index = 0;
            if (processesPayload.Count() > index)
            {
                delaysInPayload.AddRange(from value in processesPayload
                                         select processesPayload.ElementAt(index++).MsDelay);
            }
            IEnumerable<Tuple<string, int>> targetProceses = processInPayload.Zip(delaysInPayload, (a, b) => Tuple.Create(a, b));

            Console.WriteLine("LIST OF PROCESSES TO ATTACH TO VS DEBUGGER\r\n");
            foreach ((string process, int delay) in targetProceses)
            {
                Console.WriteLine($"{process} - delay: {delay} ms.");
            }
            Console.WriteLine("");

            return targetProceses;
        }
    }
}
