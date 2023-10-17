using EnvDTE80;
using System;
using System.Diagnostics;
using System.Linq;

namespace DTE_ATTACHER.DTE
{
    public static class DTEAttacher
    {
        public static bool Attach(string targetProcess)
        {
            bool result = false;

            try
            {
                DTE2 dte = GetCurrent();

                EnvDTE.Processes processes = dte.Debugger.LocalProcesses;

                foreach (EnvDTE.Process process in processes.Cast<EnvDTE.Process>().Where(proc => proc.Name.IndexOf(targetProcess) != -1))
                {
                    process.Attach();
                    DateTime dt = DateTime.Now;
                    string timestamp = dt.ToString("hh:mm:ss");
                    string processId = string.Format("{0}", process.ProcessID).PadLeft(5);
                    Console.WriteLine($"attached with PID {processId} - [{timestamp}]");
                    result = true;
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                Debug.WriteLine($"ATTACH exception: {ex.Message}");
                if (ex.Message.Equals("A debugger is already attached.", StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    Console.WriteLine("DEBUGGER IS ALREADY ATTACHED.");
                }
            }

            return result;
        }

        internal static DTE2 GetCurrent()
        {
            //DTE2 dte2 = (DTE2)Marshal2.GetActiveObject("VisualStudio.DTE.16.0"); // For VisualStudio 2019
            DTE2 dte2 = (DTE2)Marshal2.GetActiveObject("VisualStudio.DTE.17.0"); // For VisualStudio 2022
            return dte2;
        }
    }
}
