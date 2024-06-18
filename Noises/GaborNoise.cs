using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
          public sealed class GaborNoise : System.IDisposable
          {
                    private readonly int _seed;
                    private readonly float _scale;
                    private readonly int _width;
                    private readonly int _height;
                    private readonly float _frequency;
                    private readonly float _orientation;
                    private readonly float _aspectRatio;
                    private readonly float _phase;
                    private readonly float _amplitude;

                    private NativeArray<float> _noiseValuesNative;
                    private NativeArray<int> _permutationTable;
                    private bool _arraysInitialized;

                    public GaborNoise(int seed, float scale, int width, int height, float frequency, float orientation,
                                        float aspectRatio, float phase, float amplitude)
                    {
                              _seed = seed;
                              _scale = scale;
                              _width = width;
                              _height = height;
                              _frequency = frequency;
                              _orientation = orientation;
                              _aspectRatio = aspectRatio;
                              _phase = phase;
                              _amplitude = amplitude;
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
                    private struct GaborNoiseJob : IJobParallelFor
                    {
                              public NativeArray<float> NoiseValues;
                              public float Scale;
                              public int Width;
                              public float Frequency;
                              public float Orientation;
                              public float AspectRatio;
                              public float Phase;
                              public float Amplitude;

                              public void Execute(int index)
                              {
                                        int x = index % Width;
                                        int y = index / Width;
                                        var point = new float2(x * Scale, y * Scale);
                                        NoiseValues[index] = GenerateGaborNoise(point);
                              }

                              private float GenerateGaborNoise(float2 point)
                              {
                                        float angle = Orientation * math.PI / 180f;
                                        var rotationMatrix = new float2x2(math.cos(angle), -math.sin(angle),
                                                            math.sin(angle), math.cos(angle));
                                        point = math.mul(rotationMatrix, point);
                                        var gaborPoint = new float2(point.x * AspectRatio, point.y);
                                        float wave = math.sin(2 * math.PI * Frequency * gaborPoint.x + Phase);
                                        float gaussian = math.exp(-0.5f * math.dot(gaborPoint, gaborPoint));
                                        return Amplitude * wave * gaussian;
                              }
                    }

                    public float[] Generate()
                    {
                              var gaborNoiseJob = new GaborNoiseJob
                              {
                                                  NoiseValues = _noiseValuesNative,
                                                  Scale = _scale,
                                                  Width = _width,
                                                  Frequency = _frequency,
                                                  Orientation = _orientation,
                                                  AspectRatio = _aspectRatio,
                                                  Phase = _phase,
                                                  Amplitude = _amplitude
                              };

                              JobHandle jobHandle = gaborNoiseJob.Schedule(_width * _height, 64);
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

                    ~GaborNoise()
                    {
                              Dispose();
                    }
          }
}