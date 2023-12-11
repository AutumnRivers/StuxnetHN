<center>
<h1>Stuxnet.Actions</h1>
</center>

---

# RadioV3 Actions
## Allowing/Preventing Radio Access
### `<PreventRadioAccess DelayHost="delay" Delay="3.0" />`
Prevents radio access to the user. Will show the text "-- Radio access is denied --" on the executable display. This can be used for cutscenes, sequencers, or any other time you may not want the player to have control over the music.

### `<AllowRadioAccess DelayHost="delay" Delay="5.0" />`
Does the opposite of above.

## Adding/Removing Songs
### `<AddSongToRadio SongID="song1,song2" />`
Adds access to the song ID(s) in the radio for the player. Multiple songs can be added at once with a comma.

[More information about song IDs](./StuxnetFiles.md#radio-file)

### `<RemoveSongFromRadio SongID="song2" />`
Removes access to the song Id in the radio for the player. Only one song can be removed at a time.

[More information about song IDs](./StuxnetFiles.md#radio-file)

# Sequencer Actions
### `<ChangeSequencerManually RequiredFlag="testflag1" TargetID="testseq1" SpinUpTime="5" ActionsToRun="Actions/TestSequencers/1.xml" />`
Manually changes information for the `ESequencer`.

### `<ChangeSequencerFromID SequencerID="seq3" />`
Changes information for the `ESequencer` as defined for the `SequencerID` in [the sequencers.json file](./StuxnetFiles.md#sequencers-file).

### `<ClearCustomSequencer DelayHost="delay" Delay="5.0" />`
Clears any custom information for the sequencer and defaults to whatever is in `ExtensionInfo.xml`.

# Save Actions
## Enabling/Disabling Saves
All of the below actions are delayable.

### `<DenySaves />`
Denies saves for the user.

### `<AllowSaves />`
The opposite of above.

### `<RequireFlagForSaves Flag="string" AvoidFlag="bool" />`
Requires a flag in order for the user to be able to save.
* `Flag` - The flag the user must have in order to save. If empty, then the user will no longer require a flag to save.
* `AvoidFlag` - Whether or not to *avoid* the above flag in order for the user to save.
    * For example, if `Flag` is set to "badflag" and `AvoidFlag` is set to "true", then the user will not be able to save so long as they have the "badflag" flag applied to them.

# Vault Actions
For use with the [Vault Daemon](./Daemons.md#vault-daemon).

### `<AddVaultKey KeyName="TestVault" /> / <RemoveVaultKey KeyName="TestVault" />`
**Delayable.** Adds/removes a key for the set key name of the vault. If the user has not visited the vault yet, adding a vault key will set the key amount to 1, while removing the key will set the key amount to 0.

A maximum of 10 keys can be added (unrelated to the vault daemon itself) and the user can have a minimum of 0 keys. If you go over or under these limits, nothing will happen. It just won't add/remove the key.

# Dialogue Actions
Actions relating to dialogue / "story elements"

## Chapter Titles
### `<ShowChapterTitle ChapterTitle="string" ChapterSubTitle="string" [HideTopBar="bool" BackingOpacity="float"] />`
**Delayable.** Shows a chapter title and subtitle. This hides all user modules, along with the top bar if `HideTopBar` is `true`.
* `ChapterTitle` - The title. Shows up in the Hacknet logo font. e.g. `"Chapter 1"`
* `ChapterSubTitle` - Admittedly, a misleading name. Typically you'll put the *actual* chapter title here. e.g. `"The first chapter"`
* `HideTopBar` - Whether or not to hide the top bar. Defaults to `true`, and you'll probably want to keep it like that.
* `BackingOpacity` - How opaque the dimmed background should be. Higher values means the background will be less visible.

**NOTE**: You should *not* save the player's game while the chapter title is shown, it'll break things! Only save before you've shown it, or after you've hidden it.

### `<HideChapterTitle />`
**Delayable.** Does the opposite of above. This will also enable the alerts icon, even if you've disabled it with `DisableAlertsIcon`. If you really need the alerts icon to stay hidden, you can just disable it right after hiding the chapter title.

## Dialogue
### `<ShowCTCDialogue [TextSpeed="int" HideTopBar="bool" TextColor="int,int,int" EndDialogueActions="string" BackingOpacity="float"]>string</ShowCTCDialogue>`
Shows dialogue that, when completed, will prompt the user to click the screen.
* `TextSpeed` - Multiplier of the default text speed (10chars/s)
* `HideTopBar` - Whether or not to hide the top bar. Defaults to `true`, and you'll probably want to keep it like that.
* `TextColor` - The color of the text. Similar to how theme colors are done. Defaults to white, or `255,255,255`.
* `EndDialogueActions` - Actions to run when the user clicks the screen after being prompted. If omitted, then the top bar and module visibility will return when the user has clicked.
* `BackingOpacity` - How opaque the dimmed background should be. Higher values means the background will be less visible.

### `<ShowAutoDialogue [TextSpeed="1" HideTopBar="false" TextColor="255,0,0" ContinueDelay="3"]>string</ShowCTCDialogue>`
Same as above, but for auto text. Auto text will fire end dialogue actions after the delay is finished. No user input required.
* `ContinueDelay` - How long to wait until after the text is finished (in seconds) until `EndDialogueActions` is fired. Defaults to `0`.

## Node Actions
### `<PlaceNodeOnNetMap TargetCompID="jmail" [StartingPosition="string" Offset="float,float"]>`
**Delayable.** Places the target node onto the netmap where you specify it.

* `TargetCompID` - The ID of the target node.
* `StartingPosition` - Where to start your offset from. Valid positions are `topleft,centerleft,bottomleft,topcenter,truecenter,bottomcenter,topright,centerright,bottomright`. Defaults to `truecenter`
* `Offset` - The offset from the starting position. Percentage based. Defaults to `"0,0"`
    * For example, a starting position of `topleft` and an offset of `0.5,0.25` will place the node halfway across the netmap, and a halfway to the center vertically.

# Misc. Actions
### `<DisableAlertsIcon /> / <EnableAlertsIcon />`
**Delayable.** Turns the alert icon (email, irc, etc.) off and on, respectively. And yes, that means *completely* off. Nothing will be at the top right of the user's game window. Useful for cutscenes, sequencers, blah blah blah. You get the gist of what most of these actions are for.

### `<ForceConnectPlayer TargetCompID="CompID" LikeReallyForce="bool" />`
Forcefully connects the user to the target computer, or at least tries to.

Setting `LikeReallyForce` to `"true"` will constantly try to connect the player to the target computer. It's recommended **NOT** to use this unless you have some specific edge case where you *absolutely* have to.

### `<WriteToTerminal [Quietly="bool"]>string</WriteToTerminal>`
Ported from [LunarOSPathfinder](https://github.com/AutumnRivers/LunarOSPathfinder#writetoterminal-quietlyboolmessagewritetoterminal).