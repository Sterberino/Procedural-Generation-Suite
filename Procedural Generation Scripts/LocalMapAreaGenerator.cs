using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LocalMapAreaGenerator
{ 
    public static IEnumerator GenerateWorld(Vector2Int areaSize, Vector3 offset, List<Tilemap> tilemaps, 
        List<Biome> biomes, CellularAutomataRules cellularAutomataRules)
    {

        //Initialize the dictionary
        Dictionary<TileBase, Biome> tileBiomeMap = new Dictionary<TileBase, Biome>();
        for(int i = 0; i < biomes.Count; i++)
        {
            for(int j = 0; j < biomes[i].FloorTiles.Count; j++)
            {
                tileBiomeMap.Add(biomes[i].FloorTiles[j], biomes[i]);
            }
        }

        System.Random rand = new System.Random(Time.time.GetHashCode());
        
        GameObject biomeObjectHolder = new GameObject();
        biomeObjectHolder.name = "Biome Objects";
        biomeObjectHolder.transform.position = new Vector3(0, 0, 0);

        PlaceCellularAutomata(tileBiomeMap, tilemaps, areaSize, offset, cellularAutomataRules);
        
        for (int x = 0; x < areaSize.x; x++)
        {
            for (int y = 0; y < areaSize.y; y++)
            {

                Vector3Int pos = tilemaps[0].WorldToCell(new Vector3(offset.x + x, offset.y + y, tilemaps[0].transform.position.z));
                TileBase tile = tilemaps[0].GetTile(pos);

                if(tile == null)
                {
                    continue;
                }
                //We found a valid biome at the position
                if(tileBiomeMap.TryGetValue(tile, out Biome biome))
                {

                    foreach(Biome.BiomeFoliage biomeFoliage in biome.biomeFoliage)
                    {
                        if(!biomeFoliage.CanPlace(pos, tilemaps))
                        {
                            continue;
                        }

                        int val = rand.Next(0, 100);
                        if(val <= biomeFoliage.spawnChance)
                        {
                            tilemaps[biomeFoliage.orderInLayer].SetTile(pos, biomeFoliage.tile);
                        }
                    }

                    foreach(Biome.BiomeObject biomeObject in biome.biomeObjects)
                    {
                        if (!biomeObject.CanPlace(pos, tilemaps))
                        {
                            continue;
                        }

                        float val = (float)(rand.NextDouble() * 100);
                        if (val <= biomeObject.spawnChance)
                        {
                            bool isEditor = false;
#if UNITY_EDITOR
                            isEditor = true;
#endif
                            GameObject g;
                            if(isEditor)
                            {

                                if(UnityEditor.PrefabUtility.IsPartOfAnyPrefab(biomeObject.prefabObject))
                                {
                                    g = UnityEditor.PrefabUtility.InstantiatePrefab(biomeObject.prefabObject) as GameObject;
                                }
                                else
                                {
                                    g = GameObject.Instantiate(biomeObject.prefabObject);
                                }
                            }
                            else
                            {
                                g = GameObject.Instantiate(biomeObject.prefabObject);
                            }
                            
                            float xOffset = (float)rand.Next(-10000, 10000) / 100000f;
                            float yOffset = (float)rand.Next(-10000, 10000) / 100000f;

                            g.transform.position = (Vector3)pos + new Vector3(xOffset, yOffset, 0);
                            g.transform.parent = biomeObjectHolder.transform;
                        }
                    }

                }
                
                if((y * areaSize.x + x) % 1000 == 0)
                {
                    yield return null;
                }

            }
        }

        if(biomeObjectHolder.transform.childCount == 0)
        {
           GameObject.DestroyImmediate(biomeObjectHolder);
        }

        yield return null;
    }

    /// <summary>
    /// Places areas of denser foliage 
    /// </summary>
    public static void PlaceCellularAutomata(Dictionary<TileBase, Biome> tileBiomeMap, List<Tilemap> tilemaps, Vector2Int size, Vector2 offset, CellularAutomataRules cellularAutomataRules)
    {
        bool[] automata = CellularAutomataGenerator.GenerateCellularAutomata(size, cellularAutomataRules.seed /*+ transform.position.ToString() */, cellularAutomataRules.iterationCount, cellularAutomataRules.fillPercent, 
            cellularAutomataRules.lowerBoundRule, cellularAutomataRules.upperBoundRule, cellularAutomataRules.borderRule);

        //The indices that have been placed or checked for placing
        HashSet<int> indices = new HashSet<int>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {

                int index = size.x * y + x;
                if(index < 0 || index >= automata.Length || !automata[index] || indices.Contains(index))
                {
                    continue;
                }
      
                Vector3Int pos = tilemaps[0].WorldToCell(new Vector3(offset.x + x, offset.y + y, tilemaps[0].transform.position.z));
                TileBase tile = tilemaps[0].GetTile(pos);

                if (tile == null)
                {
                    continue;
                }
                //We found a valid biome at the position
                if (tileBiomeMap.TryGetValue(tile, out Biome initialBiome))
                {
                    if(initialBiome.automataFoliage.tile == null)
                    {
                        continue;
                    }

                    //Initialize a list for the unchecked neighbors, the checked indices, and add the initial index to the hashset for local and global indices
                    List<int> uncheckedNeighbors = new List<int>();
                    HashSet<int> localIndices = new HashSet<int>();

                    uncheckedNeighbors.Add(index);
                    bool demonCrossesBiomes = false;

                    while(uncheckedNeighbors.Count > 0)
                    {
                        int currentIndex = uncheckedNeighbors[uncheckedNeighbors.Count - 1];
                        uncheckedNeighbors.RemoveAt(uncheckedNeighbors.Count - 1);
                        
                        uncheckedNeighbors.TrimExcess();

                        //Add current index to the hashset of local neighbors and globally checked indices.
                        localIndices.Add(currentIndex);
                        indices.Add(currentIndex);

                        //Get the current 2D location from the current index.
                        int currentX = currentIndex % size.x;
                        int currentY = currentIndex / size.y;

                        //Sample the tilemap given the 2d Location
                        Vector3Int currentPos = tilemaps[0].WorldToCell(new Vector3(offset.x + currentX, offset.y + currentY, tilemaps[0].transform.position.z));
                        TileBase currentTile = tilemaps[0].GetTile(currentPos);

                        //No tile or biome at location means that the demon does not cross biome. If the biome at location doesn't equal the initial biome, flag it.
                        if (currentTile != null && tileBiomeMap.TryGetValue(currentTile, out Biome currentBiome))
                        {
                            if(currentBiome != initialBiome)
                            {
                                demonCrossesBiomes = true;
                            }
                        }

                        //For the 8 neighbors surrounding the location, add them to the list of unchecked neighbors
                        for (int neighborX = -1; neighborX <= 1; neighborX++)
                        {
                            for (int neighborY = -1; neighborY <= 1; neighborY++)
                            {
                                //If the neighbor is the center (not a neighbor), continue
                                if (neighborX == 0 && neighborY == 0)
                                {
                                    continue;
                                }

                                //If the neighboring index is outside of the indices of our automata, continue;
                                int neighborIndex = (neighborY + currentY) * size.x + (currentX + neighborX);
                                if (neighborIndex < 0 || neighborIndex >= automata.Length || indices.Contains(neighborIndex) || localIndices.Contains(neighborIndex) || !automata[neighborIndex])
                                {
                                    continue;
                                }
                                //Debug.Log(neighborIndex + (" (" + (currentX + neighborX + offset.x) + ", " + (currentY + neighborY + offset.y) + ")"));
                                uncheckedNeighbors.Add(neighborIndex);
                            }
                        }
      

                    }

                    if (demonCrossesBiomes)
                    {
                        foreach(int automataNeighborhoodIndex in localIndices)
                        {
                            automata[automataNeighborhoodIndex] = false;
                        }
                    }

                    foreach(int localIndex in localIndices)
                    {
                        if (automata[localIndex])
                        {
                            Vector3Int localPos = tilemaps[initialBiome.automataFoliage.orderInLayer].WorldToCell(new Vector3((localIndex % size.x) + offset.x,
                                (localIndex / size.x) + offset.y, tilemaps[initialBiome.automataFoliage.orderInLayer].transform.position.z));
                            tilemaps[initialBiome.automataFoliage.orderInLayer].SetTile(localPos, initialBiome.automataFoliage.tile);
                        }
                    }
                   

                }

            }
        }

    }

 

}
