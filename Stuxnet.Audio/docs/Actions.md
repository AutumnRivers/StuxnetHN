# `Stuxnet.Audio.Actions`
All actions below are **delayable.**
---

## `<PlaySound SoundID="string" />`
Play a built-in sound effect from Hacknet. If I missed a sound, let me know!  
Sounds played with this action will not interrupt the currently playing music (if any)

Valid IDs (case-insensitive):
* `beep` / `warning` / `1`
    * Beep SFX (played when `writel` or `write` is ran)
* `crash` / `harsh` / `stinger` / `melt` / `meltimpact` / `2`
    * HackerScript `stopMusic` stinger
* `connect` / `addnode` / `bip` / `3`
    * Bip (played when adding a node from IRC)
* `mail` / `newmail` / `4`
    * You've Got Mail! from the OST
* `etasspindown` / `spindown` / `etas1` / `5`
    * Spin-down SFX from when the Emergency Trace Aversion fallback kicks in
* `etasspinup` / `spinup` / `etas2` / `6`
    * Spin-up SFX from when the Emergency Trace Aversion Sequence starts
* `etasimpact` / `impact` / `etas3` / `7`
    * Impact SFX from when the Emergency Trace Aversion fallback kicks in

---

## `<PlayCustomSound SoundFile="string" />`
Allows you to play a custom OGG file (or WAV, though this is not fully supported) as a sound effect.

`SoundFile` should be the path to the file, relative to the extension's root folder. (ex. `Sounds/Example.ogg`)

---

## `<PlayCustomSong SongFile="string" Immediately="bool(default=false)" LoopBegin="int(default=-1)" LoopEnd="int(default=-1)" />`
Action-ized version of `playCustomSong` mission function. Works the same.
* `LoopBegin` - Where to begin the loop when SMM reaches `LoopEnd`. Value in milliseconds.
* `LoopEnd` - Where to end the loop, in milliseconds.

---

## `<StopMusic FadeOut="bool(default=true)" />`
Stops the music, similar to `stopMusic` via HackerScripts, but without the stinger. When `FadeOut` is true, it will first fade the music out (a la `playCustomSong`) before going quiet.