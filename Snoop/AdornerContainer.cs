// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;

namespace Snoop
{
    /// <summary>
    /// Simple helper class to allow any UIElements to be used as an Adorner.
    /// </summary>
    public class AdornerContainer : Adorner
    {
        private UIElement _child;

        public UIElement Child
        {
            get { return this._child; }
            set
            {
                this.AddVisualChild(value);
                this._child = value;
            }
        }

        public AdornerContainer(UIElement adornedElement) : base(adornedElement)
        {
        }

        protected override int VisualChildrenCount
        {
            get { return this._child == null ? 0 : 1; }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0 && this._child != null)
                return this._child;
            return base.GetVisualChild(index);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this._child != null)
                this._child.Arrange(new Rect(finalSize));
            return finalSize;
        }
    }
}
