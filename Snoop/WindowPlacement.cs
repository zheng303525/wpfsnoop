// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Runtime.InteropServices;

namespace Snoop
{
	// AK: TODO: Move this to NativeMethods.cs

	// RECT structure required by WINDOWPLACEMENT structure
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public RECT(int left, int top, int right, int bottom)
		{
			this.Left = left;
			this.Top = top;
			this.Right = right;
			this.Bottom = bottom;
		}
	}

	// POINT structure required by WINDOWPLACEMENT structure
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct POINT
	{
		public int X;
		public int Y;

		public POINT(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}
	}

    /// <summary>
    /// 保存程序的位置
    /// </summary>
	// WINDOWPLACEMENT stores the position, size, and state of a window
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct WINDOWPLACEMENT
	{
        /// <summary>
        /// 结构体<see cref="WINDOWPLACEMENT"/>的大小，Marshal.SizeOf(typeof(WINDOWPLACEMENT))
        /// </summary>
		public uint length;

        /// <summary>
        /// 控制最小化窗口位置的标志和恢复窗口的方法
        /// </summary>
		public uint flags;

        /// <summary>
        /// 窗口当前状态  具体值可查看MSDN
        /// </summary>
		public uint showCmd;
		public POINT minPosition;
		public POINT maxPosition;
		public RECT normalPosition;
	}

	public static class Win32
	{
        /// <summary>
        /// 用于获取窗口的位置信息
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpwndpl"></param>
        /// <returns></returns>
		[DllImport("user32.dll")]
		public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        /// <summary>
        /// 用于设置窗口的位置
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpwndpl"></param>
        /// <returns></returns>
		[DllImport("user32.dll")]
		public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

        #region flags可设值(位标志，可以设置多个值)

        public const uint WPF_ASYNCWINDOWPLACEMENT = 0x0004;

        public const uint WPF_RESTORETOMAXIMIZED = 0x0002;

        public const uint WPF_SETMINPOSITION = 0x0001;

        #endregion

        #region showCmd值

        public const uint SW_HIDE = 0;
        public const uint SW_MAXIMIZE = 3;
        public const uint SW_MINIMIZE = 6;
        public const uint SW_RESTORE = 9;
        public const uint SW_SHOW = 5;
        public const uint SW_SHOWMAXIMIZED = 3;
        public const uint SW_SHOWMINIMIZED = 2;
        public const uint SW_SHOWMINNOACTIVE = 7;
        public const uint SW_SHOWNA = 8;
        public const uint SW_SHOWNOACTIVATE = 4;
        public const uint SW_SHOWNORMAL = 1;

        #endregion
    }
}
