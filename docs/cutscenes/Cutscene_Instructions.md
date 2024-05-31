# Stuxnet.Cutscenes.Instructions
For more information about cutscenes, [read here](./What_Are_Cutscenes.md).

---

### Table of Contents

* [Ground Rules](#ground-rules)
* [Showing Objects](#showing-objects)
    * [Instantly](#instantly)
* [Movement](#movement)
    * [Move Object](#move-object)
    * [Rotate Image](#rotate-image)

---

## Ground Rules
Let's lay some ground rules:
* Position-related values (like `pos`) operate on a percentage-based placement system. For example:
    * `(0,0)` would set the object at the top left of the screen
    * `(0.5,0.25)` would set the object halfway across the screen, and a quarter of the way down
    * `(-1, 0.35)` would set the object way off past the left of the screen, and 35% of the screen's height down
* The same goes for size-related values, though they depend on the size of the screen:
    * `(1,1)` would set the object to the size of the screen, proportionate to its aspect ratio.
    * `(0.5, 0.75)` would set the object to either half of the screen's width, or 3/4s of the screen's height, dependent on the aspect ratio
    * Values lower than zero will not do anything, as they will just reset back to zero.
* *Most* `float` values will reset back to zero if set to lower than zero, though this isn't always the case.
* Every instruction requires a `delay` attribute. Delays cannot be smaller than `0.0f`.

## Showing Objects
### Instantly
```xml
<!-- Show Rectangle -->
<ShowRectangle id="string" delay="float"/>
<HideRectangle id="string" delay="float"/>

<!-- Show Image -->
<ShowImage id="string" delay="float"/>
<HideImage id="string" delay="float"/>

<!-- Images can also be faded in and out -->
<FadeInImage id="string" duration="float" delay="float"/>
<FadeOutImage id="string" duration="float" delay="float"/>
```
Shows/hides the given object, respectively.

## Movement
### Move Object
```xml
<MoveImage id="string" pos="float,float" tween="bool" tweenDuration="float" delay="float" />
```
```xml
<MoveRectangle id="string" pos="float,float" tween="bool" tweenDuration="float" delay="float" />
```
Moves an image / rectangle to `pos`, using a percentage-based placement system across the game window.

* `pos` - The new position, percentage-based.
* `tween` - Whether or not the movement should be "tweened." If set to `false`, the image will move instantly.
* `tweenDuration` - How long the tweening should take, in seconds. This is only acknowledged if `tween` is `true`.

### Rotate Image
```xml
<!-- Timed Rotation -->
<RotateImage id="string" forever="false" angle="float" duration="float" delay="float" />

<!-- Infinite Rotation -->
<RotateImage id="img1" forever="true" speed="float" clockwise="bool" delay="float" />
```
Rotates an image in place. Rectangles cannot be rotated.

* `forever` - Whether or not the image should rotate in place for an indefinite amount of time.
#### Timed Rotations
When `forever` is set to `false`, then the rotation will simply go from the current angle to the target angle in an amount of time.
* `angle` - The target angle in degrees. (e.g., `180`)
* `duration` - How long it should take for the rotation to complete, in seconds.
#### Infinite Rotations
When `forever` is set to `true`, then the image will continue to be rotated at a constant speed.
* `speed` - How fast the image should spin. This is a multiplier - for example, `2.0` will make the image spin twice as fast.
    * The default speed (`1.0`) will spin the image a whole 360 degrees per second.
* `clockwise` - Whether or not the image should spin clockwise.

```xml
<StopRotation id="string" delay="float" />
```
Stops the current rotation for the image with the id of `id`.

## Resizing
### Resize Rectangle
```xml
<ResizeRectangle id="string" resizeTo="float,float" maintainAspect="bool" tween="bool" tweenDuration="float" delay="float" />
```
Resizes a rectangle to `resizeTo`.

* `resizeTo` - The target size for the rectangle, formatted as [Vector2](https://learn.microsoft.com/en-us/previous-versions/windows/silverlight/dotnet-windows-silverlight/bb199660(v=xnagamestudio.35)).
* `maintainAspect` - Whether or not to maintain the aspect ratio of the rectangle when resizing.
* `tween` - Whether or not to tween between the current size and the new size.
* `tweenDuration` - How long it should take for the tweening animation to complete, in seconds. This is only required if `tween` is set to true.

More cutscene features will be added in the future.