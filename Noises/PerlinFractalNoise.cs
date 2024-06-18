using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
          public sealed class PerlinFractalNoise : IDisposable
          {
                    private readonly int _seed;
                    private readonly float _scale;
                    private readonly int _width;
                    private readonly int _height;
                    private readonly float _lacunarity;
                    private readonly float _gain;
                    private readonly int _octaves;

                    private NativeArray<int> _permutationTable;
                    private NativeArray<float> _noiseValuesNative;
                    private NativeArray<float2> _gradients;
                    private bool _arraysInitialized;

                    public PerlinFractalNoise(int seed, float scale, int width, int height, float lacunarity,
                                        float gain, int octaves)
                    {
                              _seed = seed;
                              _scale = scale;
                              _width = width;
                              _height = height;
                              _lacunarity = lacunarity;
                              _gain = gain;
                              _octaves = octaves;
                              InitializeArrays();
                              GeneratePermutationTable();
                              GenerateGradients();
                    }

                    private void InitializeArrays()
                    {
                              if (!_arraysInitialized)
                              {
                                        _noiseValuesNative =
                                                            new NativeArray<float>(_width * _height,
                                                                                Allocator.Persistent);
                                        _permutationTable = new NativeArray<int>(512, Allocator.Persistent);
                                        _gradients = new NativeArray<float2>(512, Allocator.Persistent);
                                        _arraysInitialized = true;
                              }
                              else if (_noiseValuesNative.Length != _width * _height)
                              {
                                        _noiseValuesNative.Dispose();
                                        _noiseValuesNative =
                                                            new NativeArray<float>(_width * _height,
                                                                                Allocator.Persistent);
                              }
                    }

                    private void GeneratePermutationTable()
                    {
                              var job = new PermutationTableJob
                              {
                                                  PermutationTable = _permutationTable,
                                                  Seed = _seed
                              };

                              JobHandle jobHandle = job.Schedule();
                              jobHandle.Complete();
                    }

                    private void GenerateGradients()
                    {
                              var job = new GradientsJob
                              {
                                                  Gradients = _gradients,
                                                  Seed = _seed
                              };

                              JobHandle jobHandle = job.Schedule();
                              jobHandle.Complete();
                    }

                    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                    private struct PermutationTableJob : IJob
                    {
                              public NativeArray<int> PermutationTable;
                              public int Seed;

                              public void Execute()
                              {
                                        var random = new Random((uint)Seed);
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
                                                  PermutationTable[i] = p[i & 255];
                                        }

                                        p.Dispose();
                              }
                    }

                    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                    private struct GradientsJob : IJob
                    {
                              public NativeArray<float2> Gradients;
                              public int Seed;

                              public void Execute()
                              {
                                        var random = new Random((uint)Seed);
                                        for (int i = 0; i < 256; i++)
                                        {
                                                  float angle = random.NextFloat(0, 2 * math.PI);
                                                  Gradients[i] = new float2(math.cos(angle), math.sin(angle));
                                        }

                                        for (int i = 0; i < 512; i++)
                                        {
                                                  Gradients[i] = Gradients[i & 255];
                                        }
                              }
                    }

                    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                    private struct PerlinNoiseJob : IJobParallelFor
                    {
                              [ReadOnly] public NativeArray<int> PermutationTable;
                              [ReadOnly] public NativeArray<float2> Gradients;
                              public NativeArray<float> NoiseValues;
                              public float Scale;
                              public int Width;
                              public float Lacunarity;
                              public float Gain;
                              public int Octaves;

                              public void Execute(int index)
                              {
                                        int x = index % Width;
                                        int y = index / Width;
                                        NoiseValues[index] = FractalPerlinNoise(new float2(x * Scale, y * Scale));
                              }

                              private float FractalPerlinNoise(float2 point)
                              {
                                        float total = 0;
                                        float frequency = 1;
                                        float amplitude = 1;
                                        float maxValue = 0;

                                        for (int i = 0; i < Octaves; i++)
                                        {
                                                  total += PerlinNoise(point * frequency) * amplitude;
                                                  maxValue += amplitude;
                                                  amplitude *= Gain;
                                                  frequency *= Lacunarity;
                                        }

                                        return total / maxValue;
                              }

                              private float PerlinNoise(float2 point)
                              {
                                        int x0 = (int)math.floor(point.x);
                                        int y0 = (int)math.floor(point.y);
                                        int x1 = x0 + 1;
                                        int y1 = y0 + 1;

                                        float2 g00 = Gradients[Hash(x0, y0)];
                                        float2 g10 = Gradients[Hash(x1, y0)];
                                        float2 g01 = Gradients[Hash(x0, y1)];
                                        float2 g11 = Gradients[Hash(x1, y1)];

                                        float2 p00 = new float2(x0, y0);
                                        float2 p10 = new float2(x1, y0);
                                        float2 p01 = new float2(x0, y1);
                                        float2 p11 = new float2(x1, y1);

                                        float2 f00 = point - p00;
                                        float2 f10 = point - p10;
                                        float2 f01 = point - p01;
                                        float2 f11 = point - p11;

                                        float n00 = math.dot(g00, f00);
                                        float n10 = math.dot(g10, f10);
                                        float n01 = math.dot(g01, f01);
                                        float n11 = math.dot(g11, f11);

                                        float sx = math.smoothstep(0, 1, f00.x);
                                        float sy = math.smoothstep(0, 1, f00.y);

                                        return math.lerp(math.lerp(n00, n10, sx), math.lerp(n01, n11, sx), sy);
                              }

                              private int Hash(int x, int y)
                              {
                                        return PermutationTable[(x + PermutationTable[y & 255]) & 255];
                              }
                    }

                    public float[] Generate()
                    {
                              InitializeArrays();

                              var perlinNoiseJob = new PerlinNoiseJob
                              {
                                                  PermutationTable = _permutationTable,
                                                  Gradients = _gradients,
                                                  NoiseValues = _noiseValuesNative,
                                                  Scale = _scale,
                                                  Width = _width,
                                                  Lacunarity = _lacunarity,
                                                  Gain = _gain,
                                                  Octaves = _octaves,
                              };

                              JobHandle jobHandle = perlinNoiseJob.Schedule(_width * _height, 64);
                              jobHandle.Complete();

                              float[] result = new float[_width * _height];
                              _noiseValuesNative.CopyTo(result);

                              return result;
                    }

                    public void Dispose()
                    {
                              if (_arraysInitialized)
                              {
                                        _noiseValuesNative.Dispose();
                                        _permutationTable.Dispose();
                                        _gradients.Dispose();
                                        _arraysInitialized = false;
                              }

                              GC.SuppressFinalize(this);
                    }

                    ~PerlinFractalNoise()
                    {
                              Dispose();
                    }
          }
}