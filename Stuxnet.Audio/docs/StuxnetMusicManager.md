# Stuxnet Music Manager (SMM)
## `MusicManager`'s cooler younger brother

---

`StuxnetMusicManager` is a drop-in replacement for Hacknet's built-in `MusicManager`. It loads OGG files, just like Hacknet, but allows for all sorts of fancy things to be done with music files. Mainly, it's used for *dynamic looping*.

*Dynamic looping* is where the song reaches a certain point, then kicks itself back to its starting loop point. This way, you can keep the music going without awkward silence between the end and beginning of your song file. It's awesome, it's smooth, and it feels almost like it was always at home in vanilla. It is, however, not without its downsides.

SMM has extremely low mod compatibility. If any other plugin touches `FNA.MediaPlayer` or `Hacknet.MusicManager`, then it's immediately incompatible with SMM. SMM replaces nearly the entirety of `MusicManager`, and a few core methods of `MediaPlayer`. SMM also has longer load times on HDDs and weaker PCs. This is due to the fact the entire file is loaded in when the song starts playing. The audio cache that SMM has makes certain this won't happen with the same file twice, but it's still fairly noticeable. It will not, however, freeze up the game.

If you don't want to use SMM, but still wish to use the rest of `Stuxnet.Audio`, you can turn it off in your global Stuxnet configuration file.