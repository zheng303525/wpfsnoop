// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows.Controls;

namespace Snoop
{
    /// <summary>
    /// <see cref="PropertyGrid2"/>»ùÀà
    /// </summary>
	public class Inspector : Grid
    {
        private PropertyFilter _filter;

        public PropertyFilter Filter
        {
            get { return this._filter; }
            set
            {
                this._filter = value;
                this.OnFilterChanged();
            }
        }

        protected virtual void OnFilterChanged() { }
    }
}
