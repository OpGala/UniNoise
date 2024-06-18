using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
          public sealed class FractalNoise : IDisposable
          {
                    private readonly int _seed;
                    private readonly float _scale;
                    private readonly int _width;
                    private readonly int _height;
                    private readonly int _octaves;
                    private readonly float _lacunarity;
                    private readonly float _persistence;

                    private NativeArray<float> _noiseValuesNative;
                    private NativeArray<int> _permutationTable;
                    private bool _arraysInitialized;

                    public FractalNoise(int seed, float scale, int width, int height, int octaves, float lacunarity,
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
                              GeneratePermutationTable();
                    }

                    private void InitializeArrays()
                    {
                              if (_arraysInitialized && _noiseValuesNative.Length == _width * _height) return;
                              if (_arraysInitialized)
                              {
                                        _noiseValuesNative.Dispose();
                                        _permutationTable.Dispose();
                              }

                              _noiseValuesNative =
                                                  new NativeArray<float>(_width * _height,
                                                                      Allocator.Persistent);
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

                    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                    private struct FractalNoiseJob : IJobParallelFor
                    {
                              [ReadOnly] public NativeArray<int> PermutationTable;
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
                                        NoiseValues[index] = GenerateFractalPerlinNoise(point);
                              }

                              private float GenerateFractalPerlinNoise(float2 point)
                              {
                                        float total = 0;
                                        float amplitude = 1;
                                        float frequency = 1;
                                        float maxValue = 0;

                                        for (int i = 0; i < Octaves; i++)
                                        {
                                                  total += GeneratePerlinNoise(point * frequency) * amplitude;
                                                  maxValue += amplitude;
                                                  amplitude *= Persistence;
                                                  frequency *= Lacunarity;
                                        }

                                        return total / maxValue;
                              }

                              private float GeneratePerlinNoise(float2 point)
                              {
                                        int x0 = (int)math.floor(point.x);
                                        int x1 = x0 + 1;
                                        int y0 = (int)math.floor(point.y);
                                        int y1 = y0 + 1;

                                        float sx = point.x - x0;
                                        float sy = point.y - y0;

                                        float n0 = PerlinGradient(Hash(x0, y0), sx, sy);
                                        float n1 = PerlinGradient(Hash(x1, y0), sx - 1, sy);
                                        float ix0 = math.lerp(n0, n1, sx);

                                        float n2 = PerlinGradient(Hash(x0, y1), sx, sy - 1);
                                        float n3 = PerlinGradient(Hash(x1, y1), sx - 1, sy - 1);
                                        float ix1 = math.lerp(n2, n3, sx);

                                        return math.lerp(ix0, ix1, sy);
                              }

                              private static float PerlinGradient(int hash, float x, float y)
                              {
                                        int h = hash & 15;
                                        float u = h < 8 ? x : y;
                                        float v = h < 4 ? y : x;
                                        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
                              }

                              private int Hash(int x, int y)
                              {
                                        int h = PermutationTable[(x & 255) + PermutationTable[y & 255]];
                                        return h;
                              }
                    }

                    public float[] Generate()
                    {
                              var fractalNoiseJob = new FractalNoiseJob
                              {
                                                  PermutationTable = _permutationTable,
                                                  NoiseValues = _noiseValuesNative,
                                                  Scale = _scale,
                                                  Width = _width,
                                                  Octaves = _octaves,
                                                  Lacunarity = _lacunarity,
                                                  Persistence = _persistence
                              };

                              JobHandle jobHandle = fractalNoiseJob.Schedule(_width * _height, 64);
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

                    ~FractalNoise()
                    {
                              Dispose();
                    }
          }
}