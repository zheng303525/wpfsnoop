// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;

namespace Snoop.Shell
{
    /// <summary>
    /// 嵌入式的PowerShell界面
    /// Interaction logic for EmbeddedShellView.xaml
    /// </summary>
    public partial class EmbeddedShellView : UserControl
    {
        public event Action<VisualTreeItem> ProviderLocationChanged = delegate { }; 

        private readonly Runspace _runspace;
        private readonly SnoopPSHost _host;
        private int _historyIndex;

        public EmbeddedShellView()
        {
            InitializeComponent();

            this.CommandTextBox.PreviewKeyDown += OnCommandTextBoxPreviewKeyDown;

            // ignore execution-policy
            var iis = InitialSessionState.CreateDefault();
            iis.AuthorizationManager = new AuthorizationManager(Guid.NewGuid().ToString());
            iis.Providers.Add(new SessionStateProviderEntry(ShellConstants.DriveName, typeof(VisualTreeProvider), string.Empty));

            this._host = new SnoopPSHost(x => this.OutputTextBox.AppendText(x));
            this._runspace = RunspaceFactory.CreateRunspace(this._host, iis);
            this._runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
            this._runspace.ApartmentState = ApartmentState.STA;
            this._runspace.Open();

            // default required if you intend to inject scriptblocks into the host application
            Runspace.DefaultRunspace = this._runspace;
        }

        /// <summary>
        /// Initiates the startup routine and configures the runspace for use.
        /// </summary>
        public void Start(SnoopUI ui)
        {
            Invoke(string.Format("new-psdrive {0} {0} -root /", ShellConstants.DriveName));

            // synchronize selected and root tree elements
            ui.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(SnoopUI.CurrentSelection):
                        SetVariable(ShellConstants.Selected, ui.CurrentSelection);
                        break;
                    case nameof(SnoopUI.Root):
                        SetVariable(ShellConstants.Root, ui.Root);
                        break;
                }
            };

            // allow scripting of the host controls
            SetVariable("snoopui", ui);
            SetVariable("ui", this);

            // marshall back to the UI thread when the provider notifiers of a location change
            var action = new Action<VisualTreeItem>(item => this.Dispatcher.BeginInvoke(new Action(() => this.ProviderLocationChanged(item))));
            this.SetVariable(ShellConstants.LocationChangedActionKey, action);

            string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Scripts");
            Invoke($"import-module \"{Path.Combine(folder, ShellConstants.SnoopModule)}\"");

            this.OutputTextBox.Clear();
            Invoke("write-host 'Welcome to the Snoop PowerShell console!'");
            Invoke("write-host '----------------------------------------'");
            Invoke($"write-host 'To get started, try using the ${ShellConstants.Root} and ${ShellConstants.Selected} variables.'");

            FindAndLoadProfile(folder);
        }

        public void SetVariable(string name, object instance)
        {
            // add to the host so the provider has access to exposed variables
            this._host[name] = instance;

            // expose to the current runspace
            Invoke(string.Format("${0} = $host.PrivateData['{0}']", name));
        }

        public void NotifySelected(VisualTreeItem item)
        {
            if (this.AutoExpandCheckBox.IsChecked == true)
            {
                item.IsExpanded = true;
            }

            this.Invoke($"cd {ShellConstants.DriveName}:\\{item.NodePath()}");
        }

        private void FindAndLoadProfile(string scriptFolder)
        {
            if (LoadProfile(Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ShellConstants.SnoopProfile)))
            {
                return;
            }

            if (LoadProfile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\WindowsPowerShell", ShellConstants.SnoopProfile)))
            {
                return;
            }

            LoadProfile(Path.Combine(scriptFolder, ShellConstants.SnoopProfile));
        }

        private bool LoadProfile(string scriptPath)
        {
            if (File.Exists(scriptPath))
            {
                Invoke("write-host ''");
                Invoke(string.Format("${0} = '{1}'; . ${0}", ShellConstants.Profile, scriptPath));
                Invoke($"write-host \"Loaded `$profile: ${ShellConstants.Profile}\"");

                return true;
            }

            return false;
        }

        /// <summary>
        /// 在命令输入框输入时触发此方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCommandTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up://↑上一条命令
                    SetCommandTextToHistory(++this._historyIndex);
                    break;
                case Key.Down://↓下一条命令，如果不存在则清空命令输入框
                    if (this._historyIndex - 1 <= 0)
                    {
                        this.CommandTextBox.Clear();
                    }
                    else
                    {
                        SetCommandTextToHistory(--this._historyIndex);
                    }
                    break;
                case Key.Return://回车，执行命令
                    this.OutputTextBox.AppendText(Environment.NewLine);
                    this.OutputTextBox.AppendText(this.CommandTextBox.Text);
                    this.OutputTextBox.AppendText(Environment.NewLine);

                    Invoke(this.CommandTextBox.Text, true);
                    this.CommandTextBox.Clear();
                    break;
            }
        }

        /// <summary>
        /// 执行PowerShell命令
        /// </summary>
        /// <param name="script">命令</param>
        /// <param name="addToHistory">是否添加到历史</param>
        private void Invoke(string script, bool addToHistory = false)
        {
            this._historyIndex = 0;

            try
            {
                using (var pipe = this._runspace.CreatePipeline(script, addToHistory))
                {
                    var cmd = new Command("Out-String");
                    cmd.Parameters.Add("Width", Math.Max(2, (int)(this.ActualWidth * 0.7)));
                    pipe.Commands.Add(cmd);

                    foreach (var item in pipe.Invoke())
                    {
                        this.OutputTextBox.AppendText(item.ToString());
                    }

                    foreach (PSObject item in pipe.Error.ReadToEnd())
                    {
                        var error = (ErrorRecord)item.BaseObject;
                        this.OutputErrorRecord(error);
                    }
                }
            }
            catch (RuntimeException ex)
            {
                this.OutputErrorRecord(ex.ErrorRecord);
            }
            catch (Exception ex)
            {
                this.OutputTextBox.AppendText(string.Format("Oops!  Uncaught exception invoking on the PowerShell runspace: {0}", ex.Message));
            }

            this.OutputTextBox.ScrollToEnd();
        }

        private void OutputErrorRecord(ErrorRecord error)
        {
            this.OutputTextBox.AppendText($"{error}{(error.InvocationInfo != null ? error.InvocationInfo.PositionMessage : string.Empty)}");
            this.OutputTextBox.AppendText(string.Format("{1}  + CategoryInfo          : {0}", error.CategoryInfo, Environment.NewLine));
            this.OutputTextBox.AppendText(string.Format("{1}  + FullyQualifiedErrorId : {0}", error.FullyQualifiedErrorId, Environment.NewLine));
        }

        /// <summary>
        /// 设置为历史命令
        /// </summary>
        /// <param name="history"></param>
        private void SetCommandTextToHistory(int history)
        {
            var cmd = GetHistoryCommand(history);
            if (cmd != null)
            {
                this.CommandTextBox.Text = cmd;
                this.CommandTextBox.SelectionStart = cmd.Length;
            }
        }

        /// <summary>
        /// 取历史命令
        /// </summary>
        /// <param name="history"></param>
        /// <returns></returns>
        private string GetHistoryCommand(int history)
        {
            using (var pipe = this._runspace.CreatePipeline("get-history -count " + history, false))
            {
                var results = pipe.Invoke();
                if (results.Count > 0)
                {
                    var item = results[0];
                    return (string)item.Properties["CommandLine"].Value;
                }
                return null;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            switch (e.Key)
            {
                case Key.F5:
                    Invoke(string.Format("if (${0}) {{ . ${0} }}", ShellConstants.Profile));
                    break;
                case Key.F12:
                    this.OutputTextBox.Clear();
                    break;
            }
        }
    }
}