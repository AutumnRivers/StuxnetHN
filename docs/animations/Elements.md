## Basic Elements
### Rectangles
Ol' Reliable.

`<Rectangle ID="string" Position="float,float" Size="float,float" Color="int,int,int" [Rotation="float"] />`
Draws a solid-color rectangle. I'm not sure what else you'd like me to say.

### Images
Look at this photograph...

`<Image ID="string" Position="float,float" Size="float,float" FilePath="string" [Rotation="float"] />`
Draws the image placed at `FilePath` to the screen.
* `FilePath` is relative to the extension's directory. For example: `Images/SomeCoolImage.png`

---

## Advanced Elements
### Text
Lorem Ipsum

`<Text ID="string" Position="float,float" Size="float,float" StartingValue="string" FontScale="float" Color="float,float,float" />`
Shows the `StartingValue` text at `Position`.
* With `Size`, only the `X` value matters. This determines the "width" of the Text's bounds, and will wrap if it is wider.
* This element is not affected by rotation.

### Raindrops FX
The itsy bitsy spider went up the water spout~

`<RaindropsFX ID="string" FallRate="float" MaxDropsRadius="float" DropsPerSecond="float" [DropsColor="float,float,float"] />`
Replicates the raindrops effect used in `RadioV3` and `TorrentStreamInjector`.
* This effect will always draw to its entire bounds. (In most cases, the full window.)
* This element is not affected by rotation.
* `FallRate` determines how fast the drops fall. `0.5` is usually the sweet spot for me.
* `MaxDropsRadius` determines how large the drop puddles should be when a raindrop lands.
* `DropsPerSecond` - How many raindrops should fall per second. This does not determine how many should land per second.

### Shifting Grid FX
I don't have a witty phrase for this one. Sorry.

`<GridFX ID="string" Position="float,float" Size="float,float" Color="float,float,float" />`
Shows the shifting grid effect, like whats used in `MemoryForensics`.
* This element is not affected by rotation.
* `Color` will also have a dark and light value applied to it, so do not go pure white or pure black.

---

## Cutscene Specific
### Delay End
`<DelayEnd DelayFromStart="float" />`
Delays the ending of the cutscene. Yep, that's it. Set this as your last element.