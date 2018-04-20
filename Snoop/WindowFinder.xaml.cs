// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Reflection;

namespace Snoop
{
    /// <summary>
    /// <see cref="WindowFinder"/>的类型
    /// </summary>
    public enum WindowFinderType
    {
        Snoop,
        Magnify
    };

    /// <summary>
    /// Interaction logic for WindowFinder.xaml
    /// </summary>
    public partial class WindowFinder : UserControl
    {
        /// <summary>
        /// 当前鼠标指向的窗口信息
        /// </summary>
        private WindowInfo _windowUnderCursor;

        /// <summary>
        /// 回馈图标窗口
        /// </summary>
        private SnoopabilityFeedbackWindow _feedbackWindow;

        /// <summary>
        /// 回馈图标窗口句柄
        /// </summary>
        private IntPtr _feedbackWindowHandle;

        /// <summary>
        /// 鼠标拖拽查找窗口时的鼠标样式
        /// </summary>
        private readonly Cursor _crosshairsCursor;

        /// <summary>
        /// 正在拖拽
        /// </summary>
        private bool IsDragging { get; set; }

        public WindowFinder()
        {
            InitializeComponent();

            _crosshairsCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Snoop.Resources.SnoopCrosshairsCursor.cur"));

            PreviewMouseLeftButtonDown += WindowFinderMouseLeftButtonDown;
            MouseMove += WindowFinderMouseMove;
            MouseLeftButtonUp += WindowFinderMouseLeftButtonUp;
        }

        /// <summary>
        /// 类型<see cref="WindowFinderType"/>
        /// </summary>
        public WindowFinderType WindowFinderType { get; set; }

        /// <summary>
        /// 处理鼠标按下事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowFinderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartSnoopTargetsSearch();
            e.Handled = true;
        }

        /// <summary>
        /// 处理鼠标移动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowFinderMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsDragging) return;

            if (Mouse.LeftButton == MouseButtonState.Released)
            {
                StopSnoopTargetsSearch();
                return;
            }

            var windowUnderCursor = NativeMethods.GetWindowUnderMouse();
            if (_windowUnderCursor == null)
            {
                _windowUnderCursor = new WindowInfo(windowUnderCursor);
            }

            if (IsVisualFeedbackWindow(windowUnderCursor))
            {
                // if the window under the cursor is the feedback window, just ignore it.
                return;
            }

            if (windowUnderCursor != _windowUnderCursor.HWnd)
            {
                // the window under the cursor has changed

                RemoveVisualFeedback();
                _windowUnderCursor = new WindowInfo(windowUnderCursor);
                if (_windowUnderCursor.IsValidProcess)
                {
                    ShowVisualFeedback();
                }
            }

            UpdateFeedbackWindowPosition();
        }

        /// <summary>
        /// 处理鼠标释放事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowFinderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopSnoopTargetsSearch();
            if (_windowUnderCursor != null && _windowUnderCursor.IsValidProcess)
            {
                if (WindowFinderType == WindowFinderType.Snoop)
                {
                    AttachSnoop();
                }
                else if (WindowFinderType == WindowFinderType.Magnify)
                {
                    AttachMagnify();
                }
            }
        }

        /// <summary>
        /// 开始查找窗口
        /// </summary>
        private void StartSnoopTargetsSearch()
        {
            CaptureMouse();
            IsDragging = true;
            Cursor = _crosshairsCursor;
            SnoopCrosshairsImage.Visibility = Visibility.Hidden;
            _windowUnderCursor = null;
        }

        /// <summary>
        /// 停止查找目标窗口
        /// </summary>
        private void StopSnoopTargetsSearch()
        {
            ReleaseMouseCapture();
            IsDragging = false;
            Cursor = Cursors.Arrow;
            SnoopCrosshairsImage.Visibility = Visibility.Visible;
            RemoveVisualFeedback();
        }

        /// <summary>
        /// 显示回馈图标窗口
        /// </summary>
        private void ShowVisualFeedback()
        {
            if (_feedbackWindow == null)
            {
                _feedbackWindow = new SnoopabilityFeedbackWindow();

                // we don't have to worry about not having an application or not having a main window,
                // for, we are still in Snoop's process and not in the injected process.
                // so, go ahead and grab the Application.Current.MainWindow.
                _feedbackWindow.Owner = Application.Current.MainWindow;
            }

            if (!_feedbackWindow.IsVisible)
            {
                _feedbackWindow.SnoopTargetName = _windowUnderCursor.Description;

                UpdateFeedbackWindowPosition();
                _feedbackWindow.Show();

                if (_feedbackWindowHandle == IntPtr.Zero)
                {
                    var wih = new WindowInteropHelper(_feedbackWindow);
                    _feedbackWindowHandle = wih.Handle;
                }
            }
        }

        /// <summary>
        /// 隐藏回馈图标窗口
        /// </summary>
        private void RemoveVisualFeedback()
        {
            if (_feedbackWindow != null && _feedbackWindow.IsVisible)
            {
                _feedbackWindow.Hide();
            }
        }

        private bool IsVisualFeedbackWindow(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && hwnd == _feedbackWindowHandle;
        }

        /// <summary>
        /// 更新反馈图标窗口位置
        /// </summary>
        private void UpdateFeedbackWindowPosition()
        {
            if (_feedbackWindow != null)
            {
                var mouse = NativeMethods.GetCursorPosition();
                _feedbackWindow.Left = mouse.X - 34;//.Left;
                _feedbackWindow.Top = mouse.Y + 10; // windowRect.Top;
            }
        }

        /// <summary>
        /// 开始嗅探
        /// </summary>
        private void AttachSnoop()
        {
            new AttachFailedHandler(_windowUnderCursor);
            _windowUnderCursor.Snoop();
        }

        /// <summary>
        /// 开始放大
        /// </summary>
        private void AttachMagnify()
        {
            new AttachFailedHandler(_windowUnderCursor);
            _windowUnderCursor.Magnify();
        }
    }
}
