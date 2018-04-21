// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Text;
using System.Windows;
using Snoop.Infrastructure;

namespace Snoop
{
    /// <summary>
    /// Interaction logic for ErrorDialog.xaml
    /// </summary>
    public partial class ErrorDialog : Window
    {
        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception Exception { get; set; }

        public ErrorDialog()
        {
            InitializeComponent();

            this.Loaded += ErrorDialog_Loaded;
            this.Closed += ErrorDialog_Closed;
        }

        private void ErrorDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this._textBlockException.Text = this.GetExceptionMessage();

            SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);
        }

        private void ErrorDialog_Closed(object sender, EventArgs e)
        {
            SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(this);
        }

        private void _buttonCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(this.GetExceptionMessage());
            }
            catch (Exception ex)
            {
                string message = string.Format("There was an error copying to the clipboard:\nMessage = {0}\n\nPlease copy the exception from the above textbox manually!", ex.Message);
                MessageBox.Show(message, "Error copying to clipboard");
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
            }
            catch (Exception)
            {
                string message = $"There was an error starting the browser. Please visit \"{e.Uri.AbsoluteUri}\" to create the issue.";
                MessageBox.Show(message, "Error starting browser");
            }
        }

        private void CloseDoNotMarkHandled_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            if (CheckBoxRememberIsChecked())
            {
                SnoopModes.IgnoreExceptions = true;
            }
            this.Close();
        }

        private void CloseAndMarkHandled_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            if (CheckBoxRememberIsChecked())
            {
                SnoopModes.SwallowExceptions = true;
            }
            this.Close();
        }

        /// <summary>
        /// 记住选择的CheckBox是否勾选
        /// </summary>
        /// <returns></returns>
        private bool CheckBoxRememberIsChecked()
        {
            return this._checkBoxRemember.IsChecked.HasValue && this._checkBoxRemember.IsChecked.Value;
        }

        /// <summary>
        /// 获取异常信息
        /// </summary>
        /// <returns></returns>
        private string GetExceptionMessage()
        {
            StringBuilder builder = new StringBuilder();
            GetExceptionString(this.Exception, builder);
            return builder.ToString();
        }

        /// <summary>
        /// 获取异常信息
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="builder"></param>
        /// <param name="isInner"></param>
        private static void GetExceptionString(Exception exception, StringBuilder builder, bool isInner = false)
        {
            if (exception == null)
            {
                return;
            }

            if (isInner)
            {
                builder.AppendLine("\n\nInnerException:\n");
            }

            builder.AppendLine($"Message: {exception.Message}");
            builder.AppendLine($"Stacktrace:\n{exception.StackTrace}");

            GetExceptionString(exception.InnerException, builder, true);
        }
    }
}
