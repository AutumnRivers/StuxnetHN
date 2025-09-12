# `Stuxnet.Quests`
A new hand touches the beacon.

---

## Table of Contents
* [Actions](#actions)
* [XMOD](#xmod-compatibility)

---

## Actions
### `<LoadSideQuest MissionName="string" [ID="string"] />`
Loads up a sidequest into the Quests system. Please note that sidequests do *not* send emails!
* `MissionName` - Path to the mission (e.g., `Missions/ExampleMission.xml`)
* `ID` - *Optional.* The ID to associate with this quest.
    * If omitted, then the mission file's ID value is used.

### `<UnloadSideQuest QuestID="string" />`
Unloads a sidequest, without running its mission end actions.
* `QuestID` - Remove the sidequest with this ID value.

### `<ReplaceMainMission MissionName="string" />`
If you have `replaceLoadMission` set to `true`, then this allows you to replace the current main mission.

---

## XMOD Compatibility
`Stuxnet.Quests` *should* have compatibility with [XMOD](https://github.com/tenesiss/Hacknet-Pathfinder-XMOD-Dev)'s parallel missions system. Any parallel mission loaded in XMOD will automatically be loaded as a sidequest in Stuxnet, but will leave XMOD to do all the mission logic handling. (Similarly, any parallel missions removed from XMOD will also be removed from the Sidequests list. This also goes both ways, so a parallel mission removed via `UnloadSideQuest` will also be removed from XMOD.)

You can disable this in your global Stuxnet configuration file by setting `quests.ignoreXMODMissions` to `true`.