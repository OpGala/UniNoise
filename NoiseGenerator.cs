using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace UniNoise
{
    [ExecuteInEditMode]
    public sealed class NoiseGenerator : MonoBehaviour
    {
        public NoiseType noiseType = NoiseType.Perlin;
        public int seed = 42;
        public float scale = 1.0f;
        public int width = 256;
        public int height = 256;
        public float lacunarity = 2.0f;
        public float gain = 0.5f;
        public int octaves = 4;
        public int numCells = 64;
        public float jitter = 1.0f;
        public DistanceFunction distanceFunction = DistanceFunction.Euclidean;
        public int numberOfFeatures = 1;
        public float amplitude = 1.0f;
        public float frequency = 1.0f;
        public float bias;
        public int kernelSize = 3;
        public float orientation;
        public float aspectRatio = 1.0f;
        public float phase;
        public float2 offset = new float2(0.0f, 0.0f);
        internal Texture2D NoiseTexture;
        internal string GenerationTime = "";

        private NativeArray<float> _noiseValuesNative;
        private NativeArray<Color> _colorsNative;
        private bool _arraysInitialized;

        private void OnValidate()
        {
            GenerateNoiseTexture();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void Dispose()
        {
            if (!_arraysInitialized) return;
            if (_noiseValuesNative.IsCreated)
                _noiseValuesNative.Dispose();
            if (_colorsNative.IsCreated)
                _colorsNative.Dispose();
            _arraysInitialized = false;
        }

        private void InitializeArrays()
        {
            if (_arraysInitialized && _noiseValuesNative.Length == width * height) return;
            if (_noiseValuesNative.IsCreated)
                _noiseValuesNative.Dispose();
            if (_colorsNative.IsCreated)
                _colorsNative.Dispose();

            _noiseValuesNative = new NativeArray<float>(width * height, Allocator.Persistent);
            _colorsNative = new NativeArray<Color>(width * height, Allocator.Persistent);
            _arraysInitialized = true;
        }

        [BurstCompile]
        private struct GenerateTextureJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> NoiseValues;
            public NativeArray<Color> Colors;

            public void Execute(int index)
            {
                float value = NoiseValues[index];
                Colors[index] = new Color(value, value, value);
            }
        }

        public void GenerateNoiseTexture()
        {
            if (width <= 0 || height <= 0) return;

            InitializeArrays();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            float[] noiseValues = Noise.GetNoise(
                noiseType, seed, width, height, scale, octaves, lacunarity, gain, 
                gain, amplitude, frequency, bias, numCells, jitter, distanceFunction, 
                numberOfFeatures, orientation, aspectRatio, phase, kernelSize, offset
            );

            _noiseValuesNative.CopyFrom(noiseValues);

            stopwatch.Stop();
            double microseconds = stopwatch.ElapsedTicks * (1000000.0 / Stopwatch.Frequency);
            GenerationTime = $"Generation time: {microseconds:F2} µs";

            var generateTextureJob = new GenerateTextureJob
            {
                NoiseValues = _noiseValuesNative,
                Colors = _colorsNative
            };

            JobHandle jobHandle = generateTextureJob.Schedule(width * height, 64);
            jobHandle.Complete();

            NoiseTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            NoiseTexture.SetPixels(_colorsNative.ToArray());
            NoiseTexture.Apply();
        }
    }

    [CustomEditor(typeof(NoiseGenerator))]
    public sealed class NoiseGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var generator = (NoiseGenerator)target;

            // Draw Noise Type field
            generator.noiseType = (NoiseType)EditorGUILayout.EnumPopup("Noise Type", generator.noiseType);

            // Draw common fields
            generator.seed = EditorGUILayout.IntField("Seed", generator.seed);
            generator.width = EditorGUILayout.IntField("Width", generator.width);
            generator.height = EditorGUILayout.IntField("Height", generator.height);

            switch (generator.noiseType)
            {
                // Draw specific fields based on noise type
                case NoiseType.Perlin:
                    generator.scale = EditorGUILayout.FloatField("Scale", generator.scale);
                    break;
                case NoiseType.PerlinFractal:
                    generator.scale = EditorGUILayout.FloatField("Scale", generator.scale);
                    generator.lacunarity = EditorGUILayout.FloatField("Lacunarity", generator.lacunarity);
                    generator.gain = EditorGUILayout.FloatField("Gain", generator.gain);
                    generator.octaves = EditorGUILayout.IntField("Octaves", generator.octaves);
                    break;
                case NoiseType.Worley:
                    generator.numCells = EditorGUILayout.IntField("Number of Cells", generator.numCells);
                    generator.scale = EditorGUILayout.FloatField("Scale", generator.scale);
                    generator.jitter = EditorGUILayout.FloatField("Jitter", generator.jitter);
                    generator.distanceFunction = (DistanceFunction)EditorGUILayout.EnumPopup("Distance Function", generator.distanceFunction);
                    generator.numberOfFeatures = EditorGUILayout.IntField("Number of Features", generator.numberOfFeatures);
                    break;
                case NoiseType.Simplex:
                    generator.scale = EditorGUILayout.FloatField("Scale", generator.scale);
                    break;
                case NoiseType.SimplexFractal:
                    generator.scale = EditorGUILayout.FloatField("Scale", generator.scale);
                    generator.octaves = EditorGUILayout.IntField("Octaves", generator.octaves);
                    generator.lacunarity = EditorGUILayout.FloatField("Lacunarity", generator.lacunarity);
                    generator.gain = EditorGUILayout.FloatField("Persistence", generator.gain);
                    generator.amplitude = EditorGUILayout.FloatField("Amplitude", generator.amplitude);
                    generator.frequency = EditorGUILayout.FloatField("Frequency", generator.frequency);
                    break;
                case NoiseType.White:
                    generator.amplitude = EditorGUILayout.FloatField("Amplitude", generator.amplitude);
                    generator.bias = EditorGUILayout.FloatField("Bias", generator.bias);
                    break;
                case NoiseType.Value:
                    generator.scale = EditorGUILayout.FloatField("Scale", generator.scale);
                    generator.octaves = EditorGUILayout.IntField("Octaves", generator.octaves);
                    generator.lacunarity = EditorGUILayout.FloatField("Lacunarity", generator.lacunarity);
                    generator.gain = EditorGUILayout.FloatField("Persistence", generator.gain);
                    break;
                case NoiseType.Wavelet:
                    generator.scale = EditorGUILayout.FloatField("Scale", generator.scale);
                    generator.octaves = EditorGUILayout.IntField("Octaves", generator.octaves);
                    generator.lacunarity = EditorGUILayout.FloatField("Lacunarity", generator.lacunarity);
                    generator.gain = EditorGUILayout.FloatField("Persistence", generator.gain);
                    break;
                case NoiseType.Gabor:
                    generator.scale = EditorGUILayout.FloatField("Scale", generator.scale);
                    generator.frequency = EditorGUILayout.FloatField("Frequency", generator.frequency);
                    generator.orientation = EditorGUILayout.FloatField("Orientation", generator.orientation);
                    generator.aspectRatio = EditorGUILayout.FloatField("Aspect Ratio", generator.aspectRatio);
                    generator.phase = EditorGUILayout.FloatField("Phase", generator.phase);
                    generator.amplitude = EditorGUILayout.FloatField("Amplitude", generator.amplitude);
                    break;
                case NoiseType.SparseConvolution:
                    generator.scale = EditorGUILayout.FloatField("Scale", generator.scale);
                    generator.kernelSize = EditorGUILayout.IntField("Kernel Size", generator.kernelSize);
                    break;
                case NoiseType.Gradient:
                    generator.scale = EditorGUILayout.FloatField("Scale", generator.scale);
                    generator.amplitude = EditorGUILayout.FloatField("Amplitude", generator.amplitude);
                    generator.frequency = EditorGUILayout.FloatField("Frequency", generator.frequency);
                    generator.offset = EditorGUILayout.Vector2Field("Offset", generator.offset);
                    break;
                case NoiseType.Fractal:
                    generator.scale = EditorGUILayout.FloatField("Scale", generator.scale);
                    generator.octaves = EditorGUILayout.IntField("Octaves", generator.octaves);
                    generator.lacunarity = EditorGUILayout.FloatField("Lacunarity", generator.lacunarity);
                    generator.gain = EditorGUILayout.FloatField("Persistence", generator.gain);
                    break;
            }

            if (GUILayout.Button("Generate Noise Texture"))
            {
                generator.GenerateNoiseTexture();
            }

            if (generator.NoiseTexture != null)
            {
                GUILayout.Label(generator.NoiseTexture);
            }

            GUILayout.Label(generator.GenerationTime);
        }
    }
}
