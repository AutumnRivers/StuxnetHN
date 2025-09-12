```xml
<StuxnetAnimatedTheme>
    <BaseTheme>string</BaseTheme>
    <Elements>
    </Elements>
    <Instructions>
    </Instructions>
</StuxnetAnimatedTheme>
```
* `BaseTheme` should be the relative path to your underlying vanilla theme.
    * e.g., `Themes/ExampleTheme.xml`
* Unlike cutscenes, when animated theme animations end, they restart.
    * Indefinitely rotating elements are not affected by this.
    * `RaindropsFX` is not affected by this.
    * `GridFX` will not reset its animation, but will reset its position / scale / opacity.
* If you don't want this effect to be jarring, you should move everything back to its original position.