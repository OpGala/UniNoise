using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
        public static class PerlinNoise
        {
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

                private static NativeArray<int> GeneratePermutation(int seed)
                {
                        var random = new Random((uint)seed);
                        var permutation = new NativeArray<int>(512, Allocator.TempJob);
                        for (int i = 0; i < 256; i++)
                        {
                                permutation[i] = i;
                        }

                        for (int i = 0; i < 256; i++)
                        {
                                int j = random.NextInt(256);
                                (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
                        }

                        for (int i = 0; i < 256; i++)
                        {
                                permutation[256 + i] = permutation[i];
                        }

                        return permutation;
                }

                [BurstCompile]
                private struct Generate2DNoiseJob : IJobParallelFor
                {
                        public int Width;
                        public int Height;
                        public float Scale;
                        public float2 Offset;
                        public int Octaves;
                        public float Persistence;
                        public float Lacunarity;
                        public NativeArray<float> NoiseMap;
                        [ReadOnly] public NativeArray<int> Permutation;

                        public void Execute(int index)
                        {
                                int x = index % Width;
                                int y = index / Width;

                                float amplitude = 1;
                                float frequency = 1;
                                float noiseHeight = 0;
                                float maxValue = 0; // Normalization factor

                                for (int i = 0; i < Octaves; i++)
                                {
                                        float xCoord = (x / (float)Width) * Scale * frequency + Offset.x;
                                        float yCoord = (y / (float)Height) * Scale * frequency + Offset.y;

                                        float perlinValue = ImprovedPerlinNoise(xCoord, yCoord, Permutation) * 2 - 1;
                                        noiseHeight += perlinValue * amplitude;

                                        maxValue += amplitude;

                                        amplitude *= Persistence;
                                        frequency *= Lacunarity;
                                }

                                // Normalize the result
                                noiseHeight = (noiseHeight / maxValue + 1) / 2;
                                NoiseMap[index] = noiseHeight;
                        }

                        private static float ImprovedPerlinNoise(float x, float y, NativeArray<int> p)
                        {
                                int xx = (int)math.floor(x) & 255;
                                int yy = (int)math.floor(y) & 255;

                                x -= math.floor(x);
                                y -= math.floor(y);

                                float u = Fade(x);
                                float v = Fade(y);

                                int a = p[xx] + yy;
                                int aa = p[a];
                                int ab = p[a + 1];
                                int b = p[xx + 1] + yy;
                                int ba = p[b];
                                int bb = p[b + 1];

                                return Lerp(v, Lerp(u, Grad(p[aa], x, y), Grad(p[ba], x - 1, y)), Lerp(u, Grad(p[ab], x, y - 1), Grad(p[bb], x - 1, y - 1)));
                        }

                        private static float Fade(float t)
                        {
                                return t * t * t * (t * (t * 6 - 15) + 10);
                        }

                        private static float Lerp(float t, float a, float b)
                        {
                                return math.lerp(a, b, t);
                        }

                        private static float Grad(int hash, float x, float y)
                        {
                                int h = hash & 15;
                                float u = h < 8 ? x : y;
                                float v = h < 4 ? y : 0;
                                return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
                        }
                }
        }
}