# UniNoise

UniNoise is a high performance noise generation library for Unity that includes multiple types of basic and fractal noises, as well as methods to combine different noises. The library leverages Burst and Jobs for optimal performance and thus those packages are required.

**This asset is still early in development.**

## Supported Noise Types

- **Perlin**: A gradient noise function known for its smoothness and coherence.
- **PerlinFractal**: An extension of Perlin noise that adds multiple layers of detail.
- **Worley**: Also known as Voronoi or cellular noise, often used for generating cell-like patterns.
- **Simplex**: An alternative to Perlin noise that reduces directional artifacts.
- **SimplexFractal**: An extension of Simplex noise that adds multiple layers of detail.
- **White**: A random noise function with no coherence or smoothness.
- **Value**: A smooth noise function where values at grid points are interpolated.
- **Wavelet**: A noise function that uses wavelet basis functions for generating smooth noise.
- **Fractal**: A combination of multiple noise functions to create complex patterns.
- **Gradient**: A noise function that uses gradients at grid points to generate smooth transitions.
- **SparseConvolution**: A noise function that uses sparse convolution kernels for generating patterns.
- **Gabor**: A noise function that uses Gabor kernels for generating patterns.

## How It Works

### Noise Generation

To generate noise, use the `GetNoise` method from the `Noise` static class. You can specify the parameters individually or use the `NoiseConfiguration` class.

- **NoiseConfiguration**: A class to define noise parameters for generating noise.
- **CombinedNoiseConfiguration**: A class to define multiple noise configurations and a method to combine them.

### Combining Noises

To combine multiple noise arrays, use the `CombineNoise` method from the `Noise` class. The available combination methods are `Add`, `Subtract`, `Average`, and `Multiply`.

### Example in the Inspector

The `NoiseGenerator` script demonstrates how to integrate various types of noise and their parameters in the Unity Inspector.

### Noise Generation Methods

- `GetNoise(NoiseType type, int seed, int width, int height, float scale, int octaves, float lacunarity, float gain, float persistence, float amplitude, float frequency, float bias, int numCells, float jitter, DistanceFunction distanceFunction, int numberOfFeatures, float orientation, float aspectRatio, float phase, int kernelSize, float2 offset)`: Generates noise based on the specified parameters. All parameters are optionnal and have default values.

- `GetNoise(NoiseConfiguration config)`: Generates noise based on the specified noise configuration. All parameters are optionnal and have default values.

### Noise Combination Method

- `CombineNoise(CombineMethod method, float[][] noiseArrays)`: Combines multiple noise arrays using the specified combine method.

## Features

- **High Performance**: Utilizes Burst and Jobs for efficient noise generation.
- **Multiple Noise Types**: Supports various types of noise for different applications.
- **Flexible Combination**: Easily combine multiple noises using different methods.
- **Inspector Integration**: Demonstrates how to integrate noise generation in the Unity Inspector.

## Future Development

Currently working on a node-based editor for more intuitive noise configuration and combination.
Some algorithms will be reworked (especially Perlin noise that will implement the "Improved" one and not the gradient one)

## How to Use

1. Reference the `Noise` class to generate different types of noise.
2. Use `NoiseConfiguration` to define your noise parameters.
3. Combine multiple noises using the `CombineNoise` method with your desired `CombineMethod`.

Explore the `NoiseGenerator` script to see an example of how to use these features within the Unity Inspector.

## Contact

For issues or contributions, please open an issue or a pull request on GitHub.
