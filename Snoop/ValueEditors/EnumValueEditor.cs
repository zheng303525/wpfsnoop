// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Windows.Data;

namespace Snoop
{
    /// <summary>
    /// Ã¶¾ÙÀàÐÍ±à¼­Æ÷
    /// </summary>
	public partial class EnumValueEditor : ValueEditor
    {
        private bool _isValid = false;
        private readonly ListCollectionView _valuesView;

        public EnumValueEditor()
		{
			this._valuesView = (ListCollectionView)CollectionViewSource.GetDefaultView(this._values);
			this._valuesView.CurrentChanged += this.HandleSelectionChanged;
		}

	    private readonly List<object> _values = new List<object>();

        public IList<object> Values
		{
			get { return this._values; }
		}

		protected override void OnTypeChanged()
		{
			base.OnTypeChanged();

			this._isValid = false;

			this._values.Clear();

			Type propertyType = this.PropertyType;
			if (propertyType != null)
			{
				Array values = Enum.GetValues(propertyType);
				foreach(object value in values)
				{
					this._values.Add(value);

					if (this.Value != null && this.Value.Equals(value))
						this._valuesView.MoveCurrentTo(value);
				}
			}

			this._isValid = true;
		}

		protected override void OnValueChanged(object newValue)
		{
			base.OnValueChanged(newValue);

			this._valuesView.MoveCurrentTo(newValue);

			// sneaky trick here.  only if both are non-null is this a change
			// caused by the user.  If so, set the bool to track it.
			if ( PropertyInfo != null && newValue != null )
			{
				PropertyInfo.IsValueChangedByUser = true;
			}
		}

		private void HandleSelectionChanged(object sender, EventArgs e)
		{
			if (this._isValid && this.Value != null)
			{
				if (!this.Value.Equals(this._valuesView.CurrentItem))
					this.Value = this._valuesView.CurrentItem;
			}
		}
	}
}
