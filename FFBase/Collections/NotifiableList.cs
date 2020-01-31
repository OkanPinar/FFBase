using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace FFBase.Collections
{
   public class ListItemPropertyDescriptor<T>: PropertyDescriptor
    {
        private readonly IList<T> _owner;
        private readonly int _index;

        public ListItemPropertyDescriptor(IList<T> owner, int index): base($"[{index}]", null)
        {
            this._owner = owner;
            this._index = index;
        }

        public override AttributeCollection Attributes
        {
            get
            {
                var attributes = TypeDescriptor.GetAttributes(GetValue(null), false);
                if (!attributes.OfType<ExpandableObjectAttribute>().Any())
                {
                    attributes = AddAttribute(new ExpandableObjectAttribute(), attributes);
                }
                attributes = AddAttribute(new PropertyOrderAttribute(_index), attributes);

                return attributes;
            }
        }

        private AttributeCollection AddAttribute(Attribute newAttribute, AttributeCollection oldAttributes)
        {
            Attribute[] newAttributes = new Attribute[oldAttributes.Count + 1];
            oldAttributes.CopyTo(newAttributes, 1);
            newAttributes[0] = newAttribute;

            return new AttributeCollection(newAttributes);
        }

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object component) => Value;

        private T Value => _owner[_index];

        public override void SetValue(object component, object value)
        {
            string s = "";
            _owner[_index] = (T)value;
        }

        public override bool ShouldSerializeValue(object component) => false;

        public override void ResetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override Type ComponentType => _owner.GetType();

        public override bool IsReadOnly => false;

        public override Type PropertyType => Value?.GetType();
    }

    public class  MyExpandableIListConverter<T>: ExpandableObjectConverter
    {
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            if (value is IList<T> list)
            {
                PropertyDescriptorCollection propDescriptions = new PropertyDescriptorCollection(null);
                IEnumerator enumerator = list.GetEnumerator();
                int counter = -1;
                while (enumerator.MoveNext())
                {
                    counter++;
                    propDescriptions.Add(new ListItemPropertyDescriptor<T>(list, counter));
                }
                return propDescriptions;
            }

            else
            {
                return base.GetProperties(context, value, attributes);
            }
        }
    }

    public class NotifiableCollectionValueChangedEventArgs
    {
        public int Index { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public NotifiableCollectionValueChangedEventArgs(int index, object oldVal, object newVal)
        {
            Index = index;
            OldValue = oldVal;
            NewValue = newVal;
        }
    }

    public class NotifiableCollectionChangedEventArgs
    {
        public IEnumerable NewItems { get; set; }
        public IEnumerable OldItems { get; set; }

        public CollectionChangeAction CollectionAction { get; set; }

        public NotifiableCollectionChangedEventArgs(IEnumerable addedItems, IEnumerable removedItems, CollectionChangeAction collectionAction)
        {
            NewItems = addedItems;
            OldItems = removedItems;
            CollectionAction = collectionAction;
        }

    }

    public delegate void NotifiableCollectionValueChanged(object sender, NotifiableCollectionValueChangedEventArgs e);
    public delegate void NotifiableCollectionChanged(object sender, NotifiableCollectionChangedEventArgs e);

    public interface INotifiableCollection
    {
        event NotifiableCollectionValueChanged NotifyValueChanged;
        event NotifiableCollectionChanged NotifyCollectionChaged;
    }

    public class ItemPropertyDescriptor<T>: PropertyDescriptor
    {
        private readonly IList<T> _owner;
        private readonly int _index;

        public ItemPropertyDescriptor(IList<T> owner, int index): base("#" + index, null)
        {
            _owner = owner;
            _index = index;
        }

        public override AttributeCollection Attributes
        {
            get
            {
                var attributes = TypeDescriptor.GetAttributes(GetValue(null), false);

                if (!attributes.OfType<ExpandableObjectAttribute>().Any())
                {
                    var newAttributes = new Attribute[attributes.Count + 1];
                    attributes.CopyTo(newAttributes, newAttributes.Length - 1);
                    newAttributes[0] = new ExpandableObjectAttribute();

                    attributes = new AttributeCollection(newAttributes);
                }
                return attributes;
            }
        }

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object component) => Value;

        private T Value => _owner[_index];

        public override void SetValue(object component, object value) => _owner[_index] = (T)value;

        public override void ResetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override bool ShouldSerializeValue(object component) => false;

        public override Type ComponentType => _owner.GetType();

        public override bool IsReadOnly => false;

        public override Type PropertyType => Value?.GetType();

    }

    public class NotifiableList<T>: IList<T>, ICustomTypeDescriptor, INotifiableCollection
	{
        private readonly List<T> _items = new List<T>();

        public event NotifiableCollectionValueChanged NotifyValueChanged;
        public event NotifiableCollectionChanged NotifyCollectionChaged;

        public NotifiableList()
        {

        }

        public T this[int index] 
        {
            get => _items[index];
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_items[index], value))
                {
                    T oldValue = _items[index];
                    _items[index] = value;
                    NotifyValueChanged?.Invoke(this, new NotifiableCollectionValueChangedEventArgs(index, oldValue, value));
                }
            }
        }

        public void AddRange(IEnumerable<T> collection) 
        {
            _items.AddRange(collection);
            NotifyCollectionChaged?.Invoke(this, new NotifiableCollectionChangedEventArgs(collection, null, CollectionChangeAction.Add));
        }

        #region IList Implementation
        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public void Add(T item) 
        {
            _items.Add(item);
            NotifyCollectionChaged?.Invoke(this, new NotifiableCollectionChangedEventArgs(new T[]{ item }, null, CollectionChangeAction.Add));
        }

        public void Clear()
        {
            NotifyCollectionChaged?.Invoke(this, new NotifiableCollectionChangedEventArgs(_items, null, CollectionChangeAction.Remove));
            _items.Clear();   
        }

        public bool Contains(T item) => _items.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        public int IndexOf(T item) => _items.IndexOf(item);

        public void Insert(int index, T item)
        {
            T oldValue = _items[index];
            _items.Insert(index, item);
            NotifyValueChanged?.Invoke(this, new NotifiableCollectionValueChangedEventArgs(index, oldValue, item));
        }

        public bool Remove(T item)
        {
            NotifyCollectionChaged?.Invoke(this, new NotifiableCollectionChangedEventArgs(null, new T[] { item }, CollectionChangeAction.Remove));
            return _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            var item = _items[index];
            NotifyCollectionChaged?.Invoke(this, new NotifiableCollectionChangedEventArgs(null, new T[] { item }, CollectionChangeAction.Remove));
            _items.RemoveAt(index);
        }

        public void RemoveRange(IEnumerable<T> items)
        {
            NotifyCollectionChaged?.Invoke(this, new NotifiableCollectionChangedEventArgs(null, items, CollectionChangeAction.Remove));
            foreach (var item in items)
            {
                _items.Remove(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
        #endregion

        #region CustomTypeDescription
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            PropertyDescriptorCollection pds = new PropertyDescriptorCollection(null);

            for (int i = 0; i < Count; i++)
            {
                pds.Add(new ItemPropertyDescriptor<T>(this, i));
            }

            return pds;
        }

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, noCustomTypeDesc: true);
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return TypeDescriptor.GetClassName(this, noCustomTypeDesc: true);
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, noCustomTypeDesc: true);
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(this, noCustomTypeDesc: true);
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, noCustomTypeDesc: true);
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, noCustomTypeDesc: true);
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, noCustomTypeDesc: true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, noCustomTypeDesc: true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, noCustomTypeDesc: true);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(this, attributes, noCustomTypeDesc: true);
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
		#endregion

	}

	public static class TypeDecorationManager
    {
        public static void AddExpandableObjectConverter(Type T)
        {
            TypeDescriptor.AddAttributes(T, new TypeConverterAttribute(typeof(ExpandableObjectConverter)));
            TypeDescriptor.AddAttributes(T, new ExpandableObjectAttribute());
        }
        public static void AddExpandableIListConverter<I>(Type T)
        {
            TypeDescriptor.AddAttributes(T, new TypeConverterAttribute(typeof(MyExpandableIListConverter<I>)));
            TypeDescriptor.AddAttributes(T, new ExpandableObjectAttribute());
        }
    }
}
