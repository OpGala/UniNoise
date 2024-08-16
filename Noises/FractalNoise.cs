using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
      public static class FractalNoise
      {
            [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
            public static float[] Generate2D(int width, int height, float scale, int seed, float2 offset, int octaves, float persistence, float lacunarity)
            {
                  var noiseMap = new NativeArray<float>(width * height, Allocator.TempJob);

                  var noiseJob = new Generate2DFractalNoiseJob
                  {
                              Width = width,
                              Height = height,
                              Scale = scale,
                              Offset = offset,
                              Octaves = octaves,
                              Persistence = persistence,
                              Lacunarity = lacunarity,
                              NoiseMap = noiseMap,
                              Permutation = GeneratePermutation(seed)
                  };

                  JobHandle jobHandle = noiseJob.Schedule(width * height, 64);
                  jobHandle.Complete();

                  float[] noiseArray = noiseMap.ToArray();
                  noiseMap.Dispose();
                  noiseJob.Permutation.Dispose();
                  return noiseArray;
            }

            [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
            private static NativeArray<int> GeneratePermutation(int seed)
            {
                  var random = new Random((uint)seed);
                  var permutation = new NativeArray<int>(512, Allocator.TempJob);

                  // Initialiser la permutation avec Fisher-Yates
                  for (int i = 0; i < 256; i++)
                  {
                        permutation[i] = i;
                  }

                  for (int i = 255; i > 0; i--)
                  {
                        int j = random.NextInt(i + 1);
                        (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
                  }

                  for (int i = 0; i < 256; i++)
                  {
                        permutation[256 + i] = permutation[i];
                  }

                  return permutation;
            }

            [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
            private struct Generate2DFractalNoiseJob : IJobParallelFor
            {
                  [ReadOnly] public int Width;
                  [ReadOnly] public int Height;
                  [ReadOnly] public float Scale;
                  [ReadOnly] public float2 Offset;
                  [ReadOnly] public int Octaves;
                  [ReadOnly] public float Persistence;
                  [ReadOnly] public float Lacunarity;
                  [NativeDisableParallelForRestriction] public NativeArray<float> NoiseMap;
                  [ReadOnly] public NativeArray<int> Permutation;

                  public void Execute(int index)
                  {
                        int x = index % Width;
                        int y = index / Width;

                        float amplitude = 1f;
                        float frequency = 1f;
                        float noiseHeight = 0f;
                        float maxValue = 0f;

                        float baseXCoord = x / (float)Width;
                        float baseYCoord = y / (float)Height;

                        for (int i = 0; i < Octaves; i++)
                        {
                              float xCoord = math.mad(baseXCoord, Scale * frequency, Offset.x);
                              float yCoord = math.mad(baseYCoord, Scale * frequency, Offset.y);

                              float perlinValue = GeneratePerlinNoise(xCoord, yCoord) * 2 - 1;
                              noiseHeight += perlinValue * amplitude;

                              maxValue += amplitude;

                              amplitude *= Persistence;
                              frequency *= Lacunarity;
                        }

                        NoiseMap[index] = math.saturate((noiseHeight / maxValue + 1) / 2);
                  }

                  [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                  private float GeneratePerlinNoise(float x, float y)
                  {
                        int xi = (int)math.floor(x) & 255;
                        int yi = (int)math.floor(y) & 255;
                        float xf = x - math.floor(x);
                        float yf = y - math.floor(y);

                        float u = Fade(xf);
                        float v = Fade(yf);

                        int aa = Permutation[Permutation[xi] + yi];
                        int ab = Permutation[Permutation[xi] + yi + 1];
                        int ba = Permutation[Permutation[xi + 1] + yi];
                        int bb = Permutation[Permutation[xi + 1] + yi + 1];

                        float x1 = math.lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
                        float x2 = math.lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

                        return math.lerp(x1, x2, v);
                  }

                  [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                  private static float Fade(float t)
                  {
                        return t * t * t * (t * (t * 6f - 15f) + 10f);
                  }

                  [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                  private static float Grad(int hash, float x, float y)
                  {
                        int h = hash & 7;
                        float u = h < 4 ? x : y;
                        float v = h < 4 ? y : x;
                        return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -2f * v : 2f * v);
                  }
            }
      }
}