# BYOD Elements List
---
## Preface
`Position` and `Size` on every element behaves the same way. If both the X and Y value are between `0` and `1`, then the value will be treated as a *percentage* of the daemon's bounds. Anything above `1` will be treated as exact proximation.

For example: `0.1,0.5` would be treated as `10% of the daemon's width, and 50% of the daemon's height`, while `85,220` would be treated as `85 pixels wide, 220 pixels tall`.

For `Size`, setting the Y value to anything below a `0` will instead use the `X`'s percentage value for both width and height, creating a square.

---
## Common Attributes
* `Color="int,int,int"` - `float r, float g, float b`, same as how you'd format colors in custom theme files.

---

## `<Rectangle Position="float,float" Size="float,float" Color="int,int,int" />`
Creates a solid-colored rectangular prism wherever you choose to place it.

---

## `<Image Position="float,float" Size="float,float" Path="string" />`
Loads an image, and places it in the daemon. `Path` should be the relative path to the image, such as `Images/SomeImage.png`.

---

## `<Label Position="float,float" FontType="string" FontSize="float" TextColor="int,int,int">string</Label>`
* `FontType` can be either `normal`, `title`, `small`, or `tiny`.
* `FontSize` is a *multiplier*, and will multiply the size of the text by its value.
* The text you want on the label should be in the content of the XML element. Can be multi-line.

Renders text.

---

## `<Button Position="float,float" Size="float,float" OnPressedActions="string" Color="int,int,int" IsExitButton="bool">string</Button>`
* `OnPressedActions` should point towards an Actions file in your extension.
* `IsExitButton` will have the player exit the daemon when the button is pressed.
* The text you want inside the button should be in the content of the XML element.

Renders a button that, when pressed, will fire off an action file.

---

## `<WarningTape Position="float,float" Size="float,float" PrimaryColor="int,int,int" SecondaryColor="int,int,int" />`
Renders a "warning tape," similar to the kind you see when a proxy or firewall is active.