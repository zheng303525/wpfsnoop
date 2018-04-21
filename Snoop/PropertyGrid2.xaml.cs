// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Snoop.Infrastructure;

namespace Snoop
{
    public partial class PropertyGrid2 : INotifyPropertyChanged
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
        /// ��ʾ�󶨴�����Ϣ������
        /// </summary>
        public static readonly RoutedCommand ShowBindingErrorsCommand = new RoutedCommand();

        /// <summary>
        /// ���������������
        /// </summary>
        public static readonly RoutedCommand ClearCommand = new RoutedCommand();

        /// <summary>
        /// �����������
        /// </summary>
        public static readonly RoutedCommand SortCommand = new RoutedCommand();

        /// <summary>
        /// ���ڱ���<see cref="Target"/>����������ֵ
        /// </summary>
        private object _target;

        private readonly DispatcherTimer _filterTimer;

        private readonly ObservableCollection<PropertyInformation> _allProperties = new ObservableCollection<PropertyInformation>();

        private IEnumerator<PropertyInformation> _propertiesToAdd;

        /// <summary>
        /// <see cref="ProcessIncrementalPropertyAdd"/>�ص�������װ
        /// </summary>
        private readonly DelayedCall _processIncrementalCall;

        /// <summary>
        /// <see cref="ProcessFilter"/>�ص�������װ
        /// </summary>
        private readonly DelayedCall _filterCall;
        private int _visiblePropertyCount = 0;
        private bool _unloaded = false;
        private ListSortDirection _direction = ListSortDirection.Ascending;

        private bool _nameValueOnly = false;

        public bool NameValueOnly
        {
            get
            {
                return _nameValueOnly;
            }
            set
            {
                _nameValueOnly = value;
                GridView gridView = this.ListView != null && this.ListView.View != null ? this.ListView.View as GridView : null;
                if (_nameValueOnly && gridView != null && gridView.Columns.Count != 2)
                {
                    gridView.Columns.RemoveAt(0);
                    while (gridView.Columns.Count > 2)
                    {
                        gridView.Columns.RemoveAt(2);
                    }
                }
            }
        }

        private readonly ObservableCollection<PropertyInformation> _properties = new ObservableCollection<PropertyInformation>();

        /// <summary>
        /// ������ʾ������
        /// </summary>
        public ObservableCollection<PropertyInformation> Properties
        {
            get { return this._properties; }
        }

        private PropertyInformation _selection;

        /// <summary>
        /// ѡ�е�����
        /// </summary>
        public PropertyInformation Selection
        {
            get { return this._selection; }
            set
            {
                this._selection = value;
                this.OnPropertyChanged(nameof(Selection));
            }
        }

        /// <summary>
        /// ��������
        /// </summary>
        public Type Type
        {
            get
            {
                if (this._target != null)
                    return this._target.GetType();
                return null;
            }
        }

        public PropertyGrid2()
        {
            this._processIncrementalCall = new DelayedCall(this.ProcessIncrementalPropertyAdd, DispatcherPriority.Background);
            this._filterCall = new DelayedCall(this.ProcessFilter, DispatcherPriority.Background);

            this.InitializeComponent();

            this.Loaded += this.HandleLoaded;
            this.Unloaded += this.HandleUnloaded;

            this.CommandBindings.Add(new CommandBinding(PropertyGrid2.ShowBindingErrorsCommand, this.HandleShowBindingErrors, this.CanShowBindingErrors));
            this.CommandBindings.Add(new CommandBinding(PropertyGrid2.ClearCommand, this.HandleClear, this.CanClear));
            this.CommandBindings.Add(new CommandBinding(PropertyGrid2.SortCommand, this.HandleSort));

            _filterTimer = new DispatcherTimer();
            _filterTimer.Interval = TimeSpan.FromSeconds(0.3);
            _filterTimer.Tick += (s, e) =>
            {
                this._filterCall.Enqueue();
                _filterTimer.Stop();
            };
        }

        /// <summary>
        /// �鿴���ԵĶ���
        /// </summary>
        public object Target
        {
            get { return this.GetValue(PropertyGrid2.TargetProperty); }
            set { this.SetValue(PropertyGrid2.TargetProperty, value); }
        }

        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(object), typeof(PropertyGrid2), new PropertyMetadata(new PropertyChangedCallback(PropertyGrid2.HandleTargetChanged)));

        private static void HandleTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyGrid2 propertyGrid = (PropertyGrid2)d;
            propertyGrid.ChangeTarget(e.NewValue);
        }

        /// <summary>
        /// Target����
        /// </summary>
        /// <param name="newTarget"></param>
        private void ChangeTarget(object newTarget)
        {
            if (this._target != newTarget)
            {
                this._target = newTarget;

                foreach (PropertyInformation property in this._properties)
                {
                    property.Teardown();
                }
                this.RefreshPropertyGrid();

                this.OnPropertyChanged(nameof(Type));
            }
        }

        protected override void OnFilterChanged()
        {
            base.OnFilterChanged();

            _filterTimer.Stop();
            _filterTimer.Start();
        }

        /// <summary>
        /// Delayed loading of the property inspector to avoid creating the entire list of property
        /// editors immediately after selection. Keeps that app running smooth.
        /// </summary>
        /// <returns></returns>
        private void ProcessIncrementalPropertyAdd()
        {
            int numberToAdd = 10;

            if (this._propertiesToAdd == null)
            {
                this._propertiesToAdd = PropertyInformation.GetProperties(this._target).GetEnumerator();

                numberToAdd = 0;
            }
            int i = 0;
            for (; i < numberToAdd && this._propertiesToAdd.MoveNext(); ++i)
            {
                // iterate over the PropertyInfo objects,
                // setting the property grid's filter on each object,
                // and adding those properties to the observable collection of propertiesToSort (this.properties)
                PropertyInformation property = this._propertiesToAdd.Current;
                property.Filter = this.Filter;

                if (property.IsVisible)
                {
                    this._properties.Add(property);
                }
                _allProperties.Add(property);

                // checking whether a property is visible ... actually runs the property filtering code
                if (property.IsVisible)
                    property.Index = this._visiblePropertyCount++;
            }

            if (i == numberToAdd)
                this._processIncrementalCall.Enqueue();
            else
                this._propertiesToAdd = null;
        }

        /// <summary>
        /// ����<see cref="ShowBindingErrorsCommand"/>��ʾ�󶨴�����Ϣ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleShowBindingErrors(object sender, ExecutedRoutedEventArgs eventArgs)
        {
            PropertyInformation propertyInformation = (PropertyInformation)eventArgs.Parameter;
            Window window = new Window();
            TextBox textbox = new TextBox();
            textbox.IsReadOnly = true;
            textbox.Text = propertyInformation.BindingError;
            textbox.TextWrapping = TextWrapping.Wrap;
            window.Content = textbox;
            window.Width = 400;
            window.Height = 300;
            window.Title = "Binding Errors for " + propertyInformation.DisplayName;
            SnoopPartsRegistry.AddSnoopVisualTreeRoot(window);
            window.Closing +=
                (s, e) =>
                {
                    Window w = (Window)s;
                    SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(w);
                };
            window.Show();
        }

        /// <summary>
        /// ����<see cref="ShowBindingErrorsCommand"/>�Ƿ����ִ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanShowBindingErrors(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && !string.IsNullOrEmpty(((PropertyInformation)e.Parameter).BindingError))
                e.CanExecute = true;
            e.Handled = true;
        }

        /// <summary>
        /// <see cref="ClearCommand"/>�����Ƿ����ִ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanClear(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && ((PropertyInformation)e.Parameter).IsLocallySet)
                e.CanExecute = true;
            e.Handled = true;
        }

        /// <summary>
        /// ִ��<see cref="ClearCommand"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleClear(object sender, ExecutedRoutedEventArgs e)
        {
            ((PropertyInformation)e.Parameter).Clear();
        }

        /// <summary>
        /// ִ��<see cref="SortCommand"/>��������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleSort(object sender, ExecutedRoutedEventArgs args)
        {
            GridViewColumnHeader headerClicked = (GridViewColumnHeader)args.OriginalSource;

            _direction = GetNewSortDirection(headerClicked);
            if (headerClicked.Column == null)
                return;

            var columnHeader = headerClicked.Column.Header as TextBlock;
            if (columnHeader == null)
                return;

            switch (columnHeader.Text)
            {
                case "Name":
                    this.Sort(PropertyGrid2.CompareNames, _direction);
                    break;
                case "Value":
                    this.Sort(PropertyGrid2.CompareValues, _direction);
                    break;
                case "Value Source":
                    this.Sort(PropertyGrid2.CompareValueSources, _direction);
                    break;
            }
        }

        /// <summary>
        /// ���������
        /// </summary>
        /// <param name="columnHeader"></param>
        /// <returns></returns>
        private ListSortDirection GetNewSortDirection(GridViewColumnHeader columnHeader)
        {
            if (!(columnHeader.Tag is ListSortDirection))
                return (ListSortDirection)(columnHeader.Tag = ListSortDirection.Descending);

            ListSortDirection direction = (ListSortDirection)columnHeader.Tag;
            return (ListSortDirection)(columnHeader.Tag = (ListSortDirection)(((int)direction + 1) % 2));
        }

        private void Sort(Comparison<PropertyInformation> comparator, ListSortDirection direction)
        {
            Sort(comparator, direction, this._properties);
            Sort(comparator, direction, this._allProperties);
        }

        private void Sort(Comparison<PropertyInformation> comparator, ListSortDirection direction, ObservableCollection<PropertyInformation> propertiesToSort)
        {
            List<PropertyInformation> sorter = new List<PropertyInformation>(propertiesToSort);
            sorter.Sort(comparator);

            if (direction == ListSortDirection.Descending)
                sorter.Reverse();

            propertiesToSort.Clear();
            foreach (PropertyInformation property in sorter)
                propertiesToSort.Add(property);
        }

        /// <summary>
        /// ִ��ɸѡ
        /// </summary>
        private void ProcessFilter()
        {
            foreach (var property in this._allProperties)
            {
                if (property.IsVisible)
                {
                    if (!this._properties.Contains(property))
                    {
                        InsertInPropertOrder(property);
                    }
                }
                else
                {
                    if (_properties.Contains(property))
                    {
                        this._properties.Remove(property);
                    }
                }
            }

            SetIndexesOfProperties();
        }

        /// <summary>
        /// �������ԣ�����˳��
        /// </summary>
        /// <param name="property"></param>
        private void InsertInPropertOrder(PropertyInformation property)
        {
            if (this._properties.Count == 0)
            {
                this._properties.Add(property);
                return;
            }

            if (PropertiesAreInOrder(property, this._properties[0]))
            {
                this._properties.Insert(0, property);
                return;
            }

            for (int i = 0; i < this._properties.Count - 1; i++)
            {
                if (PropertiesAreInOrder(this._properties[i], property) && PropertiesAreInOrder(property, this._properties[i + 1]))
                {
                    this._properties.Insert(i + 1, property);
                    return;
                }
            }

            this._properties.Add(property);
        }

        /// <summary>
        /// ������������������Ƿ���ȷ
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        private bool PropertiesAreInOrder(PropertyInformation first, PropertyInformation last)
        {
            if (_direction == ListSortDirection.Ascending)
            {
                return first.CompareTo(last) <= 0;
            }
            else
            {
                return last.CompareTo(first) <= 0;
            }
        }

        /// <summary>
        /// �������Ե�<see cref="PropertyInformation.Index"/>
        /// </summary>
        private void SetIndexesOfProperties()
        {
            for (int i = 0; i < this._properties.Count; i++)
            {
                this._properties[i].Index = i;
            }
        }

        private void HandleLoaded(object sender, EventArgs e)
        {
            if (this._unloaded)
            {
                this.RefreshPropertyGrid();
                this._unloaded = false;
            }
        }

        private void HandleUnloaded(object sender, EventArgs e)
        {
            foreach (PropertyInformation property in this._properties)
                property.Teardown();

            _unloaded = true;
        }

        private void HandleNameClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                PropertyInformation property = (PropertyInformation)((FrameworkElement)sender).DataContext;

                object newTarget = null;

                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    newTarget = property.Binding;
                else if (Keyboard.Modifiers == ModifierKeys.Control)
                    newTarget = property.BindingExpression;
                else if (Keyboard.Modifiers == ModifierKeys.None)
                    newTarget = property.Value;

                if (newTarget != null)
                {
                    PropertyInspector.DelveCommand.Execute(property, this);
                }
            }
        }

        private void RefreshPropertyGrid()
        {
            this._allProperties.Clear();
            this._properties.Clear();
            this._visiblePropertyCount = 0;

            this._propertiesToAdd = null;
            this._processIncrementalCall.Enqueue();
        }

        private static int CompareNames(PropertyInformation one, PropertyInformation two)
        {
            // use the PropertyInformation CompareTo method, instead of the string.Compare method
            // so that collections get sorted correctly.
            return one.CompareTo(two);
        }

        private static int CompareValues(PropertyInformation one, PropertyInformation two)
        {
            return string.Compare(one.StringValue, two.StringValue);
        }

        private static int CompareValueSources(PropertyInformation one, PropertyInformation two)
        {
            return string.Compare(one.ValueSource.BaseValueSource.ToString(), two.ValueSource.BaseValueSource.ToString());
        }
    }
}
