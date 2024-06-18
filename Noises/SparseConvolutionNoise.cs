using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
    public sealed class SparseConvolutionNoise : System.IDisposable
    {
        private readonly int _seed;
        private readonly float _scale;
        private readonly int _width;
        private readonly int _height;
        private readonly int _kernelSize;

        private NativeArray<float> _noiseValuesNative;
        private NativeArray<int> _permutationTable;
        private bool _arraysInitialized;

        public SparseConvolutionNoise(int seed, float scale, int width, int height, int kernelSize)
        {
            _seed = seed;
            _scale = scale;
            _width = width;
            _height = height;
            _kernelSize = kernelSize;
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
        private struct SparseConvolutionNoiseJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> PermutationTable;
            public NativeArray<float> NoiseValues;
            public float Scale;
            public int Width;
            public int KernelSize;

            public void Execute(int index)
            {
                int x = index % Width;
                int y = index / Width;
                var point = new float2(x * Scale, y * Scale);
                NoiseValues[index] = GenerateSparseConvolutionNoise(point, KernelSize);
            }

            private float GenerateSparseConvolutionNoise(float2 point, int kernelSize)
            {
                float sum = 0f;
                float weight = 0f;
                for (int i = -kernelSize; i <= kernelSize; i++)
                {
                    for (int j = -kernelSize; j <= kernelSize; j++)
                    {
                        var offset = new float2(i, j);
                        float distance = math.length(offset);
                        float influence = math.exp(-distance * distance);
                        sum += influence * Gradient(Hash(point + offset), offset.x, offset.y);
                        weight += influence;
                    }
                }
                return sum / weight;
            }

            private static float Gradient(int hash, float x, float y)
            {
                int h = hash & 7;
                float u = h < 4 ? x : y;
                float v = h < 4 ? y : x;
                return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
            }

            private int Hash(float2 point)
            {
                int xi = (int)math.floor(point.x) & 255;
                int yi = (int)math.floor(point.y) & 255;
                return PermutationTable[xi + PermutationTable[yi]];
            }
        }

        public float[] Generate()
        {
            var sparseConvolutionNoiseJob = new SparseConvolutionNoiseJob
            {
                PermutationTable = _permutationTable,
                NoiseValues = _noiseValuesNative,
                Scale = _scale,
                Width = _width,
                KernelSize = _kernelSize
            };

            JobHandle jobHandle = sparseConvolutionNoiseJob.Schedule(_width * _height, 64);
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

        ~SparseConvolutionNoise()
        {
            Dispose();
        }
    }
}
