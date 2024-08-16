using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
      public static class PerlinNoise
      {
            [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
            public static float[] Generate2D(int width, int height, float scale, int seed, float2 offset, int octaves, float persistence, float lacunarity)
            {
                  var noiseMap = new NativeArray<float>(width * height, Allocator.TempJob);

                  var noiseJob = new Generate2DNoiseJob
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
            private struct Generate2DNoiseJob : IJobParallelFor
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

                        float amplitude = 1;
                        float frequency = 1;
                        float noiseHeight = 0;
                        float maxValue = 0;

                        float baseXCoord = x / (float)Width;
                        float baseYCoord = y / (float)Height;

                        for (int i = 0; i < Octaves; i++)
                        {
                              float xCoord = math.mad(baseXCoord, Scale * frequency, Offset.x);
                              float yCoord = math.mad(baseYCoord, Scale * frequency, Offset.y);

                              float perlinValue = ImprovedPerlinNoise(xCoord, yCoord, (uint)Permutation[0]) * 2 - 1;
                              noiseHeight += perlinValue * amplitude;

                              maxValue += amplitude;

                              amplitude *= Persistence;
                              frequency *= Lacunarity;
                        }

                        NoiseMap[index] = (noiseHeight / maxValue + 1) / 2;
                  }

                  [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                  private static float ImprovedPerlinNoise(float x, float y, uint seed)
                  {
                        int xi = (int)math.floor(x);
                        int yi = (int)math.floor(y);
                        float xf = x - xi;
                        float yf = y - yi;

                        float u = Fade(xf);
                        float v = Fade(yf);

                        uint aa = XxHash32(math.asint(math.mad(xi, 1, seed)) + yi, seed);
                        uint ab = XxHash32(math.asint(math.mad(xi, 1, seed)) + yi + 1, seed);
                        uint ba = XxHash32(math.asint(math.mad(xi + 1, 1, seed)) + yi, seed);
                        uint bb = XxHash32(math.asint(math.mad(xi + 1, 1, seed)) + yi + 1, seed);

                        float x1 = math.lerp(Grad((int)aa, xf, yf), Grad((int)ba, xf - 1, yf), u);
                        float x2 = math.lerp(Grad((int)ab, xf, yf - 1), Grad((int)bb, xf - 1, yf - 1), u);

                        return math.lerp(x1, x2, v);
                  }

                  [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                  private static uint XxHash32(int input, uint seed)
                  {
                        const uint prime322 = 2_246_822_519U;
                        const uint prime323 = 3_266_489_917U;
                        const uint prime324 = 668_265_263U;
                        const uint prime325 = 374_761_393U;

                        uint h32 = seed + prime325;
                        h32 += 4U;

                        h32 += (uint)input * prime323;
                        h32 = ((h32 << 17) | (h32 >> 15)) * prime324;

                        h32 ^= h32 >> 15;
                        h32 *= prime322;
                        h32 ^= h32 >> 13;
                        h32 *= prime323;
                        h32 ^= h32 >> 16;

                        return h32;
                  }

                  [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                  private static float Fade(float t)
                  {
                        return t * t * t * (t * (t * 6 - 15) + 10);
                  }

                  [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                  private static float Grad(int hash, float x, float y)
                  {
                        int h = hash & 7;
                        float u = h < 4 ? x : y;
                        float v = h < 4 ? y : x;
                        return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -2.0f * v : 2.0f * v);
                  }
            }
      }
}