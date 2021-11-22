using RGDReader;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RGDViewer
{
    public class RGDViewModel
    {
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
            get => string.Format("{0}: {1}", Key, Children.Count == 0 ? Value : $"List size={Children.Count}");
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
