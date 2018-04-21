using System;
using System.ComponentModel;

namespace Snoop.DebugListenerTab
{
	[Serializable]
	public abstract class SnoopFilter : INotifyPropertyChanged
	{
		//protected string _isInverseText = string.Empty;

		public void ResetDirtyFlag()
		{
			_isDirty = false;
		}

	    protected bool _isDirty = false;

        public bool IsDirty
		{
			get
			{
				return _isDirty;
			}
		}

		public abstract bool FilterMatches(string debugLine);

		public virtual bool SupportsGrouping
		{
			get
			{
				return true;
			}
		}

	    protected bool _isInverse;

        public bool IsInverse
		{
			get
			{
				return _isInverse;
			}
			set
			{
				if (value != _isInverse)
				{
					_isInverse = value;
					RaisePropertyChanged("IsInverse");
					RaisePropertyChanged("IsInverseText");
				}
			}
		}

		public string IsInverseText
		{
			get
			{
				return _isInverse ? "NOT" : string.Empty;
			}
		}

	    protected bool _isGrouped = false;

        public bool IsGrouped
		{
			get
			{
				return _isGrouped;
			}
			set
			{
				_isGrouped = value;
				this.RaisePropertyChanged("IsGrouped");
				GroupId = string.Empty;
			}
		}

	    protected string _groupId = string.Empty;

        public virtual string GroupId
		{
			get
			{
				return _groupId;
			}
			set
			{
				_groupId = value;
				this.RaisePropertyChanged("GroupId");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged(string propertyName)
		{
			_isDirty = true;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
	}
}
