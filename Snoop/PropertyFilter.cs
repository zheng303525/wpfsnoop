// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Text.RegularExpressions;

namespace Snoop
{
    /// <summary>
    /// 属性筛选器
    /// </summary>
	public class PropertyFilter
    {
        private Regex _filterRegex;

        private bool _showDefaults;

        /// <summary>
        /// 是否显示依赖项属性为默认值的属性
        /// </summary>
	    public bool ShowDefaults
        {
            get { return this._showDefaults; }
            set { this._showDefaults = value; }
        }

        private string _filterString;

        /// <summary>
        /// 筛选文本字符串
        /// </summary>
        public string FilterString
        {
            get { return this._filterString; }
            set
            {
                this._filterString = value.ToLower();
                try
                {
                    this._filterRegex = new Regex(this._filterString, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                }
                catch
                {
                    this._filterRegex = null;
                }
            }
        }

        /// <summary>
        /// 选中的筛选组
        /// </summary>
	    public PropertyFilterSet SelectedFilterSet { get; set; }

        /// <summary>
        /// 是否使用筛选组
        /// </summary>
	    public bool IsPropertyFilterSet
        {
            get
            {
                return (SelectedFilterSet != null && SelectedFilterSet.Properties != null);
            }
        }

        public PropertyFilter(string filterString, bool showDefaults)
        {
            this._filterString = filterString.ToLower();
            this._showDefaults = showDefaults;
        }

        /// <summary>
        /// 筛选回调函数
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
		public bool Show(PropertyInformation property)
        {
            // use a regular expression if we have one and we also have a filter string.
            if (this._filterRegex != null && !string.IsNullOrEmpty(this.FilterString))
            {
                return
                (
                    this._filterRegex.IsMatch(property.DisplayName) ||
                    property.Property != null && this._filterRegex.IsMatch(property.Property.PropertyType.Name)
                );
            }
            // else just check for containment if we don't have a regular expression but we do have a filter string.
            else if (!string.IsNullOrEmpty(this.FilterString))
            {
                if (property.DisplayName.ToLower().Contains(this.FilterString))
                    return true;
                if (property.Property != null && property.Property.PropertyType.Name.ToLower().Contains(this.FilterString))
                    return true;
                return false;
            }
            // else use the filter set if we have one of those.
            else if (IsPropertyFilterSet)
            {
                if (SelectedFilterSet.IsPropertyInFilter(property.DisplayName))
                    return true;
                else
                    return false;
            }
            // finally, if none of the above applies
            // just check to see if we're not showing properties at their default values
            // and this property is actually set to its default value
            else
            {
                if (!this.ShowDefaults && property.ValueSource.BaseValueSource == BaseValueSource.Default)
                    return false;
                else
                    return true;
            }
        }
    }

    /// <summary>
    /// 筛选器组
    /// </summary>
    [Serializable]
    public class PropertyFilterSet
    {
        /// <summary>
        /// 组名称
        /// </summary>
        public string DisplayName
        {
            get;
            set;
        }

        public bool IsDefault
        {
            get;
            set;
        }

        public bool IsEditCommand
        {
            get;
            set;
        }

        /// <summary>
        /// 属性组
        /// </summary>
        public string[] Properties
        {
            get;
            set;
        }

        /// <summary>
        /// 是否属于筛选组
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
		public bool IsPropertyInFilter(string property)
        {
            string lowerProperty = property.ToLower();
            foreach (var filterProp in Properties)
            {
                if (lowerProperty.StartsWith(filterProp))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
