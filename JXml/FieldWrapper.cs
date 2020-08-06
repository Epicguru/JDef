using System;
using System.Reflection;

namespace JXml
{
    public struct FieldWrapper
    {
        public bool IsField
        {
            get
            {
                return Field != null;
            }
        }
        public bool IsProperty
        {
            get
            {
                return Property != null;
            }
        }
        public bool IsValid
        {
            get
            {
                return IsField || IsProperty;
            }
        }

        public FieldInfo Field { get; }
        public PropertyInfo Property { get; }

        public Type FieldType
        {
            get
            {
                if (IsField)
                    return Field.FieldType;
                if (IsProperty)
                    return Property.PropertyType;
                return null;
            }
        }

        public FieldWrapper(FieldInfo field)
        {
            this.Field = field;
            this.Property = null;
        }

        public FieldWrapper(PropertyInfo property)
        {
            this.Field = null;
            this.Property = property;
        }
    }
}