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
* `bang` / `gunshot` / `8`
    * Gunshot SFX from Tutorial / the incoming connection overlay.
* `irc` / `notification` / `9`
    * IRC alert icon notification SFX

---

## `<PlayCustomSound SoundFile="string" />`
Allows you to play a custom OGG file (or WAV, though this is not fully supported) as a sound effect.

`SoundFile` should be the path to the file, relative to the extension's root folder. (ex. `Sounds/Example.ogg`)

---

## `<PlayCustomSong SongFile="string" Immediately="bool(default=false)" BeginLoop="int(default=-1)" EndLoop="int(default=-1)" />`
Action-ized version of `playCustomSong` mission function. Works the same.
* `BeginLoop` - Where to begin the loop when SMM reaches `EndLoop`. Value in milliseconds.
* `EndLoop` - Where to end the loop, in milliseconds.

---

## `<StopMusic FadeOut="bool(default=true)" />`
Stops the music, similar to `stopMusic` via HackerScripts, but without the stinger. When `FadeOut` is true, it will first fade the music out (a la `playCustomSong`) before going quiet.

---

## `<PreloadSong SongFile="string" />`
Preloads the song file into the cache.

---
# Conditions
## `<OnSongInCache SongFile="string" [CacheIfNotExists="bool"]>`
Runs a set of actions when the `SongFile` has been successfully cached by SMM.
* If SMM is disabled, this will always return true.
* `CacheIfNotExists` - If `true`, will cache the song if it's not in the cache when this runs.