// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Snoop.Infrastructure;
using System.Linq;

namespace Snoop
{
    /// <summary>
    /// 属性信息对象
    /// </summary>
	public class PropertyInformation : DependencyObject, IComparable, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            Debug.Assert(this.GetType().GetProperty(propertyName) != null);
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            int thisIndex = this.CollectionIndex();
            int objIndex = ((PropertyInformation)obj).CollectionIndex();
            if (thisIndex >= 0 && objIndex >= 0)
            {
                return thisIndex.CompareTo(objIndex);
            }
            return this.DisplayName.CompareTo(((PropertyInformation)obj).DisplayName);
        }

        #endregion

        private DispatcherTimer _changeTimer;

        private bool _ignoreUpdate = false;

        private bool _isRunning = false;

        private readonly object _component;
        private readonly bool _isCopyable;

        private readonly object _target;

        /// <summary>
        /// 目标对象
        /// </summary>
	    public object Target
        {
            get { return this._target; }
        }

        private readonly PropertyDescriptor _property;

        /// <summary>
        /// 属性描述
        /// </summary>
        public PropertyDescriptor Property
        {
            get { return this._property; }
        }

        private readonly string _displayName;

        /// <summary>
        /// 属性的显示名称
        /// </summary>
        public string DisplayName
        {
            get { return this._displayName; }
        }

        /// <summary>
        /// Returns the DependencyProperty identifier for the property that this PropertyInformation wraps.
        /// If the wrapped property is not a DependencyProperty, null is returned.
        /// </summary>
        private DependencyProperty DependencyProperty
        {
            get
            {
                if (this._property != null)
                {
                    // in order to be a DependencyProperty, the object must first be a regular property,
                    // and not an item in a collection.

                    DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(this._property);
                    if (dpd != null)
                        return dpd.DependencyProperty;
                }

                return null;
            }
        }

        public Type ComponentType
        {
            get
            {
                if (this._property == null)
                {
                    // if this is a PropertyInformation object constructed for an item in a collection
                    // then this.property will be null, but this.component will contain the collection.
                    // use this object to return the type of the collection for the ComponentType.
                    return this._component.GetType();
                }
                else
                {
                    return this._property.ComponentType;
                }
            }
        }

        public Type PropertyType
        {
            get
            {
                if (this._property == null)
                {
                    // if this is a PropertyInformation object constructed for an item in a collection
                    // just return typeof(object) here, since an item in a collection ... really isn't a property.
                    return typeof(object);
                }
                else
                {
                    return this._property.PropertyType;
                }
            }
        }

        public Type ValueType
        {
            get
            {
                if (this.Value != null)
                {
                    return this.Value.GetType();
                }
                else
                {
                    return typeof(object);
                }
            }
        }

        private string _bindingError = string.Empty;

        /// <summary>
        /// 错误的绑定信息
        /// </summary>
        public string BindingError
        {
            get { return this._bindingError; }
        }

        private bool _isInvalidBinding = false;

        /// <summary>
        /// 是否是无效的绑定
        /// </summary>
        public bool IsInvalidBinding
        {
            get { return this._isInvalidBinding; }
        }

        private bool _isLocallySet = false;

        /// <summary>
        /// 依赖项属性是否存在本地值（不是默认值）
        /// </summary>
        public bool IsLocallySet
        {
            get { return this._isLocallySet; }
        }

        /// <summary>
        /// 是否被用户修改
        /// </summary>
        public bool IsValueChangedByUser { get; set; }

        /// <summary>
        /// 是否可以被编辑
        /// </summary>
        public bool CanEdit
        {
            get
            {
                if (this._property == null)
                {
                    // if this is a PropertyInformation object constructed for an item in a collection
                    //return false;
                    return this._isCopyable;
                }
                else
                {
                    return !this._property.IsReadOnly;
                }
            }
        }

        private bool _isDatabound = false;

        /// <summary>
        /// 该属性是否被绑定
        /// </summary>
        public bool IsDatabound
        {
            get { return this._isDatabound; }
        }

        private ValueSource _valueSource;

        /// <summary>
        /// 依赖项属性的各种特征
        /// </summary>
        public ValueSource ValueSource
        {
            get { return this._valueSource; }
        }

        /// <summary>
        /// 该依赖项属性是否通过表达式计算，如绑定或者动态资源等
        /// </summary>
        public bool IsExpression
        {
            get { return this._valueSource.IsExpression; }
        }

        /// <summary>
        /// 是否对属性进行动画处理
        /// </summary>
        public bool IsAnimated
        {
            get { return this._valueSource.IsAnimated; }
        }

        private int _index = 0;

        /// <summary>
        /// 在属性表格中的排列序号
        /// </summary>
        public int Index
        {
            get { return this._index; }
            set
            {
                if (this._index != value)
                {
                    this._index = value;
                    this.OnPropertyChanged(nameof(Index));
                    this.OnPropertyChanged(nameof(IsOdd));
                }
            }
        }

        /// <summary>
        /// 排列序号是否是奇数
        /// </summary>
        public bool IsOdd
        {
            get { return this._index % 2 == 1; }
        }

        /// <summary>
        /// 该属性的绑定信息
        /// </summary>
        public BindingBase Binding
        {
            get
            {
                DependencyProperty dp = this.DependencyProperty;
                DependencyObject d = this._target as DependencyObject;
                if (dp != null && d != null)
                    return BindingOperations.GetBindingBase(d, dp);
                return null;
            }
        }

        public BindingExpressionBase BindingExpression
        {
            get
            {
                DependencyProperty dp = this.DependencyProperty;
                DependencyObject d = this._target as DependencyObject;
                if (dp != null && d != null)
                    return BindingOperations.GetBindingExpressionBase(d, dp);
                return null;
            }
        }

        private PropertyFilter _filter;

        /// <summary>
        /// 属性筛选器
        /// </summary>
        public PropertyFilter Filter
        {
            get { return this._filter; }
            set
            {
                this._filter = value;

                this.OnPropertyChanged(nameof(IsVisible));
            }
        }

        /// <summary>
        /// 属性是否可见
        /// </summary>
        public bool IsVisible
        {
            get { return this._filter.Show(this); }
        }

        private bool _breakOnChange = false;

        /// <summary>
        /// 当属性改变时，是否启动调试器
        /// </summary>
        public bool BreakOnChange
        {
            get { return this._breakOnChange; }
            set
            {
                this._breakOnChange = value;
                this.OnPropertyChanged(nameof(BreakOnChange));
            }
        }

        private bool _changedRecently = false;

        /// <summary>
        /// 属性是否最近有更改，持续1.5秒
        /// </summary>
        public bool HasChangedRecently
        {
            get { return this._changedRecently; }
            set
            {
                this._changedRecently = value;
                this.OnPropertyChanged(nameof(HasChangedRecently));
            }
        }

        /// <summary>
        /// 显示在界面上的字符串数据，可以通过字符串设置属性值
        /// </summary>
        public string StringValue
        {
            get
            {
                object value = this.Value;
                if (value != null)
                    return value.ToString();
                return string.Empty;
            }
            set
            {
                if (this._property == null)
                {
                    // if this is a PropertyInformation object constructed for an item in a collection
                    // then just return, since setting the value via a string doesn't make sense.
                    return;
                }

                Type targetType = this._property.PropertyType;
                if (targetType.IsAssignableFrom(typeof(string)))
                {
                    this._property.SetValue(this._target, value);
                }
                else
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(targetType);
                    if (converter != null)
                    {
                        try
                        {
                            this._property.SetValue(this._target, converter.ConvertFrom(value));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 属性值的描述
        /// </summary>
        public string DescriptiveValue
        {
            get
            {
                object value = this.Value;
                if (value == null)
                {
                    return string.Empty;
                }

                string stringValue = value.ToString();

                if (stringValue.Equals(value.GetType().ToString()))
                {
                    // Add brackets around types to distinguish them from values.
                    // Replace long type names with short type names for some specific types, for easier readability.
                    // FUTURE: This could be extended to other types.
                    if (this._property != null &&
                        (this._property.PropertyType == typeof(Brush) || this._property.PropertyType == typeof(Style)))
                    {
                        stringValue = $"[{value.GetType().Name}]";
                    }
                    else
                    {
                        stringValue = $"[{stringValue}]";
                    }
                }

                // Display #00FFFFFF as Transparent for easier readability
                if (this._property != null &&
                    this._property.PropertyType == typeof(Brush) &&
                    stringValue.Equals("#00FFFFFF"))
                {
                    stringValue = "Transparent";
                }

                DependencyObject dependencyObject = this.Target as DependencyObject;
                if (dependencyObject != null && this.DependencyProperty != null)
                {
                    // Cache the resource key for this item if not cached already. This could be done for more types, but would need to optimize perf.
                    string resourceKey = null;
                    if (this._property != null &&
                        (this._property.PropertyType == typeof(Style) || this._property.PropertyType == typeof(Brush)))
                    {
                        object resourceItem = dependencyObject.GetValue(this.DependencyProperty);
                        resourceKey = ResourceKeyCache.GetKey(resourceItem);
                        if (string.IsNullOrEmpty(resourceKey))
                        {
                            resourceKey = ResourceDictionaryKeyHelpers.GetKeyOfResourceItem(dependencyObject, this.DependencyProperty);
                            ResourceKeyCache.Cache(resourceItem, resourceKey);
                        }
                        Debug.Assert(resourceKey != null);
                    }

                    // Display both the value and the resource key, if there's a key for this property.
                    if (!string.IsNullOrEmpty(resourceKey))
                    {
                        return string.Format("{0} {1}", resourceKey, stringValue);
                    }

                    // if the value comes from a Binding, show the path in [] brackets
                    if (IsExpression && Binding is Binding)
                    {
                        stringValue = string.Format("{0} {1}", stringValue, BuildBindingDescriptiveString((Binding)Binding, true));
                    }

                    // if the value comes from a MultiBinding, show the binding paths separated by , in [] brackets
                    else if (IsExpression && Binding is MultiBinding)
                    {
                        stringValue = stringValue + BuildMultiBindingDescriptiveString(((MultiBinding)this.Binding).Bindings.OfType<Binding>().ToArray());
                    }

                    // if the value comes from a PriorityBinding, show the binding paths separated by , in [] brackets
                    else if (IsExpression && Binding is PriorityBinding)
                    {
                        stringValue = stringValue + BuildMultiBindingDescriptiveString(((PriorityBinding)this.Binding).Bindings.OfType<Binding>().ToArray());
                    }
                }

                return stringValue;
            }
        }

        /// <summary>
        /// Normal constructor used when constructing PropertyInformation objects for properties.
        /// </summary>
        /// <param name="target">target object being shown in the property grid</param>
        /// <param name="property">the property around which we are contructing this PropertyInformation object</param>
        /// <param name="propertyName">the property name for the property that we use in the binding in the case of a non-dependency property</param>
        /// <param name="propertyDisplayName">the display name for the property that goes in the name column</param>
        public PropertyInformation(object target, PropertyDescriptor property, string propertyName, string propertyDisplayName)
        {
            this._target = target;
            this._property = property;
            this._displayName = propertyDisplayName;

            if (property != null)
            {
                // create a data binding between the actual property value on the target object
                // and the Value dependency property on this PropertyInformation object
                Binding binding;
                DependencyProperty dp = this.DependencyProperty;
                if (dp != null)
                {
                    binding = new Binding();
                    binding.Path = new PropertyPath("(0)", new object[] { dp });
                }
                else
                {
                    binding = new Binding(propertyName);
                }

                binding.Source = target;
                binding.Mode = property.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;

                try
                {
                    BindingOperations.SetBinding(this, PropertyInformation.ValueProperty, binding);
                }
                catch (Exception)
                {
                    // cplotts note:
                    // warning: i saw a problem get swallowed by this empty catch (Exception) block.
                    // in other words, this empty catch block could be hiding some potential future errors.
                }
            }

            this.Update();

            this._isRunning = true;
        }

        /// <summary>
        /// Constructor used when constructing PropertyInformation objects for an item in a collection.
        /// In this case, we set the PropertyDescriptor for this object (in the property Property) to be null.
        /// This kind of makes since because an item in a collection really isn't a property on a class.
        /// That is, in this case, we're really hijacking the PropertyInformation class
        /// in order to expose the items in the Snoop property grid.
        /// </summary>
        /// <param name="target">the item in the collection</param>
        /// <param name="component">the collection</param>
        /// <param name="displayName">the display name that goes in the name column, i.e. this[x]</param>
        /// <param name="isCopyable"></param>
        public PropertyInformation(object target, object component, string displayName, bool isCopyable = false) : this(target, null, displayName, displayName)
        {
            this._component = component;
            this._isCopyable = isCopyable;
        }

        /// <summary>
        /// 属性值
        /// </summary>
        public object Value
        {
            get { return this.GetValue(PropertyInformation.ValueProperty); }
            set { this.SetValue(PropertyInformation.ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(PropertyInformation), new PropertyMetadata(new PropertyChangedCallback(PropertyInformation.HandleValueChanged)));

        private static void HandleValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PropertyInformation)d).OnValueChanged();
        }

        protected virtual void OnValueChanged()
        {
            this.Update();

            if (this._isRunning)
            {
                if (this._breakOnChange)
                {
                    if (!Debugger.IsAttached)
                        Debugger.Launch();
                    Debugger.Break();
                }

                this.HasChangedRecently = true;
                if (this._changeTimer == null)
                {
                    this._changeTimer = new DispatcherTimer();
                    this._changeTimer.Interval = TimeSpan.FromSeconds(1.5);
                    this._changeTimer.Tick += this.HandleChangeExpiry;
                    this._changeTimer.Start();
                }
                else
                {
                    this._changeTimer.Stop();
                    this._changeTimer.Start();
                }
            }
        }

        private void HandleChangeExpiry(object sender, EventArgs e)
        {
            this._changeTimer.Stop();
            this._changeTimer = null;

            this.HasChangedRecently = false;
        }

        /// <summary>
        /// 集合中的值，在集合中的序号
        /// </summary>
        /// <returns></returns>
        public int CollectionIndex()
        {
            if (this.IsCollection())
            {
                return int.Parse(this.DisplayName.Substring(5, this.DisplayName.Length - 6));
            }
            return -1;
        }

        /// <summary>
        /// 是否是集合中的值（集合中的值，会显示在集合属性列表中）
        /// </summary>
        /// <returns></returns>
        public bool IsCollection()
        {
            string pattern = "^this\\[\\d+\\]$";
            return Regex.IsMatch(this.DisplayName, pattern);
        }

        /// <summary>
        /// 清空属性值
        /// </summary>
        public void Clear()
        {
            DependencyProperty dp = this.DependencyProperty;
            DependencyObject d = this._target as DependencyObject;
            if (dp != null && d != null)
                ((DependencyObject)this._target).ClearValue(dp);
        }

        /// <summary>
        /// 清除界面属性绑定
        /// </summary>
        public void Teardown()
        {
            this._isRunning = false;
            BindingOperations.ClearAllBindings(this);
        }

        /// <summary>
        /// Build up a string of Paths for a MultiBinding separated by ;
        /// </summary>
        private string BuildMultiBindingDescriptiveString(IEnumerable<Binding> bindings)
        {
            string ret = " {Paths=";
            foreach (Binding binding in bindings)
            {
                ret += BuildBindingDescriptiveString(binding, false);
                ret += ";";
            }
            ret = ret.Substring(0, ret.Length - 1); // remove trailing ,
            ret += "}";

            return ret;
        }

        /// <summary>
        /// Build up a string describing the Binding.  Path and ElementName (if present)
        /// </summary>
        private string BuildBindingDescriptiveString(Binding binding, bool isSinglePath)
        {
            var sb = new StringBuilder();
            var bindingPath = binding.Path.Path;
            var elementName = binding.ElementName;

            if (isSinglePath)
            {
                sb.Append("{Path=");
            }

            sb.Append(bindingPath);
            if (!String.IsNullOrEmpty(elementName))
            {
                sb.AppendFormat(", ElementName={0}", elementName);
            }

            if (isSinglePath)
            {
                sb.Append("}");
            }

            return sb.ToString();
        }

        private void Update()
        {
            if (_ignoreUpdate)
                return;

            this._isLocallySet = false;
            this._isInvalidBinding = false;
            this._isDatabound = false;

            DependencyProperty dp = this.DependencyProperty;
            DependencyObject d = _target as DependencyObject;

            if (SnoopModes.MultipleDispatcherMode && d != null && d.Dispatcher != this.Dispatcher)
                return;

            if (dp != null && d != null)
            {
                if (d.ReadLocalValue(dp) != DependencyProperty.UnsetValue)
                    this._isLocallySet = true;

                BindingExpressionBase expression = BindingOperations.GetBindingExpressionBase(d, dp);
                if (expression != null)
                {
                    this._isDatabound = true;

                    if (expression.HasError || expression.Status != BindingStatus.Active && !(expression is PriorityBindingExpression))
                    {
                        this._isInvalidBinding = true;

                        StringBuilder builder = new StringBuilder();
                        StringWriter writer = new StringWriter(builder);
                        TextWriterTraceListener tracer = new TextWriterTraceListener(writer);
                        PresentationTraceSources.DataBindingSource.Listeners.Add(tracer);

                        // reset binding to get the error message.
                        _ignoreUpdate = true;
                        d.ClearValue(dp);
                        BindingOperations.SetBinding(d, dp, expression.ParentBindingBase);
                        _ignoreUpdate = false;

                        // cplotts note: maciek ... are you saying that this is another, more concise way to dispatch the following code?
                        //Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
                        //    {
                        //        bindingError = builder.ToString();
                        //        this.OnPropertyChanged("BindingError");
                        //        PresentationTraceSources.DataBindingSource.Listeners.Remove(tracer);
                        //        writer.Close();
                        //    });

                        // this needs to happen on idle so that we can actually run the binding, which may occur asynchronously.
                        Dispatcher.BeginInvoke
                        (
                            DispatcherPriority.ApplicationIdle,
                            new DispatcherOperationCallback
                            (
                                delegate (object source)
                                {
                                    _bindingError = builder.ToString();
                                    this.OnPropertyChanged(nameof(BindingError));
                                    PresentationTraceSources.DataBindingSource.Listeners.Remove(tracer);
                                    writer.Close();
                                    return null;
                                }
                            ),
                            null
                        );
                    }
                    else
                    {
                        _bindingError = string.Empty;
                    }
                }

                this._valueSource = DependencyPropertyHelper.GetValueSource(d, dp);
            }

            this.OnPropertyChanged(nameof(IsLocallySet));
            this.OnPropertyChanged(nameof(IsInvalidBinding));
            this.OnPropertyChanged(nameof(StringValue));
            this.OnPropertyChanged(nameof(DescriptiveValue));
            this.OnPropertyChanged(nameof(IsDatabound));
            this.OnPropertyChanged(nameof(IsExpression));
            this.OnPropertyChanged(nameof(IsAnimated));
            this.OnPropertyChanged(nameof(ValueSource));
        }

        /// <summary>
        /// 传入一个对象，返回该对象的所有属性信息列表
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
		public static List<PropertyInformation> GetProperties(object obj)
        {
            return PropertyInformation.GetProperties(obj, new PertinentPropertyFilter(obj).Filter);
        }

        /// <summary>
        /// 传入一个对象及对应的属性筛选器，返回属性信息列表
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
		public static List<PropertyInformation> GetProperties(object obj, Predicate<PropertyDescriptor> filter)
        {
            List<PropertyInformation> props = new List<PropertyInformation>();

            // get the properties
            List<PropertyDescriptor> propertyDescriptors = GetAllProperties(obj, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) });

            // filter the properties
            foreach (PropertyDescriptor property in propertyDescriptors)
            {
                if (filter(property))
                {
                    PropertyInformation prop = new PropertyInformation(obj, property, property.Name, property.DisplayName);
                    props.Add(prop);
                }
            }

            //delve path. also, issue 4919
            var extendedProps = GetExtendedProperties(obj);
            props.AddRange(extendedProps);

            // if the object is a collection, add the items in the collection as properties
            ICollection collection = obj as ICollection;
            int index = 0;
            if (collection != null)
            {
                foreach (object item in collection)
                {
                    PropertyInformation info = new PropertyInformation(item, collection, "this[" + index + "]");
                    index++;
                    info.Value = item;
                    props.Add(info);
                }
            }

            // sort the properties
            props.Sort();

            return props;
        }

        /// <summary>
        /// 4919 + Delve
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
		private static IList<PropertyInformation> GetExtendedProperties(object obj)
        {
            List<PropertyInformation> props = new List<PropertyInformation>();

            if (obj != null && ResourceKeyCache.Contains(obj))
            {
                string key = ResourceKeyCache.GetKey(obj);
                PropertyInformation prop = new PropertyInformation(key, new object(), "x:Key", true);
                prop.Value = key;
                props.Add(prop);
            }

            return props;
        }

        /// <summary>
        /// 获取对象的所有属性的<see cref="PropertyDescriptor"/>格式列表
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
		private static List<PropertyDescriptor> GetAllProperties(object obj, Attribute[] attributes)
        {
            List<PropertyDescriptor> propertiesToReturn = new List<PropertyDescriptor>();

            // keep looping until you don't have an AmbiguousMatchException exception
            // and you normally won't have an exception, so the loop will typically execute only once.
            bool noException = false;
            while (!noException && obj != null)
            {
                try
                {
                    // try to get the properties using the GetProperties method that takes an instance
                    var properties = TypeDescriptor.GetProperties(obj, attributes);
                    noException = true;

                    MergeProperties(properties, propertiesToReturn);
                }
                catch (System.Reflection.AmbiguousMatchException)
                {
                    // if we get an AmbiguousMatchException, the user has probably declared a property that hides a property in an ancestor
                    // see issue 6258 (http://snoopwpf.codeplex.com/workitem/6258)
                    //
                    // public class MyButton : Button
                    // {
                    //     public new double? Width
                    //     {
                    //         get { return base.Width; }
                    //         set { base.Width = value.Value; }
                    //     }
                    // }

                    Type t = obj.GetType();
                    var properties = TypeDescriptor.GetProperties(t, attributes);

                    MergeProperties(properties, propertiesToReturn);

                    var nextBaseTypeWithDefaultConstructor = GetNextTypeWithDefaultConstructor(t);
                    obj = Activator.CreateInstance(nextBaseTypeWithDefaultConstructor);
                }
            }

            return propertiesToReturn;
        }

        /// <summary>
        /// 是否有默认构造函数
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool HasDefaultConstructor(Type type)
        {
            var constructors = type.GetConstructors();

            foreach (var constructor in constructors)
            {
                if (constructor.GetParameters().Length == 0)
                    return true;
            }
            return false;

        }

        /// <summary>
        /// 查询有默认构造函数的基类
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type GetNextTypeWithDefaultConstructor(Type type)
        {
            var t = type.BaseType;

            while (!HasDefaultConstructor(t))
                t = t.BaseType;

            return t;
        }

        /// <summary>
        /// 合并属性列表
        /// </summary>
        /// <param name="newProperties"></param>
        /// <param name="allProperties"></param>
		private static void MergeProperties(IEnumerable newProperties, ICollection<PropertyDescriptor> allProperties)
        {
            foreach (var newProperty in newProperties)
            {
                PropertyDescriptor newPropertyDescriptor = newProperty as PropertyDescriptor;
                if (newPropertyDescriptor == null)
                    continue;

                if (!allProperties.Contains(newPropertyDescriptor))
                    allProperties.Add(newPropertyDescriptor);
            }
        }
    }
}
