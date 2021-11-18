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
                    using var reader = new ChunkyFileReader(File.Open(RGDPath, FileMode.Open), Encoding.ASCII);

                    var fileHeader = reader.ReadChunkyFileHeader();

                    KeyValueDataChunk? kvs = null;
                    KeysDataChunk? keys = null;

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        var chunkHeader = reader.ReadChunkHeader();
                        if (chunkHeader.Type == "DATA")
                        {
                            if (chunkHeader.Name == "AEGD")
                            {
                                kvs = reader.ReadKeyValueDataChunk(chunkHeader.Length);
                            }

                            if (chunkHeader.Name == "KEYS")
                            {
                                keys = reader.ReadKeysDataChunk();
                                break;
                            }
                        }
                    }

                    if (kvs != null && keys != null)
                    {
                        var keysInv = ChunkyUtil.ReverseReadOnlyDictionary(keys.StringKeys);

                        RGDViewModel nodeToViewModel((ulong Hash, int Type, object Value) node)
                        {
                            var childViewModels = new List<RGDViewModel>();

                            if (node.Value is IReadOnlyDictionary<ulong, (int Type, object Value)> nodeChildren)
                            {
                                foreach (var child in nodeChildren)
                                {
                                    childViewModels.Add(nodeToViewModel((child.Key, child.Value.Type, child.Value.Value)));
                                }
                            }

                            return new RGDViewModel(keysInv[node.Hash], node.Hash, node.Type, node.Value, childViewModels);
                        }

                        rootChildren = kvs.KeyValues.Select(kv => nodeToViewModel((kv.Key, kv.Value.Type, kv.Value.Value))).ToArray();
                    }
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RootChildren)));
            }
        }
    }
}
