# Stuxnet 2.1
## NotPetya

---

## Custom Computer Icons
Can you believe it was this easy?

You can now define custom computer icons in your Stuxnet configuration file. Please [read the documentation](./docs/StuxnetConfig.md) carefully, as there are a few limits in place for memory reasons!

Additionally, all icons that were only available via security levels are now also available as regular icons:
* `sec0` = `Sec0Computer`
* `sec1` = `Sec1Computer`
* `computer` = `Computer`
* `oldServer` = `OldServer`
* `sec2` = `Sec2Computer`

Seriously, why wasn't the latter in vanilla?

---

## New Configuration Values
### Quests
You can now disable the Quests feature entirely, by setting `quests.disableQuestsSystem` to `true`.

You can also do this conditionally via actions, with `<ToggleQuestsButton Enabled="bool" />`

### Message Board Fix
Stuxnet 2.1 comes with a patch to `MessageBoardDaemon`s that automatically assign a unique ID to every thread, even if one wasn't set in its text content. You can disable this by setting `enableMessageBoardFix` to `false`.