using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.UI;

public class SimplexNoiseGenerator
{
    //public double[] lastResults; 

    private static int[] Perm = {151,160,137,91,90, 15,131,13,201,95,96,53, 194,
        233,7,225,140,36,103,30,69,142,8,99,37,240 ,21,10,23,190,6,148,247, 120,
        234,75,0,26,197,62,94,252,219,203, 117,35,11,32,57,177,33,88,237,149,56,
        87,174,20,125,136,171,168,68,175, 74, 165,71,134, 139,48,27,166,77, 146,
        158, 231,83,111, 229,122,60,211, 133,230,220,105,92,41,55,46,245,40,244,
        102,143,54,65,25,63,161,1, 216,80,73,209,76,132, 187,208,89,18,169, 200,
        196,135,130,116,188,159,86, 164,100,109,198,173,186,3,64,52,217,226,250,
        124,123,5,202,38,147,118,126, 255,82,85,212,207,206,59,227,47, 16,58,17,
        182,189,28,42,223, 183,170,213, 119,248,152,2,44,154,163,70,221,153,101,
        155,167,43,172,9,129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,
        104,218,246,97,228,251,34,242,193,238, 210,144,12,191,179,162,241,81,51,
        145,235,249,14,239,107,49,192,214,31,181,199,106,157,184,84,204,176,115,
        121,50,45,127,4,150,254,138,236,205, 93,222,114,67,29,24,72,243,141,128,
        195,78,66,215,61,156,180,151,160,137, 91,90,15,131,13,201,95, 96,53,194,
        233,7,225,140,36,103,30,69,142,8, 99,37, 240,21,10,23,190,6,148,247,120,
        234,75,0,26,197,62,94,252,219,203, 117,35,11,32,57,177,33,88,237,149,56,
        87,174,20,125,136,171,168,68,175,74,165,71,134,139,48,27,166,77,146,158,
        231,83,111,229, 122,60,211,133,230, 220,105,92,41,55,46, 245,40,244,102,
        143,54,65, 25,63,161, 1,216,80, 73,209,76,132,187,208,89,18,169,200,196,
        135,130,116, 188,159,86,164,100,109,198,173,186,3,64,52,217,226,250,124,
        123,5,202,38,147, 118,126,255, 82,85,212,207,206,59,227,47,16,58,17,182,
        189,28,42,223,183, 170,213,119,248,152,2,44, 154,163,70,221,153,101,155,
        167,43,172,9,129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,
        218,246,97,228,251,34,242,193,238, 210,144,12,191,179,162,241,81,51,145,
        235,249,14,239,107,49,192,214,31,181,199,106,157,184,84,204,176,115,121,
        50,45,127,4,150,254,138,236,205,93,222,114,67,29,24,72,243,141,128, 195,
        78,66,215,61,156,180 }; 

    private static int[] Grad3 = { 1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1, 0, 1, 0, 1, -1, 0, 1, 1, 0, -1, -1, 0, -1, 0, 1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1 };

    public static Texture2D GenerateSimplexTexture(int width, int height, int seed, double lacunarity, double persistance, double scale, Vector2 offset)
    {
        //float startTime = Time.realtimeSinceStartup;

        double2 positionOffset = new double2(offset.x, offset.y);

        NativeArray<int> jobGrad3 = new NativeArray<int>(Grad3.Length, Allocator.Persistent);
        jobGrad3.CopyFrom(Grad3);

        NativeArray<int> jobPerm = new NativeArray<int>(Perm.Length, Allocator.Persistent);
        jobPerm.CopyFrom(Perm);

        NativeArray<double> results = new NativeArray<double>(width * height, Allocator.Persistent);

        System.Random rand = new System.Random(seed);

        int maxLength = Mathf.Max(width, height);
        int octaves = (int)math.log2( Mathf.NextPowerOfTwo(maxLength));
        
        NativeArray<double2> octaveOffsets = new NativeArray<double2>(octaves, Allocator.Persistent);

        for (int i = 0; i < octaves; i++)
        {
            float x = rand.Next(-100000, 100000);
            float y = rand.Next(-100000, 100000);
            octaveOffsets[i] = new double2(x, y);
        }


        //Create and schedule the job
        SimplexJob simplexJob = new SimplexJob()
        {
            width = width,
            height = height,
            octaves = octaveOffsets,
            grad3 = jobGrad3,
            perm = jobPerm,
            result = results,
            persistance = persistance,
            lacunarity = lacunarity,
            scale = scale,
            offset = positionOffset
        };
        JobHandle jobHandle = simplexJob.Schedule(width * height, 64);
        jobHandle.Complete();

        /*
        startTime = Time.realtimeSinceStartup - startTime;
        Debug.Log("Simplex Job runtime: " + startTime);
        startTime = Time.realtimeSinceStartup;
        */

        NativeArray<double> maxValueResult = new NativeArray<double>(1, Allocator.Persistent);
        maxValueResult[0] = double.MinValue;
        NativeArray<double> minValueResult = new NativeArray<double>(1, Allocator.Persistent);
        minValueResult[0] = double.MaxValue;

        SimplexMinMax minMaxJob = new SimplexMinMax()
        {
            unsmoothedValues = simplexJob.result,
            MaxOutput = maxValueResult,
            MinOutput = minValueResult
        };
        JobHandle minMaxJobHandle = minMaxJob.Schedule(width * height, 64);
        minMaxJobHandle.Complete();

        /*
        startTime = Time.realtimeSinceStartup - startTime;
        Debug.Log("Min-Max Job runtime: " + startTime);
        startTime = Time.realtimeSinceStartup;
        */

        NativeArray<double> smootherResults = new NativeArray<double>(width * height, Allocator.Persistent);
        SimplexSmoother smootherJob = new SimplexSmoother()
        {
            unsmoothedValues = simplexJob.result,
            results = smootherResults,
            minNoiseHeight = minMaxJob.MinOutput[0],
            maxNoiseHeight = minMaxJob.MaxOutput[0]
        };
        JobHandle smootherJobHandle = smootherJob.Schedule(width * height, 64);
        smootherJobHandle.Complete();

        /*
        startTime = Time.realtimeSinceStartup - startTime;
        Debug.Log("Smoother Job runtime: " + startTime);
        startTime = Time.realtimeSinceStartup;
        */

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        NativeArray<Color32> colors = tex.GetRawTextureData<Color32>();
        TextureSetJob texSetJob = new TextureSetJob()
        {
            Texture = colors,
            values = smootherJob.results
        };

        JobHandle texSetJobHandle = texSetJob.Schedule(width * height, 64);
        texSetJobHandle.Complete();

        /*
        startTime = Time.realtimeSinceStartup - startTime;
        Debug.Log("Texture set job runtime: " + startTime);
        startTime = Time.realtimeSinceStartup;
        */

        tex.filterMode = FilterMode.Point;
        tex.Apply();

        smootherResults.Dispose();
        jobGrad3.Dispose();
        jobPerm.Dispose();
        octaveOffsets.Dispose();
        results.Dispose();
        maxValueResult.Dispose();
        minValueResult.Dispose();


        return tex;
    }

    /// <summary>
    /// Generates a Simplex noise texture. Parameter persistance must be between (0,1].
    /// </summary>
    public static Texture2D GenerateSimplexTexture(int width, int height, int seed, double lacunarity, double persistance, double scale)
    {
        Vector2 offset = Vector2.zero;
        return GenerateSimplexTexture(width, height, seed, lacunarity, persistance, scale, offset);
    }

    [BurstCompile]
    [BurstCompatible]
    private struct TextureSetJob : IJobParallelFor
    {
        [WriteOnly] 
        public NativeArray<Color32> Texture;
        public NativeArray<double> values;

        public void Execute(int i)
        {
            Texture[i] = ColorFromNoiseValue(values[i]);
        }

        public Color32 ColorFromNoiseValue(double noiseValue)
        {
            return new Color32((byte)(noiseValue * 255), (byte)(noiseValue * 255), (byte)(noiseValue * 255), 255);
        }

    }


    [BurstCompatible]
    [BurstCompile]
    private struct SimplexJob : IJobParallelFor
    {
        public int width;
        public int height;
        public double persistance;
        public double scale;
        public double lacunarity;
        public double2 offset;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<double2> octaves;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> grad3;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<int> perm;
        public NativeArray<double> result;

        public void Execute(int i)
        {
            int x = i % width;
            int y = i / width;

            double amplitude = 1d;
            double frequency = 1d;
            double simplexResult = 0;

            for (int j = 0; j < octaves.Length; j++)
            {
                double sampleX = (x + offset.x - width / 2d) / scale * frequency + octaves[j].x;
                double sampleY = (y + offset.y - height / 2d) / scale * frequency + octaves[j].y;

                simplexResult += (Noise(sampleX, sampleY) * (double)amplitude);
                       
                frequency *= lacunarity;
                amplitude *= persistance;
            }
            result[i] = simplexResult;
        }

        // 2D simplex noise
        public  double Noise(double xin, double yin)
        {
            double n0, n1, n2; // Noise contributions from the three corners
                               // Skew the input space to determine which simplex cell we're in
            double F2 = 0.5 * (math.sqrt(3.0) - 1.0);
            double s = (xin + yin) * F2; // Hairy factor for 2D
            int i0 = fastfloor(xin + s);
            int j0 = fastfloor(yin + s);
            
            double G2 = (3.0 - math.sqrt(3.0)) / 6.0;
            double t = (i0 + j0) * G2;
            double X0 = i0 - t; // Unskew the cell origin back to (x,y) space
            double Y0 = j0 - t;
            double x0 = xin - X0; // The x,y distances from the cell origin
            double y0 = yin - Y0;
            
            // For the 2D case, the simplex shape is an equilateral triangle.
            // Determine which simplex we are in.
            int i1, j1; // Offsets for second (middle) corner of simplex in (i,j) coords
            if (x0 > y0) { i1 = 1; j1 = 0; } // lower triangle, XY order: (0,0)->(1,0)->(1,1)
            else { i1 = 0; j1 = 1; } // upper triangle, YX order: (0,0)->(0,1)->(1,1)
            
            // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
            // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
            // c = (3-sqrt(3))/6
            
            double x1 = x0 - i1 + G2; // Offsets for middle corner in (x,y) unskewed coords
            double y1 = y0 - j1 + G2;
            double x2 = x0 - 1.0 + 2.0 * G2; // Offsets for last corner in (x,y) unskewed coords
            double y2 = y0 - 1.0 + 2.0 * G2;
            
            // Work out the hashed gradient indices of the three simplex corners
            int ii = i0 & 255;
            int jj = j0 & 255;
            int gi0 = perm[ii + perm[jj]] % 12;
            int gi1 = perm[ii + i1 + perm[jj + j1]] % 12;
            int gi2 = perm[ii + 1 + perm[jj + 1]] % 12;
            
            // Calculate the contribution from the three corners
            double t0 = 0.5 - x0 * x0 - y0 * y0;
            if (t0 < 0) n0 = 0.0;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(gi0, x0, y0); // (x,y) of grad3 used for 2D gradient
            }
            double t1 = 0.5 - x1 * x1 - y1 * y1;
            if (t1 < 0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot(gi1, x1, y1);
            }
            double t2 = 0.5 - x2 * x2 - y2 * y2;
            if (t2 < 0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(gi2, x2, y2);
            }
            // Add contributions from each corner to get the final noise value.
            // The result is scaled to return values in the interval [-1,1].
            return 70.0 * (n0 + n1 + n2);
        }

        public int Grad3(int row, int col)
        {
            return grad3[3 * row + col];
        }

        public int fastfloor(double x)
        {
            return x > 0 ? (int)x : (int)x - 1;
        }

        public double Dot(int gradRow, double x, double y)
        {
            return (Grad3(gradRow, 0) * x + (Grad3(gradRow, 1)*y));
        }
    }

    /// <summary>
    /// Finds the minimum and maximum values of the unsmoothed simplex texture
    /// </summary>
    [BurstCompatible]
    [BurstCompile]
    private struct SimplexMinMax : IJobParallelFor{
        public NativeArray<double> unsmoothedValues;

        [NativeDisableParallelForRestriction]
        public NativeArray<double> MinOutput;
        [NativeDisableParallelForRestriction]
        public NativeArray<double> MaxOutput;

        public void Execute(int i)
        {
            double simplexResult = unsmoothedValues[i];

            if (simplexResult > MaxOutput[0])
            {
                MaxOutput[0] = simplexResult;
            }
            if (simplexResult < MinOutput[0])
            {
                MinOutput[0] = simplexResult;
            }
        }
                    
    }

    [BurstCompatible]
    [BurstCompile]
    public struct SimplexSmoother : IJobParallelFor
    {

        public NativeArray<double> unsmoothedValues;
        public NativeArray<double> results;
        public double maxNoiseHeight;
        public double minNoiseHeight;

        public void Execute(int i)
        {
            results[i] = math.unlerp((double)minNoiseHeight, (double)maxNoiseHeight, unsmoothedValues[i]);
        }
    }
}
