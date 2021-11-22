using RGDReader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RGDViewer
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public string? RGDPath
        {
            get => rgdPath;
            set
            {
                rgdPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RGDPath)));
            }
        }

        public RGDViewModel[]? RootChildren
        {
            get => rootChildren;
        }

        private RGDViewModel[]? rootChildren = null;

        private string? rgdPath = null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel(string? rgdPath)
        {
            PropertyChanged += OnRGDPathChanged;

            RGDPath = rgdPath;
        }

        private void OnRGDPathChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RGDPath))
            {
                if (RGDPath == null)
                {
                    rootChildren = null;
                }
                else
                {
                    var nodes = ChunkyUtil.ReadRGD(RGDPath);

                    RGDViewModel nodeToViewModel(RGDNode node)
                    {
                        IList<RGDViewModel> childViewModels;

                        if (node.Value is IList<RGDNode> nodeChildren)
                        {
                            childViewModels = nodeChildren.Select(node => nodeToViewModel(node)).ToArray();
                        }
                        else
                        {
                            childViewModels = new RGDViewModel[0];
                        }

                        return new RGDViewModel(node.Key, node.Value, childViewModels);
                    }

                    rootChildren = nodes.Select(nodeToViewModel).ToArray();
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RootChildren)));
            }
        }
    }
}
