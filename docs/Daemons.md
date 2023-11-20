<center>
<h1>Stuxnet.Daemons</h1>
</center>

---

# Code Redemption Daemon
### `<CodeRedemptionDaemon CodePrefix="string" Message="string"/>`
![Code Redemption Daemon Example](./images/code_daemon_example.png)

A daemon for accepting codes inputted by the user. These codes can be anywhere as long as they're defined [in the codes file](./StuxnetFiles.md#codes-file). They can be in the extension, or out of it. It doesn't matter. You could also use this as a cheat tool for debugging if you associate a code with, say, a bunch of executables.

Shows the extension's logo as defined in `ExtensionInfo.xml`.

* `CodePrefix` - A string indicating what prefix to put behind the displayed user input. If omitted, it will not display a prefix.
* `Message` - The server message. Displays `Enter your code here...` if omitted.