# RGD Reader
This project provides a library, a command line interface too and a graphical user interface for reading Age of Empire 4 Relic Chunky archives such as `.rgd` and `.rrtex`.

Made with C# and .NET 6. The GUI is made with WPF.

The project is mostly in a proof of concept stage right now. It can read all RGD files of AOE4 that I tried. However there are no tests yet and the code is messy.

![](Media/RGDViewer.png)

## Download
See [Releases page](https://github.com/RobinKa/RGDReader/releases) for downloads.

Requires .NET 6 runtime:
- For Windows get https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-6.0.0-windows-x64-installer
- Or see https://dotnet.microsoft.com/download/dotnet/6.0 for a complete list

The library is also available on [nuget](https://www.nuget.org/packages/RGDReader/).

## Projects
- RGDReader: Library for reading Relic Chunky archives such as .rgd, .rrtex
- RGDReaderCLI: Command line interface for reading RGD files, usage: `RGDReaderCLI <rgd path>`
- RGDViewer: Graphical user interface for reading RGD files, files can be dragged on the window to view them. Also accepts the rgd path as first command line parameter.
- RGDJSONConverter: Converts all rgd files in a folder to json, usage: `RGDJsonConverter <input folder> <output folder>`
- ChunkyHeaderReaderCLI: Writes out which chunks a relic chunky archive contains
- RRTexConverter: Converts all rrtex files in a folder to png, usage: `RRTexConverter <input folder> <output folder`