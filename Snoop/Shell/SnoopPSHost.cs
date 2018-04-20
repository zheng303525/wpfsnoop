// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Reflection;
using System.Threading;

namespace Snoop.Shell
{
    /// <summary>
    /// ¶Ô<see cref="PSHost"/>µÄ·â×°
    /// </summary>
    internal class SnoopPSHost : PSHost
    {
        private readonly Guid _id = Guid.NewGuid();
        private readonly SnoopPSHostUserInterface _ui;
        private readonly Hashtable _privateHashtable;

        public SnoopPSHost(Action<string> onOutput)
        {
            this._ui = new SnoopPSHostUserInterface();
            this._ui.OnDebug += onOutput;
            this._ui.OnError += onOutput;
            this._ui.OnVerbose += onOutput;
            this._ui.OnWarning += onOutput;
            this._ui.OnWrite += onOutput;

            this._privateHashtable = new Hashtable();
            this.PrivateData = new PSObject(this._privateHashtable);
        }

        public override void SetShouldExit(int exitCode)
        {
        }

        public override void EnterNestedPrompt()
        {
        }

        public override void ExitNestedPrompt()
        {
        }

        public override void NotifyBeginApplication()
        {
        }

        public override void NotifyEndApplication()
        {
        }

        public override CultureInfo CurrentCulture
        {
            get { return Thread.CurrentThread.CurrentCulture; }
        }

        public override CultureInfo CurrentUICulture
        {
            get { return Thread.CurrentThread.CurrentUICulture; }
        }

        public override Guid InstanceId
        {
            get { return this._id; }
        }

        public override string Name
        {
            get { return this._id.ToString(); }
        }

        public override PSHostUserInterface UI
        {
            get { return this._ui; }
        }

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public override PSObject PrivateData { get; }

        public object this[string name]
        {
            get { return this._privateHashtable[name]; }
            set { this._privateHashtable[name] = value; }
        }
    }
}