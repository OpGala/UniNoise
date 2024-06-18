using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
          public sealed class WaveletNoise : IDisposable
          {
                    private readonly int _seed;
                    private readonly float _scale;
                    private readonly int _width;
                    private readonly int _height;
                    private readonly int _octaves;
                    private readonly float _lacunarity;
                    private readonly float _persistence;

                    private NativeArray<float> _noiseValuesNative;
                    private NativeArray<float> _gradientTable;
                    private bool _arraysInitialized;

                    public WaveletNoise(int seed, float scale, int width, int height, int octaves, float lacunarity,
                                        float persistence)
                    {
                              _seed = seed;
                              _scale = scale;
                              _width = width;
                              _height = height;
                              _octaves = octaves;
                              _lacunarity = lacunarity;
                              _persistence = persistence;
                              InitializeArrays();
                              GenerateGradientTable();
                    }

                    private void InitializeArrays()
                    {
                              switch (_arraysInitialized)
                              {
                                        case true when _noiseValuesNative.Length == _width * _height:
                                                  return;
                                        case true:
                                                  _noiseValuesNative.Dispose();
                                                  _gradientTable.Dispose();
                                                  break;
                              }

                              _noiseValuesNative = new NativeArray<float>(_width * _height, Allocator.Persistent);
                              _gradientTable = new NativeArray<float>(512, Allocator.Persistent);
                              _arraysInitialized = true;
                    }

                    private void GenerateGradientTable()
                    {
                              uint uSeed = (uint)_seed;
                              var random = new Random(uSeed);

                              for (int i = 0; i < 512; i++)
                              {
                                        _gradientTable[i] = random.NextFloat();
                              }
                    }

                    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                    private struct WaveletNoiseJob : IJobParallelFor
                    {
                              [ReadOnly] public NativeArray<float> GradientTable;
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
                                        var point = new float2(x * Scale, y * Scale);
                                        NoiseValues[index] = GenerateFractalWaveletNoise(point);
                              }

                              private float GenerateFractalWaveletNoise(float2 point)
                              {
                                        float total = 0;
                                        float amplitude = 1;
                                        float frequency = 1;
                                        float maxValue = 0;

                                        for (int i = 0; i < Octaves; i++)
                                        {
                                                  total += GenerateWaveletNoise(point * frequency) * amplitude;
                                                  maxValue += amplitude;
                                                  amplitude *= Persistence;
                                                  frequency *= Lacunarity;
                                        }

                                        return total / maxValue;
                              }

                              private float GenerateWaveletNoise(float2 point)
                              {
                                        int x0 = (int)math.floor(point.x);
                                        int x1 = x0 + 1;
                                        int y0 = (int)math.floor(point.y);
                                        int y1 = y0 + 1;

                                        float sx = point.x - x0;
                                        float sy = point.y - y0;

                                        float n0 = WaveletFunction(Hash(x0, y0), sx, sy);
                                        float n1 = WaveletFunction(Hash(x1, y0), sx - 1, sy);
                                        float ix0 = math.lerp(n0, n1, sx);

                                        float n2 = WaveletFunction(Hash(x0, y1), sx, sy - 1);
                                        float n3 = WaveletFunction(Hash(x1, y1), sx - 1, sy - 1);
                                        float ix1 = math.lerp(n2, n3, sy);

                                        return math.lerp(ix0, ix1, sy);
                              }

                              private float WaveletFunction(int hash, float x, float y)
                              {
                                        float gradient = GradientTable[hash];
                                        return gradient * (x + y);
                              }

                              private static int Hash(int x, int y)
                              {
                                        int h = x * 374761393 + y * 668265263;
                                        return h & 511;
                              }
                    }

                    public float[] Generate()
                    {
                              var waveletNoiseJob = new WaveletNoiseJob
                              {
                                                  GradientTable = _gradientTable,
                                                  NoiseValues = _noiseValuesNative,
                                                  Scale = _scale,
                                                  Width = _width,
                                                  Octaves = _octaves,
                                                  Lacunarity = _lacunarity,
                                                  Persistence = _persistence
                              };

                              JobHandle jobHandle = waveletNoiseJob.Schedule(_width * _height, 64);
                              jobHandle.Complete();

                              float[] result = new float[_width * _height];
                              _noiseValuesNative.CopyTo(result);

                              return result;
                    }

                    public void Dispose()
                    {
                              if (!_arraysInitialized) return;
                              _noiseValuesNative.Dispose();
                              _gradientTable.Dispose();
                              _arraysInitialized = false;
                    }

                    ~WaveletNoise()
                    {
                              Dispose();
                    }
          }
}