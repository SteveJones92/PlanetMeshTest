# PlanetMeshTest
 
The project was created in Unity 2020.3.32f1.

The final implementation is currently in Scene - DemoForClass.

There are 3 main objects in the scene:

* Ship
    * Main Camera
* Representations
    * Maps
* Sun
    * SunLight
    * Planets

The representations is just for looking at laid out noise and color maps easily. Now that the planet works, this is unneeded and can be ignored. The project is still being developed, so it is being left as a preview of generated maps at the moment.

The ship is what the player controls. Controls are WASD and EQ for moving and turning. Space to fly forward.

The Sun is a unity sphere with a sun texture on it and is not generated. It contains a Solar System script for finding planet children and rotating them around naiively in a circle. It also has a light source for the environment as if it was a sun. The Solar System script also has controls of - for preloading the scene and = for loading it again, to see a new Solar System with new planets.

The planets are the main generated items for this project. You need to put a noise group and a palette group on it (it should be there already), which are script assets preset for different ranges of output. Detail has to do with how many colors to generate, more being a better amount of color detail. Ratios need to match the colors from the palette group, of which there are 4 colors in each right now. There must be 4 ratios and the ratios define how much of the colors from detail is a given color and shades of it. Anything over 1 will create more colors than listed as detail count.

Current generation should be set to the level of detail you want for that planet (6 being a good median value). Output Name is the location of the meshes that were generated. It will regenerate if they dont exist, which could take awhile and makes a 120mb files for generation 8 (x4 each step).

Load map is for leading maps from the representations to see what they look like. It will set the noise values to change the planet (once you hit setup after changing).

Load random selections run the functions to randomly select from assets for a new planet noise or colors. Setup just reloads the planet if anything has changed (like clicking to load a new color palette). Initialize loads random for both and then calls setup.

Printout map will give an image of the heightmap and colormap output into resources.

Hit play and everything should work, if it is all setup as stated above!
