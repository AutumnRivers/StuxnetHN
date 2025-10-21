# Stuxnet 2.2
## Loveletter

---

## Saving/Loading Netmap State
This new feature allows you to save the current state of the netmap (visible nodes + sorting algorithm), to be loaded later. You can clear the netmap, and load back the player's visible nodes.

---

## New Conditions
* `<OnForkbombComplete>` - Runs a set of actions when a forkbomb on the player completes.
* `<HasFlagsStx>` - Drop-in replacement for `HasFlags` that adds `CheckOnce` functionality.
* `<DoesNotHaveFlagsStx>`
* `<OnSongInCache>` - **Stuxnet.Audio** - Runs a set of actions when a song loaded with SMM is successfully cached.

---

## StuxnetMusicManager Upgrade
**Stuxnet.Audio** - SMM has been completely rewritten to be much smoother and more reliable. As always, report any bugs you find.

---

## New SFX
**Stuxnet.Audio** - Two new built-in SFX were added:
* `bang` / `gunshot` / `8` - Tutorial / incoming connection SFX
* `irc` / `notification` / `9` - IRC alert icon notification SFX

---

## New Configuration Values
### Audio Offset
Added `audio.offsetLoopPoints`, which, when set to `true` (default), will offset your end loop point by -250ms, to compensate for the buffer.