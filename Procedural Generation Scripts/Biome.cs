using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;



[CreateAssetMenu(menuName = "Biome", fileName = "New Biome")]
public class Biome : ScriptableObject
{

    [System.Serializable]

    public struct PlacementRule
    {
        public enum TileRule { Tile, Sprite };

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

    [System.Serializable]
    public class AutomataFoliage
    {
        public TileBase tile;
        public int orderInLayer;
    }


    public TileBase defaultFloorTile;
    //[Tooltip("The tilemap layer that the default biome tile should be drawn to.")]
    //public int floorTileLayer;

    [Tooltip("The default floor tiles of the biome. Multiple tiles may be used.")]
    public List<TileBase> FloorTiles;

    public List<BiomeObject> biomeObjects;
    public List<BiomeFoliage> biomeFoliage;

    public AutomataFoliage automataFoliage;

   
}


