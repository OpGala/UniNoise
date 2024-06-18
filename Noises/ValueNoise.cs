using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
    public sealed class ValueNoise : System.IDisposable
    {
        private readonly int _seed;
        private readonly float _scale;
        private readonly int _width;
        private readonly int _height;
        private readonly int _octaves;
        private readonly float _lacunarity;
        private readonly float _persistence;

        private NativeArray<float> _noiseValuesNative;
        private NativeArray<float> _valueTable;
        private bool _arraysInitialized;

        public ValueNoise(int seed, float scale, int width, int height, int octaves, float lacunarity, float persistence)
        {
            _seed = seed;
            _scale = scale;
            _width = width;
            _height = height;
            _octaves = octaves;
            _lacunarity = lacunarity;
            _persistence = persistence;
            InitializeArrays();
            GenerateValueTable();
        }

        private void InitializeArrays()
        {
            switch (_arraysInitialized)
            {
                case true when _noiseValuesNative.Length == _width * _height:
                    return;
                case true:
                    _noiseValuesNative.Dispose();
                    _valueTable.Dispose();
                    break;
            }

            _noiseValuesNative = new NativeArray<float>(_width * _height, Allocator.Persistent);
            _valueTable = new NativeArray<float>(512, Allocator.Persistent);
            _arraysInitialized = true;
        }

        private void GenerateValueTable()
        {
            uint uSeed = (uint)_seed;
            var random = new Random(uSeed);

            for (int i = 0; i < 512; i++)
            {
                _valueTable[i] = random.NextFloat();
            }
        }

        [BurstCompile]
        private struct ValueNoiseJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> ValueTable;
            public NativeArray<float> NoiseValues;
            public float Scale;
            public int Width;
            public int Octaves;
            public float Lacunarity;
            public float Persistence;

            public void Execute(int index)
            {
                int x = index % Width;
                int y = index / Width;
                NoiseValues[index] = GenerateFractalValueNoise(x, y);
            }

            private float GenerateFractalValueNoise(int x, int y)
            {
                float total = 0;
                float frequency = Scale;
                float amplitude = 1;
                float maxValue = 0;

                for (int i = 0; i < Octaves; i++)
                {
                    total += GenerateValueNoise(x * frequency, y * frequency) * amplitude;
                    maxValue += amplitude;
                    amplitude *= Persistence;
                    frequency *= Lacunarity;
                }

                return total / maxValue;
            }

            private float GenerateValueNoise(float x, float y)
            {
                int x0 = (int)math.floor(x);
                int x1 = x0 + 1;
                int y0 = (int)math.floor(y);
                int y1 = y0 + 1;

                float sx = x - x0;
                float sy = y - y0;

                float n0 = ValueTable[Hash(x0, y0)];
                float n1 = ValueTable[Hash(x1, y0)];
                float ix0 = math.lerp(n0, n1, sx);

                float n2 = ValueTable[Hash(x0, y1)];
                float n3 = ValueTable[Hash(x1, y1)];
                float ix1 = math.lerp(n2, n3, sy);

                return math.lerp(ix0, ix1, sy);
            }

            private static int Hash(int x, int y)
            {
                int h = x * 374761393 + y * 668265263;
                return h & 511;
            }
        }

        public float[] Generate()
        {
            var valueNoiseJob = new ValueNoiseJob
            {
                ValueTable = _valueTable,
                NoiseValues = _noiseValuesNative,
                Scale = _scale,
                Width = _width,
                Octaves = _octaves,
                Lacunarity = _lacunarity,
                Persistence = _persistence
            };

            JobHandle jobHandle = valueNoiseJob.Schedule(_width * _height, 64);
            jobHandle.Complete();

            float[] result = new float[_width * _height];
            _noiseValuesNative.CopyTo(result);

            return result;
        }

        public void Dispose()
        {
            if (!_arraysInitialized) return;
            _noiseValuesNative.Dispose();
            _valueTable.Dispose();
            _arraysInitialized = false;
        }

        ~ValueNoise()
        {
            Dispose();
        }
    }
}
