using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RGDViewer
{
    public class RGDViewModel
    {
        private static readonly IReadOnlyDictionary<int, string> typeDisplayName = new Dictionary<int, string>
        {
            [0] = "Float",
            [1] = "Integer",
            [2] = "Boolean",
            [3] = "String",
            [100] = "Table",
            [101] = "List",
        };

        public string Key
        {
            get;
            set;
        }

        public ulong Hash
        {
            get;
            set;
        }

        public object Value
        {
            get;
            set;
        }

        public int Type
        {
            get;
            set;
        }

        public string DisplayValue
        {
            get => string.Format("{0} <0x{1:X8}>: [{2}] {3}", Key, Hash, typeDisplayName[Type], Value is IList<(ulong Key, int Type, object Value)> table ? $"Count: {table.Count}" : Value);
        }

        public ObservableCollection<RGDViewModel> Children
        {
            get;
            set;
        }

        public RGDViewModel(string key, ulong hash, int type, object value, IList<RGDViewModel> children)
        {
            Key = key;
            Hash = hash;
            Type = type;
            Value = value;
            Children = new ObservableCollection<RGDViewModel>(children);
        }
    }
}
