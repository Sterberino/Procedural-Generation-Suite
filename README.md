# Procedural-Generation-Suite

<h1>Introduction</h1>
<p>This is collection of precedural generation scripts intended to be used as as a base for 2D tile-based Unity games. in order to use these scripts, you must have the follorwing packages installed in your project:</p>

<ul>
  <li>Editor Coroutines</li>
  <li>Mathematics</li>
  <li>Collections</li>
  <li>Burst</li>
</ul>

<h2>World Generation results</h2>
<table align = "center">
  <tr>
    <td>Generated Image</td>
    <td>Resulting World Generation</td>
  </tr>
  <tr>
    <td><img src = "https://github.com/Sterberino/Procedural-Generation-Suite/blob/main/Images/IslandTextureSaveTest2.png" width = 300 height = 300 align = "center"/> </td>
    <td><img src = "https://github.com/Sterberino/Procedural-Generation-Suite/blob/main/Images/Island%20Result.png" width = 300 height = 300 align = "center"/></td>
  </tr>
  <tr>
    <td><img src = "https://github.com/Sterberino/Procedural-Generation-Suite/blob/main/Images/Texture%20result.png" width = 300px align = "center"/> </td>
    <td><img src = "https://github.com/Sterberino/Procedural-Generation-Suite/blob/main/Images/Tilemap%20result.png" width = 300px align = "center"/></td>
  </tr>
 </table>

<h2 align>Biome Close-ups</h2>
<table align = "center">
  <tr>
    <td>Sparse Forest Biome</td>
    <td>Dense Forest Biome</td>
  </tr>
  <tr>
    <td><img src = "https://github.com/Sterberino/Procedural-Generation-Suite/blob/main/Images/Sparse%20Forest.png" width = 300 align = "center"/> </td>
    <td><img src = "https://github.com/Sterberino/Procedural-Generation-Suite/blob/main/Images/Dense%20Forest.png" width = 300 align = "center"/></td>
  </tr>
  
 </table>
 
 <h2>How to use</h2>
  
  <p>Your first order of business will be to add ProceduralIslandGenerator.cs as a component to a gameobject that has an Image component and is parented to a valid canvas. From there you will see an object field on the ProcerualIslandGenerator that takes a Texture2D reference. Add the Island Generator Moisture-Height Map.png to your project as a Texture2D and assign it as the ProcerualIslandGenerator's Moisture Height Graph variable.</p>
  <br/>
  <br/>
  <p>Next, you must create a new Biome Asset. In the project Hierarchy (in assets), right-click Create->Biome. The Biome has a default floor tile, which will be placed by the ProceduralIslandGenerator before populating the biome with Foliage and prefabs. It has a list of floor tiles which are considered valid floor tiles for the Biome. It also has a list of prefab objects which can be spawned into the biome, each with their own set of tile conditions and spawn chances. You wouldn't want a fish tile to spawn on land, for instance, if water and land were both found in the biome. The Biome Foliage represents a list of tiles that can replace the default floor tile or be rendered to a higher tilemap layer to add variety.</p>

  <br/>
  <br/>
  <p>You will then map each pixel color found in the height moisture graph to a biome of your choice/ creation. If a color is not mapped to a biome, the assigned default biome will be placed. If no default biome is assigned, the tiles will be null (unassigned). You may experiment with different parameters for the height and moisture noise textures used to generate the island, and you will find interesting and varied end-results. If you wish to save the noise textures use for the island, assign a valid filename and filepath in the noise parameters, and check the bool to Save the image.</p> 
  
  <br/>
  
<h2 align>Biome Saved Noise Images</h2>
<table align = "center">
  <tr>
    <td>The heightmap</td>
    <td>The moisture map</td>
  </tr>
  <tr>
    <td><img src = "https://github.com/Sterberino/Procedural-Generation-Suite/blob/main/Images/Height%20Noise%20Texture.png" width = 300 align = "center"/> </td>
    <td><img src = "https://github.com/Sterberino/Procedural-Generation-Suite/blob/main/Images/Moisture%20Noise%20Texture.png" width = 300 align = "center"/></td>
  </tr>
  
  <br/>
  
  <p>When you are ready, click the Generate Island Texture Button. If you have elected to save the noise textures, they will be saved at this time. Then, you can place the tiles and the gameobjects in the scene referencing the texture by pressing "Place Island."</p>
  
  
