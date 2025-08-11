# Integrating with `Stuxnet.Audio`
---

If you're a third-party mod developer that wants to take advantage of SASS' audio playing capabilities, you can! The mod's GUID is `autumnrivers.stuxnet.audio`, so you'll need to include that in a `BepInDependency` attribute for your plugin's root class.
Don't worry about requiring Stuxnet - `Stuxnet.Audio` already has it as a hard requirement, so `Stuxnet` will *always* load before `Stuxnet.Audio`.

To load an OGG file as a `SoundEffect`, run `OGGSoundEffectLoader.LoadOgg(filepath)`. This method automatically checks the file to make sure its a valid OGG file, but you can also check yourself with `OGGSoundEffectLoader.IsOgg(filepath)`.

`Stuxnet.Audio` holds its own cache for custom sound effects, and cleans itself up when unloaded. To add to or read from Stuxnet's sound effect cache, you can find the dictionary field at `StuxnetHN.Audio.StuxnetAudioCore.SFXCache`. The key is the filepath, and the value will be the `SoundEffect` instance itself. (DO NOT clear the cache manually! Let `Stuxnet.Audio` work for it.)