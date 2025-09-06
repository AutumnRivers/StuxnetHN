# Codes
For use with the [Code Redemption Daemon](./Daemons.md#code-redemption-daemon).
```json
{
    "codeID": {
        "files": {
            "filename": "filedata"
        },
        "radio": [
            "song3"
        ],
        "themes": {
            "ThemeName": "/Path/To/Theme.xml"
        },
        "pfthemes": { "Unsupported for now": "Do not use" },
        "email": "/Path/To/Email.xml"
    }
}
```
Alright, let's break this down.

* `codeID` - The code the player will put into the code redemption.
    * `files` - Files to be added to the player's PC.
        * `filename` - The name of the file. Any file ending in `.exe` will automatically be added to the player's `/bin`, anything else will be added to `/home`.
        * `filedata` - The data of the file. You can use wildcards here.
    * `radio` - An array of [Song IDs](#radio-file) to give the player access to.
    * `themes` - Themes to be added to the player's `/sys` folder.
        * `ThemeName` - Name of the theme. Added to the player's PC as `<ThemeName>-x-server.sys`.
        * `PathToTheme` - The path to the theme XML.
    * `pfthemes` - [ RESERVED FOR FUTURE USE ]
    * `email` - The path to a mission file to send the player the E-Mail of. This will *not* overwrite the current mission.

If any of the above values (besides `codeID`) are omitted, they will be skipped.

---

# Radio
For use with [RadioV3](../README.md#radiov3--radio_v3).
```json
{
    "songID": {
        "artist": "Song Artist",
        "title": "Song Title",
        "path": "/Music/Song.ogg",
        "initial": false,
        "beginLoop": -1,
        "endLoop": -1
    }
}
```
* `songID` - The ID of the song to be used with actions and [the codes file](#codes-file).
    * `artist` - The song artist. Shown at the bottom of RadioV3 when currently playing.
    * `title` - The song title.
    * `path` - The path to the song.
    * `initial` - *Optional.* Whether or not this song is available from the beginning of the extension.
    * `beginLoop` - *Optional.* For use with [`StuxnetMusicManager`](../Stuxnet.Audio/docs/StuxnetMusicManager.md). Value in milliseconds.
    * `endLoop` - *Optional.* For use with [`StuxnetMusicManager`](../Stuxnet.Audio/docs/StuxnetMusicManager.md). Value in milliseconds.

---

# Sequencers
For use with [ChangeSequencerFromID](./Actions.md#changesequencerfromid-sequenceridseq3).
```json
{
    "sequencerID": {
        "requiredFlag": "flagName",
        "spinUpTime": 5.0,
        "targetIDorIP": "nodeID",
        "sequencerActions": "Path/To/Action.xml"
    }
}
```

* `sequencerID` - Unique ID of the sequencer preset.
    * `requiredFlag` - The required flag for the sequencer.
    * `spinUpTime` - The spin up time. Should be a float.
    * `targetIDorIP` - ID of target node, IP should theoretically work, too.
    * `sequencerActions` - Actions to run when the sequencer is activated.