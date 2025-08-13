# Getting Started
---

The base element of every BYOD daemon is `<BYODDaemon Name="string" IncludeExitButton="bool(default=true)"></BYODDaemon>`.

* `Name` will set the name of the daemon, as it will be shown in-game.
* `IncludeExitButton` will, when `true`, automatically register an exit button in the bottom left corner of the daemon. (When omitted, is `true`.)

---

Each BYOD daemon is made up of elements. These elements determine what will be drawn, and *when* it will be drawn. When something is defined first, it will be drawn first, so it will also be under everything else. For example:
```xml
<BYODDaemon Name="Example BYOD" IncludeExitButton="true">
    <Rectangle Position="0.1,0.1" Size="0.1,-1.0" Color="255,255,255" />
    <Rectangle Position="0.1,0.1" Size="0.15,-1.0" Color="0,0,0" />
</BYODDaemon>
```
In this scenario, the black square is drawn second, so it will render *over* the white square. Keep this in mind when placing elements!

---

For a full list of the supported BYOD elements, [check this out](Elements.md).