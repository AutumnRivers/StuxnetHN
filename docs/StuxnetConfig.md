# Stuxnet Global Configuration
---

As of version 2.0.0 of Stuxnet, everything you'd want to modify in Stuxnet is now located in a global configuration file, placed at your extension's root folder.

To get started, place a file titled `stuxnet_config.json` in your extension's root directory. There should also be a template JSON file with the latest release of Stuxnet.

```json
{
    "audio": {
        "replaceMusicManager": true,
        "songs": {}
    },
    "quests": {
        "replaceLoadMissionAction": false,
        "ignoreXMODMissions": false
    },
    "sms": {
        "authorColors": {}
    },
    "gamemode": {
        "gamemodes": [],
        "selectPathText": "",
        "requirePersistentFlag": ""
    },
    "codes": {},
    "sequencers": {}
}
```
## `Audio`
For configuration related to `RadioV3` and `Stuxnet.Audio`.
* `replaceMusicManager` - If `true`, replaces Hacknet's built-in `MusicManager` with Stuxnet's `StuxnetMusicManager`.
    * For more information, [read here](../Stuxnet.Audio/docs/StuxnetMusicManager.md).
    * If `Stuxnet.Audio` is not installed, this option is ignored.
* `songs` - [Song Configuration](StuxnetFiles.md#radio)

## `Quests`
For configuration related to `Stuxnet.Quests`.
* `replaceLoadMissionAction` - If `true`, replaces the built-in `LoadMission` action to auto-load missions as sidequests.
    * It's recommended to keep this as false if you're placing Stuxnet in an existing project.
* `ignoreXMODMissions` - Whether or not to automatically include XMOD's parallel missions as sidequests.
    * If XMOD is not installed, this option is ignored.

## `SMS`
* `authorColors` - A dictionary of author colors, like so: `{string username, string color}`
    * e.g., `{"Autumn": "180,180,180"}`

## `Gamemode`
* `gamemodes` - An array of `Gamemode`s.
* `selectPathText` - If not empty, this will replace "Select Your Starting Path"
* `requirePersistentFlag` - If not empty, the Gamemode menu will not appear until the player has gotten this persistent flag.

> **[!] NOTICE** 
>
> When `Stuxnet.Gamemode` is active, you *must* switch off Pathfinder's built-in `Preload All Themes` option. Otherwise, your save will be corrupted.
> 
> This time, it's not my fault!

### `Gamemode` Object
```json
{
    "title": "",
    "description": "",
    "allowSaves": true,
    "requiredFlagForVisibility": "",
    "requiredFlagForSelection": "",
    "startingMission": "",
    "startingActions": "",
    "startingSong": "",
    "playerCompID": "",
    "startingFlags": ""
}
```
* `title` - The title of the gamemode. This is only aesthetic.
* `description` - A description of the gamemode. This is only aesthetic.
* `allowSaves` - *Optional.* Whether or not to allow saves by default.
* `requiredFlagForVisibility` - *Optional.* Required persistent flag for this gamemode to show up in `Stuxnet.Gamemode`'s menu.
* `requiredFlagForSelection` - *Optional.* Required persistent flag for this gamemode to be selectable.
* `startingMission` - *Optional.*
* `startingActions` - *Optional.*
* `startingSong` - *Optional.*
* `startingTheme` - *Optional.* Can be a custom theme path or built-in theme.
* `playerCompID` - *Optional.* The ID of the computer you'd like to replace the playerComp with.
    * This can be used to give the player a PC with any executables they might've had up until a certain point.
    * Or you can use this to have the player play as someone completely different!
* `startingFlags` - *Optional.* Currently unused, use the `addFlags` mission function.

## `Codes`
[See here.](StuxnetFiles.md#codes)

## `Sequencers`
[See here.](StuxnetFiles.md#sequencers)