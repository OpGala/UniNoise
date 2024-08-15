using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
    public static class PerlinFractalNoise
    {
        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
        public static float[] Generate2D(int width, int height, float scale, int seed, float lacunarity, float gain, int octaves)
        {
            var noiseMap = new NativeArray<float>(width * height, Allocator.TempJob);

            var noiseJob = new Generate2DPerlinFractalNoiseJob
            {
                Width = width,
                Height = height,
                Scale = scale,
                Lacunarity = lacunarity,
                Gain = gain,
                Octaves = octaves,
                NoiseMap = noiseMap,
                Permutation = GeneratePermutation(seed),
                Gradients = GenerateGradients(seed)
            };

            JobHandle jobHandle = noiseJob.Schedule(width * height, 64);
            jobHandle.Complete();

            float[] noiseArray = noiseMap.ToArray();
            noiseMap.Dispose();
            noiseJob.Permutation.Dispose();
            noiseJob.Gradients.Dispose();
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
        private static NativeArray<float2> GenerateGradients(int seed)
        {
            var random = new Random((uint)seed);
            var gradients = new NativeArray<float2>(512, Allocator.TempJob);

            for (int i = 0; i < 256; i++)
            {
                float angle = random.NextFloat(0, 2 * math.PI);
                gradients[i] = new float2(math.cos(angle), math.sin(angle));
            }

            for (int i = 0; i < 256; i++)
            {
                gradients[256 + i] = gradients[i];
            }

            return gradients;
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
        private struct Generate2DPerlinFractalNoiseJob : IJobParallelFor
        {
            [ReadOnly] public int Width;
            [ReadOnly] public int Height;
            [ReadOnly] public float Scale;
            [ReadOnly] public float Lacunarity;
            [ReadOnly] public float Gain;
            [ReadOnly] public int Octaves;
            [NativeDisableParallelForRestriction] public NativeArray<float> NoiseMap;
            [ReadOnly] public NativeArray<int> Permutation;
            [ReadOnly] public NativeArray<float2> Gradients;

            public void Execute(int index)
            {
                int x = index % Width;
                int y = index / Height;
                NoiseMap[index] = FractalPerlinNoise(new float2(x * Scale, y * Scale));
            }

            private float FractalPerlinNoise(float2 point)
            {
                float total = 0;
                float frequency = 1;
                float amplitude = 1;
                float maxValue = 0;

                for (int i = 0; i < Octaves; i++)
                {
                    total += PerlinNoise(point * frequency) * amplitude;
                    maxValue += amplitude;
                    amplitude *= Gain;
                    frequency *= Lacunarity;
                }

                return total / maxValue;
            }

            private float PerlinNoise(float2 point)
            {
                int x0 = (int)math.floor(point.x);
                int y0 = (int)math.floor(point.y);
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                float2 g00 = Gradients[Hash(x0, y0)];
                float2 g10 = Gradients[Hash(x1, y0)];
                float2 g01 = Gradients[Hash(x0, y1)];
                float2 g11 = Gradients[Hash(x1, y1)];

                float2 p00 = new float2(x0, y0);
                float2 p10 = new float2(x1, y0);
                float2 p01 = new float2(x0, y1);
                float2 p11 = new float2(x1, y1);

                float2 f00 = point - p00;
                float2 f10 = point - p10;
                float2 f01 = point - p01;
                float2 f11 = point - p11;

                float n00 = math.dot(g00, f00);
                float n10 = math.dot(g10, f10);
                float n01 = math.dot(g01, f01);
                float n11 = math.dot(g11, f11);

                float sx = Fade(f00.x);
                float sy = Fade(f00.y);

                return math.lerp(math.lerp(n00, n10, sx), math.lerp(n01, n11, sx), sy);
            }

            private static float Fade(float t)
            {
                return t * t * t * (t * (t * 6 - 15) + 10);
            }

            private int Hash(int x, int y)
            {
                return Permutation[(x + Permutation[y & 255]) & 255];
            }
        }
    }
}
