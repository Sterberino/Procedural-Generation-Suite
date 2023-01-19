using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

/*References: 
https://towardsdatascience.com/image-processing-and-pixel-manipulation-photo-filters-5d37a2f992fa
https://en.wikipedia.org/wiki/Bilateral_filter
*/
public class ImageFiltering
{
    public static Texture2D ApplyBilateralFilter(Texture2D originalTexture, int kernelSize, float spatialWeight, float intensityWeight)
    {
        int width = originalTexture.width;
        int height = originalTexture.height;

        NativeArray<Color32> originalTextureArray = originalTexture.GetRawTextureData<Color32>();
        NativeArray<Color32> newTextureArray = new NativeArray<Color32>(width * height, Allocator.Persistent);

        BilateralFilter BilateralFilterJob = new BilateralFilter()
        {
            height = height,
            width = width,
            kernelSize = kernelSize,
            originalTexture = originalTextureArray,
            newTexture = newTextureArray,
            
            spatialWeight = spatialWeight,
            intensityWeight = intensityWeight
        };

        JobHandle handle = BilateralFilterJob.Schedule(width * height, 32);
        handle.Complete();

        Texture2D tex = new Texture2D(width, height);
        NativeArray<Color32> texValues = tex.GetRawTextureData<Color32>();

        /*
        for(int i = 0; i < width * height; i ++)
        {
            Color32 c = newTextureArray[i];
            Debug.Log("R:" + c.r + ", G: " + c.g +", B: " +  c.b);
        }*/

        TextureSetJob textureSetJob = new TextureSetJob()
        {
            Texture = texValues,
            newValues = newTextureArray
        };

        JobHandle handle2 = textureSetJob.Schedule(width * height, 32);
        handle2.Complete();

        newTextureArray.Dispose();

        tex.filterMode = originalTexture.filterMode;
        tex.Apply();
        return tex;
    }

    //Returns the column and row of the original 2D array that the index i represents
    private static int2 GetXY(int i, int width)
    {
        return new int2(i % width, (int)(i / width));
    }

    [BurstCompile]
    [BurstCompatible]
    private struct TextureSetJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<Color32> Texture;
        
        public NativeArray<Color32> newValues;

        public void Execute(int i)
        {
            Texture[i] = newValues[i];
        }



    }

    [BurstCompatible]
    [BurstCompile(CompileSynchronously = true)]

    private struct BilateralFilter : IJobParallelFor
    {
       
        [NativeDisableParallelForRestriction]
        public NativeArray<Color32> originalTexture;
        [NativeDisableParallelForRestriction]
        public NativeArray<Color32> newTexture;

        public int kernelSize;
        public int width;
        public int height;

        public float spatialWeight;
        public float intensityWeight;

        public void Execute(int i)
        {
            //x and y index of our 2D array, corresponds to Wikipedia i, j: https://en.wikipedia.org/wiki/Bilateral_filter
            int2 xy = GetXY(i);

            float neighborWeightSum = 0;
            float Id = 0;

            //The xx and yy of our loops will correspond to k and l wikipedia formula values.
            //They are the positions of the pixels that are neighbors to xy (i index) 
            for(int xx = xy.x - kernelSize; xx <= xy.x + kernelSize; xx++)
            {
                for (int yy = xy.y - kernelSize; yy <= xy.y + kernelSize; yy++)
                {
                    //Get the index in our 1D array of the xx and yy positioned pixel
                    int neighborIndex = CalculateIndex(xx, yy);
                    //if outside of our texture width / height, continue
                    if(neighborIndex >= originalTexture.Length || neighborIndex < 0)
                    {
                        continue;
                    }

                    float SpacialGaussian = (((xy.x - xx) * (xy.x - xx)) + ((xy.y - yy) * (xy.y - yy)))
                        / (2 * spatialWeight * spatialWeight);

                    //Get difference in intensity between current pixel and neighbor
                    float IntensityGaussian = GetPixelIntensity(originalTexture[i]) - GetPixelIntensity(originalTexture[neighborIndex]);
                    //Square the difference in intensity between center pixel and neighbor
                    IntensityGaussian *= IntensityGaussian;
                    //Adjust using the intensity weighting value
                    IntensityGaussian /= (2 * intensityWeight * intensityWeight);

                    //Negative Spacial and Intensity Gaussian values
                    float exponent = 0 - SpacialGaussian - IntensityGaussian;

                    float neighborWeight;
                    if(exponent == 0)
                    {
                        neighborWeight = 0;   
                    }
                    else
                    {
                        //weight for this neighbor is exp(exponent)
                        neighborWeight = math.pow(2.71828182845f, exponent);
                    }

                    neighborWeightSum += neighborWeight;

                    //Sum (intensity of the current pixel * weight of each neighbor pixel) 
                    Id += (GetPixelIntensity(originalTexture[neighborIndex]) * neighborWeight);
                }
            }
            

            //Divide Id by the sum of all surrounding pixel weights
            Id /= neighborWeightSum;


            byte IdAsByte = (byte)(Id);
       
            //For now, just return the color as new Color32(intensity, intensity, intensity). (greyscale)
            //Later you can figure out if you want to scale the pixel by the intensity.
            newTexture[i] = new Color32(IdAsByte, IdAsByte, IdAsByte, originalTexture[i].a);
        }

        //Pass in one of the Color32 values from the Texture pixel array
        public float GetPixelIntensity(Color32 c)
        {
            return ((float)c.r + (float)c.g + (float)c.b) / 3;
        }

        public int CalculateIndex(int x, int y)
        {
            return (width * y + x);
        }

        //Returns the column and row of the original 2D array that the index i represents
        public int2 GetXY(int i)
        {
            return new int2(i % width, (int)(i/ width));
        }
    }

    public static float GetAverageImageBrightness(Texture2D texture)
    {
        NativeArray<Color32> textureData = texture.GetRawTextureData<Color32>();
        NativeArray<float> average = new NativeArray<float>(1, Allocator.Persistent);
        int length = texture.width * texture.height;

        CalculateAverageBrightnessJob brightnessJob = new CalculateAverageBrightnessJob
        {
            image = textureData,
            length = length,
            average = average
        };

        JobHandle handle = brightnessJob.Schedule();
        handle.Complete();

        float av = average[0];
        average.Dispose();

        return av;
    }

    [BurstCompatible]
    [BurstCompile]
    private struct CalculateAverageBrightnessJob : IJob
    {
        public NativeArray<Color32> image;
        public int length;
        //NativeArray of size 1 for extracting average info
        [NativeDisableParallelForRestriction]
        public NativeArray<float> average;

        public void Execute()
        {
            if(image.Length == 0)
            {
                average[0] = 0;
                return;
            }
        

            average[0] = GetPixelIntensity(image[0]);

            if(image.Length > 1)
            {
                for (int i = 1; i < length; i++)
                {
                    average[0] = (GetPixelIntensity(image[i]) + (float)(i - 1) * average[0]) / (float)(i);
                }
            }
           
        }

        public float GetPixelIntensity(Color32 c)
        {
            return ((float)c.r + (float)c.g + (float)c.b) / 3;
        }
    }

    /// <summary>
    /// Creates a texture that is a contrast adjusted copy of the originalTexture. contrastAmount (-255, 255) 
    /// </summary>
    public static Texture2D AdjustContrast(Texture2D originalTexture, float contrastAmount)
    {
        Texture2D tex = new Texture2D(originalTexture.width, originalTexture.height);

        contrastAmount = contrastAmount <= -255 ? -254.99f : contrastAmount;
        contrastAmount = contrastAmount >= 255 ? 254.99f : contrastAmount;

        float alpha = (255 + contrastAmount) / (255 - contrastAmount);
        float averageBrightness = GetAverageImageBrightness(originalTexture);

        NativeArray<Color32> result = tex.GetRawTextureData<Color32>();
        NativeArray<Color32> originalTextureColors = originalTexture.GetRawTextureData<Color32>();

        ContrastAdjustJob contrastAdjustJob = new ContrastAdjustJob
        { 
            alpha = alpha,
            averageBrightness = averageBrightness,
            originalTextureColors = originalTextureColors,
            result = result
        };

        JobHandle jobHandle = contrastAdjustJob.Schedule(originalTexture.width * originalTexture.height, 64);
        jobHandle.Complete();

        tex.filterMode = originalTexture.filterMode;
        tex.Apply();

        return tex;
    }

    [BurstCompatible]
    [BurstCompile]
    private struct ContrastAdjustJob : IJobParallelFor
    {
        public float averageBrightness;
        public float alpha;

        public NativeArray<Color32> originalTextureColors;
        public NativeArray<Color32> result;

        public void Execute(int i)
        {
            Color32 c = originalTextureColors[i];

            int r = (int)math.round(alpha * ((float)c.r - averageBrightness) + averageBrightness);
            r = math.clamp(r, 0, 255);
            int g = (int)math.round(alpha * ((float)c.g - averageBrightness) + averageBrightness);
            g = math.clamp(g, 0, 255);
            int b = (int)math.round(alpha * ((float)c.b - averageBrightness) + averageBrightness);
            b = math.clamp(b, 0, 255);

            result[i] = new Color32((byte)r, (byte)g, (byte)b, c.a);
        }
    }


    /// <summary>
    /// Adjusts the brightness of the provided image. Brightness adjustment should be passed as a small value (eg 0.1f).
    /// </summary>
    public static Texture2D AdjustBrightness(Texture2D originalImage, float brightnessAdjustment)
    {
        Texture2D tex = new Texture2D(originalImage.width, originalImage.height);

        NativeArray<Color32> originalColors = originalImage.GetRawTextureData<Color32>();
        NativeArray<Color32> results = new NativeArray<Color32>(originalImage.width * originalImage.height, Allocator.Persistent);
        BrightnessAdjustmentJob brightnessAdjustmentJob = new BrightnessAdjustmentJob
        {
            originalColors = originalColors,
            result = results,
            adjustmentValue = brightnessAdjustment
        };

        JobHandle jobHandle = brightnessAdjustmentJob.Schedule(originalImage.width * originalImage.height, 64);
        jobHandle.Complete();


        NativeArray<Color32> texValues = tex.GetRawTextureData<Color32>();
        TextureSetJob textureSetJob = new TextureSetJob()
        {
            Texture = texValues,
            newValues = results
        };

        JobHandle handle2 = textureSetJob.Schedule(originalImage.width * originalImage.height, 64);
        handle2.Complete();

        results.Dispose();

        tex.filterMode = originalImage.filterMode;
        tex.Apply();

        GetAverageImageBrightness(tex);

        return tex;
    }


    [BurstCompatible]
    [BurstCompile]
    private struct BrightnessAdjustmentJob : IJobParallelFor
    {
        public NativeArray<Color32> originalColors;
        public NativeArray<Color32> result;
        public float adjustmentValue;

        public void Execute(int i)
        {
            Color32 c = originalColors[i];

            Color32 cnew = ColorAdjustment(c, adjustmentValue);
          //  originalColors[i] = cnew;
            result[i] = cnew;

        }

        public Color32 ColorAdjustment(Color32 original, float adjustmentAmount)
        {
            return new Color32(
                (byte)(int)math.clamp((float)((original.r) + adjustmentValue), 0, 255), 
                (byte)(int)math.clamp((float)((original.g) + adjustmentValue), 0, 255), 
                (byte)(int)math.clamp((float)((original.b) + adjustmentValue), 0, 255), 
                original.a);
        }
    }

    /// <summary>
    /// Creates a new Texture2D that is a saturation adjusted version of the original. Saturation amount is clamped between (-255, 255).
    /// </summary>
    public static Texture2D AdjustSaturation(Texture2D originalTexture, float saturationAmount)
    {
        Texture2D tex = new Texture2D(originalTexture.width, originalTexture.height);

        saturationAmount = saturationAmount <= -255 ? -254.99f : saturationAmount;
        saturationAmount = saturationAmount >= 255 ? 254.99f : saturationAmount;

        float alpha = (255 + saturationAmount) / (255 - saturationAmount);
       
        NativeArray<Color32> result = tex.GetRawTextureData<Color32>();
        NativeArray<Color32> originalTextureColors = originalTexture.GetRawTextureData<Color32>();

        SaturationAdjustJob contrastAdjustJob = new SaturationAdjustJob
        {
            alpha = alpha,
            originalColors = originalTextureColors,
            result = result
        };

        JobHandle jobHandle = contrastAdjustJob.Schedule(originalTexture.width * originalTexture.height, 64);
        jobHandle.Complete();

        tex.filterMode = originalTexture.filterMode;
        tex.Apply();

        return tex;
    }

    [BurstCompatible]
    [BurstCompile]
    private struct SaturationAdjustJob : IJobParallelFor
    {
        public float alpha;
        public NativeArray<Color32> originalColors;
        public NativeArray<Color32> result;

        public void Execute(int i)
        {
            Color32 c = originalColors[i];
            float intensity = GetPixelIntensity(c);

            int r = (int)(alpha * (((float)c.r - intensity)) + intensity);
            r = (int)math.clamp(r, 0, 255);

            int g = (int)(alpha * (((float)c.g - intensity)) + intensity);
            g = (int)math.clamp(g, 0, 255);

            int b = (int)(alpha * (((float)c.b - intensity)) + intensity);
            b = (int)math.clamp(b, 0, 255);

            Color32 newC = new Color32((byte)r, (byte)g, (byte)b, c.a);
            result[i] = newC;
        }

        public float GetPixelIntensity(Color32 c)
        {
            return ((float)c.r + (float)c.g + (float)c.b) / 3;
        }
    }

    /// <summary>
    /// Creates a new Texture2D that is a gamma adjusted version of the original. Gamma amount is clamped between (0, 2).
    /// </summary>
    public static Texture2D AdjustGamma(Texture2D originalTexture, float gammaAmount)
    {
        Texture2D tex = new Texture2D(originalTexture.width, originalTexture.height);

        gammaAmount = gammaAmount < 0 ? 0 : gammaAmount;
        gammaAmount = gammaAmount > 2 ? 2 : gammaAmount;

        NativeArray<Color32> result = tex.GetRawTextureData<Color32>();
        NativeArray<Color32> originalTextureColors = originalTexture.GetRawTextureData<Color32>();

        GammaAdjustJob gammaAdjustJob = new GammaAdjustJob
        {
            gamma = gammaAmount,
            originalColors = originalTextureColors,
            result = result
        };

        JobHandle jobHandle = gammaAdjustJob.Schedule(originalTexture.width * originalTexture.height, 64);
        jobHandle.Complete();

        tex.filterMode = originalTexture.filterMode;
        tex.Apply();

        return tex;
    }


    private struct GammaAdjustJob : IJobParallelFor
    {
        public float gamma;
        public NativeArray<Color32> originalColors;
        public NativeArray<Color32> result;

        public void Execute(int i)
        {
            Color32 c = originalColors[i];
            
            int r = (int)math.pow(255 * ((float)c.r / 255f), gamma);
            r = (int)math.clamp(r, 0, 255);

            int g = (int)math.pow(255 * ((float)c.g / 255f), gamma);
            g = (int)math.clamp(g, 0, 255);

            int b = (int)math.pow(255 * ((float)c.b / 255f), gamma);
            b = (int)math.clamp(b, 0, 255);

            Color32 newC = new Color32((byte)r, (byte)g, (byte)b, c.a);
            result[i] = newC;
        }
    }
}
