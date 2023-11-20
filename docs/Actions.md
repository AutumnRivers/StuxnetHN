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