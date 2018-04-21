using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Snoop.DebugListenerTab
{
	[Serializable]
	public class FiltersViewModel : INotifyPropertyChanged
	{
	    #region INotifyPropertyChanged Members

	    public event PropertyChangedEventHandler PropertyChanged;

	    protected void OnPropertyChanged(string propertyName)
	    {
	        Debug.Assert(this.GetType().GetProperty(propertyName) != null);
	        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	    }

        #endregion

	    private List<SnoopMultipleFilter> multipleFilters = new List<SnoopMultipleFilter>();
	    private string _filterStatus;

        private bool _isSet;
	   
	    public bool IsSet
	    {
	        get
	        {
	            return _isSet;
	        }
	        set
	        {
	            _isSet = value;
	            OnPropertyChanged("IsSet");
	            FilterStatus = _isSet ? "Filter is ON" : "Filter is OFF";
	        }
	    }

	    public string FilterStatus
	    {
	        get
	        {
	            return _filterStatus;
	        }
	        set
	        {
	            _filterStatus = value;
	            OnPropertyChanged("FilterStatus");
	        }
	    }

	    private readonly ObservableCollection<SnoopFilter> _filters = new ObservableCollection<SnoopFilter>();

	    public IEnumerable<SnoopFilter> Filters
	    {
	        get
	        {
	            return _filters;
	        }
	    }

		public void ResetDirtyFlag()
		{
			_isDirty = false;
			foreach (var filter in this._filters)
			{
				filter.ResetDirtyFlag();
			}
		}

	    private bool _isDirty = false;

        public bool IsDirty
		{
			get
			{
				if (_isDirty)
					return true;

				foreach (var filter in this._filters)
				{
					if (filter.IsDirty)
						return true;
				}
				return false;
			}
		}

		public FiltersViewModel()
		{
			_filters.Add(new SnoopSingleFilter());
			FilterStatus = _isSet ? "Filter is ON" : "Filter is OFF";
		}

		public FiltersViewModel(IList<SnoopSingleFilter> singleFilters)
		{
			InitializeFilters(singleFilters);
		}

		public void InitializeFilters(IList<SnoopSingleFilter> singleFilters)
		{
			this._filters.Clear();

			if (singleFilters == null)
			{
				_filters.Add(new SnoopSingleFilter());
				this.IsSet = false;
				return;
			}

			foreach (var filter in singleFilters)
				this._filters.Add(filter);

			var groupings = (from x in singleFilters where x.IsGrouped select x).GroupBy(x => x.GroupId);
			foreach (var grouping in groupings)
			{
				var multipleFilter = new SnoopMultipleFilter();
				var groupedFilters = grouping.ToArray();
				if (groupedFilters.Length == 0)
					continue;

				multipleFilter.AddRange(groupedFilters, groupedFilters[0].GroupId);
				this.multipleFilters.Add(multipleFilter);
			}

			SetIsSet();
		}

		internal void SetIsSet()
		{
			if (_filters == null)
				this.IsSet = false;

			if (_filters.Count == 1 && _filters[0] is SnoopSingleFilter && string.IsNullOrEmpty(((SnoopSingleFilter)_filters[0]).Text))
				this.IsSet = false;
			else
				this.IsSet = true;
		}

		public void ClearFilters()
		{
			this.multipleFilters.Clear();
			this._filters.Clear();
			this._filters.Add(new SnoopSingleFilter());
			this.IsSet = false;
		}

		public bool FilterMatches(string str)
		{
			foreach (var filter in Filters)
			{
				if (filter.IsGrouped)
					continue;

				if (filter.FilterMatches(str))
					return true;
			}

			foreach (var multipleFilter in this.multipleFilters)
			{
				if (multipleFilter.FilterMatches(str))
					return true;
			}

			return false;
		}

		private string GetFirstNonUsedGroupId()
		{
			int index = 1;
			while (true)
			{
				if (!GroupIdTaken(index.ToString()))
					return index.ToString();

				index++;
			}
		}

		private bool GroupIdTaken(string groupID)
		{
			foreach (var filter in multipleFilters)
			{
				if (groupID.Equals(filter.GroupId))
					return true;
			}
			return false;
		}

		public void GroupFilters(IEnumerable<SnoopFilter> filtersToGroup)
		{
			SnoopMultipleFilter multipleFilter = new SnoopMultipleFilter();
			multipleFilter.AddRange(filtersToGroup, (multipleFilters.Count + 1).ToString());

			multipleFilters.Add(multipleFilter);
		}

		public void AddFilter(SnoopFilter filter)
		{
			_isDirty = true;
			this._filters.Add(filter);
		}

		public void RemoveFilter(SnoopFilter filter)
		{
			_isDirty = true;
			var singleFilter = filter as SnoopSingleFilter;
			if (singleFilter != null)
			{
				//foreach (var multipeFilter in this.multipleFilters)
				int index = 0;
				while (index < this.multipleFilters.Count)
				{
					var multipeFilter = this.multipleFilters[index];
					if (multipeFilter.ContainsFilter(singleFilter))
						multipeFilter.RemoveFilter(singleFilter);

					if (!multipeFilter.IsValidMultipleFilter)
						this.multipleFilters.RemoveAt(index);
					else
						index++;
				}
			}
			this._filters.Remove(filter);
		}

		public void ClearFilterGroups()
		{
			foreach (var filterGroup in this.multipleFilters)
			{
				filterGroup.ClearFilters();
			}
			this.multipleFilters.Clear();
		}
	}
}
