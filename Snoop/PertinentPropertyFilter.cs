// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Windows;

namespace Snoop
{
    /// <summary>
    /// ��������ɸѡ��
    /// </summary>
	public class PertinentPropertyFilter
    {
        /// <summary>
        /// Ŀ�����
        /// </summary>
        private readonly object _target;

        /// <summary>
        /// Ŀ������Ƿ���<see cref="FrameworkElement"/>���������ΪĿ����󣬷���Ϊ�ա�
        /// </summary>
        private readonly FrameworkElement _element;

        public PertinentPropertyFilter(object target)
		{
			this._target = target;
			this._element = this._target as FrameworkElement;
		}

        /// <summary>
        /// ɸѡ����
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
		public bool Filter(PropertyDescriptor property)
		{
			if (this._element == null)//�������FrameworkElement����������ɸѡ��ȫ������true
                return true;

			// Filter the 20 stylistic set properties that I've never seen used.
			if (property.Name.Contains("Typography.StylisticSet"))
				return false;

			AttachedPropertyBrowsableForChildrenAttribute attachedPropertyForChildren = (AttachedPropertyBrowsableForChildrenAttribute)property.Attributes[typeof(AttachedPropertyBrowsableForChildrenAttribute)];
			AttachedPropertyBrowsableForTypeAttribute attachedPropertyForType = (AttachedPropertyBrowsableForTypeAttribute)property.Attributes[typeof(AttachedPropertyBrowsableForTypeAttribute)];
			AttachedPropertyBrowsableWhenAttributePresentAttribute attachedPropertyForAttribute = (AttachedPropertyBrowsableWhenAttributePresentAttribute)property.Attributes[typeof(AttachedPropertyBrowsableWhenAttributePresentAttribute)];

			if (attachedPropertyForChildren != null)
			{
				DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(property);
				if (dpd == null)
					return false;

				FrameworkElement element = this._element;
				do
				{
					element = element.Parent as FrameworkElement;
					if (element != null && dpd.DependencyProperty.OwnerType.IsInstanceOfType(element))
						return true;
				}
				while (attachedPropertyForChildren.IncludeDescendants && element != null);
				return false;
			}
			else if (attachedPropertyForType != null)
			{
				// when using [AttachedPropertyBrowsableForType(typeof(IMyInterface))] and IMyInterface is not a DependencyObject, Snoop crashes.
				// see http://snoopwpf.codeplex.com/workitem/6712

				if (attachedPropertyForType.TargetType.IsSubclassOf(typeof(DependencyObject)))
				{
					DependencyObjectType doType = DependencyObjectType.FromSystemType(attachedPropertyForType.TargetType);
					if (doType != null && doType.IsInstanceOfType(this._element))
						return true;
				}

				return false;
			}
			else if (attachedPropertyForAttribute != null)
			{
				Attribute dependentAttribute = TypeDescriptor.GetAttributes(this._target)[attachedPropertyForAttribute.AttributeType];
				if (dependentAttribute != null)
					return !dependentAttribute.IsDefaultAttribute();
				return false;
			}

			return true;
		}
	}
}
