using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
    public static class SimplexFractalNoise
    {
        private static readonly float3[] Grad3;

        static SimplexFractalNoise()
        {
            Grad3 = new[]
            {
                new float3(1, 1, 0), new float3(-1, 1, 0), new float3(1, -1, 0),
                new float3(-1, -1, 0), new float3(1, 0, 1), new float3(-1, 0, 1),
                new float3(1, 0, -1), new float3(-1, 0, -1), new float3(0, 1, 1),
                new float3(0, -1, 1), new float3(0, 1, -1), new float3(0, -1, -1)
            };
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
        public static float[] Generate2D(int width, int height, float scale, int seed, int octaves, float lacunarity, float persistence, float amplitude, float frequency)
        {
            var noiseMap = new NativeArray<float>(width * height, Allocator.TempJob);

            var noiseJob = new Generate2DSimplexFractalNoiseJob
            {
                Width = width,
                Height = height,
                Scale = scale,
                Octaves = octaves,
                Lacunarity = lacunarity,
                Persistence = persistence,
                Amplitude = amplitude,
                Frequency = frequency,
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
        private struct Generate2DSimplexFractalNoiseJob : IJobParallelFor
        {
            [ReadOnly] public int Width;
            [ReadOnly] public int Height;
            [ReadOnly] public float Scale;
            [ReadOnly] public int Octaves;
            [ReadOnly] public float Lacunarity;
            [ReadOnly] public float Persistence;
            [ReadOnly] public float Amplitude;
            [ReadOnly] public float Frequency;
            [NativeDisableParallelForRestriction] public NativeArray<float> NoiseMap;
            [ReadOnly] public NativeArray<int> Permutation;

            private static readonly float F2 = 0.5f * (math.sqrt(3.0f) - 1.0f);
            private static readonly float G2 = (3.0f - math.sqrt(3.0f)) / 6.0f;

            public void Execute(int index)
            {
                int x = index % Width;
                int y = index / Height;
                var point = new float2(x * Scale, y * Scale);
                NoiseMap[index] = GenerateFractalSimplexNoise(point);
            }

            private float GenerateFractalSimplexNoise(float2 point)
            {
                float total = 0;
                float amplitude = Amplitude;
                float frequency = Frequency;
                float maxValue = 0;

                for (int i = 0; i < Octaves; i++)
                {
                    total += GenerateSimplexNoise(point * frequency) * amplitude;
                    maxValue += amplitude;
                    amplitude *= Persistence;
                    frequency *= Lacunarity;
                }

                return total / maxValue;
            }

            private float GenerateSimplexNoise(float2 point)
            {
                float s = (point.x + point.y) * F2;
                int i = (int)math.floor(point.x + s);
                int j = (int)math.floor(point.y + s);

                float t = (i + j) * G2;
                float2 offset = point - new float2(i - t, j - t);

                int i1 = offset.x > offset.y ? 1 : 0;
                int j1 = offset.x > offset.y ? 0 : 1;

                float2 v1 = offset - new float2(i1, j1) + G2;
                float2 v2 = offset - 1.0f + 2.0f * G2;

                int ii = i & 255;
                int jj = j & 255;

                float t0 = 0.5f - math.dot(offset, offset);
                float n0 = t0 < 0 ? 0.0f : math.pow(t0, 4) * math.dot(Grad3[Permutation[ii + Permutation[jj]] % 12], new float3(offset, 0));

                float t1 = 0.5f - math.dot(v1, v1);
                float n1 = t1 < 0 ? 0.0f : math.pow(t1, 4) * math.dot(Grad3[Permutation[ii + i1 + Permutation[jj + j1]] % 12], new float3(v1, 0));

                float t2 = 0.5f - math.dot(v2, v2);
                float n2 = t2 < 0 ? 0.0f : math.pow(t2, 4) * math.dot(Grad3[Permutation[ii + 1 + Permutation[jj + 1]] % 12], new float3(v2, 0));

                return 70.0f * (n0 + n1 + n2);
            }
        }
    }
}
