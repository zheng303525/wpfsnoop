// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Linq;
using System.Threading;

namespace Snoop
{
    public partial class AppChooser
    {
        static AppChooser()
        {
            //将刷新命令与F5绑定
            AppChooser.RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
        }

        public AppChooser()
        {
            this._windowsView = CollectionViewSource.GetDefaultView(this._windows);

            this.InitializeComponent();

            //将命令绑定到方法
            this.CommandBindings.Add(new CommandBinding(AppChooser.RefreshCommand, this.HandleRefreshCommand));
            this.CommandBindings.Add(new CommandBinding(AppChooser.InspectCommand, this.HandleInspectCommand, this.HandleCanInspectOrMagnifyCommand));
            this.CommandBindings.Add(new CommandBinding(AppChooser.MagnifyCommand, this.HandleMagnifyCommand, this.HandleCanInspectOrMagnifyCommand));
            this.CommandBindings.Add(new CommandBinding(AppChooser.MinimizeCommand, this.HandleMinimizeCommand));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, this.HandleCloseCommand));
        }

        #region Commands

        /// <summary>
        /// 分析探查命令
        /// </summary>
        public static readonly RoutedCommand InspectCommand = new RoutedCommand();

        /// <summary>
        /// 刷新命令，刷新所有正在运行的wpf程序列表<see cref="AppChooser.Windows"/>>，快捷方式F5
        /// </summary>
		public static readonly RoutedCommand RefreshCommand = new RoutedCommand();

        /// <summary>
        /// 放大命令
        /// </summary>
		public static readonly RoutedCommand MagnifyCommand = new RoutedCommand();

        /// <summary>
        /// 最小化命令
        /// </summary>
		public static readonly RoutedCommand MinimizeCommand = new RoutedCommand();

        #endregion

        #region Fields && Properties

        /// <summary>
        /// 窗口列表
        /// </summary>
        private readonly ObservableCollection<WindowInfo> _windows = new ObservableCollection<WindowInfo>();

        private readonly ICollectionView _windowsView;

        /// <summary>
        /// 窗口列表视图
        /// </summary>
		public ICollectionView Windows
        {
            get { return this._windowsView; }
        }

        #endregion

        #region Methods

        #region Override Methods

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                //恢复窗口位置
                // load the window placement details from the user settings.
                WINDOWPLACEMENT wp = (WINDOWPLACEMENT)Properties.Settings.Default.AppChooserWindowPlacement;
                wp.length = (uint)Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                wp.flags = 0;
                wp.showCmd = (wp.showCmd == Win32.SW_SHOWMINIMIZED ? Win32.SW_SHOWNORMAL : wp.showCmd);
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                Win32.SetWindowPlacement(hwnd, ref wp);
            }
            catch
            {
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            //保存窗口位置
            // persist the window placement details to the user settings.
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            WINDOWPLACEMENT wp;
            Win32.GetWindowPlacement(hwnd, out wp);
            Properties.Settings.Default.AppChooserWindowPlacement = wp;
            Properties.Settings.Default.Save();
        }

        #endregion

        #endregion

        /// <summary>
        /// 刷新wpf程序窗口列表
        /// </summary>
        public void Refresh()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (DispatcherOperationCallback)delegate
            {
                this._windows.Clear();
                try
                {
                    //设置鼠标状态
                    Mouse.OverrideCursor = Cursors.Wait;

                    foreach (IntPtr windowHandle in NativeMethods.ToplevelWindows)
                    {
                        WindowInfo window = new WindowInfo(windowHandle);
                        if (window.IsValidProcess && !this.HasProcess(window.OwningProcess))
                        {
                            new AttachFailedHandler(window, this);
                            this._windows.Add(window);
                        }
                    }

                    if (this._windows.Count > 0)
                        this._windowsView.MoveCurrentTo(this._windows[0]);
                }
                finally
                {
                    //恢复鼠标状态
                    Mouse.OverrideCursor = null;
                }
                return null;
            }, null);
        }

        /// <summary>
        /// 进程是否已经添加到<see cref="_windows"/>
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private bool HasProcess(Process process)
        {
            foreach (WindowInfo window in this._windows)
            {
                if (window.OwningProcess.Id == process.Id)
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleCanInspectOrMagnifyCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            if (this._windowsView.CurrentItem != null)
                e.CanExecute = true;
            e.Handled = true;
        }

        /// <summary>
        /// 执行嗅探命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void HandleInspectCommand(object sender, ExecutedRoutedEventArgs e)
        {
            WindowInfo window = (WindowInfo)this._windowsView.CurrentItem;
            if (window != null)
            {
                window.Snoop();
            }
        }

        /// <summary>
        /// 执行放大命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void HandleMagnifyCommand(object sender, ExecutedRoutedEventArgs e)
        {
            WindowInfo window = (WindowInfo)this._windowsView.CurrentItem;
            if (window != null)
            {
                window.Magnify();
            }
        }

        /// <summary>
        /// 执行刷新命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void HandleRefreshCommand(object sender, ExecutedRoutedEventArgs e)
        {
            // clear out cached process info to make the force refresh do the process check over again.
            WindowInfo.ClearCachedProcessInfo();
            this.Refresh();
        }

        private void HandleMinimizeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void HandleCloseCommand(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }

    /// <summary>
    /// 窗口信息
    /// </summary>
	public class WindowInfo
    {
        /// <summary>
        /// 用于缓存窗口进程是否可用
        /// </summary>
        private static readonly Dictionary<int, bool> ProcessIDToValidityMap = new Dictionary<int, bool>();

        #region Fields && Properties

        private readonly IntPtr _hwnd;

        /// <summary>
        /// 窗口的句柄
        /// </summary>
        public IntPtr HWnd
        {
            get { return this._hwnd; }
        }

        /// <summary>
        /// 窗口进程
        /// </summary>
        public Process OwningProcess
        {
            get { return NativeMethods.GetWindowThreadProcess(this._hwnd); }
        }

        /// <summary>
        /// 窗口进程描述
        /// </summary>
        public string Description
        {
            get
            {
                Process process = this.OwningProcess;
                return process.MainWindowTitle + " - " + process.ProcessName + " [" + process.Id.ToString() + "]";
            }
        }

        /// <summary>
        /// 进程是否可用（WPF进程）
        /// </summary>
        public bool IsValidProcess
        {
            get
            {
                bool isValid = false;
                try
                {
                    if (this._hwnd == IntPtr.Zero)
                        return false;

                    Process process = this.OwningProcess;
                    if (process == null)
                        return false;

                    // see if we have cached the process validity previously, if so, return it.
                    if (WindowInfo.ProcessIDToValidityMap.TryGetValue(process.Id, out isValid))
                        return isValid;

                    // else determine the process validity and cache it.
                    if (process.Id == Process.GetCurrentProcess().Id)
                    {
                        isValid = false;

                        // the above line stops the user from snooping on snoop, since we assume that ... that isn't their goal.
                        // to get around this, the user can bring up two snoops and use the second snoop ... to snoop the first snoop.
                        // well, that let's you snoop the app chooser. in order to snoop the main snoop ui, you have to bring up three snoops.
                        // in this case, bring up two snoops, as before, and then bring up the third snoop, using it to snoop the first snoop.
                        // since the second snoop inserted itself into the first snoop's process, you can now spy the main snoop ui from the
                        // second snoop (bring up another main snoop ui to do so). pretty tricky, huh! and useful!
                    }
                    else
                    {
                        // a process is valid to snoop if it contains a dependency on PresentationFramework, PresentationCore, or milcore (wpfgfx).
                        // this includes the files:
                        // PresentationFramework.dll, PresentationFramework.ni.dll
                        // PresentationCore.dll, PresentationCore.ni.dll
                        // wpfgfx_v0300.dll (WPF 3.0/3.5)
                        // wpfgrx_v0400.dll (WPF 4.0)

                        // note: sometimes PresentationFramework.dll doesn't show up in the list of modules.
                        // so, it makes sense to also check for the unmanaged milcore component (wpfgfx_vxxxx.dll).
                        // see for more info: http://snoopwpf.codeplex.com/Thread/View.aspx?ThreadId=236335

                        // sometimes the module names aren't always the same case. compare case insensitive.
                        // see for more info: http://snoopwpf.codeplex.com/workitem/6090

                        foreach (var module in Modules)
                        {
                            if
                            (
                                module.szModule.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
                                module.szModule.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
                                module.szModule.StartsWith("wpfgfx", StringComparison.OrdinalIgnoreCase)
                            )
                            {
                                isValid = true;
                                break;
                            }
                        }
                    }

                    WindowInfo.ProcessIDToValidityMap[process.Id] = isValid;
                }
                catch (Exception)
                {
                }
                return isValid;
            }
        }

        private IEnumerable<NativeMethods.MODULEENTRY32> _modules;

        /// <summary>
        /// 获取窗口进程模块
        /// </summary>
        public IEnumerable<NativeMethods.MODULEENTRY32> Modules
        {
            get
            {
                if (_modules == null)
                {
                    var temp = GetModules().ToArray();
                    Interlocked.CompareExchange(ref _modules, temp, null);
                }
                return _modules;
            }
        }

        #endregion

        public event EventHandler<AttachFailedEventArgs> AttachFailed;

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        public WindowInfo(IntPtr hwnd)
        {
            this._hwnd = hwnd;
        }

        #endregion

        /// <summary>
        /// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
        /// except that we include 32 bit modules when Snoop runs in 64 bit mode.
        /// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
        /// </summary>
        private IEnumerable<NativeMethods.MODULEENTRY32> GetModules()
        {
            int processId;
            NativeMethods.GetWindowThreadProcessId(_hwnd, out processId);

            var me32 = new NativeMethods.MODULEENTRY32();
            var hModuleSnap = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.SnapshotFlags.Module | NativeMethods.SnapshotFlags.Module32, processId);
            if (!hModuleSnap.IsInvalid)
            {
                using (hModuleSnap)
                {
                    me32.dwSize = (uint)Marshal.SizeOf(me32);
                    if (NativeMethods.Module32First(hModuleSnap, ref me32))
                    {
                        do
                        {
                            yield return me32;
                        } while (NativeMethods.Module32Next(hModuleSnap, ref me32));
                    }
                }
            }
        }

        /// <summary>
        /// 开始嗅探
        /// </summary>
		public void Snoop()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                Injector.Launch(this.HWnd, typeof(SnoopUI).Assembly, typeof(SnoopUI).FullName, nameof(SnoopUI.GoBabyGo));
            }
            catch (Exception e)
            {
                OnFailedToAttach(e);
            }
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// 开始放大
        /// </summary>
		public void Magnify()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                Injector.Launch(this.HWnd, typeof(Zoomer).Assembly, typeof(Zoomer).FullName, nameof(Zoomer.GoBabyGo));
            }
            catch (Exception e)
            {
                OnFailedToAttach(e);
            }
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// 触发<see cref="WindowInfo.AttachFailed"/>事件
        /// </summary>
        /// <param name="e"></param>
		private void OnFailedToAttach(Exception e)
        {
            AttachFailed?.Invoke(this, new AttachFailedEventArgs(e, this.Description));
        }

        public override string ToString()
        {
            return this.Description;
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public static void ClearCachedProcessInfo()
        {
            WindowInfo.ProcessIDToValidityMap.Clear();
        }
    }

    /// <summary>
    /// <see cref="WindowInfo.AttachFailed"/>事件参数
    /// </summary>
    public class AttachFailedEventArgs : EventArgs
    {
        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception AttachException { get; }

        /// <summary>
        /// 窗口信息
        /// </summary>
        public string WindowName { get; }

        public AttachFailedEventArgs(Exception attachException, string windowName)
        {
            AttachException = attachException;
            WindowName = windowName;
        }
    }

    /// <summary>
    /// 用于处理<see cref="WindowInfo.AttachFailed"/>事件
    /// TODO 使用AttachFailedHandler类型，而不是直接使用事件处理函数的原因？
    /// </summary>
    public class AttachFailedHandler
    {
        private readonly AppChooser _appChooser;

        public AttachFailedHandler(WindowInfo window, AppChooser appChooser = null)
        {
            window.AttachFailed += OnSnoopAttachFailed;
            _appChooser = appChooser;
        }

        /// <summary>
        /// <see cref="WindowInfo.AttachFailed"/>事件处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnSnoopAttachFailed(object sender, AttachFailedEventArgs e)
        {
            System.Windows.MessageBox.Show($"Failed to attach to {e.WindowName}. Exception occured:{Environment.NewLine}{e.AttachException.ToString()}", "Can't Snoop the process!");
            // TODO This should be implmemented through the event broker, not like this.
            _appChooser?.Refresh();
        }
    }
}
