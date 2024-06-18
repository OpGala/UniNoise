using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace UniNoise.Noises
{
          public sealed class WorleyNoise : System.IDisposable
          {
                    private readonly int _seed;
                    private readonly int _width;
                    private readonly int _height;
                    private readonly int _numCells;
                    private readonly float _scale;
                    private readonly float _jitter;
                    private readonly DistanceFunction _distanceFunction;
                    private readonly int _numberOfFeatures;

                    private NativeArray<float3> _cellPointsNative;
                    private NativeArray<float> _noiseValuesNative;
                    private bool _arraysInitialized;

                    public WorleyNoise(int seed, int width, int height, int numCells, float scale, float jitter,
                                        DistanceFunction distanceFunction, int numberOfFeatures)
                    {
                              _seed = seed;
                              _width = width;
                              _height = height;
                              _numCells = numCells;
                              _scale = scale;
                              _jitter = jitter;
                              _distanceFunction = distanceFunction;
                              _numberOfFeatures = numberOfFeatures;
                              InitializeArrays();
                              GenerateCellPoints();
                    }

                    private void InitializeArrays()
                    {
                              switch (_arraysInitialized)
                              {
                                        case true when _cellPointsNative.Length == _numCells:
                                                  return;
                                        case true:
                                                  _cellPointsNative.Dispose();
                                                  _noiseValuesNative.Dispose();
                                                  break;
                              }

                              _cellPointsNative = new NativeArray<float3>(_numCells, Allocator.Persistent);
                              _noiseValuesNative = new NativeArray<float>(_width * _height, Allocator.Persistent);
                              _arraysInitialized = true;
                    }

                    private void GenerateCellPoints()
                    {
                              var job = new GenerateCellPointsJob
                              {
                                                  Seed = _seed,
                                                  NumCells = _numCells,
                                                  Width = _width,
                                                  Scale = _scale,
                                                  Height = _height,
                                                  CellPointsNative = _cellPointsNative
                              };

                              JobHandle jobHandle = job.Schedule();
                              jobHandle.Complete();
                    }

                    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                    private struct GenerateCellPointsJob : IJob
                    {
                              public int Seed;
                              public int NumCells;
                              public float Width;
                              public float Scale;
                              public float Height;
                              public NativeArray<float3> CellPointsNative;

                              public void Execute()
                              {
                                        uint uSeed = (uint)Seed;
                                        var random = new Random(uSeed);

                                        for (int i = 0; i < NumCells; i++)
                                        {
                                                  CellPointsNative[i] = new float3(
                                                                      random.NextFloat(0, Width / Scale),
                                                                      random.NextFloat(0, Height / Scale),
                                                                      0);
                                        }
                              }
                    }

                    internal float GetDistance(float3 point, float3 cellPoint)
                    {
                              switch (_distanceFunction)
                              {
                                        case DistanceFunction.Manhattan:
                                                  return math.abs(point.x - cellPoint.x) +
                                                         math.abs(point.y - cellPoint.y);
                                        case DistanceFunction.Chebyshev:
                                                  return math.max(math.abs(point.x - cellPoint.x),
                                                                      math.abs(point.y - cellPoint.y));
                                        case DistanceFunction.Euclidean:
                                        default:
                                                  return math.distance(point, cellPoint);
                              }
                    }

                    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                    private struct WorleyNoiseJob : IJobParallelFor
                    {
                              [ReadOnly] public NativeArray<float3> CellPoints;
                              public NativeArray<float> NoiseValues;
                              public int Width;
                              public int Height;
                              public int NumCells;
                              public float Scale;
                              public float Jitter;
                              public DistanceFunction DistanceFunc;
                              public int NumberOfFeatures;

                              public void Execute(int index)
                              {
                                        int x = index % Width;
                                        int y = index / Width;
                                        var point = new float3(x / Scale, y / Scale, 0);

                                        var distances = new NativeArray<float>(NumberOfFeatures, Allocator.Temp);
                                        for (int i = 0; i < NumberOfFeatures; i++)
                                        {
                                                  distances[i] = float.MaxValue;
                                        }

                                        for (int i = 0; i < NumCells; i++)
                                        {
                                                  float3 jitteredPoint = CellPoints[i] +
                                                                         new float3(
                                                                                             Jitter * Random
                                                                                                                 .CreateFromIndex(
                                                                                                                                     (uint)
                                                                                                                                     (index +
                                                                                                                                                         i))
                                                                                                                 .NextFloat2Direction(),
                                                                                             0);
                                                  float dist = GetDistance(point, jitteredPoint, DistanceFunc);

                                                  for (int j = 0; j < NumberOfFeatures; j++)
                                                  {
                                                            if (dist >= distances[j]) continue;
                                                            for (int k = NumberOfFeatures - 1; k > j; k--)
                                                            {
                                                                      distances[k] = distances[k - 1];
                                                            }

                                                            distances[j] = dist;
                                                            break;
                                                  }
                                        }

                                        NoiseValues[index] = distances[NumberOfFeatures - 1] /
                                                             math.sqrt(Width * Width + Height * Height);
                                        distances.Dispose();
                              }

                              private static float GetDistance(float3 point, float3 cellPoint,
                                                  DistanceFunction distanceFunction)
                              {
                                        float3 diff = point - cellPoint;
                                        switch (distanceFunction)
                                        {
                                                  case DistanceFunction.Manhattan:
                                                            return math.csum(math.abs(diff).xy);
                                                  case DistanceFunction.Chebyshev:
                                                            return math.cmax(math.abs(diff).xy);
                                                  case DistanceFunction.Euclidean:
                                                  default:
                                                            return math.length(diff.xy);
                                        }
                              }
                    }

                    public float[] Generate()
                    {
                              var worleyNoiseJob = new WorleyNoiseJob
                              {
                                                  CellPoints = _cellPointsNative,
                                                  NoiseValues = _noiseValuesNative,
                                                  Width = _width,
                                                  Height = _height,
                                                  NumCells = _numCells,
                                                  Scale = _scale,
                                                  Jitter = _jitter,
                                                  DistanceFunc = _distanceFunction,
                                                  NumberOfFeatures = _numberOfFeatures
                              };

                              JobHandle jobHandle = worleyNoiseJob.Schedule(_width * _height, 64);
                              jobHandle.Complete();

                              float[] result = new float[_width * _height];
                              _noiseValuesNative.CopyTo(result);

                              return result;
                    }

                    public void Dispose()
                    {
                              if (!_arraysInitialized) return;
                              _cellPointsNative.Dispose();
                              _noiseValuesNative.Dispose();
                              _arraysInitialized = false;
                    }

                    ~WorleyNoise()
                    {
                              Dispose();
                    }
          }
}