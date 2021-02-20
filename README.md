# Dungeons

A project for creating and moving through randomly created dungeons!

My first Unity project (2017) is a tile scroll dungeon with an animated avatar. The concept was inspired by a blog post:

https://journal.stuffwithstuff.com/2014/12/21/rooms-and-mazes/

but the code is original.

Features:

    random map creation by seed
    carving and backtracking algorithm ensures the entire map is utilized
    all rooms and passages can be reached from any starting location (no dead ends)
    invisible secret doors provide additional routes through the dungeon...
    ...but they are visible on the minimap for the time being if you look carefully!!!
    animated avatar
    minimap fog (currently turned off)
    shift-key increases movement rate / animation speed
    sprite array for managing map tiles
    object serialization (save/load level and other game state data)
    escape key menu for user input and informative/instructive text

Next step: Populating the map with item sprites and managing interactions.  This was previously implemented using a third-party tool from the Unity store but I decided to remove it and start on my own implementation.

![dungeons](https://user-images.githubusercontent.com/74695555/108611189-59e06200-7399-11eb-9f95-7fcdd9c643f2.png)
