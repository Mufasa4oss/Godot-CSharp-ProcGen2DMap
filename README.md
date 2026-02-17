# KingsOfResource (Godot version 4.4 .NET)

#Controls
q: see provinces map mode
F5: save .json save file ---- Save location (Windows): C:\Users\USERNAME\AppData\Roaming\Godot\app_userdata\KingsOfResource
F9: load .json save file (not implemented)
WASD: camera control

Added gameContext class for the easier passing of vital game objects 7-21

TODO:

1st: scatter resources on the map according to some logic - done

2nd: if resource type exist then company type gets created 

3rd: country naming generator

4th - images for resources. What is the best source or AI art generator for 64x64


You don’t need the final vision right now. You just need a vertical slice of mechanics that create:



1. Choice



2\. Consequence



3\. Feedback



Example – First Gameplay Loop:

“Player selects one of 3 government policies (Subsidies, Tariffs, Tax Cuts). Each affects company growth rate in the country differently. After 3 turns, they get feedback: company count changed, revenue changed, approval changed.”



This is tiny, but it creates a full loop:



Player acts



Game responds



Player evaluates



Repeat



Once that works, you can expand the systems below it (population, logistics, markets) to enrich the loop rather than replacing it.

