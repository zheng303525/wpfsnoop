// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Forms.Integration;
using Snoop.Infrastructure;

namespace Snoop
{
    /// <summary>
    /// 放大界面
    /// </summary>
	public partial class Zoomer
    {
        private delegate void Action();

        static Zoomer()
        {
            Zoomer.ResetCommand = new RoutedCommand("Reset", typeof(Zoomer));
            Zoomer.ZoomInCommand = new RoutedCommand("ZoomIn", typeof(Zoomer));
            Zoomer.ZoomOutCommand = new RoutedCommand("ZoomOut", typeof(Zoomer));
            Zoomer.PanLeftCommand = new RoutedCommand("PanLeft", typeof(Zoomer));
            Zoomer.PanRightCommand = new RoutedCommand("PanRight", typeof(Zoomer));
            Zoomer.PanUpCommand = new RoutedCommand("PanUp", typeof(Zoomer));
            Zoomer.PanDownCommand = new RoutedCommand("PanDown", typeof(Zoomer));
            Zoomer.SwitchTo2DCommand = new RoutedCommand("SwitchTo2D", typeof(Zoomer));
            Zoomer.SwitchTo3DCommand = new RoutedCommand("SwitchTo3D", typeof(Zoomer));

            //为命令绑定快捷键
            Zoomer.ResetCommand.InputGestures.Add(new MouseGesture(MouseAction.LeftDoubleClick));
            Zoomer.ResetCommand.InputGestures.Add(new KeyGesture(Key.F5));
            Zoomer.ZoomInCommand.InputGestures.Add(new KeyGesture(Key.OemPlus));
            Zoomer.ZoomInCommand.InputGestures.Add(new KeyGesture(Key.Up, ModifierKeys.Control));
            Zoomer.ZoomOutCommand.InputGestures.Add(new KeyGesture(Key.OemMinus));
            Zoomer.ZoomOutCommand.InputGestures.Add(new KeyGesture(Key.Down, ModifierKeys.Control));
            Zoomer.PanLeftCommand.InputGestures.Add(new KeyGesture(Key.Left));
            Zoomer.PanRightCommand.InputGestures.Add(new KeyGesture(Key.Right));
            Zoomer.PanUpCommand.InputGestures.Add(new KeyGesture(Key.Up));
            Zoomer.PanDownCommand.InputGestures.Add(new KeyGesture(Key.Down));
            Zoomer.SwitchTo2DCommand.InputGestures.Add(new KeyGesture(Key.F2));
            Zoomer.SwitchTo3DCommand.InputGestures.Add(new KeyGesture(Key.F3));
        }

        private const double ZoomFactor = 1.1;

        private readonly TranslateTransform _translateTransform = new TranslateTransform();
        private readonly ScaleTransform _scaleTransform = new ScaleTransform();
        private readonly TransformGroup _transform = new TransformGroup();
        private Point _downPoint;
        private VisualTree3DView _visualTree3DView;

        public Zoomer()
        {
            //为命令绑定处理函数
            this.CommandBindings.Add(new CommandBinding(Zoomer.ResetCommand, this.HandleReset, this.CanReset));
            this.CommandBindings.Add(new CommandBinding(Zoomer.ZoomInCommand, this.HandleZoomIn));
            this.CommandBindings.Add(new CommandBinding(Zoomer.ZoomOutCommand, this.HandleZoomOut));
            this.CommandBindings.Add(new CommandBinding(Zoomer.PanLeftCommand, this.HandlePanLeft));
            this.CommandBindings.Add(new CommandBinding(Zoomer.PanRightCommand, this.HandlePanRight));
            this.CommandBindings.Add(new CommandBinding(Zoomer.PanUpCommand, this.HandlePanUp));
            this.CommandBindings.Add(new CommandBinding(Zoomer.PanDownCommand, this.HandlePanDown));
            this.CommandBindings.Add(new CommandBinding(Zoomer.SwitchTo2DCommand, this.HandleSwitchTo2D));
            this.CommandBindings.Add(new CommandBinding(Zoomer.SwitchTo3DCommand, this.HandleSwitchTo3D, this.CanSwitchTo3D));

            this.InheritanceBehavior = InheritanceBehavior.SkipToThemeNext;

            this.InitializeComponent();

            this._transform.Children.Add(this._scaleTransform);
            this._transform.Children.Add(this._translateTransform);

            this.Viewbox.RenderTransform = this._transform;
        }

        /// <summary>
        /// 开始执行放大
        /// </summary>
		public static void GoBabyGo()
        {
            Dispatcher dispatcher;
            if (Application.Current == null && !SnoopModes.MultipleDispatcherMode)
            {
                dispatcher = Dispatcher.CurrentDispatcher;
            }
            else
            {
                if (Application.Current == null)
                {
                    return;
                }
                dispatcher = Application.Current.Dispatcher;
            }

            if (dispatcher.CheckAccess())
            {
                Zoomer zoomer = new Zoomer();
                zoomer.Magnify();
            }
            else
            {
                dispatcher.Invoke((Action)GoBabyGo);
            }
        }

        /// <summary>
        /// 开始执行放大
        /// </summary>
        public void Magnify()
        {
            object root = FindRoot();
            if (root == null)
            {
                MessageBox.Show("Can't find a current application or a PresentationSource root visual!", "Can't Magnify", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            Magnify(root);
        }

        /// <summary>
        /// 放大指定的对象
        /// </summary>
        /// <param name="root"></param>
        public void Magnify(object root)
        {
            this.Target = root;
            Set2DView(Target);

            Window ownerWindow = SnoopWindowUtils.FindOwnerWindow();
            if (ownerWindow != null)
                this.Owner = ownerWindow;

            SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);

            this.Show();
            this.Activate();
        }

        /// <summary>
        /// 被放大的界面对象，<see cref="Viewbox"/>包含一个<see cref="UIElement"/>类型的子项，该子项以此对象作为背景。参考<seealso cref="VisualBrush"/>
        /// </summary>
        public object Target { get; set; }

        /// <summary>
        /// 重置命令
        /// </summary>
		public static readonly RoutedCommand ResetCommand;

        /// <summary>
        /// 放大命令
        /// </summary>
		public static readonly RoutedCommand ZoomInCommand;

        /// <summary>
        /// 缩小命令
        /// </summary>
		public static readonly RoutedCommand ZoomOutCommand;

        /// <summary>
        /// 向左平移命令
        /// </summary>
		public static readonly RoutedCommand PanLeftCommand;

        /// <summary>
        /// 向右平移命令
        /// </summary>
		public static readonly RoutedCommand PanRightCommand;

        /// <summary>
        /// 向上平移命令
        /// </summary>
		public static readonly RoutedCommand PanUpCommand;

        /// <summary>
        /// 向下平移命令
        /// </summary>
		public static readonly RoutedCommand PanDownCommand;

        /// <summary>
        /// 设置为二维视图命令
        /// </summary>
        public static readonly RoutedCommand SwitchTo2DCommand;

        /// <summary>
        /// 设置为三维视图命令
        /// </summary>
        public static readonly RoutedCommand SwitchTo3DCommand;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                //用于恢复窗口位置
                // load the window placement details from the user settings.
                WINDOWPLACEMENT wp = (WINDOWPLACEMENT)Properties.Settings.Default.ZoomerWindowPlacement;
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

            this.Viewbox.Child = null;

            //用于保存窗口位置
            // persist the window placement details to the user settings.
            WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            Win32.GetWindowPlacement(hwnd, out wp);
            Properties.Settings.Default.ZoomerWindowPlacement = wp;
            Properties.Settings.Default.Save();

            SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(this);
        }

        /// <summary>
        /// 执行重置命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
		private void HandleReset(object sender, ExecutedRoutedEventArgs args)
        {
            this._translateTransform.X = 0;
            this._translateTransform.Y = 0;
            this._scaleTransform.ScaleX = 1;
            this._scaleTransform.ScaleY = 1;
            this._scaleTransform.CenterX = 0;
            this._scaleTransform.CenterY = 0;

            if (this._visualTree3DView != null)
            {
                this._visualTree3DView.Reset();
                this.ZScaleSlider.Value = 0;
            }
        }

        /// <summary>
        /// 是否可以执行重置命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
		private void CanReset(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
            args.Handled = true;
        }

        /// <summary>
        /// 执行放大命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
		private void HandleZoomIn(object sender, ExecutedRoutedEventArgs args)
        {
            Point offset = Mouse.GetPosition(this.Viewbox);
            this.Zoom(Zoomer.ZoomFactor, offset);
        }

        /// <summary>
        /// 执行缩小命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
		private void HandleZoomOut(object sender, ExecutedRoutedEventArgs args)
        {
            Point offset = Mouse.GetPosition(this.Viewbox);
            this.Zoom(1 / Zoomer.ZoomFactor, offset);
        }

        /// <summary>
        /// 执行向左平移命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
		private void HandlePanLeft(object sender, ExecutedRoutedEventArgs args)
        {
            this._translateTransform.X -= 5;
        }

        /// <summary>
        /// 执行向右平移命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
		private void HandlePanRight(object sender, ExecutedRoutedEventArgs args)
        {
            this._translateTransform.X += 5;
        }

        /// <summary>
        /// 执行向上平移命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
		private void HandlePanUp(object sender, ExecutedRoutedEventArgs args)
        {
            this._translateTransform.Y -= 5;
        }

        /// <summary>
        /// 执行向下平移命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
		private void HandlePanDown(object sender, ExecutedRoutedEventArgs args)
        {
            this._translateTransform.Y += 5;
        }

        /// <summary>
        /// 设置为二维视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleSwitchTo2D(object sender, ExecutedRoutedEventArgs args)
        {
            if (this._visualTree3DView != null)
            {
                Set2DView(Target);
                this._visualTree3DView = null;
                this.ZScaleSlider.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 设置为三维视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleSwitchTo3D(object sender, ExecutedRoutedEventArgs args)
        {
            Visual visual = this.Target as Visual;
            if (this._visualTree3DView == null && visual != null)
            {
                Set3DView(visual);
                this.ZScaleSlider.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 设置二维视图
        /// </summary>
        /// <param name="target"></param>
        private void Set2DView(object target)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                UIElement uiElement2DView = this.CreateIfPossible(target);
                if (uiElement2DView != null)
                    this.Viewbox.Child = uiElement2DView;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// 设置三维视图
        /// </summary>
        /// <param name="visual"></param>
        private void Set3DView(Visual visual)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                this._visualTree3DView = new VisualTree3DView(visual);
                this.Viewbox.Child = this._visualTree3DView;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// 命令<see cref="SwitchTo3DCommand"/>是否可用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CanSwitchTo3D(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = (this.Target is Visual);
            args.Handled = true;
        }

        #region 鼠标平移操作

        private void Content_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this._downPoint = e.GetPosition(this.DocumentRoot);
            this.DocumentRoot.CaptureMouse();
        }

        private void Content_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.DocumentRoot.IsMouseCaptured)
            {
                Vector delta = e.GetPosition(this.DocumentRoot) - this._downPoint;
                this._translateTransform.X += delta.X;
                this._translateTransform.Y += delta.Y;

                this._downPoint = e.GetPosition(this.DocumentRoot);
            }
        }

        private void Content_MouseUp(object sender, MouseEventArgs e)
        {
            this.DocumentRoot.ReleaseMouseCapture();
        }

        #endregion

        /// <summary>
        /// 滚轮执行缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Content_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoom = Math.Pow(Zoomer.ZoomFactor, e.Delta / 120.0);
            Point offset = e.GetPosition(this.Viewbox);
            this.Zoom(zoom, offset);
        }

        private void ZScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this._visualTree3DView != null)
            {
                this._visualTree3DView.ZScale = Math.Pow(10, e.NewValue);
            }
        }

        /// <summary>
        /// 创建二维视图
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private UIElement CreateIfPossible(object target)
        {
            return ZoomerUtilities.CreateIfPossible(target);
        }

        //private UIElement CreateIfPossible(object item)
        //{
        //    if (item is Window && VisualTreeHelper.GetChildrenCount((Visual)item) == 1)
        //        item = VisualTreeHelper.GetChild((Visual)item, 0);

        //    if (item is FrameworkElement)
        //    {
        //        FrameworkElement uiElement = (FrameworkElement)item;
        //        VisualBrush brush = new VisualBrush(uiElement);
        //        brush.Stretch = Stretch.Uniform;
        //        Rectangle rect = new Rectangle();
        //        rect.Fill = brush;
        //        rect.Width = uiElement.ActualWidth;
        //        rect.Height = uiElement.ActualHeight;
        //        return rect;
        //    }

        //    else if (item is ResourceDictionary)
        //    {
        //        StackPanel stackPanel = new StackPanel();

        //        foreach (object value in ((ResourceDictionary)item).Values)
        //        {
        //            UIElement element = CreateIfPossible(value);
        //            if (element != null)
        //                stackPanel.Children.Add(element);
        //        }
        //        return stackPanel;
        //    }
        //    else if (item is Brush)
        //    {
        //        Rectangle rect = new Rectangle();
        //        rect.Width = 10;
        //        rect.Height = 10;
        //        rect.Fill = (Brush)item;
        //        return rect;
        //    }
        //    else if (item is ImageSource)
        //    {
        //        Image image = new Image();
        //        image.Source = (ImageSource)item;
        //        return image;
        //    }
        //    return null;
        //}

        /// <summary>
        /// 执行缩放 //TODO 坐标转换
        /// </summary>
        /// <param name="zoom">缩放比例</param>
        /// <param name="offset"></param>
        private void Zoom(double zoom, Point offset)
        {
            Vector v = new Vector((1 - zoom) * offset.X, (1 - zoom) * offset.Y);

            Vector translationVector = v * this._transform.Value;
            this._translateTransform.X += translationVector.X;
            this._translateTransform.Y += translationVector.Y;

            this._scaleTransform.ScaleX = this._scaleTransform.ScaleX * zoom;
            this._scaleTransform.ScaleY = this._scaleTransform.ScaleY * zoom;
        }

        /// <summary>
        /// 查找需要放大的对象，逻辑与<see cref="SnoopWindowUtils.FindOwnerWindow"/>方法相似
        /// </summary>
        /// <returns></returns>
		private object FindRoot()
        {
            object root = null;
            if (SnoopModes.MultipleDispatcherMode)
            {
                foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
                {
                    if
                    (
                        presentationSource.RootVisual != null &&
                        presentationSource.RootVisual is UIElement &&
                        ((UIElement)presentationSource.RootVisual).Dispatcher.CheckAccess()
                    )
                    {
                        root = presentationSource.RootVisual;
                        break;
                    }
                }
            }
            else if (Application.Current != null)
            {
                // try to use the application's main window (if visible) as the root
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.Visibility == Visibility.Visible)
                {
                    root = Application.Current.MainWindow;
                }
                else
                {
                    // else search for the first visible window in the list of the application's windows
                    // Application.Current.Windows的集合数据是快照
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.Visibility == Visibility.Visible)
                        {
                            root = window;
                            break;
                        }
                    }
                }
            }
            else
            {
                // if we don't have a current application,
                // then we must be in an interop scenario (win32 -> wpf or windows forms -> wpf).

                if (System.Windows.Forms.Application.OpenForms.Count > 0)
                {
                    // this is windows forms -> wpf interop

                    // call ElementHost.EnableModelessKeyboardInterop
                    // to allow the Zoomer window to receive keyboard messages.
                    ElementHost.EnableModelessKeyboardInterop(this);
                }
            }

            if (root == null)
            {
                // if we still don't have a root to magnify

                // let's iterate over PresentationSource.CurrentSources,
                // and use the first non-null, visible RootVisual we find as root to inspect.
                foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
                {
                    if
                    (
                        presentationSource.RootVisual != null &&
                        presentationSource.RootVisual is UIElement &&
                        ((UIElement)presentationSource.RootVisual).Visibility == Visibility.Visible
                    )
                    {
                        root = presentationSource.RootVisual;
                        break;
                    }
                }
            }

            // if the root is a window, let's magnify the window's content.
            // this is better, as otherwise, you will have window background along with the window's content.
            if (root is Window && ((Window)root).Content != null)
                root = ((Window)root).Content;

            return root;
        }

        private void SetOwnerWindow()
        {
            Window ownerWindow = null;

            if (SnoopModes.MultipleDispatcherMode)
            {
                foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
                {
                    if
                    (
                        presentationSource.RootVisual is Window &&
                        ((Window)presentationSource.RootVisual).Dispatcher.CheckAccess()
                    )
                    {
                        ownerWindow = (Window)presentationSource.RootVisual;
                        break;
                    }
                }
            }
            else if (Application.Current != null)
            {
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.Visibility == Visibility.Visible)
                {
                    // first: set the owner window as the current application's main window, if visible.
                    ownerWindow = Application.Current.MainWindow;
                }
                else
                {
                    // second: try and find a visible window in the list of the current application's windows
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.Visibility == Visibility.Visible)
                        {
                            ownerWindow = window;
                            break;
                        }
                    }
                }
            }

            if (ownerWindow == null)
            {
                // third: try and find a visible window in the list of current presentation sources
                foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
                {
                    if
                    (
                        presentationSource.RootVisual is Window &&
                        ((Window)presentationSource.RootVisual).Visibility == Visibility.Visible
                    )
                    {
                        ownerWindow = (Window)presentationSource.RootVisual;
                        break;
                    }
                }
            }

            if (ownerWindow != null)
                this.Owner = ownerWindow;
        }
    }
}
