using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
          public sealed class SimplexFractalNoise : System.IDisposable
          {
                    private readonly int _seed;
                    private readonly float _scale;
                    private readonly int _width;
                    private readonly int _height;
                    private readonly int _octaves;
                    private readonly float _lacunarity;
                    private readonly float _persistence;
                    private readonly float _amplitude;
                    private readonly float _frequency;

                    private NativeArray<int> _perm;
                    private NativeArray<float> _noiseValuesNative;
                    private bool _arraysInitialized;

                    private static readonly float3[] Grad3;

                    static SimplexFractalNoise()
                    {
                              Grad3 = new[]
                              {
                                                  new float3(1, 1, 0), new float3(-1, 1, 0), new float3(1, -1, 0),
                                                  new float3(-1, -1, 0),
                                                  new float3(1, 0, 1), new float3(-1, 0, 1), new float3(1, 0, -1),
                                                  new float3(-1, 0, -1),
                                                  new float3(0, 1, 1), new float3(0, -1, 1), new float3(0, 1, -1),
                                                  new float3(0, -1, -1)
                              };
                    }

                    public SimplexFractalNoise(int seed, float scale, int width, int height, int octaves,
                                        float lacunarity, float persistence, float amplitude, float frequency)
                    {
                              _seed = seed;
                              _scale = scale;
                              _width = width;
                              _height = height;
                              _octaves = octaves;
                              _lacunarity = lacunarity;
                              _persistence = persistence;
                              _amplitude = amplitude;
                              _frequency = frequency;
                              InitializeArrays();
                              GeneratePermutationTable();
                    }

                    private void InitializeArrays()
                    {
                              if (!_arraysInitialized)
                              {
                                        _perm = new NativeArray<int>(512, Allocator.Persistent);
                                        _noiseValuesNative =
                                                            new NativeArray<float>(_width * _height,
                                                                                Allocator.Persistent);
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
                                        _perm[i] = p[i & 255];
                              }

                              p.Dispose();
                    }

                    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                    private struct SimplexNoiseJob : IJobParallelFor
                    {
                              [ReadOnly] public NativeArray<int> Perm;
                              public NativeArray<float> NoiseValues;
                              public float Scale;
                              public int Width;
                              public int Octaves;
                              public float Lacunarity;
                              public float Persistence;
                              public float Amplitude;
                              public float Frequency;

                              private static readonly float F2 = 0.5f * (math.sqrt(3.0f) - 1.0f);
                              private static readonly float G2 = (3.0f - math.sqrt(3.0f)) / 6.0f;

                              public void Execute(int index)
                              {
                                        int x = index % Width;
                                        int y = index / Width;
                                        var point = new float2(x * Scale, y * Scale);
                                        NoiseValues[index] = GenerateFractalSimplexNoise(point);
                              }

                              private float GenerateFractalSimplexNoise(float2 point)
                              {
                                        float total = 0;
                                        float amplitude = Amplitude;
                                        float frequency = Frequency;
                                        float maxValue = 0;

                                        for (int i = 0; i < Octaves; i++)
                                        {
                                                  total += GenerateSimplexNoise(point * frequency) * amplitude;
                                                  maxValue += amplitude;
                                                  amplitude *= Persistence;
                                                  frequency *= Lacunarity;
                                        }

                                        return total / maxValue;
                              }

                              private float GenerateSimplexNoise(float2 point)
                              {
                                        float s = (point.x + point.y) * F2;
                                        int i = (int)math.floor(point.x + s);
                                        int j = (int)math.floor(point.y + s);

                                        float t = (i + j) * G2;
                                        float2 offset = point - new float2(i - t, j - t);

                                        int i1 = offset.x > offset.y ? 1 : 0;
                                        int j1 = offset.x > offset.y ? 0 : 1;

                                        float2 v1 = offset - new float2(i1, j1) + G2;
                                        float2 v2 = offset - 1.0f + 2.0f * G2;

                                        int ii = i & 255;
                                        int jj = j & 255;

                                        float t0 = 0.5f - math.dot(offset, offset);
                                        float n0 = t0 < 0
                                                            ? 0.0f
                                                            : t0 * t0 * t0 * t0 *
                                                              math.dot(Grad3[Perm[ii + Perm[jj]] % 12],
                                                                                  new float3(offset, 0));

                                        float t1 = 0.5f - math.dot(v1, v1);
                                        float n1 = t1 < 0
                                                            ? 0.0f
                                                            : t1 * t1 * t1 * t1 *
                                                              math.dot(Grad3[Perm[ii + i1 + Perm[jj + j1]] % 12],
                                                                                  new float3(v1, 0));

                                        float t2 = 0.5f - math.dot(v2, v2);
                                        float n2 = t2 < 0
                                                            ? 0.0f
                                                            : t2 * t2 * t2 * t2 *
                                                              math.dot(Grad3[Perm[ii + 1 + Perm[jj + 1]] % 12],
                                                                                  new float3(v2, 0));

                                        return 70.0f * (n0 + n1 + n2);
                              }
                    }

                    public float[] Generate()
                    {
                              var simplexNoiseJob = new SimplexNoiseJob
                              {
                                                  Perm = _perm,
                                                  NoiseValues = _noiseValuesNative,
                                                  Scale = _scale,
                                                  Width = _width,
                                                  Octaves = _octaves,
                                                  Lacunarity = _lacunarity,
                                                  Persistence = _persistence,
                                                  Amplitude = _amplitude,
                                                  Frequency = _frequency
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
                              _perm.Dispose();
                              _noiseValuesNative.Dispose();
                              _arraysInitialized = false;
                    }

                    ~SimplexFractalNoise()
                    {
                              Dispose();
                    }
          }
}