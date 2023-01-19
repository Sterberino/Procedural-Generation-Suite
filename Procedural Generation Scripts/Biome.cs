using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Represents tiles that are placed using Cellular Automata and the Cellular Automata Rules.
/// </summary>
[System.Serializable]
public class CellularAutomataRules
{
    public string seed;

    public int iterationCount = 5;
    [Tooltip("How much of the area should be filled with cellular automata values.")]
    public int fillPercent = 34;
    [Tooltip("pixels with more 'false' neighbors than this bound are replaced with 'false.'")] 
    public int lowerBoundRule = 4;
    [Tooltip("pixels with more 'true' neighbors than this bound are replaced with 'true.'")]
    public int upperBoundRule = 4;

    public CellularAutomataGenerator.BorderRule borderRule = CellularAutomataGenerator.BorderRule.StripBorder;
}

[System.Serializable]

public struct PlacementRule
{
    public enum TileRule { Tile, Sprite};

    [Header("Check For:")]
    public TileRule TileOrSprite;
    
    [DrawIf("TileOrSprite", TileRule.Tile, DrawIfAttribute.DisablingType.DontDraw)]
    [Tooltip("The Tilebase tile contained by the Tilemap at the given position.")]
    public TileBase tileAtLocation;

    [DrawIf("TileOrSprite", TileRule.Sprite, DrawIfAttribute.DisablingType.DontDraw)]
    [Tooltip("The sprite contained by the Tilemap at the given position.")]
    public Sprite SpriteAtLocation;
    public enum PresentRule { Present, NotPresent };
    public PresentRule Rule;

    public int tilemapLayerToCheck;
}

[CreateAssetMenu(menuName = "Custom Scriptable Objects/Biome", fileName = "New Biome")]
public class Biome : ScriptableObject
{
    public TileBase defaultFloorTile;
    //[Tooltip("The tilemap layer that the default biome tile should be drawn to.")]
    //public int floorTileLayer;

    [Tooltip("The default floor tiles of the biome. Multiple tiles may be used.")]
    public List<TileBase> FloorTiles;





    [System.Serializable]
    public abstract class BiomePlaceable
    {
        [Range(0, 100f)]
        public float spawnChance;
        public List<PlacementRule> placementRules = new List<PlacementRule>();

        public virtual bool CanPlace(Vector3Int position, List<Tilemap> tilemapLayers)
        {
            if (placementRules.Count == 0)
            {
                return true;
            }

            foreach (PlacementRule placementRule in placementRules)
            {
                if(placementRule.TileOrSprite == PlacementRule.TileRule.Tile)
                {
                    if(placementRule.Rule == PlacementRule.PresentRule.Present)
                    {
                        if (placementRule.tilemapLayerToCheck >= tilemapLayers.Count || placementRule.tilemapLayerToCheck < 0)
                        {
                            return false;
                        }
                        else if (tilemapLayers[placementRule.tilemapLayerToCheck].GetTile(position) != placementRule.tileAtLocation)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (placementRule.tilemapLayerToCheck >= tilemapLayers.Count || placementRule.tilemapLayerToCheck < 0)
                        {
                            continue;
                        }
                        else if (tilemapLayers[placementRule.tilemapLayerToCheck].GetTile(position) == placementRule.tileAtLocation)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if(placementRule.Rule == PlacementRule.PresentRule.Present)
                    {
                        if (placementRule.tilemapLayerToCheck >= tilemapLayers.Count || placementRule.tilemapLayerToCheck < 0)
                        {
                            return false;
                        }
                        else if (tilemapLayers[placementRule.tilemapLayerToCheck].GetSprite(position) != placementRule.SpriteAtLocation)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (placementRule.tilemapLayerToCheck >= tilemapLayers.Count || placementRule.tilemapLayerToCheck < 0)
                        {
                            continue;
                        }
                        else if (tilemapLayers[placementRule.tilemapLayerToCheck].GetSprite(position) == placementRule.SpriteAtLocation)
                        {
                            return false;
                        }
                    }
                }
            }


            return true;
        }
    }


    [System.Serializable]
    public class BiomeFoliage : BiomePlaceable
    {
        public TileBase tile;
        public int orderInLayer;
  

        public override bool CanPlace(Vector3Int position, List<Tilemap> tilemapLayers)
        {
            if(orderInLayer < 0 || orderInLayer >= tilemapLayers.Count)
            {
                return false;
            }

            return base.CanPlace(position, tilemapLayers);
        }
    }

    [System.Serializable]
    public class BiomeObject : BiomePlaceable
    {
        public GameObject prefabObject;
 

    }

    public List<BiomeObject> biomeObjects;
    public List<BiomeFoliage> biomeFoliage;

    public BiomeFoliage automataFoliage;

    public void PlaceBiome(Vector3Int position, List<Tilemap> tilemaps, bool automataFoliage, bool overrideExistingBiome)
    {
        if(tilemaps.Count == 0)
        {
            return;
        }


        //If not overriding the biome at the location, we check that a valid biome floor tile is present at the location befor placing tiles or objects
        if(!overrideExistingBiome)
        {
            if (!FloorTiles.Contains(tilemaps[0].GetTile(position)))
            {
                return;
            }
        }


    }
}
