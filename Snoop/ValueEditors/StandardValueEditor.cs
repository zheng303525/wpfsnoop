// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using System;
using System.ComponentModel;
using System.Windows.Data;

namespace Snoop
{
    /// <summary>
    /// Ä¬ÈÏ±ê×¼±à¼­Æ÷
    /// </summary>
	public partial class StandardValueEditor: ValueEditor
    {
        private bool _isUpdatingValue = false;

        public StandardValueEditor()
		{
		}

		public string StringValue
		{
			get { return (string)this.GetValue(StandardValueEditor.StringValueProperty); }
			set { this.SetValue(StandardValueEditor.StringValueProperty, value); }
		}

		public static readonly DependencyProperty StringValueProperty =DependencyProperty.Register("StringValue",typeof(string),typeof(StandardValueEditor),new PropertyMetadata(StandardValueEditor.HandleStringPropertyChanged));

		private static void HandleStringPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((StandardValueEditor)sender).OnStringPropertyChanged((string)e.NewValue);
		}

		protected virtual void OnStringPropertyChanged(string newValue)
		{
			if (this._isUpdatingValue)
				return;

			if (PropertyInfo != null)
			{
				PropertyInfo.IsValueChangedByUser = true;
			}

			Type targetType = this.PropertyType;

			if (targetType.IsAssignableFrom(typeof(string)))
			{
				this.Value = newValue;
			}
			else
			{
				TypeConverter converter = TypeDescriptor.GetConverter(targetType);
				if (converter != null)
				{
					try
					{
						SetValueFromConverter(newValue, targetType, converter);
					}
					catch (Exception)
					{
					}
				}
			}
		}

		private void SetValueFromConverter(string newValue, Type targetType, TypeConverter converter)
		{
			if (!converter.CanConvertFrom(targetType) && string.IsNullOrEmpty(newValue))
			{
				this.Value = null;
			}
			else
			{
				this.Value = converter.ConvertFrom(newValue);
			}
		}

		protected override void OnValueChanged(object newValue)
		{
			this._isUpdatingValue = true;

			object value = this.Value;
			if (value != null)
				this.StringValue = value.ToString();
			else
				this.StringValue = string.Empty;

			this._isUpdatingValue = false;

			BindingExpression binding = BindingOperations.GetBindingExpression(this, StandardValueEditor.StringValueProperty);
			if (binding != null)
				binding.UpdateSource();
		}
	}
}
