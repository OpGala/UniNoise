using System.Collections.Generic;
using UniNoise.Noises;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UniNoise
{
        /// <summary>
        /// Enumeration of supported noise types.
        /// </summary>
        public enum NoiseType
        {
                /// <summary>
                /// Perlin noise, a gradient noise function known for its smoothness and coherence.
                /// </summary>
                Perlin,

                /// <summary>
                /// Fractal Perlin noise, an extension of Perlin noise that adds multiple layers of detail.
                /// </summary>
                PerlinFractal,

                /// <summary>
                /// Worley noise, also known as Voronoi or cellular noise, often used for generating cell-like patterns.
                /// </summary>
                Worley,

                /// <summary>
                /// Simplex noise, an alternative to Perlin noise that reduces directional artifacts.
                /// </summary>
                Simplex,

                /// <summary>
                /// Fractal Simplex noise, an extension of Simplex noise that adds multiple layers of detail.
                /// </summary>
                SimplexFractal,

                /// <summary>
                /// White noise, a random noise function with no coherence or smoothness.
                /// </summary>
                White,

                /// <summary>
                /// Value noise, a smooth noise function where the values at grid points are interpolated.
                /// </summary>
                Value,

                /// <summary>
                /// Wavelet noise, a noise function that uses wavelet basis functions for generating smooth noise.
                /// </summary>
                Wavelet,

                /// <summary>
                /// Fractal noise, a combination of multiple noise functions to create complex patterns.
                /// </summary>
                Fractal,

                /// <summary>
                /// Gradient noise, a noise function that uses gradients at grid points to generate smooth transitions.
                /// </summary>
                Gradient,

                /// <summary>
                /// Sparse convolution noise, a noise function that uses sparse convolution kernels for generating patterns.
                /// </summary>
                SparseConvolution,

                /// <summary>
                /// Gabor noise, a noise function that uses Gabor kernels for generating patterns.
                /// </summary>
                Gabor
        }

        /// <summary>
        /// Enumeration of distance functions used in Worley noise.
        /// </summary>
        public enum DistanceFunction
        {
                /// <summary>
                /// Euclidean distance function, which calculates the straight-line distance between points.
                /// This is the most commonly used distance function and results in circular cells.
                /// </summary>
                Euclidean,

                /// <summary>
                /// Manhattan distance function, which calculates the distance between points along grid lines.
                /// This function results in diamond-shaped cells and is sometimes used for grid-based maps.
                /// </summary>
                Manhattan,

                /// <summary>
                /// Chebyshev distance function, which calculates the maximum of the absolute differences between points.
                /// This function results in square cells and can create interesting tiling patterns.
                /// </summary>
                Chebyshev
        }

        /// <summary>
        /// Enumeration of combine noise methods.
        /// </summary>
        public enum CombineMethod
        {
                Add,
                Subtract,
                Average,
                Multiply
        }

        [System.Serializable]
        public class NoiseConfiguration
        {
                public NoiseType noiseType = NoiseType.Perlin;
                public int seed = 42;
                public float scale = 1.0f;
                public int width = 256;
                public int height = 256;
                public float lacunarity = 2.0f;
                public float gain = 0.5f;
                public float persistence = 1.0f;
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
        }

        [System.Serializable]
        public class CombinedNoiseConfiguration
        {
                public List<NoiseConfiguration> noiseConfigurations = new List<NoiseConfiguration>();
                public CombineMethod combineMethod = CombineMethod.Add;
        }

        /// <summary>
        /// Static class for generating various types of noise.
        /// </summary>
        public static class Noise
        {
                /// <summary>
                /// Generates a noise map based on the specified noise configuration.
                /// </summary>
                /// <param name="config">The configuration for the noise generation.</param>
                /// <returns>An array of floats representing the generated noise map.</returns>
                public static float[] GetNoise(NoiseConfiguration config)
                {
                        return GetNoise(config.noiseType, config.seed, config.width, config.height, config.scale, config.octaves, config.lacunarity, config.gain, config.persistence, config.amplitude,
                                        config.frequency, config.bias, config.numCells, config.jitter, config.distanceFunction, config.numberOfFeatures, config.orientation, config.aspectRatio,
                                        config.phase, config.kernelSize, config.offset);
                }

                /// <summary>
                /// Generates a noise map based on the specified noise type and parameters.
                /// </summary>
                /// <param name="type">The type of noise to generate. Default is Perlin.</param>
                /// <param name="seed">The seed for random generation. Default is 1.</param>
                /// <param name="width">The width of the noise map. Default is 256.</param>
                /// <param name="height">The height of the noise map. Default is 256.</param>
                /// <param name="scale">The scale of the noise. Determines how zoomed in or out the noise appears. Default is 1.0f.</param>
                /// <param name="octaves">The number of octaves for fractal noise. More octaves add finer detail but increase computation time. Default is 1.</param>
                /// <param name="lacunarity">The lacunarity for fractal noise. Determines the frequency gap between successive octaves. Default is 1.0f.</param>
                /// <param name="gain">The gain for fractal noise. Controls the reduction in amplitude of each successive octave. Default is 0.5f.</param>
                /// <param name="persistence">The persistence for some types of noise. Controls the amplitude of successive octaves. Default is 1.0f.</param>
                /// <param name="amplitude">The amplitude for some types of noise. Controls the overall strength of the noise. Default is 1.0f.</param>
                /// <param name="frequency">The frequency for some types of noise. Controls how often the noise pattern repeats. Default is 1.0f.</param>
                /// <param name="bias">The bias for white noise. Adjusts the baseline value of the noise. Default is 0.0f.</param>
                /// <param name="numCells">The number of cells for Worley noise. More cells create a more detailed pattern. Default is 64.</param>
                /// <param name="jitter">The jitter for Worley noise. Controls the randomness of cell points. Default is 1.0f.</param>
                /// <param name="distanceFunction">The distance function for Worley noise. Determines how distances are calculated (e.g., Euclidean). Default is DistanceFunction.Euclidean.</param>
                /// <param name="numberOfFeatures">The number of features for Worley noise. Determines how many features are considered for each point. Default is 1.</param>
                /// <param name="orientation">The orientation for Gabor noise. Controls the direction of the Gabor patterns. Default is 0.0f.</param>
                /// <param name="aspectRatio">The aspect ratio for Gabor noise. Controls the width-to-height ratio of the Gabor patterns. Default is 1.0f.</param>
                /// <param name="phase">The phase for Gabor noise. Controls the phase shift of the Gabor patterns. Default is 0.0f.</param>
                /// <param name="kernelSize">The kernel size for sparse convolution noise. Determines the size of the convolution kernel. Default is 3.</param>
                /// <param name="offset">The offset for gradient noise. Shifts the noise pattern by the given amount. Default is float2(0.0f, 0.0f).</param>
                /// <returns>An array of floats representing the generated noise map.</returns>
                public static float[] GetNoise(NoiseType type = NoiseType.Perlin, int seed = 1, int width = 256, int height = 256, float scale = 1.0f, int octaves = 1, float lacunarity = 1.0f,
                                float gain = 0.5f, float persistence = 1.0f, float amplitude = 1.0f, float frequency = 1.0f, float bias = 0.0f, int numCells = 64, float jitter = 1.0f,
                                DistanceFunction distanceFunction = DistanceFunction.Euclidean, int numberOfFeatures = 1, float orientation = 0.0f, float aspectRatio = 1.0f, float phase = 0.0f,
                                int kernelSize = 3, float2 offset = default)
                {
                        switch (type)
                        {
                                case NoiseType.Perlin:
                                        return PerlinNoise.Generate2D(width, height, scale, seed, offset, octaves, persistence, lacunarity);

                                case NoiseType.PerlinFractal:
                                        var perlinFractal = new PerlinFractalNoise(seed, scale, width, height, lacunarity, gain, octaves);
                                        return perlinFractal.Generate();

                                case NoiseType.Worley:
                                        var worley = new WorleyNoise(seed, width, height, numCells, scale, jitter, distanceFunction, numberOfFeatures);
                                        return worley.Generate();

                                case NoiseType.Simplex:
                                        var simplex = new SimplexNoise(seed, scale, width, height);
                                        return simplex.Generate();

                                case NoiseType.SimplexFractal:
                                        var simplexFractal = new SimplexFractalNoise(seed, scale, width, height, octaves, lacunarity, persistence, amplitude, frequency);
                                        return simplexFractal.Generate();

                                case NoiseType.White:
                                        var white = new WhiteNoise(seed, width, height, amplitude, bias);
                                        return white.Generate();

                                case NoiseType.Value:
                                        var value = new ValueNoise(seed, scale, width, height, octaves, lacunarity, persistence);
                                        return value.Generate();

                                case NoiseType.Wavelet:
                                        var wavelet = new WaveletNoise(seed, scale, width, height, octaves, lacunarity, persistence);
                                        return wavelet.Generate();

                                case NoiseType.Fractal:
                                        var fractal = new FractalNoise(seed, scale, width, height, octaves, lacunarity, persistence);
                                        return fractal.Generate();

                                case NoiseType.Gradient:
                                        var gradient = new GradientNoise(seed, scale, width, height, amplitude, frequency, offset);
                                        return gradient.Generate();

                                case NoiseType.SparseConvolution:
                                        var sparse = new SparseConvolutionNoise(seed, scale, width, height, kernelSize);
                                        return sparse.Generate();

                                case NoiseType.Gabor:
                                        var gabor = new GaborNoise(seed, scale, width, height, frequency, orientation, aspectRatio, phase, amplitude);
                                        return gabor.Generate();

                                default:
                                        throw new System.ArgumentException("Unsupported noise type");
                        }
                }

                /// <summary>
                /// Combines multiple noise arrays using the specified combine method.
                /// </summary>
                /// <param name="method">The method to use for combining the noise arrays.</param>
                /// <param name="noiseArrays">The noise arrays to combine.</param>
                /// <returns>An array of floats representing the combined noise.</returns>
                public static float[] CombineNoise(CombineMethod method, float[][] noiseArrays)
                {
                        int noiseCount = noiseArrays.Length;
                        int noiseLength = noiseArrays[0].Length;
                        var noiseInputs = new NativeArray<float>(noiseCount * noiseLength, Allocator.TempJob);
                        var combinedNoise = new NativeArray<float>(noiseLength, Allocator.TempJob);

                        for (int i = 0; i < noiseCount; i++)
                        {
                                NativeArray<float>.Copy(noiseArrays[i], 0, noiseInputs, i * noiseLength, noiseLength);
                        }

                        var job = new CombineNoiseJob
                        {
                                        NoiseInputs = noiseInputs,
                                        CombinedNoise = combinedNoise,
                                        NoiseCount = noiseCount,
                                        NoiseLength = noiseLength,
                                        Method = method
                        };

                        JobHandle handle = job.Schedule(noiseLength, 64);
                        handle.Complete();

                        float[] result = combinedNoise.ToArray();

                        noiseInputs.Dispose();
                        combinedNoise.Dispose();

                        return result;
                }

                [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
                private struct CombineNoiseJob : IJobParallelFor
                {
                        [ReadOnly] public NativeArray<float> NoiseInputs;
                        public NativeArray<float> CombinedNoise;
                        public int NoiseCount;
                        public int NoiseLength;
                        public CombineMethod Method;

                        public void Execute(int index)
                        {
                                float combinedValue = NoiseInputs[index];
                                for (int i = 1; i < NoiseCount; i++)
                                {
                                        float value = NoiseInputs[i * NoiseLength + index];
                                        switch (Method)
                                        {
                                                case CombineMethod.Add:
                                                        combinedValue += value;
                                                        break;
                                                case CombineMethod.Subtract:
                                                        combinedValue -= value;
                                                        break;
                                                case CombineMethod.Multiply:
                                                        combinedValue *= value;
                                                        break;
                                                case CombineMethod.Average:
                                                        combinedValue += value;
                                                        break;
                                        }
                                }

                                if (Method == CombineMethod.Average)
                                {
                                        combinedValue /= NoiseCount;
                                }

                                CombinedNoise[index] = combinedValue;
                        }
                }
        }
}