// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Snoop
{
    /// <summary>
    /// 属性编辑模板选择器
    /// </summary>
	public class ValueEditorTemplateSelector : DataTemplateSelector
    {
        private DataTemplate _standardTemplate;

        public DataTemplate StandardTemplate
        {
            get { return this._standardTemplate; }
            set { this._standardTemplate = value; }
        }

        private DataTemplate _enumTemplate;

        /// <summary>
        /// 枚举类型编辑模板
        /// </summary>
        public DataTemplate EnumTemplate
        {
            get { return this._enumTemplate; }
            set { this._enumTemplate = value; }
        }

        private DataTemplate _boolTemplate;

        /// <summary>
        /// 布尔类型编辑模板
        /// </summary>
        public DataTemplate BoolTemplate
        {
            get { return this._boolTemplate; }
            set { this._boolTemplate = value; }
        }

        private DataTemplate _brushTemplate;

        /// <summary>
        /// <see cref="Brush"/>类型编辑模板
        /// </summary>
        public DataTemplate BrushTemplate
        {
            get { return this._brushTemplate; }
            set { this._brushTemplate = value; }
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            PropertyInformation property = (PropertyInformation)item;

            if (property.PropertyType.IsEnum)
                return this.EnumTemplate;
            else if (property.PropertyType.Equals(typeof(bool)))
                return this.BoolTemplate;
            else if (property.PropertyType.IsGenericType
                && Nullable.GetUnderlyingType(property.PropertyType) == typeof(bool))
                return this.BoolTemplate;
            else if (typeof(Brush).IsAssignableFrom(property.PropertyType))
                return this._brushTemplate;

            return this.StandardTemplate;
        }
    }
}
