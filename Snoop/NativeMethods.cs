// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;
using System.Windows;

namespace Snoop
{
    public static class NativeMethods
    {
        /// <summary>
        /// ί�У����ڱ�ʾ<see cref="NativeMethods.EnumWindows"/>�ص�����ǩ��
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private delegate bool EnumWindowsCallBackDelegate(IntPtr hwnd, IntPtr lParam);

        /// <summary>
        /// <see cref="NativeMethods.EnumWindows"/>�����ص�����
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam)
        {
            ((List<IntPtr>)((GCHandle)lParam).Target).Add(hwnd);
            return true;
        }

        /// <summary>
        /// ��ȡ���ж���Windows�ľ���б�
        /// </summary>
        public static IntPtr[] ToplevelWindows
        {
            get
            {
                List<IntPtr> windowList = new List<IntPtr>();
                GCHandle handle = GCHandle.Alloc(windowList);
                try
                {
                    NativeMethods.EnumWindows(NativeMethods.EnumWindowsCallback, (IntPtr)handle);
                }
                finally
                {
                    handle.Free();
                }
                return windowList.ToArray();
            }
        }

        /// <summary>
        /// ͨ�����ھ����ȡ����
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        public static Process GetWindowThreadProcess(IntPtr hwnd)
        {
            int processID;
            NativeMethods.GetWindowThreadProcessId(hwnd, out processID);
            try
            {
                return Process.GetProcessById(processID);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct MODULEENTRY32
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblcntUsage;
            public uint ProccntUsage;
            IntPtr modBaseAddr;
            public uint modBaseSize;
            IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        };

        public class ToolHelpHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private ToolHelpHandle()
                : base(true)
            {
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            protected override bool ReleaseHandle()
            {
                return NativeMethods.CloseHandle(handle);
            }
        }

        [Flags]
        public enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F
        }

        /// <summary>
        /// ��ȡ���ж���Windows�ľ���б�
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern int EnumWindows(EnumWindowsCallBackDelegate callback, IntPtr lParam);

        /// <summary>
        /// ͨ�����ھ����ȡ����ID
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="processId"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processId);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32")]
        public static extern IntPtr LoadLibrary(string librayName);

        /// <summary>
        /// ��ȡ��ǰ�������еĿ���
        /// </summary>
        /// <param name="dwFlags"></param>
        /// <param name="th32ProcessID"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern ToolHelpHandle CreateToolhelp32Snapshot(SnapshotFlags dwFlags, int th32ProcessID);

        /// <summary>
        /// ��ȡ��һ��ģ��
        /// </summary>
        /// <param name="hSnapshot"></param>
        /// <param name="lpme"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern bool Module32First(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

        /// <summary>
        /// ��ȡ��һ��ģ��
        /// </summary>
        /// <param name="hSnapshot"></param>
        /// <param name="lpme"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern bool Module32Next(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);


        // anvaka's changes below

        /// <summary>
        /// ��ȡ��굱ǰ����
        /// </summary>
        /// <returns></returns>
        public static Point GetCursorPosition()
        {
            var pos = new Point();
            var win32Point = new POINT();
            if (GetCursorPos(ref win32Point))
            {
                pos.X = win32Point.X;
                pos.Y = win32Point.Y;
            }
            return pos;
        }

        /// <summary>
        /// ��ȡ��ǰ���Ĵ��ھ��
        /// </summary>
        /// <returns></returns>
        public static IntPtr GetWindowUnderMouse()
        {
            POINT pt = new POINT();
            if (GetCursorPos(ref pt))
            {
                return WindowFromPoint(pt);
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// ��ȡ���ڵ�λ�ô�С
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        public static Rect GetWindowRect(IntPtr hwnd)
        {
            RECT rect;
            GetWindowRect(hwnd, out rect);
            return new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        /// <summary>
        /// ��ȡ��굱ǰ����
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref POINT pt);

        /// <summary>
        /// ��ȡ��ǰ����Ĵ��ھ��
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        /// <summary>
        /// ��ȡ���ڵĴ�С
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpRect"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    }
}
