using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
          public sealed class GradientNoise : System.IDisposable
          {
                    private readonly int _seed;
                    private readonly float _scale;
                    private readonly int _width;
                    private readonly int _height;
                    private readonly float _amplitude;
                    private readonly float _frequency;
                    private readonly float2 _offset;

                    private NativeArray<float> _noiseValuesNative;
                    private NativeArray<int> _permutationTable;
                    private bool _arraysInitialized;

                    public GradientNoise(int seed, float scale, int width, int height, float amplitude, float frequency,
                                        float2 offset)
                    {
                              _seed = seed;
                              _scale = scale;
                              _width = width;
                              _height = height;
                              _amplitude = amplitude;
                              _frequency = frequency;
                              _offset = offset;
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

                    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                    private struct GradientNoiseJob : IJobParallelFor
                    {
                              [ReadOnly] public NativeArray<int> PermutationTable;
                              public NativeArray<float> NoiseValues;
                              public float Scale;
                              public int Width;
                              public float Amplitude;
                              public float Frequency;
                              public float2 Offset;

                              public void Execute(int index)
                              {
                                        int x = index % Width;
                                        int y = index / Width;
                                        float2 point = (new float2(x, y) + Offset) * Scale * Frequency;
                                        NoiseValues[index] = GenerateGradientNoise(point) * Amplitude;
                              }

                              private float GenerateGradientNoise(float2 point)
                              {
                                        int x0 = (int)math.floor(point.x);
                                        int x1 = x0 + 1;
                                        int y0 = (int)math.floor(point.y);
                                        int y1 = y0 + 1;

                                        float sx = point.x - x0;
                                        float sy = point.y - y0;

                                        float n0 = Gradient(Hash(x0, y0), sx, sy);
                                        float n1 = Gradient(Hash(x1, y0), sx - 1, sy);
                                        float ix0 = math.lerp(n0, n1, sx);

                                        float n2 = Gradient(Hash(x0, y1), sx, sy - 1);
                                        float n3 = Gradient(Hash(x1, y1), sx - 1, sy - 1);
                                        float ix1 = math.lerp(n2, n3, sx);

                                        return math.lerp(ix0, ix1, sy);
                              }

                              private static float Gradient(int hash, float x, float y)
                              {
                                        int h = hash & 7;
                                        float u = h < 4 ? x : y;
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
                              var gradientNoiseJob = new GradientNoiseJob
                              {
                                                  PermutationTable = _permutationTable,
                                                  NoiseValues = _noiseValuesNative,
                                                  Scale = _scale,
                                                  Width = _width,
                                                  Amplitude = _amplitude,
                                                  Frequency = _frequency,
                                                  Offset = _offset
                              };

                              JobHandle jobHandle = gradientNoiseJob.Schedule(_width * _height, 64);
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

                    ~GradientNoise()
                    {
                              Dispose();
                    }
          }
}