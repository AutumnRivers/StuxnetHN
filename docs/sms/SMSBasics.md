# `Stuxnet.SMS`
`IRCDaemon` wishes it could be this cool.

---

## Table of Contents
* [What is SMS!?](#what-is-sms)
* [How to Use](#how-to-use)
* [Additional Reading](#additional-reading)

---

## What is SMS!?
`Stuxnet.SMS` is a specialized module that acts sort of like a global IRC. Except, like, way cooler.

Compared to IRC, SMS also has:
* Multiple channels
* Stylized system messages
* Scrolling (!!!)
* Choices system
* Cancellable message queue

Since SMS is a module, like RAM or the terminal, you can also interact with the module by using `SetLock`:
```xml
<SetLock Module="sms" IsHidden="false" IsLocked="true" />
```
This will also prevent any buttons (apart from choice buttons) showing up in SMS, which can be useful if you want to force the player to stay in the SMS module.

Just like **RadioV3**, you can also disable the user's access to the SMS module entirely, if you so wish.

---

## How to Use
Run `messenger` in your in-game terminal. Yep, that's it! Everything else is self-explanatory.

---

## Additional Reading

* [Actions](Actions.md)