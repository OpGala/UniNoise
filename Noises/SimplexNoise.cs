using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
    public sealed class SimplexNoise : System.IDisposable
    {
        private readonly int _seed;
        private readonly float _scale;
        private readonly int _width;
        private readonly int _height;

        private NativeArray<float> _noiseValuesNative;
        private NativeArray<int> _permutationTable;
        private bool _arraysInitialized;

        public SimplexNoise(int seed, float scale, int width, int height)
        {
            _seed = seed;
            _scale = scale;
            _width = width;
            _height = height;
            InitializeArrays();
            GeneratePermutationTable();
        }

        private void InitializeArrays()
        {
            switch (_arraysInitialized)
            {
                case true when _noiseValuesNative.Length == _width * _height:
                    return;
                case true:
                    _noiseValuesNative.Dispose();
                    _permutationTable.Dispose();
                    break;
            }

            _noiseValuesNative = new NativeArray<float>(_width * _height, Allocator.Persistent);
            _permutationTable = new NativeArray<int>(512, Allocator.Persistent);
            _arraysInitialized = true;
        }

        private void GeneratePermutationTable()
        {
            uint uSeed = (uint)_seed;
            var random = new Random(uSeed);
            var p = new NativeArray<int>(256, Allocator.Temp);
            for (int i = 0; i < 256; i++)
            {
                p[i] = i;
            }

            for (int i = 255; i > 0; i--)
            {
                int j = random.NextInt(0, i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }

            for (int i = 0; i < 512; i++)
            {
                _permutationTable[i] = p[i & 255];
            }

            p.Dispose();
        }

        [BurstCompile]
        private struct SimplexNoiseJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> PermutationTable;
            public NativeArray<float> NoiseValues;
            public float Scale;
            public int Width;

            public void Execute(int index)
            {
                int x = index % Width;
                int y = index / Width;
                var point = new float2(x * Scale, y * Scale);
                NoiseValues[index] = GenerateSimplexNoise(point);
            }

            private float GenerateSimplexNoise(float2 point)
            {
                float f2 = 0.5f * (math.sqrt(3.0f) - 1.0f);
                float g2 = (3.0f - math.sqrt(3.0f)) / 6.0f;

                float s = (point.x + point.y) * f2;
                int i = (int)math.floor(point.x + s);
                int j = (int)math.floor(point.y + s);

                float t = (i + j) * g2;
                var origin = new float2(i - t, j - t);
                float2 offset = point - origin;

                int i1, j1;
                if (offset.x > offset.y)
                {
                    i1 = 1;
                    j1 = 0;
                }
                else
                {
                    i1 = 0;
                    j1 = 1;
                }

                float2 v1 = offset - new float2(i1, j1) + g2;
                float2 v2 = offset - new float2(1.0f, 1.0f) + 2.0f * g2;

                int ii = i & 255;
                int jj = j & 255;

                float t0 = 0.5f - math.dot(offset, offset);
                float n0 = (t0 < 0.0f) ? 0.0f : math.pow(t0, 4) * Grad(PermutationTable[ii + PermutationTable[jj]], offset);

                float t1 = 0.5f - math.dot(v1, v1);
                float n1 = (t1 < 0.0f) ? 0.0f : math.pow(t1, 4) * Grad(PermutationTable[ii + i1 + PermutationTable[jj + j1]], v1);

                float t2 = 0.5f - math.dot(v2, v2);
                float n2 = (t2 < 0.0f) ? 0.0f : math.pow(t2, 4) * Grad(PermutationTable[ii + 1 + PermutationTable[jj + 1]], v2);

                return 70.0f * (n0 + n1 + n2);
            }

            private static float Grad(int hash, float2 pos)
            {
                int h = hash & 7;
                var grad = new float2(h < 4 ? 1.0f : 0.0f, h < 4 ? 0.0f : 1.0f);
                if ((h & 1) != 0) grad.x = -grad.x;
                if ((h & 2) != 0) grad.y = -grad.y;
                return math.dot(grad, pos);
            }
        }

        public float[] Generate()
        {
            var simplexNoiseJob = new SimplexNoiseJob
            {
                PermutationTable = _permutationTable,
                NoiseValues = _noiseValuesNative,
                Scale = _scale,
                Width = _width
            };

            JobHandle jobHandle = simplexNoiseJob.Schedule(_width * _height, 64);
            jobHandle.Complete();

            float[] result = new float[_width * _height];
            _noiseValuesNative.CopyTo(result);

            return result;
        }

        public void Dispose()
        {
            if (!_arraysInitialized) return;
            _noiseValuesNative.Dispose();
            _permutationTable.Dispose();
            _arraysInitialized = false;
        }

        ~SimplexNoise()
        {
            Dispose();
        }
    }
}
