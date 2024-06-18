using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
    public sealed class WhiteNoise : System.IDisposable
    {
        private readonly int _seed;
        private readonly int _width;
        private readonly int _height;
        private readonly float _amplitude;
        private readonly float _bias;

        private NativeArray<float> _noiseValuesNative;
        private bool _arraysInitialized;

        public WhiteNoise(int seed, int width, int height, float amplitude, float bias)
        {
            _seed = seed;
            _width = width;
            _height = height;
            _amplitude = amplitude;
            _bias = bias;
            InitializeArrays();
        }

        private void InitializeArrays()
        {
            switch (_arraysInitialized)
            {
                case true when _noiseValuesNative.Length == _width * _height:
                    return;
                case true:
                    _noiseValuesNative.Dispose();
                    break;
            }

            _noiseValuesNative = new NativeArray<float>(_width * _height, Allocator.Persistent);
            _arraysInitialized = true;
        }

        [BurstCompile]
        private struct WhiteNoiseJob : IJobParallelFor
        {
            public NativeArray<float> NoiseValues;
            public float Amplitude;
            public float Bias;
            public Random RandomGen;

            public void Execute(int index)
            {
                NoiseValues[index] = RandomGen.NextFloat() * Amplitude + Bias;
            }
        }

        public float[] Generate()
        {
            var randomGen = new Random((uint)_seed);

            var whiteNoiseJob = new WhiteNoiseJob
            {
                NoiseValues = _noiseValuesNative,
                Amplitude = _amplitude,
                Bias = _bias,
                RandomGen = randomGen
            };

            JobHandle jobHandle = whiteNoiseJob.Schedule(_width * _height, 64);
            jobHandle.Complete();

            float[] result = new float[_width * _height];
            _noiseValuesNative.CopyTo(result);

            return result;
        }

        public void Dispose()
        {
            if (!_arraysInitialized) return;
            _noiseValuesNative.Dispose();
            _arraysInitialized = false;
        }

        ~WhiteNoise()
        {
            Dispose();
        }
    }
}
