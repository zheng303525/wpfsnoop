// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using ManagedInjector;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ManagedInjectorLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            Injector.LogMessage("Starting the injection process...", false);

            var windowHandle = (IntPtr)Int64.Parse(args[0]);
            var assemblyName = args[1];
            var className = args[2];
            var methodName = args[3];

            Injector.Launch(windowHandle, assemblyName, className, methodName);

            //check to see that it was injected, and if not, retry with the main window handle.
            var process = GetProcessFromWindowHandle(windowHandle);
            if (process != null && !CheckInjectedStatus(process) && process.MainWindowHandle != windowHandle)
            {
                Injector.LogMessage("Could not inject with current handle... retrying with MainWindowHandle", true);
                Injector.Launch(process.MainWindowHandle, assemblyName, className, methodName);
                CheckInjectedStatus(process);
            }
        }

        /// <summary>
        /// 通过窗口句柄获取进程
        /// </summary>
        /// <param name="windowHandle"></param>
        /// <returns></returns>
        private static Process GetProcessFromWindowHandle(IntPtr windowHandle)
        {
            int processId;
            GetWindowThreadProcessId(windowHandle, out processId);
            if (processId == 0)
            {
                Injector.LogMessage($"could not get process for window handle {windowHandle.ToString()}", true);
                return null;
            }

            try
            {
                var process = Process.GetProcessById(processId);
                return process;
                //if (process == null)
                //{
                //    Injector.LogMessage($"could not get process for PID = {processId.ToString()}", true);
                //    return null;
                //}
            }
            catch (Exception)
            {
                Injector.LogMessage($"could not get process for PID = {processId.ToString()}", true);
                return null;
            }
        }

        /// <summary>
        /// 查看当前是否已经被注入
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private static bool CheckInjectedStatus(Process process)
        {
            bool containsFile = false;
            process.Refresh();
            foreach (ProcessModule module in process.Modules)
            {
                if (module.FileName.Contains("ManagedInjector"))
                {
                    containsFile = true;
                }
            }
            if (containsFile)
            {
                Injector.LogMessage($"Successfully injected Snoop for process {process.ProcessName} (PID = {process.Id.ToString()})", true);
            }
            else
            {
                Injector.LogMessage($"Failed to inject for process {process.ProcessName} (PID = {process.Id.ToString()})", true);
            }
            return containsFile;
        }

        /// <summary>
        /// 通过窗口句柄获取进程ID，返回线程ID
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="processId">进程ID</param>
        /// <returns>线程ID</returns>
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processId);
    }
}
