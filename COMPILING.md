# Compiling Stuxnet
---
1. Set your Hacknet install.  
Go to `Directory.Build.props`, and set `HacknetInstall` to the path of your own Hacknet installation.

2. Place library files.
    * Stuxnet requires `XMOD.dll` in its `/lib` folder to compile. This is for the optional XMOD compatibility. [You can find XMOD here.](https://github.com/tenesiss/Hacknet-Pathfinder-XMOD-Dev/releases/latest)
    * `Stuxnet.Audio` also needs a `/lib` folder, but it'll be created when building the project.

3. Download NuGet packages.  
`Stuxnet.Audio` requires the `NVOrbis` NuGet package, so you'll need to install that before you can build. How you do that is up to you.

4. Compile!  
`Stuxnet_HN` will build itself first, then place the resulting DLL in `Stuxnet.Audio`'s `/lib` folder. Yes, I know this isn't clean, but it works. After that, the resulting DLLs will be placed in:  
* Stuxnet = `Stuxnet_HN/bin/<Release|Debug>`
* Stuxnet.Audio = `Stuxnet_HN/Stuxnet.Audio/bin/<Release|Debug>`