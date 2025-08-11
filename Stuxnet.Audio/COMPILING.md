# Stuxnet.Audio Compilation Instructions
---

## Prerequisites
* A *legal* copy of Hacknet from either Steam or GOG.
* [ILRepack](https://github.com/gluck/il-repack#dotnet-tool-installation)
* C# IDE (Visual Studio, JetBrains Rider, etc.)
    * Said IDE must have support for downloading [NuGet Packages](https://www.nuget.org/)

---

1. Make sure that `ILRepack` is successfully installed.
    1. You can do this by running `ilrepack --version` in your terminal.
2. Open `../Directory.Build.props` and set the `HacknetInstall` variable to the folder of your Hacknet installation
    1. Steam Example: `C:\Program Files (x86)\Steam\steamapps\common\Hacknet\`
    2. GOG Example: `C:\Users\USERNAME\Downloads\Hacknet\`
3. Ensure that all NuGet packages are installed
4. Build solution (IDE-dependent)
5. Profit!