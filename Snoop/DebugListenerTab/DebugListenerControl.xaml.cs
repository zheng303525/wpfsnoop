using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Snoop.Infrastructure;
using System.Windows.Threading;

namespace Snoop.DebugListenerTab
{
	/// <summary>
	/// Interaction logic for DebugListenerControl.xaml
	/// </summary>
	public partial class DebugListenerControl : UserControl, IListener
	{
		private readonly FiltersViewModel _filtersViewModel;// = new FiltersViewModel();
		private readonly SnoopDebugListener _snoopDebugListener = new SnoopDebugListener();
        private StringBuilder _allText = new StringBuilder();

		public DebugListenerControl()
		{
			_filtersViewModel = new FiltersViewModel(Properties.Settings.Default.SnoopDebugFilters);
			this.DataContext = _filtersViewModel;

			InitializeComponent();

			_snoopDebugListener.RegisterListener(this);
		}

		private void checkBoxStartListening_Checked(object sender, RoutedEventArgs e)
		{
			Debug.Listeners.Add(_snoopDebugListener);
			PresentationTraceSources.DataBindingSource.Listeners.Add(_snoopDebugListener);
		}

		private void checkBoxStartListening_Unchecked(object sender, RoutedEventArgs e)
		{
			Debug.Listeners.Remove(SnoopDebugListener.ListenerName);
			PresentationTraceSources.DataBindingSource.Listeners.Remove(_snoopDebugListener);
		}

		public void Write(string str)
		{
            _allText.Append(str + Environment.NewLine);
			if (!_filtersViewModel.IsSet || _filtersViewModel.FilterMatches(str))
			{
				this.Dispatcher.BeginInvoke(DispatcherPriority.Render, () => DoWrite(str));
			}
		}

		private void DoWrite(string str)
		{
			this.textBoxDebugContent.AppendText(str + Environment.NewLine);
			this.textBoxDebugContent.ScrollToEnd();
		}
        
		private void buttonClear_Click(object sender, RoutedEventArgs e)
		{
			this.textBoxDebugContent.Clear();
            _allText = new StringBuilder();
		}

		private void buttonClearFilters_Click(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show("Are you sure you want to clear your filters?", "Clear Filters Confirmation", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes)
			{
				_filtersViewModel.ClearFilters();
				Properties.Settings.Default.SnoopDebugFilters = null;
                this.textBoxDebugContent.Text = _allText.ToString();
			}
		}

		private void buttonSetFilters_Click(object sender, RoutedEventArgs e)
		{
			SetFiltersWindow setFiltersWindow = new SetFiltersWindow(_filtersViewModel);
			setFiltersWindow.Topmost = true;
			setFiltersWindow.Owner = Window.GetWindow(this);
			setFiltersWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			setFiltersWindow.ShowDialog();

            string[] allLines = _allText.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            this.textBoxDebugContent.Clear();
            foreach (string line in allLines)
            {
                if (_filtersViewModel.FilterMatches(line))
                    this.textBoxDebugContent.AppendText(line + Environment.NewLine);
            }
		}

		private void comboBoxPresentationTraceLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.comboBoxPresentationTraceLevel == null || this.comboBoxPresentationTraceLevel.Items == null || this.comboBoxPresentationTraceLevel.Items.Count <= this.comboBoxPresentationTraceLevel.SelectedIndex || this.comboBoxPresentationTraceLevel.SelectedIndex < 0)
				return;

			var selectedComboBoxItem = this.comboBoxPresentationTraceLevel.Items[this.comboBoxPresentationTraceLevel.SelectedIndex] as ComboBoxItem;
			if (selectedComboBoxItem == null || selectedComboBoxItem.Tag == null)
				return;


			var sourceLevel = (SourceLevels)Enum.Parse(typeof(SourceLevels), selectedComboBoxItem.Tag.ToString());
			PresentationTraceSources.DataBindingSource.Switch.Level = sourceLevel;
		}
	}
}
