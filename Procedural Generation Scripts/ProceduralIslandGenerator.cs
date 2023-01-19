using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Tilemaps;

public class ProceduralIslandGenerator : MonoBehaviour
{
    /// <summary>
    /// Maps a pixel color to a biome.
    /// </summary>
    [System.Serializable]
    public struct PixelBiomeMap
    {
        public Color32 color;
        public Biome biome;
    }

    [Tooltip("A Texture2D that maps noise values to pixel colors. The Y axis of the Texture2D corresponds to the Height map, X axis to the Moisture map.")]
    public Texture2D MoistureHeightGraph;

    /// <summary>
    /// A default biome to place in the event a pixel color cannot be mapped to a biome.
    /// </summary>
    [Tooltip("This biome is placed in the event a pixel color cannot be mapped to a biome.")]
    public Biome DefaultBiome;
    public Image islandImage;

    [Space]
    [Space]
    public int2 Dimensions;
    public bool smoothIslandBiomes;

    [Space]
    [Space]
    [Tooltip("The parameters used to generate the Simplex noise representing world moisture values.")]
    public IslandTextureGenerator.NoiseParameters MoistureMapParameters;
    [Tooltip("The parameters used to generate the Simplex noise representing world height values.")]
    public IslandTextureGenerator.NoiseParameters HeightMapParameters;

    private Texture2D islandTexture;


    [Tooltip("The rules governing how Cellular Automata tiles are placed in the scene.")]
    public CellularAutomataRules cellularAutomataRules;


    /// <summary>
    /// Maps each pixel color to a corresponding Biome.
    /// </summary>
    [Tooltip("Maps each pixel color to a corresponding biome")]
    public List<PixelBiomeMap> IslandBiomeMappings;

    [Tooltip("The tilemaps in the scene.")]
    public List<Tilemap> tilemaps;

    [Space]
    [Space]
    [Header("Texture Save Settings")]
    [Tooltip("The filepath your island texture will be saved to. ")]
    public string IslandTextureDirectoryPath;
    public string IslandTextureFilename;

    public void TrySaveTexture()
    {
        if(islandTexture == null)
        {
            Debug.LogError("Island texture was null. You must generate an island texture before saving it to the filepath.");
            return;
        }

        if(IslandTextureDirectoryPath == "")
        {
            Debug.LogError("Please Specify a Directory to save the texture.");
            return;
        }

        if(!System.IO.Directory.Exists(IslandTextureDirectoryPath))
        {
            Debug.LogError("The Specified Directory does not exist. Please enter a valid Directory.");
            return;
        }

        byte[] bytes = islandTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(IslandTextureDirectoryPath + IslandTextureFilename + ".png", bytes);
    }

    public void GenerateIslandTexture()
    {
        islandTexture = IslandTextureGenerator.GenerateIsland(MoistureMapParameters, HeightMapParameters, MoistureHeightGraph, 
            new Vector2Int(Dimensions.x, Dimensions.y), smoothIslandBiomes);
        
        if (islandImage == null)
        {
            islandImage = GetComponent<Image>();
        }
        if(islandImage != null)
        {
            islandImage.sprite = Sprite.Create(islandTexture, new Rect(0, 0, islandTexture.width, islandTexture.height), Vector2.zero, 32);
            islandImage.rectTransform.sizeDelta = new Vector2(islandTexture.width, islandTexture.height);
        }
    
    }

    public void PlaceIsland()
    {
        if(islandTexture == null)
        {
            Debug.LogError("You must generate an island texture before placing the island into the scene.");
            return;
        }
        EditorCoroutineUtility.StartCoroutine(IslandTextureToWorldGen(new Vector3Int((int)transform.position.x, (int)transform.position.y, 0)), this);
    }


    /// <summary>
    /// Uses the previously generated Island Texture to produce a game world. Maps Each pixel to a biome and generates the world accordingly.
    /// </summary>
    public IEnumerator IslandTextureToWorldGen(Vector3Int center)
    {
        Vector3 initialPosition = transform.position;

        //Add each of our pixel color, Biome mappings to a dictionary for quick lookup.
        Dictionary<Color32, Biome> ColorBiomeDictionary = new Dictionary<Color32, Biome>(); 

        foreach(PixelBiomeMap PBM in IslandBiomeMappings)
        {
            ColorBiomeDictionary.Add(PBM.color, PBM.biome);
        }

        //Get all the pixels in our island image
        Color32 [] pixels = islandTexture.GetPixels32();
        
        //Create an array of tilebase of the same length
        TileBase[] tiles = new TileBase[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            //If the pixel corresponds to a biome, get the default floor tile for the biome
            if (ColorBiomeDictionary.TryGetValue(pixels[i], out Biome biome))
            {
                tiles[i] = biome.defaultFloorTile;
            }
            //Otherwise, set it equal to the defaul biome default floor time, if it exists.
            else
            {
                tiles[i] = DefaultBiome != null ? DefaultBiome.defaultFloorTile : null;
            }

            if(i % 1000 == 0)
            {
                yield return null;
            }
        }

        int2 offset = new int2(islandTexture.width / 2, islandTexture.height / 2);

        tilemaps[0].SetTilesBlock(
            new BoundsInt(center.x - offset.x, center.y - offset.y,
            tilemaps[0].cellBounds.zMin,
            islandTexture.width, islandTexture.height,
            tilemaps[0].cellBounds.size.z
            ),
            tiles
            );

        List<Biome> biomes = new List<Biome>();
        foreach(PixelBiomeMap PBM in IslandBiomeMappings)
        {
            if(!biomes.Contains(PBM.biome))
            {
                biomes.Add(PBM.biome);
            }
        }

        for(int x = RoundUp(-offset.x, 100); x <= RoundUp(islandTexture.width - offset.x, 100); x+= 100)
        {
            for (int y = RoundUp(-offset.y, 100); y <= RoundUp(islandTexture.height - offset.y, 100); y += 100)
            {
                EditorCoroutineUtility.StartCoroutine(
                    LocalMapAreaGenerator.GenerateWorld(
                        new Vector2Int(100, 100),
                        new Vector3(initialPosition.x - x, initialPosition.y - y, 0),
                        tilemaps,
                        biomes,
                        cellularAutomataRules),
                    this);

                yield return null;
            }
        }




        yield return null;
    }


    private int RoundUp(int numToRound, int multiple)
    {
        //If negative, we multiply by -1 to make it positive.
        int isPositive = (numToRound >= 0) ? 1 : -1;
        numToRound *= isPositive;

        //Converts back to negative if it was negative otherwise does nothing.
        return Mathf.CeilToInt(numToRound) * isPositive;
    }

}
