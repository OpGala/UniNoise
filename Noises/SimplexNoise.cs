using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
    public static class SimplexNoise
    {
        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
        public static float[] Generate2D(int width, int height, float scale, int seed)
        {
            var noiseMap = new NativeArray<float>(width * height, Allocator.TempJob);

            var noiseJob = new Generate2DSimplexNoiseJob
            {
                Width = width,
                Height = height,
                Scale = scale,
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
        private struct Generate2DSimplexNoiseJob : IJobParallelFor
        {
            [ReadOnly] public int Width;
            [ReadOnly] public int Height;
            [ReadOnly] public float Scale;
            [NativeDisableParallelForRestriction] public NativeArray<float> NoiseMap;
            [ReadOnly] public NativeArray<int> Permutation;

            public void Execute(int index)
            {
                int x = index % Width;
                int y = index / Height;
                var point = new float2(x * Scale, y * Scale);
                NoiseMap[index] = GenerateSimplexNoise(point);
            }

            private float GenerateSimplexNoise(float2 point)
            {
                float f2 = 0.5f * (math.sqrt(3.0f) - 1.0f);
                float g2 = (3.0f - math.sqrt(3.0f)) / 6.0f;

                float s = (point.x + point.y) * f2;
                int i = (int)math.floor(point.x + s);
                int j = (int)math.floor(point.y + s);

                float t = (i + j) * g2;
                float2 origin = new float2(i - t, j - t);
                float2 offset = point - origin;

                int i1 = offset.x > offset.y ? 1 : 0;
                int j1 = offset.x > offset.y ? 0 : 1;

                float2 v1 = offset - new float2(i1, j1) + g2;
                float2 v2 = offset - 1.0f + 2.0f * g2;

                int ii = i & 255;
                int jj = j & 255;

                float t0 = 0.5f - math.dot(offset, offset);
                float n0 = t0 < 0 ? 0 : math.pow(t0, 4) * Grad(Permutation[ii + Permutation[jj]], offset);

                float t1 = 0.5f - math.dot(v1, v1);
                float n1 = t1 < 0 ? 0 : math.pow(t1, 4) * Grad(Permutation[ii + i1 + Permutation[jj + j1]], v1);

                float t2 = 0.5f - math.dot(v2, v2);
                float n2 = t2 < 0 ? 0 : math.pow(t2, 4) * Grad(Permutation[ii + 1 + Permutation[jj + 1]], v2);

                return 70.0f * (n0 + n1 + n2);
            }

            private static float Grad(int hash, float2 pos)
            {
                int h = hash & 7;
                float2 grad = new float2(h < 4 ? 1.0f : 0.0f, h < 4 ? 0.0f : 1.0f);
                grad.x = (h & 1) != 0 ? -grad.x : grad.x;
                grad.y = (h & 2) != 0 ? -grad.y : grad.y;
                return math.dot(grad, pos);
            }
        }
    }
}
