# RACD
 
Randomizing Algorithm for Creating Dungeons!

My first Unity project (2017) is a tile scroll dungeon with an animated avatar.  The algorithm was inspired by a blog post:

https://journal.stuffwithstuff.com/2014/12/21/rooms-and-mazes/

but the code is original.

Features:

* random map creation by seed
* carving and backtracking algorithm ensures the entire map is utilized
* every room and passage can be reached by a series of doors and passages
* secret doors provide additional routes through the dungeon...
* ...but they are visible on the minimap if you look carefully!!
* animated avatar
* shift-key increases movement rate / animation speed
* object serialization (save/load level)

![racd](https://user-images.githubusercontent.com/74695555/108581058-9fdeec80-72eb-11eb-95fe-32e8bc4f35fe.png)
