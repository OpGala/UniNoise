using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

namespace UniNoise
{
        [ExecuteInEditMode]
        public sealed class NoiseGenerator : MonoBehaviour
        {
                public CombinedNoiseConfiguration combinedNoiseConfig;
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
                        if (_arraysInitialized && _noiseValuesNative.Length == combinedNoiseConfig.noiseConfigurations[0].width * combinedNoiseConfig.noiseConfigurations[0].height) return;
                        if (_noiseValuesNative.IsCreated)
                                _noiseValuesNative.Dispose();
                        if (_colorsNative.IsCreated)
                                _colorsNative.Dispose();

                        _noiseValuesNative = new NativeArray<float>(combinedNoiseConfig.noiseConfigurations[0].width * combinedNoiseConfig.noiseConfigurations[0].height, Allocator.Persistent);
                        _colorsNative = new NativeArray<Color>(combinedNoiseConfig.noiseConfigurations[0].width * combinedNoiseConfig.noiseConfigurations[0].height, Allocator.Persistent);
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
                        if (combinedNoiseConfig.noiseConfigurations.Count == 0 || combinedNoiseConfig.noiseConfigurations[0].width <= 0 ||
                            combinedNoiseConfig.noiseConfigurations[0].height <= 0) return;

                        InitializeArrays();

                        var stopwatch = new Stopwatch();
                        stopwatch.Start();

                        var noiseValuesList = new List<float[]>();
                        foreach (NoiseConfiguration config in combinedNoiseConfig.noiseConfigurations)
                        {
                                float[] noiseValues = Noise.GetNoise(config.noiseType, config.seed, config.width, config.height, config.scale, config.octaves, config.lacunarity, config.gain,
                                                config.gain, config.amplitude, config.frequency, config.bias, config.numCells, config.jitter, config.distanceFunction, config.numberOfFeatures,
                                                config.orientation, config.aspectRatio, config.phase, config.kernelSize, config.offset);
                                noiseValuesList.Add(noiseValues);
                        }

                        float[] combinedNoiseValues = Noise.CombineNoise(combinedNoiseConfig.combineMethod, noiseValuesList.ToArray());
                        _noiseValuesNative.CopyFrom(combinedNoiseValues);

                        stopwatch.Stop();
                        double microseconds = stopwatch.ElapsedTicks * (1000000.0 / Stopwatch.Frequency);
                        GenerationTime = $"Generation time: {microseconds:F2} µs";

                        var generateTextureJob = new GenerateTextureJob
                        {
                                        NoiseValues = _noiseValuesNative,
                                        Colors = _colorsNative
                        };

                        JobHandle jobHandle = generateTextureJob.Schedule(combinedNoiseConfig.noiseConfigurations[0].width * combinedNoiseConfig.noiseConfigurations[0].height, 64);
                        jobHandle.Complete();

                        NoiseTexture = new Texture2D(combinedNoiseConfig.noiseConfigurations[0].width, combinedNoiseConfig.noiseConfigurations[0].height, TextureFormat.RGB24, false);
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

                        if (GUILayout.Button("Add Noise Configuration"))
                        {
                                generator.combinedNoiseConfig.noiseConfigurations.Add(new NoiseConfiguration());
                        }

                        for (int i = 0; i < generator.combinedNoiseConfig.noiseConfigurations.Count; i++)
                        {
                                GUILayout.Label($"Noise Configuration {i + 1}", EditorStyles.boldLabel);
                                NoiseConfiguration config = generator.combinedNoiseConfig.noiseConfigurations[i];

                                config.noiseType = (NoiseType)EditorGUILayout.EnumPopup("Noise Type", config.noiseType);
                                config.seed = EditorGUILayout.IntField("Seed", config.seed);
                                config.width = EditorGUILayout.IntField("Width", config.width);
                                config.height = EditorGUILayout.IntField("Height", config.height);
                                config.offset = EditorGUILayout.Vector2Field("Offset", config.offset);

                                switch (config.noiseType)
                                {
                                        case NoiseType.Perlin:
                                                config.scale = EditorGUILayout.FloatField("Scale", config.scale);
                                                config.octaves = EditorGUILayout.IntField("Octaves", config.octaves);
                                                config.persistence = EditorGUILayout.FloatField("Persistence", config.persistence);
                                                config.lacunarity = EditorGUILayout.FloatField("Lacunarity", config.lacunarity);
                                                break;
                                        case NoiseType.PerlinFractal:
                                                config.scale = EditorGUILayout.FloatField("Scale", config.scale);
                                                config.lacunarity = EditorGUILayout.FloatField("Lacunarity", config.lacunarity);
                                                config.gain = EditorGUILayout.FloatField("Gain", config.gain);
                                                config.octaves = EditorGUILayout.IntField("Octaves", config.octaves);
                                                break;
                                        case NoiseType.Worley:
                                                config.numCells = EditorGUILayout.IntField("Number of Cells", config.numCells);
                                                config.scale = EditorGUILayout.FloatField("Scale", config.scale);
                                                config.jitter = EditorGUILayout.FloatField("Jitter", config.jitter);
                                                config.distanceFunction = (DistanceFunction)EditorGUILayout.EnumPopup("Distance Function", config.distanceFunction);
                                                config.numberOfFeatures = EditorGUILayout.IntField("Number of Features", config.numberOfFeatures);
                                                break;
                                        case NoiseType.Simplex:
                                                config.scale = EditorGUILayout.FloatField("Scale", config.scale);
                                                break;
                                        case NoiseType.SimplexFractal:
                                                config.scale = EditorGUILayout.FloatField("Scale", config.scale);
                                                config.octaves = EditorGUILayout.IntField("Octaves", config.octaves);
                                                config.lacunarity = EditorGUILayout.FloatField("Lacunarity", config.lacunarity);
                                                config.gain = EditorGUILayout.FloatField("Persistence", config.gain);
                                                config.amplitude = EditorGUILayout.FloatField("Amplitude", config.amplitude);
                                                config.frequency = EditorGUILayout.FloatField("Frequency", config.frequency);
                                                break;
                                        case NoiseType.White:
                                                config.amplitude = EditorGUILayout.FloatField("Amplitude", config.amplitude);
                                                config.bias = EditorGUILayout.FloatField("Bias", config.bias);
                                                break;
                                        case NoiseType.Value:
                                                config.scale = EditorGUILayout.FloatField("Scale", config.scale);
                                                config.octaves = EditorGUILayout.IntField("Octaves", config.octaves);
                                                config.lacunarity = EditorGUILayout.FloatField("Lacunarity", config.lacunarity);
                                                config.gain = EditorGUILayout.FloatField("Persistence", config.gain);
                                                break;
                                        case NoiseType.Wavelet:
                                                config.scale = EditorGUILayout.FloatField("Scale", config.scale);
                                                config.octaves = EditorGUILayout.IntField("Octaves", config.octaves);
                                                config.lacunarity = EditorGUILayout.FloatField("Lacunarity", config.lacunarity);
                                                config.gain = EditorGUILayout.FloatField("Persistence", config.gain);
                                                break;
                                        case NoiseType.Gabor:
                                                config.scale = EditorGUILayout.FloatField("Scale", config.scale);
                                                config.frequency = EditorGUILayout.FloatField("Frequency", config.frequency);
                                                config.orientation = EditorGUILayout.FloatField("Orientation", config.orientation);
                                                config.aspectRatio = EditorGUILayout.FloatField("Aspect Ratio", config.aspectRatio);
                                                config.phase = EditorGUILayout.FloatField("Phase", config.phase);
                                                config.amplitude = EditorGUILayout.FloatField("Amplitude", config.amplitude);
                                                break;
                                        case NoiseType.SparseConvolution:
                                                config.scale = EditorGUILayout.FloatField("Scale", config.scale);
                                                config.kernelSize = EditorGUILayout.IntField("Kernel Size", config.kernelSize);
                                                break;
                                        case NoiseType.Gradient:
                                                config.scale = EditorGUILayout.FloatField("Scale", config.scale);
                                                config.amplitude = EditorGUILayout.FloatField("Amplitude", config.amplitude);
                                                config.frequency = EditorGUILayout.FloatField("Frequency", config.frequency);
                                                config.offset = EditorGUILayout.Vector2Field("Offset", config.offset);
                                                break;
                                        case NoiseType.Fractal:
                                                config.scale = EditorGUILayout.FloatField("Scale", config.scale);
                                                config.octaves = EditorGUILayout.IntField("Octaves", config.octaves);
                                                config.lacunarity = EditorGUILayout.FloatField("Lacunarity", config.lacunarity);
                                                config.gain = EditorGUILayout.FloatField("Persistence", config.gain);
                                                break;
                                }

                                if (GUILayout.Button("Remove Noise Configuration"))
                                {
                                        generator.combinedNoiseConfig.noiseConfigurations.RemoveAt(i);
                                }

                                GUILayout.Space(10);
                        }

                        generator.combinedNoiseConfig.combineMethod = (CombineMethod)EditorGUILayout.EnumPopup("Combine Method", generator.combinedNoiseConfig.combineMethod);

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