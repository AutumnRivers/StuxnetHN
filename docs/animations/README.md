# Stuxnet Animations
This README covers Stuxnet 2.0's new animations system. This system is mostly unified between the `Animated Themes` feature and the `Cutscenes` feature.

---

## Things To Note
* Unlike Stuxnet's previous system, this new animation system runs animations at a fixed 60fps, instead of relying on the player's FPS.
    * This is because all animation logic is now held in `OS.Update`, which runs at a fixed 60fps.
    * You really shouldn't notice a difference, but sorry if you do.
* Certain instructions only work with certain types of elements.
* Certain elements will ignore certain types of instructions.
* Each will be labeled as so.
* Every instruction *must* have a `DelayFromStart` attribute, a `Duration` attribute, and an `ElementID` attribute.
    * The only exception to this is the cutscene system's `DelayEnd` instruction, which only needs `DelayFromStart`.
* For `Position` and `Size` attributes, they work as follows:
    * If a value is less than `1.0`, but larger than `-1.0`, it will instead be multiplied by:
        * `X` *= `Bounds.Width`
        * `Y` *= `Bounds.Height`
    * If you only pass one `float` value instead of two, then that will be used for both values.
        * If it also applies to the rule above, it will have both values be multiplied by `Bounds.Height`.
* Color values work as `float R, float G, float B`.
* Rotation value is the angle, in *degrees*.
* Images loaded are kept in memory until the animation is unloaded.
    * This makes animations more performant than, say, switching themes.
    * By how much? Probably not by a lot. But there's a difference nonetheless!
* Elements start out as invisible, but are made visible by either:
    * Initiating an instruction, or
    * Activating the `ToggleVisibility` instruction.

---

## Table of Contents
### Elements/Instructions
* [Elements List](Elements.md)
* [Instructions List](Instructions.md)
### Formats
* [Animated Themes Format](AnimatedThemes.md)
* [Cutscene File Format](CutsceneFormat.md)