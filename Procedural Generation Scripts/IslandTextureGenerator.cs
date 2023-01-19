using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.UI;


public class IslandTextureGenerator
{
    /// <summary>
    /// A struct containing all necessary paramaters for the generation of Simplex Noise.
    /// </summary>
    [System.Serializable]
    public struct NoiseParameters
    {
        [Header("Simplex Parameters")]
        public int seed;
        public float lacunarity;
        public float scale;
        public float persistance;

        [Header("Adjustments")]
        public float contrast;
        public float brightness;

        [Header("Bilateral Filter Options")]
        public bool applyBilateralFilter;
        public float spatialBlurWeight;
        public float intensityBlurWeight;
        public int blurKernalSize;

        [Header("Texture Saving Options")]
        public bool saveGeneratedTexture;
        public string fileDirectory;
        public string filename;
    }

    /// <summary>
    /// Generates a 2D island using Simplex noise. Takes as input two noise paramater structs, a Texture that maps noise values to a color, and the dimensions of the Island in pixels. 
    /// </summary>
    public static Texture2D GenerateIsland(IslandTextureGenerator.NoiseParameters moistureMapParameters, IslandTextureGenerator.NoiseParameters heightMapParameters,
        Texture2D moistureHeightGraph, Vector2Int dimensions, bool smoothBiomes)
    {
        Texture2D HeightTexture = SimplexNoiseGenerator.GenerateSimplexTexture(dimensions.x, dimensions.y, heightMapParameters.seed,
            heightMapParameters.lacunarity, heightMapParameters.persistance, heightMapParameters.scale);


        if (heightMapParameters.contrast != 0)
        {
            HeightTexture = ImageFiltering.AdjustContrast(HeightTexture, heightMapParameters.contrast);
        }
        if (heightMapParameters.brightness != 0)
        {
            HeightTexture = ImageFiltering.AdjustBrightness(HeightTexture, heightMapParameters.brightness);
        }
        if (heightMapParameters.applyBilateralFilter)
        {
            HeightTexture = ImageFiltering.ApplyBilateralFilter(HeightTexture, heightMapParameters.blurKernalSize, heightMapParameters.spatialBlurWeight, heightMapParameters.intensityBlurWeight);
        }

        Texture2D MoistureTexture = SimplexNoiseGenerator.GenerateSimplexTexture(dimensions.x, dimensions.y, moistureMapParameters.seed,
            moistureMapParameters.lacunarity, moistureMapParameters.persistance, moistureMapParameters.scale);


        if (moistureMapParameters.contrast != 0)
        {
            MoistureTexture = ImageFiltering.AdjustContrast(MoistureTexture, moistureMapParameters.contrast);
        }
        if (moistureMapParameters.brightness != 0)
        {
            MoistureTexture = ImageFiltering.AdjustBrightness(MoistureTexture, moistureMapParameters.brightness);
        }
        if (moistureMapParameters.applyBilateralFilter)
        {
            MoistureTexture = ImageFiltering.ApplyBilateralFilter(MoistureTexture, moistureMapParameters.blurKernalSize, moistureMapParameters.spatialBlurWeight, moistureMapParameters.intensityBlurWeight);
        }
        if (moistureMapParameters.saveGeneratedTexture)
        {
            TrySaveTexture(MoistureTexture, moistureMapParameters.fileDirectory, moistureMapParameters.filename);
        }

        NativeArray<Color32> heightPixels = HeightTexture.GetRawTextureData<Color32>();
        NativeArray<Color32> moisturePixels = MoistureTexture.GetRawTextureData<Color32>();

        NativeArray<Color32> heightMoistureMap = moistureHeightGraph.GetRawTextureData<Color32>(); ;

        Texture2D result = new Texture2D(dimensions.x, dimensions.y);
        NativeArray<Color32> resultArray = result.GetRawTextureData<Color32>();

        NativeArray<Color32> squareGradientArray = new NativeArray<Color32>(dimensions.x * dimensions.y, Allocator.Persistent);
        GenerateSquareGradient gradientJob = new GenerateSquareGradient()
        {
            dimensions = new int2(dimensions.x, dimensions.y),
            result = squareGradientArray
        };

        JobHandle jobHandle = gradientJob.Schedule(dimensions.x * dimensions.y, 64);
        jobHandle.Complete();

        GenerateIslandJob generateIslandJob = new GenerateIslandJob
        {
            heightMap = heightPixels,
            moistureMap = moisturePixels,
            MapDimensions = new int2(dimensions.x, dimensions.y),

            moistureHeightGraph = heightMoistureMap,
            GraphDimensions = new int2(moistureHeightGraph.width, moistureHeightGraph.height),
            squareGradient = gradientJob.result,

            result = resultArray
        };
        JobHandle jobHandle2 = generateIslandJob.Schedule(dimensions.x * dimensions.y, 64);
        jobHandle2.Complete();

        if (heightMapParameters.saveGeneratedTexture)
        {
            TrySaveTexture(HeightTexture, heightMapParameters.fileDirectory, heightMapParameters.filename);
        }

        if (smoothBiomes)
        {
            NativeParallelHashMap<int, int> keyValues = new NativeParallelHashMap<int, int>(8, Allocator.Persistent);
            SmoothNeighborsJob smoothNeighborsJob = new SmoothNeighborsJob
            {
                size = new int2(dimensions.x, dimensions.y),
                values = resultArray,
                map = keyValues,
                upperThreshold = 4,
                lowerThreshold = 4
            };

            JobHandle jobHandle3 = smoothNeighborsJob.Schedule();
            jobHandle3.Complete();
            keyValues.Dispose();
        }


        result.filterMode = FilterMode.Point;
        result.Apply();

        squareGradientArray.Dispose();
        return result;
    }


    [BurstCompatible]
    [BurstCompile]
    private struct SmoothNeighborsJob : IJob
    {
        public int2 size;
        public NativeArray<Color32> values;
        public NativeParallelHashMap<int, int> map;

        public int upperThreshold;
        public int lowerThreshold;

        public void Execute()
        {
            int resultsLength = size.x * size.y;
            for (int i = 0; i < resultsLength; i++)
            {
                int x = i % size.x;
                int y = i / size.x;


                int neighborCount = 0;
                for (int neighborX = -1; neighborX <= 1; neighborX++)
                {
                    for (int neighborY = -1; neighborY <= 1; neighborY++)
                    {
                        int neighborIndex = (x + neighborX) + (y + neighborY) * size.x;
                        if (neighborIndex < 0 || neighborIndex >= resultsLength)
                        {
                            continue;
                        }
                        if (ColorsAreEqual(values[neighborIndex], values[i]))
                        {
                            neighborCount++;
                        }
                   

                    }
                }

                //If the number of surrounding pixels of the same color is greater than some threshold
                if (neighborCount > upperThreshold)
                {
                    continue;
                }
                //Otherwise, we need to find the most common neighbor pixel and set it equal to that.
                if (neighborCount < lowerThreshold)
                {
                    map.Clear();
                    for (int neighborX = -1; neighborX <= 1; neighborX++)
                    {
                        for (int neighborY = -1; neighborY <= 1; neighborY++)
                        {
                            int neighborIndex = (x + neighborX) + (y + neighborY) * size.x;
                            if (neighborIndex < 0 || neighborIndex >= resultsLength || neighborIndex == i)
                            {
                                continue;
                            }

                            int pixelHexValue = ToHex(values[neighborIndex]);
                            if (map.ContainsKey(pixelHexValue))
                            {
                                map[pixelHexValue] += 1;
                            }
                            else
                            {
                                map.Add(pixelHexValue, 1);
                            }


                        }
                    }

                    int frequency = 0;
                    int mostCommon = 0;

                    //Get all neighbors using Unit Circle Trig
                    for (int j = 0; j < 8; j++)
                    {
                        //0.0174532924f is a conversion factor from Degrees to Radians. It is equal to Mathf.Deg2Rad
                        int xx = x + (int)math.round(math.cos((float)j * 45f * 0.0174532924f));
                        int yy = y + (int)math.round(math.sin((float)j * 45f * 0.0174532924f));
                        int neighborIndex = xx + yy * size.x;

                        //Outside of range of our array (x or y value is less than 0 or greater than width/ heigh of the Texture.)
                        if (neighborIndex < 0 || neighborIndex >= values.Length)
                        {
                            continue;
                        }

                        int hexVal = ToHex(values[neighborIndex]);
                        if (map.TryGetValue(hexVal, out int freq))
                        {
                            if(freq > frequency)
                            {
                                frequency = freq;
                                mostCommon = hexVal;
                            }
                        }
                    }

                    if(mostCommon > neighborCount)
                    {
                        Color32 c = ToColor32(mostCommon);
                        values[i] = c;
                    }
                }

            }
        }

        private int ToHex(Color32 c)
        {
            return 0 | (c.r << 16 | c.g << 8 | c.b);
        }

        public Color32 ToColor32(int HexVal)
        {
            byte R = (byte)((HexVal >> 16) & 0xFF);
            byte G = (byte)((HexVal >> 8) & 0xFF);
            byte B = (byte)((HexVal) & 0xFF);
            return new Color32(R, G, B, 255);
        }

        private bool ColorsAreEqual(Color32 a, Color32 b)
        {
            return (a.r == b.r && a.b == b.b && a.g == b.g);
        }
    }


    private static void TrySaveTexture(Texture2D texture, string fileDirectory, string filename)
    {
       
            if (texture == null)
            {
                Debug.LogError("Texture to save was null. Something went wrong.");
                return;
            }

            if (fileDirectory == "")
            {
                Debug.LogError("Unable to save Noise texture " + filename + " because a directory was not specified.");
                return;
            }

            if (!System.IO.Directory.Exists(fileDirectory))
            {
            Debug.LogError("Unable to save Noise texture " + filename + " because the directory " + fileDirectory + " does not exist.");
            return;
            }

            byte[] bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(fileDirectory + filename + ".png", bytes);
        
    }

    [BurstCompatible]
    [BurstCompile]
    public struct GenerateSquareGradient : IJobParallelFor
    {
        public int2 dimensions;

        public NativeArray<Color32> result;

        public void Execute(int i)
        {
            int x = i % dimensions.x;
            int y = i / dimensions.x;

            x = x > (dimensions.x / 2) ? dimensions.x - x : x;
            y = y > (dimensions.y / 2) ? dimensions.y - y : y;

            int smaller = x < y ? x : y;
            int halfWidth = dimensions.x / 2 < dimensions.y / 2 ? dimensions.x / 2 : dimensions.y / 2;

            float color = smaller / (float)(halfWidth);
            color = 1 - color;
            color *= (color * color) * 255f;

            byte byteColor = (byte)math.clamp(color, 0, 255);
            result[i] = new Color32(byteColor, byteColor, byteColor, (byte)255);
        }
    }

    [BurstCompile]
    [BurstCompatible]
    private struct SubtractGradientJob : IJobParallelFor
    {
        public NativeArray<Color32> heightMap;
        public NativeArray<Color32> squareGradient;

        public NativeArray<Color32> result;
        public void Execute(int i)
        {
            int color = (int)math.round(GetPixelIntensity(heightMap[i]) - GetPixelIntensity(squareGradient[i]));
            byte byteColor = (byte)math.clamp(color, 0, 255);
            result[i] = new Color32(byteColor, byteColor, byteColor, (byte)255);

        }

        public float GetPixelIntensity(Color32 c)
        {
            //Get pixel intensity [0, 255] and convert to [0,1]
            float intensity = ((float)c.r + (float)c.g + (float)c.b) / (3f);
            return intensity;
        }
    }


    [BurstCompile]
    [BurstCompatible]
    public struct GenerateIslandJob : IJobParallelFor
    {
        public int2 MapDimensions;

        public NativeArray<Color32> moistureMap;
        public NativeArray<Color32> heightMap;
        public NativeArray<Color32> squareGradient;

        public int2 GraphDimensions;

        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public NativeArray<Color32> moistureHeightGraph;

        public NativeArray<Color32> result;

        public void Execute(int i)
        {
            int x = (int)math.round(GraphDimensions.x * GetPixelIntensity(moistureMap[i]));
            x = x >= GraphDimensions.x ? GraphDimensions.x - 1 : x;

            //----------------We must subtract out the Square gradient value from the pixel intensity------------------------------
            int y = (int)math.round(GraphDimensions.y * (GetPixelIntensity(heightMap[i]) - GetPixelIntensity(squareGradient[i])));
            
            //We will apply the subtraction to the height map here in case the use saves the png
            
            heightMap[i] = new Color32( 
                (byte)math.clamp(heightMap[i].r - squareGradient[i].r, 0, 255),
                (byte)math.clamp(heightMap[i].g - squareGradient[i].g, 0, 255),
                (byte)math.clamp(heightMap[i].b - squareGradient[i].b, 0, 255),
                255
                );
            
            y = math.clamp(y, 0, GraphDimensions.y - 1);

            y = y >= GraphDimensions.y ? GraphDimensions.y - 1 : y;

            //Get the index of the x and y values on the 1D HeightMoistureMap
            int graphIndex = y * GraphDimensions.x + x;
            Color32 graphColor = moistureHeightGraph[graphIndex];
            result[i] = graphColor;
        }


        //return the intensity value of a pixel as a percentage [0,1].
        public float GetPixelIntensity(Color32 c)
        {
            //Get pixel intensity [0, 255] and convert to [0,1]
            float intensity = ((float)c.r + (float)c.g + (float)c.b) / (3f * 255f);
            return intensity;
        }
    }


}
