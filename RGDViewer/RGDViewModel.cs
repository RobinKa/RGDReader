using RGDReader;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RGDViewer
{
    public class RGDViewModel
    {
        private static IReadOnlyDictionary<Type, string> TypeName = new Dictionary<Type, string>()
        {
            [typeof(int)] = "Integer",
            [typeof(float)] = "Float",
            [typeof(string)] = "String",
            [typeof(bool)] = "Boolean",
            [typeof(RGDNode[])] = "List",
        };

        public string Key
        {
            get;
            set;
        }

        public object Value
        {
            get;
            set;
        }

        public string DisplayValue
        {
            get => string.Format("{0}: {1} ({2})", Key, Value is IList<RGDNode> ? $"{{{Children.Count}}}" : Value, TypeName[Value.GetType()]);
        }

        public ObservableCollection<RGDViewModel> Children
        {
            get;
            set;
        }

        public RGDViewModel(string key, object value, IList<RGDViewModel> children)
        {
            Key = key;
            Value = value;
            Children = new ObservableCollection<RGDViewModel>(children);
        }
    }
}
