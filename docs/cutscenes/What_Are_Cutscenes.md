# Stuxnet.Cutscenes
A built-in cutscene system that allows for smooth movement, rotation, and scaling of images. Throw out your janky theme animations; it's Stuxnet's time to shine. Added in **1.3 - Irene**.

---

## What are cutscenes?
A collection of `Instructions` that tell `Image`s and `Rectangle`s how to move, where to move, and how to display. Cutscenes are drawn before anything in the `Illustrator` is drawn, so you can have little animations going on while showing a chapter title or such.

## Why not just use Actions?
The cutscene system is unique from the action system in the sense that cutscenes have a "definitive end" and *every* item in a cutscene **must** have a delay. In addition, only one cutscene can be ran at a time, and another cutscene cannot be loaded in until the current one finishes.

## "Definitive end"?
Yes, cutscenes have a set delay in which they will end. When a cutscene ends, it resets all its associated objects and values back to the default, in case you want to run the cutscene again at some point. There is no way to keep a cutscene's associated objects on screen when a cutscene ends. In this sense, cutscenes could be considered "temporary."

## How do I make a cutscene?
Please refer to [Making a Cutscene](./Making_A_Cutscene.md).