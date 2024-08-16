using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
      public static class GaborNoise
      {
            [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
            public static float[] Generate2D(int width, int height, float scale, int seed, float frequency, float orientation, float aspectRatio, float phase, float amplitude)
            {
                  var noiseMap = new NativeArray<float>(width * height, Allocator.TempJob);

                  var noiseJob = new Generate2DGaborNoiseJob
                  {
                              Width = width,
                              Height = height,
                              Scale = scale,
                              Frequency = frequency,
                              Orientation = orientation,
                              AspectRatio = aspectRatio,
                              Phase = phase,
                              Amplitude = amplitude,
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
            private struct Generate2DGaborNoiseJob : IJobParallelFor
            {
                  [ReadOnly] public int Width;
                  [ReadOnly] public int Height;
                  [ReadOnly] public float Scale;
                  [ReadOnly] public float Frequency;
                  [ReadOnly] public float Orientation;
                  [ReadOnly] public float AspectRatio;
                  [ReadOnly] public float Phase;
                  [ReadOnly] public float Amplitude;
                  [NativeDisableParallelForRestriction] public NativeArray<float> NoiseMap;
                  [ReadOnly] public NativeArray<int> Permutation;

                  public void Execute(int index)
                  {
                        int x = index % Width;
                        int y = index / Width;

                        var point = new float2(x * Scale, y * Scale);
                        NoiseMap[index] = GenerateGaborNoise(point);
                  }

                  private float GenerateGaborNoise(float2 point)
                  {
                        float angle = Orientation * math.PI / 180f;
                        var rotationMatrix = new float2x2(math.cos(angle), -math.sin(angle), math.sin(angle), math.cos(angle));
                        point = math.mul(rotationMatrix, point);
                        var gaborPoint = new float2(point.x * AspectRatio, point.y);

                        float wave = math.sin(2f * math.PI * Frequency * gaborPoint.x + Phase);
                        float gaussian = math.exp(-0.5f * math.dot(gaborPoint, gaborPoint));

                        return Amplitude * wave * gaussian;
                  }
            }
      }
}