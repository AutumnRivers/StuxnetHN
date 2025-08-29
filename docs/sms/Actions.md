# SMS Actions
---

## `<SendSMSMessage Author="string" ChannelName="string" OnReadActions="string" MessageID="string" MessageDelay="float">string</SendSMSMessage>`
* **Delayable.** Sends a message to the SMS system.
* `Author` - The author of the message. Is filtered, so can be set to `#PLAYERNAME#`.
* `ChannelName` - The name of the channel to send this message in. *Optional* if `Author != "#PLAYERNAME#"`.
* `OnReadActions` - *Optional.* Actions to run when the player reads the message for the first time.
* `MessageID` - *Optional.* The unique ID of the message for use in the message queue. Useless if not using `MessageDelay`.
* `MessageDelay` - *Optional.* Sends the message to the queue, to have it be loaded later.

---

## `<CancelSMSMessage MessageID="string" />`
* **Non-Delayable.** Cancels a queued SMS message.
* `MessageID` - The associated unique ID of the message in the queue.

---

## `<SMSShowChoices ChannelName="string">`
```xml
<SMSShowChoices ChannelName="Test Channel">
    <Choice OnChosenActions="Actions/SomeAction.xml">Lorem Ipsum</Choice>
    <Choice OnChosenActions="Actions/SomeOtherAction.xml">Hello World</Choice>
</SMSShowChoices>
```
* **Delayable.** Shows *up to* three choices at once. Selecting a choice will run its associated actions file.

---

## `<SMSHideChoices OnSuccessActions="string" />`
* **Delayable.** Attempts to hide choices, and runs an action if successful. Can be used to make choices time-sensitive.
* `OnSuccessActions` - Runs if there were active choices when this action was run.

---

## `<ForceOpenSMSChannel ChannelName="string" />`
* **Delayable.** Forcibly opens the associated channel in the SMS messenger, even if the SMS module isn't active.
* This overrides `ForbidSMS`.

---

## `<ForceCloseSMS />`
* **Delayable.** Forcibly closes the SMS module.

---

## `<ForbidSMS AllowSMS="bool" />`
* **Delayable.** Forbids the usage of the SMS module via command.
* `AllowSMS` - *Optional.* If true, the SMS module can be used. `false` by default.

---

## `<SMSBlockUser UserBeingBlocked="string" BlockingUser="string" />`
* **Delayable.** Sends a system message that shows the user has blocked/has been blocked by someone.
* This is purely aesthetic, and doesn't actually disable sending messages to the user.
* `UserBeingBlocked` - The name of the `Author` being blocked.
* `BlockingUser` - *Optional.* The name of the `Author` that's doing the blocking. Defaults to the player.

---

## `<SMSAddUser UserBeingAdded="string" UserAdded="string" ChannelName="string" />`
* **Delayable.** Sends a system message showing that a user has been added to a chat.
* If `UserBeingAdded` is set to `#PLAYERNAME#`, it will show alternate dialogue: `{UserAdded} added you.`
* This is purely aesthetic, and isn't required.
* `UserBeingAdded` - The name of the `Author` being added to the chat.
* `BlockingUser` - The name of the `Author` that's doing the blocking.
* `ChannelName` - *Optional.* The channel where the message should be shown.

---

## `<SMSGoOffline User="string" ChannelName="string" />`
* **Delayable.** Sends a system message showing that a user has gone offline.
* This is purely aesthetic.
* `User` - The name of the `Author` to show in the message.
* `ChannelName` - The name of the channel to send the message to.

---

## `<SMSFailSendMessage ChannelName="string" />`
* **Delayable.** Shows a generic "Message Failed To Send" error in the channel.