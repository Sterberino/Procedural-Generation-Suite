using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class CellularAutomataGenerator
{
    public enum BorderRule { AddBorder, StripBorder, Nothing }

    private static bool [] RemoveSquares(bool[] grid, Vector2Int size)
    {
        NativeArray<bool> nativegrid = new NativeArray<bool>(grid, Allocator.Persistent);
        NativeArray<bool> squaresJobOutput = new NativeArray<bool>(grid, Allocator.Persistent);
        

        RemoveSquaresJob removeSquaresJob = new RemoveSquaresJob
        {
            inputGrid = nativegrid,
            size = new int2(size.x, size.y),
            outputGrid = squaresJobOutput
        };

        JobHandle jobHandle = removeSquaresJob.Schedule();
        jobHandle.Complete();

        RemoveOrphanPixelsJob removeOrphanPixelsJob = new RemoveOrphanPixelsJob
        { 
            grid = squaresJobOutput,
            size = new int2(size.x, size.y)
        };

        JobHandle jobHandle1 = removeOrphanPixelsJob.Schedule(squaresJobOutput.Length, 64);
        jobHandle1.Complete();

        bool[] output = removeOrphanPixelsJob.grid.ToArray();

        nativegrid.Dispose();
        squaresJobOutput.Dispose();

        return output;
    }

    [BurstCompatible]
    [BurstCompile]
    private struct RemoveSquaresJob : IJob
    {
        public NativeArray<bool> inputGrid;
        public NativeArray<bool> outputGrid;
        public int2 size;

        public void Execute()
        {
            //Iterate through each location on the grid
            for (int i = 0; i < inputGrid.Length; i++)
            {
                int val = 0;

                //Get the 2d location from the 1d array
                int x = i % size.x;
                int y = i / size.x;

                //iterate through neighboring pixels in counterclockwise fashion using the unit circle
                for (int j = 0; j < 8; j++)
                {
                    int xx = x + Mathf.RoundToInt(Mathf.Cos((float)j * 45f * Mathf.Deg2Rad));
                    int yy = y + Mathf.RoundToInt(Mathf.Sin((float)j * 45f * Mathf.Deg2Rad));

                    if (xx < 0 || xx >= size.x - 1 || yy < 0 || yy >= size.y - 1)
                    {
                        continue;
                    }

                    //Store the true grid values in a single int using bitwise operation. We are checking for specific combinations of neighbor values.
                    if (inputGrid[yy * size.x + xx])
                    {
                        val |= (0x01 << j);
                    }
                }

                bool pseudoSquare = false;
                switch (val)
                {
                    //0,1,2 - Top Right
                    case 7:
                        pseudoSquare = true;
                        break;
                    //2,3,4 - Top Left
                    case 28:
                        pseudoSquare = true;
                        break;
                    //4,5,6 - Bottom Left
                    case 112:
                        pseudoSquare = true;
                        break;
                    //6,7,0 - Bottom Right
                    case 193:
                        pseudoSquare = true;
                        break;
                    default: break;
                }

                if (pseudoSquare)
                {
                    bool squareFound = true;
                    for (int ii = -2; ii <=2; ii++)
                    {
                        for (int jj = -2; jj <= 2; jj++)
                        {
                            if(math.abs(ii) != 2 && math.abs(jj) != 2)
                            {
                                continue;
                            }

                            int xx = x + ii;
                            int yy = y + jj;

                            if (xx < 0 || xx >= size.x || yy < 0 || yy >= size.y)
                            {
                                continue;
                            }

                            if (inputGrid[yy * size.x + xx])
                            {
                                squareFound = false;
                                break;
                            }
                        }

                        if(!squareFound)
                        {
                            break;
                        }
                    }

                    if (squareFound)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            //0.0174532924f = Degree to Radian conversion factor
                            int xx = x + (int)math.round(math.cos((float)j * 45f * 0.0174532924f));
                            int yy = y + (int)math.round(math.sin((float)j * 45f * 0.0174532924f));

                            if (xx < 0 || xx >= size.x || yy < 0 || yy >= size.y)
                            {
                                continue;
                            }

                            outputGrid[yy * size.x + xx] = false;
                        }
                        outputGrid[i] = false;
                    }
                    else
                    {
                        outputGrid[i] = inputGrid[i];
                    }
                }
            }
        }
    }
    private struct RemoveOrphanPixelsJob : IJobParallelFor
    {
        public int2 size;
        [NativeDisableParallelForRestriction]
        public NativeArray<bool> grid;

        public void Execute(int i)
        {
           
                int x = i % size.x;
                int y = i / size.x;

                bool foundNeighbor = false;

                for (int j = 0; j < 8; j++)
                {
                    if (j % 2 != 0)
                    {
                        continue;
                    }

                    int xx = x + Mathf.RoundToInt(Mathf.Cos((float)j * 45f * Mathf.Deg2Rad));
                    int yy = y + Mathf.RoundToInt(Mathf.Sin((float)j * 45f * Mathf.Deg2Rad));

                    if (xx < 0 || xx >= size.x - 1 || yy < 0 || yy >= size.y - 1)
                    {
                        continue;
                    }

                    if (grid[yy * size.x + xx])
                    {
                        foundNeighbor = true;
                        break;
                    }

                }

                if (!foundNeighbor)
                {
                    grid[i] = false;
                }
            }     
    }



    public static bool [] GenerateCellularAutomata(Vector2Int size, int iterations, int fillPercent, int lowerThreshold, int upperThreshold, CellularAutomataGenerator.BorderRule borderRule)
    {
        bool [] values = new bool[size.x * size.y];
        //System.Random r = new System.Random(Time.time.ToString().GetHashCode());

        NativeArray<bool> nativeValues = new NativeArray<bool>(values, Allocator.Persistent);
        RandomizePixelsJob randomizePixelsJob = new RandomizePixelsJob
        {
            values = nativeValues,
            size = new int2(size.x, size.y),
            fillPercent = fillPercent,
            borderRule = (int)borderRule,
            random = new Unity.Mathematics.Random((uint)Time.time.ToString().GetHashCode())

        };

        JobHandle jobHandle = randomizePixelsJob.Schedule(nativeValues.Length, 64);
        jobHandle.Complete();

        for (int iteration = 0; iteration < iterations; iteration++)
        {

            SmoothNeighborsJob smoothNeighborsJob = new SmoothNeighborsJob
            {
                size = new int2(size.x, size.y),
                borderRule = (int)borderRule,
                values = nativeValues,

                upperThreshold = upperThreshold,
                lowerThreshold = lowerThreshold
            };

            JobHandle jobHandle1 = smoothNeighborsJob.Schedule();
            jobHandle1.Complete();
        }


        values = randomizePixelsJob.values.ToArray();
        nativeValues.Dispose();


        return /*values;*/ RemoveSquares(values, size);
    }

    public static bool[] GenerateCellularAutomata(Vector2Int size, string seed, int iterations, int fillPercent, int lowerThreshold, int upperThreshold, CellularAutomataGenerator.BorderRule borderRule)
    {
        bool[] values = new bool[size.x * size.y];
        //System.Random r = new System.Random(Time.time.ToString().GetHashCode());

        NativeArray<bool> nativeValues = new NativeArray<bool>(values, Allocator.Persistent);
        RandomizePixelsWithSeedJob randomizePixelsJob = new RandomizePixelsWithSeedJob
        {
            values = nativeValues,
            size = new int2(size.x, size.y),
            fillPercent = fillPercent,
            borderRule = (int)borderRule,
            random = new Unity.Mathematics.Random((uint)seed.GetHashCode())

        };

        JobHandle jobHandle = randomizePixelsJob.Schedule();
        jobHandle.Complete();

        for (int iteration = 0; iteration < iterations; iteration++)
        {

            SmoothNeighborsJob smoothNeighborsJob = new SmoothNeighborsJob
            {
                size = new int2(size.x, size.y),
                borderRule = (int)borderRule,
                values = nativeValues,

                upperThreshold = upperThreshold,
                lowerThreshold = lowerThreshold
            };

            JobHandle jobHandle1 = smoothNeighborsJob.Schedule();
            jobHandle1.Complete();
        }


        values = randomizePixelsJob.values.ToArray();
        nativeValues.Dispose();


        return RemoveSquares(values, size);
    }

    //we need a separate job that uses IJob and not IJobParallelFor if using a seed to guarantee the positions are processed in sequence if using a seed.
    [BurstCompatible]
    [BurstCompile]
    private struct RandomizePixelsWithSeedJob : IJob
    {
        public int2 size;
        public NativeArray<bool> values;
        public int borderRule;
        public int fillPercent;
        public Unity.Mathematics.Random random;

        public void Execute()
        {
            for(int i = 0; i < values.Length; i++)
            {

                int x = i % size.x;
                int y = i / size.x;

                if (x == 0 || y == 0 || x == size.x - 1 || y == size.y - 1)
                {
                    if (borderRule == 0)
                    {
                        values[i] = true;
                    }
                    else if (borderRule == 1)
                    {
                        values[i] = false;
                    }

                    else
                    {
                        int val = random.NextInt(0, 100);
                        values[i] = val < fillPercent ? true : false;
                    }
                }
                else
                {
                    int val = random.NextInt(0, 100);
                    values[i] = val < fillPercent ? true : false;
                }
            }
        }
    }

    [BurstCompatible]
    [BurstCompile]
    private struct RandomizePixelsJob: IJobParallelFor
    {
        public int2 size;
        public NativeArray<bool> values;
        public int borderRule;
        public int fillPercent;
        public Unity.Mathematics.Random random;

        public void Execute(int i)
        {
            int x = i % size.x;
            int y = i / size.x;

            if (x == 0 || y == 0 || x == size.x - 1 || y == size.y - 1)
            {
                 if (borderRule == 0)
                 {
                    values[i] = true;
                 }
                else if (borderRule == 1)
                {
                    values[i] = false;
                }
             
                else
                {
                    int val = random.NextInt(0, 100);
                    values[i] = val < fillPercent ? true : false;
                }
            }
            else
            {
                int val = random.NextInt(0, 100);
                values[i] = val < fillPercent ? true : false;
            }
        }
    }

    [BurstCompatible]
    [BurstCompile]
    private struct SmoothNeighborsJob : IJob
    {
        public int2 size;
        public int borderRule;
        public NativeArray<bool> values;

        public int upperThreshold;
        public int lowerThreshold;

        public void Execute()
        {
            int resultsLength = size.x * size.y;
            for (int i = 0; i < resultsLength; i++)
            {
                int x = i % size.x;
                int y = i / size.x;

                if (x == 0 || y == 0 || x == size.x - 1 || y == size.y - 1)
                {
                    if (borderRule == 0)
                    {
                        values[i] = true;
                        continue;
                    }
                    else if (borderRule == 1)
                    {
                        values[i] = false;
                        continue;
                    }
                }

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
                        if (values[neighborIndex])
                        {
                            neighborCount++;
                        }

                    }
                }

                if (neighborCount > upperThreshold)
                {
                    values[i] = true;
                }
                if (neighborCount < lowerThreshold)
                {
                    values[i] = false;
                }

            }
        }

    }

    public bool [] RotateCellularAutomata (int2 size, bool [] input, int angle)
    {
  
        float cos = Mathf.Cos(angle * 0.0174532924f);
        float sin = Mathf.Sin(angle * 0.0174532924f);

        int newHeight = (int)math.round(math.abs(size.y * cos) + math.abs(size.x * sin)) + 1;
        int newWidth = (int)math.round(math.abs(size.x * cos) + math.abs(size.y * sin)) + 1;

        NativeArray<bool> nativeInput = new NativeArray<bool>(input.Length, Allocator.Persistent);
        nativeInput.CopyFrom(input);
        
        NativeArray<bool> nativeOutput = new NativeArray<bool>(input.Length, Allocator.Persistent);
      
        SimpleRotateJob simpleRotateJob = new SimpleRotateJob
        {
            input = nativeInput,
            output = nativeOutput,
            cs = math.cos(angle * -1 * 0.0174532924f),
            ss = math.sin(angle * -1 * 0.0174532924f),
            size = size,
            center = new float2( (float)(size.x / 2f), (float)(size.y / 2f))
        };

        JobHandle jobHandle = simpleRotateJob.Schedule(nativeInput.Length, 64);
        jobHandle.Complete();
        

        bool[] output = nativeOutput.ToArray();

        nativeInput.Dispose();
        nativeOutput.Dispose();

        return output;
    }

    [BurstCompatible]
    [BurstCompile]
    private struct ZeroJob : IJobParallelFor
    {
        public NativeArray<bool> input;
    
        public void Execute(int i)
        {
            input[i] = false;
        }
    }


    /*credits:
    https://gautamnagrawal.medium.com/rotating-image-by-any-angle-shear-transformation-using-only-numpy-d28d16eb5076
    https://datagenetics.com/blog/august32013/index.html
    */
    [BurstCompatible]
    [BurstCompile]
    public struct RotateJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public NativeArray<bool> input;
        [NativeDisableParallelForRestriction]
        public NativeArray<bool> output;

        public int2 size;
        //0 - 359 degrees
        public int angle;

        public void Execute(int i)
        {
            //Coordinates of pixel with respect to center of original image
            int y = size.y - 1 - (i / size.x) - (size.y / 2);
            int x = size.x - 1 - (i % size.x) - (size.x / 2);

            int newIndex = Shear(angle * 0.0174532924f, i, size);

            int newX = newIndex % x;
            int newY = newIndex / x;

            newX = (size.x / 2) - newX;
            newY = (size.x / 2) - newY;

            newIndex = (newY * size.x + newX);

            output[newIndex] = input[i];
        }

        //Returns the output index of a pixel given the input index, angle of rotation, and bounds of the image
        public int Shear(float angle, int index, int2 size)
        {
            /*
             |1  -tan(??/2) |  |1        0|  |1  -tan(??/2) | 
             |0      1     |  |sin(??)   1|  |0      1     |   
            */

            //Get the 2d location from the 1d array
            int x = index % size.x;
            int y = index / size.x;

            //Shear 1
            float tangent = math.tan(angle / 2);
            float newX = math.round(x - y * tangent);
            float newY = y;

            //Shear 2
            newY = math.round(newX * math.sin(angle) + newY);

            //Shear 3
            newX = math.round(newX - newY * tangent);

            //Get 1D array index from the x and y coordinates
            int newIndex = (int)newY * size.x + (int)newX;
            return newIndex;

        }
    }

    [BurstCompatible]
    [BurstCompile]
    public struct SimpleRotateJob : IJobParallelFor
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<bool> input;
        public NativeArray<bool> output;

        public int2 size;

        public float2 center;
        public float cs, ss;
        
        public void Execute(int i )
        {
            int y = i / size.x;
            int x = i % size.x;

            int rOrig = (int)(center.y + ((float)(y) - center.y) * cs - ((float)(x) - center.x) * ss);
            int cOrig = (int)(center.x + ((float)(y) - center.y) * ss + ((float)(x) - center.x) * cs);

            bool value = false;
            if (rOrig >= 0 && rOrig < size.y && cOrig >= 0 && cOrig < size.x)
            {
                value = input[rOrig * size.x + cOrig];
            }
            output[i] = value;
        }
    }



}
