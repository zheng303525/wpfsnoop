// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace Snoop
{
	public delegate void EventTrackerHandler(TrackedEvent newEvent);

	/// <summary>
	/// Random class that tries to determine what element handled a specific event.
	/// Doesn't work too well in the end, because the static ClassHandler doesn't get called
	/// in a consistent order.
	/// </summary>
	public class EventTracker : INotifyPropertyChanged, IComparable
	{
	    #region INotifyPropertyChanged Members

	    public event PropertyChangedEventHandler PropertyChanged;

	    protected void OnPropertyChanged(string propertyName)
	    {
	        Debug.Assert(this.GetType().GetProperty(propertyName) != null);
	        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	    }

	    #endregion

        public event EventTrackerHandler EventHandled;

	    private TrackedEvent _currentEvent = null;
	    private bool _everEnabled;
	    private readonly Type _targetType;

        private bool _isEnabled;

	    public bool IsEnabled
	    {
	        get { return this._isEnabled; }
	        set
	        {
	            if (this._isEnabled != value)
	            {
	                this._isEnabled = value;
	                if (this._isEnabled && !this._everEnabled)
	                {
	                    this._everEnabled = true;
	                    EventManager.RegisterClassHandler(this._targetType, _routedEvent, new RoutedEventHandler(this.HandleEvent), true);
	                }
	                this.OnPropertyChanged("IsEnabled");
	            }
	        }
	    }

        private readonly RoutedEvent _routedEvent;

	    public RoutedEvent RoutedEvent
	    {
	        get { return this._routedEvent; }
	    }

	    public string Category
	    {
	        get { return this._routedEvent.OwnerType.Name; }
	    }

	    public string Name
	    {
	        get { return this._routedEvent.Name; }
	    }

        public EventTracker(Type targetType, RoutedEvent routedEvent)
		{
			this._targetType = targetType;
			this._routedEvent = routedEvent;
		}

		private void HandleEvent(object sender, RoutedEventArgs e) 
		{
			// Try to figure out what element handled the event. Not precise.
			if (this._isEnabled) 
			{
				EventEntry entry = new EventEntry(sender, e.Handled);
				if (this._currentEvent != null && this._currentEvent.EventArgs == e) 
				{
					this._currentEvent.AddEventEntry(entry);
				}
				else 
				{
					this._currentEvent = new TrackedEvent(e, entry);
					this.EventHandled(this._currentEvent);
				}
			}
	    }

	    #region IComparable Members

	    public int CompareTo(object obj)
	    {
	        EventTracker otherTracker = obj as EventTracker;
	        if (otherTracker == null)
	            return 1;

	        if (this.Category == otherTracker.Category)
	            return this.RoutedEvent.Name.CompareTo(otherTracker.RoutedEvent.Name);
	        return this.Category.CompareTo(otherTracker.Category);
	    }

	    #endregion
    }

    [DebuggerDisplay("TrackedEvent: {EventArgs}")]
	public class TrackedEvent : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            Debug.Assert(this.GetType().GetProperty(propertyName) != null);
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private readonly RoutedEventArgs _routedEventArgs;

        public RoutedEventArgs EventArgs
        {
            get { return this._routedEventArgs; }
        }

        public EventEntry Originator
        {
            get { return this.Stack[0]; }
        }

        private bool _handled = false;

        public bool Handled
        {
            get { return this._handled; }
            set
            {
                this._handled = value;
                this.OnPropertyChanged("Handled");
            }
        }

        private object _handledBy = null;

        public object HandledBy
        {
            get { return this._handledBy; }
            set
            {
                this._handledBy = value;
                this.OnPropertyChanged("HandledBy");
            }
        }

        private readonly ObservableCollection<EventEntry> _stack = new ObservableCollection<EventEntry>();

        public ObservableCollection<EventEntry> Stack
        {
            get { return this._stack; }
        }

        public TrackedEvent(RoutedEventArgs routedEventArgs, EventEntry originator)
		{
			this._routedEventArgs = routedEventArgs;
			this.AddEventEntry(originator);
		}

		public void AddEventEntry(EventEntry eventEntry)
		{
			this.Stack.Add(eventEntry);
			if (eventEntry.Handled && !this.Handled)
			{
				this.Handled = true;
				this.HandledBy = eventEntry.Handler;
			}
		}
    }

    public class EventEntry
    {
        private readonly bool _handled;

        public bool Handled
        {
            get { return this._handled; }
        }

        private readonly object _handler;

        public object Handler
        {
            get { return this._handler; }
        }

        public EventEntry(object handler, bool handled)
		{
			this._handler = handler;
			this._handled = handled;
		}
	}
}
