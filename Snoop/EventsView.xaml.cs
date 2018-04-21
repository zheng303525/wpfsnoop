// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using Snoop.Infrastructure;

namespace Snoop
{
    public partial class EventsView : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            Debug.Assert(this.GetType().GetProperty(propertyName) != null);
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        /// <summary>
        /// «Âø’√¸¡Ó
        /// </summary>
        public static readonly RoutedCommand ClearCommand = new RoutedCommand();
        
        private readonly ObservableCollection<EventTracker> _trackers = new ObservableCollection<EventTracker>();
        
        private static readonly List<RoutedEvent> DefaultEvents = new List<RoutedEvent>(
            new RoutedEvent[]
            {
                Keyboard.KeyDownEvent,
                Keyboard.KeyUpEvent,
                TextCompositionManager.TextInputEvent,
                Mouse.MouseDownEvent,
                Mouse.PreviewMouseDownEvent,
                Mouse.MouseUpEvent,
                CommandManager.ExecutedEvent,
            }
        );

        private readonly ObservableCollection<TrackedEvent> _interestingEvents = new ObservableCollection<TrackedEvent>();

        public IEnumerable InterestingEvents
        {
            get { return this._interestingEvents; }
        }

        public object AvailableEvents
        {
            get
            {
                PropertyGroupDescription pgd = new PropertyGroupDescription();
                pgd.PropertyName = "Category";
                pgd.StringComparison = StringComparison.OrdinalIgnoreCase;

                CollectionViewSource cvs = new CollectionViewSource();
                cvs.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
                cvs.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                cvs.GroupDescriptions.Add(pgd);

                cvs.Source = this._trackers;

                cvs.View.Refresh();
                return cvs.View;
            }
        }

        public EventsView()
        {
            this.InitializeComponent();

            List<EventTracker> sorter = new List<EventTracker>();

            foreach (RoutedEvent routedEvent in EventManager.GetRoutedEvents())
            {
                EventTracker tracker = new EventTracker(typeof(UIElement), routedEvent);
                tracker.EventHandled += this.HandleEventHandled;
                sorter.Add(tracker);

                if (EventsView.DefaultEvents.Contains(routedEvent))
                    tracker.IsEnabled = true;
            }

            sorter.Sort();
            foreach (EventTracker tracker in sorter)
                this._trackers.Add(tracker);

            this.CommandBindings.Add(new CommandBinding(EventsView.ClearCommand, this.HandleClear));
        }

        private void HandleEventHandled(TrackedEvent trackedEvent)
        {
            Visual visual = trackedEvent.Originator.Handler as Visual;
            if (visual != null && !visual.IsPartOfSnoopVisualTree())
            {
                Action action =
                    () =>
                    {
                        this._interestingEvents.Add(trackedEvent);

                        while (this._interestingEvents.Count > 100)
                            this._interestingEvents.RemoveAt(0);

                        TreeViewItem tvi = (TreeViewItem)this.EventTree.ItemContainerGenerator.ContainerFromItem(trackedEvent);
                        if (tvi != null)
                            tvi.BringIntoView();
                    };

                if (!this.Dispatcher.CheckAccess())
                {
                    this.Dispatcher.BeginInvoke(action);
                }
                else
                {
                    action.Invoke();
                }
            }
        }

        private void HandleClear(object sender, ExecutedRoutedEventArgs e)
        {
            this._interestingEvents.Clear();
        }

        private void EventTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
            {
                if (e.NewValue is EventEntry)
                    SnoopUI.InspectCommand.Execute(((EventEntry)e.NewValue).Handler, this);
                else if (e.NewValue is TrackedEvent)
                    SnoopUI.InspectCommand.Execute(((TrackedEvent)e.NewValue).EventArgs, this);
            }
        }
    }

    public class InterestingEvent
    {
        private readonly RoutedEventArgs _eventArgs;

        public RoutedEventArgs EventArgs
        {
            get { return this._eventArgs; }
        }

        private readonly object _handledBy;

        public object HandledBy
        {
            get { return this._handledBy; }
        }

        private readonly object _triggeredOn;

        public object TriggeredOn
        {
            get { return this._triggeredOn; }
        }

        public bool Handled
        {
            get { return this._handledBy != null; }
        }

        public InterestingEvent(object handledBy, RoutedEventArgs eventArgs)
        {
            this._handledBy = handledBy;
            this._triggeredOn = null;
            this._eventArgs = eventArgs;
        }
    }
}
