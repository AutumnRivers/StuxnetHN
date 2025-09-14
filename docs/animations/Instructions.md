## Instructions
* `ElementID` should be the ID of the element you want to affect.
* `Duration` is in seconds.
* `DelayFromStart` is in seconds. This will tell the code when to call the instruction from the start of the animation.

## Basic Instructions
### Translate
`<Translate ElementID="string" Duration="float" Delay="float" TargetPosition="float,float" />`

### Resize
`<Resize ElementID="string" Duration="float" Delay="float" TargetSize="float,float" />`

### Rotation
`<Rotate ElementID="string" Duration="float" Delay="float" TargetAngle="float" />`  
OR  
`<Rotate ElementID="string" Delay="float" Indefinitely="bool" Speed="float" />`
* Remember that the angle is in degrees, not radians.
* `Speed` is how many angles the element should spin *per second*.
    * For example, if you set this to `360`, the element will complete a full cycle every second.
* Setting `Indefinitely` to `false` can stop an infinitely spinning rotation.

### Fade
`<Fade ElementID="string" Duration="float" Delay="float" FadeIn="bool" />`
* Setting `FadeIn` will have the element start out with *opposite* that opacity.
    * `FadeIn="true"` means the element will start invisible.
    * `FadeIn="false"` means the element will start visible, and fade out.

### Visibility
`<ToggleVisibility ElementID="string" Duration="float" Delay="float" Visible="bool" />`

## Advanced Instructions
### Typewriter
`<Typewriter ElementID="string" Duration="float" Delay="float" Text="string" />`
Creates a typewriter-like effect, similar to what's used with the VN system.
* This *only* affects `Text` elements. You will get a non-fatal error if you register this to a non-text element.
* Setting the `Text` attribute to be blank will have the effect do a "reverse typewriter" effect.
    * This means the text will slowly disappear letter by letter, instead of the opposite.

### Font Scale
`<ChangeFontScale ElementID="string" Duration="float" Delay="float" TargetScale="float" />`
* This *only* affects `Text` elements. You will get a non-fatal error if you register this to a non-text element.