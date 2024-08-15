using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
      public static class GradientNoise
      {
            [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
            public static float[] Generate2D(int width, int height, float scale, int seed, float amplitude, float frequency, float2 offset)
            {
                  var noiseMap = new NativeArray<float>(width * height, Allocator.TempJob);

                  var noiseJob = new Generate2DGradientNoiseJob
                  {
                              Width = width,
                              Height = height,
                              Scale = scale,
                              Amplitude = amplitude,
                              Frequency = frequency,
                              Offset = offset,
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
            private struct Generate2DGradientNoiseJob : IJobParallelFor
            {
                  [ReadOnly] public int Width;
                  [ReadOnly] public int Height;
                  [ReadOnly] public float Scale;
                  [ReadOnly] public float Amplitude;
                  [ReadOnly] public float Frequency;
                  [ReadOnly] public float2 Offset;
                  [NativeDisableParallelForRestriction] public NativeArray<float> NoiseMap;
                  [ReadOnly] public NativeArray<int> Permutation;

                  public void Execute(int index)
                  {
                        int x = index % Width;
                        int y = index / Height;
                        float2 point = (new float2(x, y) + Offset) * Scale * Frequency;
                        NoiseMap[index] = GenerateGradientNoise(point) * Amplitude;
                  }

                  private float GenerateGradientNoise(float2 point)
                  {
                        int x0 = (int)math.floor(point.x);
                        int x1 = x0 + 1;
                        int y0 = (int)math.floor(point.y);
                        int y1 = y0 + 1;

                        float sx = point.x - x0;
                        float sy = point.y - y0;

                        float n0 = Gradient(Hash(x0, y0), sx, sy);
                        float n1 = Gradient(Hash(x1, y0), sx - 1, sy);
                        float ix0 = math.lerp(n0, n1, sx);

                        float n2 = Gradient(Hash(x0, y1), sx, sy - 1);
                        float n3 = Gradient(Hash(x1, y1), sx - 1, sy - 1);
                        float ix1 = math.lerp(n2, n3, sx);

                        return math.lerp(ix0, ix1, sy);
                  }

                  private static float Gradient(int hash, float x, float y)
                  {
                        int h = hash & 7;
                        float u = h < 4 ? x : y;
                        float v = h < 4 ? y : x;
                        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
                  }

                  private int Hash(int x, int y)
                  {
                        return Permutation[(x & 255) + Permutation[y & 255]];
                  }
            }
      }
}