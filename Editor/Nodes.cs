using UniNoise.Noises;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace UniNoise.Editor
{
    public sealed class Nodes : MonoBehaviour
    {
        public abstract class Node
        {
            public Rect Rect;
            protected string Title;
            private bool _isDragged;
            private bool _isSelected;
            protected Texture2D PreviewTexture;

            public abstract void Draw();
            public abstract void Process();

            public virtual void DrawConnections()
            {
                Rect inputPortRect = new Rect(Rect.x - 10, Rect.y + Rect.height * 0.5f - 5, 10, 10);
                Rect outputPortRect = new Rect(Rect.x + Rect.width, Rect.y + Rect.height * 0.5f - 5, 10, 10);

                EditorGUI.DrawRect(inputPortRect, Color.black);
                EditorGUI.DrawRect(outputPortRect, Color.black);
            }

            public virtual bool ProcessEvents(Event e)
            {
                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (e.button == 0)
                        {
                            if (Rect.Contains(e.mousePosition))
                            {
                                _isDragged = true;
                                GUI.changed = true;
                                _isSelected = true;
                            }
                            else
                            {
                                GUI.changed = true;
                                _isSelected = false;
                            }
                        }
                        if (e.button == 1 && _isSelected && Rect.Contains(e.mousePosition))
                        {
                            ProcessContextMenu();
                            e.Use();
                        }
                        break;

                    case EventType.MouseUp:
                        _isDragged = false;
                        break;

                    case EventType.MouseDrag:
                        if (e.button == 0 && _isDragged)
                        {
                            Drag(e.delta);
                            e.Use();
                            return true;
                        }
                        break;
                }

                return false;
            }

            private void Drag(Vector2 delta)
            {
                Rect.position += delta;
            }

            private void ProcessContextMenu()
            {
                var genericMenu = new GenericMenu();
                genericMenu.AddItem(new GUIContent("Remove Node"), false, OnClickRemoveNode);
                genericMenu.ShowAsContext();
            }

            private void OnClickRemoveNode()
            {
                // Handle node removal logic here
            }
        }

        public sealed class PerlinNoiseNode : Node
        {
            private float _scale = 1.0f;
            private float _lacunarity = 2.0f;
            private float _gain = 0.5f;
            private int _octaves = 4;

            public PerlinNoiseNode()
            {
                Title = "Perlin Noise";
                Rect = new Rect(10, 10, 200, 200);
            }

            public override void Draw()
            {
                GUILayout.BeginArea(Rect, Title, GUI.skin.window);
                _scale = EditorGUILayout.FloatField("Scale", _scale);
                _lacunarity = EditorGUILayout.FloatField("Lacunarity", _lacunarity);
                _gain = EditorGUILayout.FloatField("Gain", _gain);
                _octaves = EditorGUILayout.IntField("Octaves", _octaves);
                GUILayout.Box(PreviewTexture, GUILayout.Width(180), GUILayout.Height(80));
                GUILayout.EndArea();
            }

            public override void Process()
            {
                GeneratePreviewTexture();
            }

            private void GeneratePreviewTexture()
            {
                int width = 180;
                int height = 80;
                PreviewTexture = new Texture2D(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float value = Mathf.PerlinNoise(x * _scale / width, y * _scale / height);
                        PreviewTexture.SetPixel(x, y, new Color(value, value, value));
                    }
                }
                PreviewTexture.Apply();
            }
        }

        public sealed class WorleyNoiseNode : Node
        {
            private int _numCells = 64;
            private float _scale = 1.0f;
            private float _jitter = 1.0f;
            private DistanceFunction _distanceFunction = DistanceFunction.Euclidean;
            private int _numberOfFeatures = 1;

            public WorleyNoiseNode()
            {
                Title = "Worley Noise";
                Rect = new Rect(10, 220, 200, 250);
            }

            public override void Draw()
            {
                GUILayout.BeginArea(Rect, Title, GUI.skin.window);
                _numCells = EditorGUILayout.IntField("Num Cells", _numCells);
                _scale = EditorGUILayout.FloatField("Scale", _scale);
                _jitter = EditorGUILayout.FloatField("Jitter", _jitter);
                _distanceFunction = (DistanceFunction)EditorGUILayout.EnumPopup("Distance Function", _distanceFunction);
                _numberOfFeatures = EditorGUILayout.IntField("Number of Features", _numberOfFeatures);
                GUILayout.Box(PreviewTexture, GUILayout.Width(180), GUILayout.Height(80));
                GUILayout.EndArea();
            }

            public override void Process()
            {
                GeneratePreviewTexture();
            }

            private void GeneratePreviewTexture()
            {
                int width = 180;
                int height = 80;
                PreviewTexture = new Texture2D(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float value = Mathf.PerlinNoise(x * _scale / width, y * _scale / height);
                        PreviewTexture.SetPixel(x, y, new Color(value, value, value));
                    }
                }
                PreviewTexture.Apply();
            }
        }

        public sealed class SimplexNoiseNode : Node
        {
            private float _scale = 1.0f;
            private int _octaves = 4;
            private float _lacunarity = 2.0f;
            private float _gain = 0.5f;
            private float _amplitude = 1.0f;
            private float _frequency = 1.0f;

            public SimplexNoiseNode()
            {
                Title = "Simplex Noise";
                Rect = new Rect(10, 480, 200, 250);
            }

            public override void Draw()
            {
                GUILayout.BeginArea(Rect, Title, GUI.skin.window);
                _scale = EditorGUILayout.FloatField("Scale", _scale);
                _octaves = EditorGUILayout.IntField("Octaves", _octaves);
                _lacunarity = EditorGUILayout.FloatField("Lacunarity", _lacunarity);
                _gain = EditorGUILayout.FloatField("Gain", _gain);
                _amplitude = EditorGUILayout.FloatField("Amplitude", _amplitude);
                _frequency = EditorGUILayout.FloatField("Frequency", _frequency);
                GUILayout.Box(PreviewTexture, GUILayout.Width(180), GUILayout.Height(80));
                GUILayout.EndArea();
            }

            public override void Process()
            {
                GeneratePreviewTexture();
            }

            private void GeneratePreviewTexture()
            {
                int width = 180;
                int height = 80;
                PreviewTexture = new Texture2D(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float value = Mathf.PerlinNoise(x * _scale / width, y * _scale / height);
                        PreviewTexture.SetPixel(x, y, new Color(value, value, value));
                    }
                }
                PreviewTexture.Apply();
            }
        }

        public sealed class WhiteNoiseNode : Node
        {
            private float _amplitude = 1.0f;
            private float _bias;
            private Texture2D _previewTexture;

            public WhiteNoiseNode()
            {
                Title = "White Noise";
                Rect = new Rect(10, 740, 200, 200);
            }

            public override void Draw()
            {
                GUILayout.BeginArea(Rect, Title, GUI.skin.window);
                _amplitude = EditorGUILayout.FloatField("Amplitude", _amplitude);
                _bias = EditorGUILayout.FloatField("Bias", _bias);
                GUILayout.Box(_previewTexture, GUILayout.Width(180), GUILayout.Height(80));
                GUILayout.EndArea();
            }

            public override void Process()
            {
                GeneratePreviewTexture();
            }

            private void GeneratePreviewTexture()
            {
                int width = 180;
                int height = 80;
                _previewTexture = new Texture2D(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float value = UnityEngine.Random.value * _amplitude + _bias;
                        _previewTexture.SetPixel(x, y, new Color(value, value, value));
                    }
                }
                _previewTexture.Apply();
            }
        }

        public sealed class ValueNoiseNode : Node
        {
            private float _scale = 1.0f;
            private int _octaves = 4;
            private float _lacunarity = 2.0f;
            private float _gain = 0.5f;
            private Texture2D _previewTexture;

            public ValueNoiseNode()
            {
                Title = "Value Noise";
                Rect = new Rect(10, 950, 200, 200);
            }

            public override void Draw()
            {
                GUILayout.BeginArea(Rect, Title, GUI.skin.window);
                _scale = EditorGUILayout.FloatField("Scale", _scale);
                _octaves = EditorGUILayout.IntField("Octaves", _octaves);
                _lacunarity = EditorGUILayout.FloatField("Lacunarity", _lacunarity);
                _gain = EditorGUILayout.FloatField("Gain", _gain);
                GUILayout.Box(_previewTexture, GUILayout.Width(180), GUILayout.Height(80));
                GUILayout.EndArea();
            }

            public override void Process()
            {
                GeneratePreviewTexture();
            }

            private void GeneratePreviewTexture()
            {
                int width = 180;
                int height = 80;
                _previewTexture = new Texture2D(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float value = Mathf.PerlinNoise(x * _scale / width, y * _scale / height);
                        _previewTexture.SetPixel(x, y, new Color(value, value, value));
                    }
                }
                _previewTexture.Apply();
            }
        }

        public sealed class WaveletNoiseNode : Node
        {
            private float _scale = 1.0f;
            private int _octaves = 4;
            private float _lacunarity = 2.0f;
            private float _gain = 0.5f;
            private Texture2D _previewTexture;

            public WaveletNoiseNode()
            {
                Title = "Wavelet Noise";
                Rect = new Rect(10, 1160, 200, 200);
            }

            public override void Draw()
            {
                GUILayout.BeginArea(Rect, Title, GUI.skin.window);
                _scale = EditorGUILayout.FloatField("Scale", _scale);
                _octaves = EditorGUILayout.IntField("Octaves", _octaves);
                _lacunarity = EditorGUILayout.FloatField("Lacunarity", _lacunarity);
                _gain = EditorGUILayout.FloatField("Gain", _gain);
                GUILayout.Box(_previewTexture, GUILayout.Width(180), GUILayout.Height(80));
                GUILayout.EndArea();
            }

            public override void Process()
            {
                GeneratePreviewTexture();
            }

            private void GeneratePreviewTexture()
            {
                int width = 180;
                int height = 80;
                _previewTexture = new Texture2D(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float value = Mathf.PerlinNoise(x * _scale / width, y * _scale / height);
                        _previewTexture.SetPixel(x, y, new Color(value, value, value));
                    }
                }
                _previewTexture.Apply();
            }
        }

        public sealed class FractalNoiseNode : Node
        {
            private float _scale = 1.0f;
            private int _octaves = 4;
            private float _lacunarity = 2.0f;
            private float _gain = 0.5f;
            private Texture2D _previewTexture;

            public FractalNoiseNode()
            {
                Title = "Fractal Noise";
                Rect = new Rect(10, 1370, 200, 200);
            }

            public override void Draw()
            {
                GUILayout.BeginArea(Rect, Title, GUI.skin.window);
                _scale = EditorGUILayout.FloatField("Scale", _scale);
                _octaves = EditorGUILayout.IntField("Octaves", _octaves);
                _lacunarity = EditorGUILayout.FloatField("Lacunarity", _lacunarity);
                _gain = EditorGUILayout.FloatField("Gain", _gain);
                GUILayout.Box(_previewTexture, GUILayout.Width(180), GUILayout.Height(80));
                GUILayout.EndArea();
            }

            public override void Process()
            {
                GeneratePreviewTexture();
            }

            private void GeneratePreviewTexture()
            {
                int width = 180;
                int height = 80;
                _previewTexture = new Texture2D(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float value = Mathf.PerlinNoise(x * _scale / width, y * _scale / height);
                        _previewTexture.SetPixel(x, y, new Color(value, value, value));
                    }
                }
                _previewTexture.Apply();
            }
        }

        public sealed class GaborNoiseNode : Node
        {
            private float _scale = 1.0f;
            private float _frequency = 1.0f;
            private float _orientation;
            private float _aspectRatio = 1.0f;
            private float _phase;
            private float _amplitude = 1.0f;
            private Texture2D _previewTexture;

            public GaborNoiseNode()
            {
                Title = "Gabor Noise";
                Rect = new Rect(10, 1580, 200, 250);
            }

            public override void Draw()
            {
                GUILayout.BeginArea(Rect, Title, GUI.skin.window);
                _scale = EditorGUILayout.FloatField("Scale", _scale);
                _frequency = EditorGUILayout.FloatField("Frequency", _frequency);
                _orientation = EditorGUILayout.FloatField("Orientation", _orientation);
                _aspectRatio = EditorGUILayout.FloatField("Aspect Ratio", _aspectRatio);
                _phase = EditorGUILayout.FloatField("Phase", _phase);
                _amplitude = EditorGUILayout.FloatField("Amplitude", _amplitude);
                GUILayout.Box(_previewTexture, GUILayout.Width(180), GUILayout.Height(80));
                GUILayout.EndArea();
            }

            public override void Process()
            {
                GeneratePreviewTexture();
            }

            private void GeneratePreviewTexture()
            {
                int width = 180;
                int height = 80;
                _previewTexture = new Texture2D(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float value = Mathf.PerlinNoise(x * _scale / width, y * _scale / height);
                        _previewTexture.SetPixel(x, y, new Color(value, value, value));
                    }
                }
                _previewTexture.Apply();
            }
        }

        public sealed class SparseConvolutionNoiseNode : Node
        {
            private float _scale = 1.0f;
            private int _kernelSize = 3;
            private Texture2D _previewTexture;

            public SparseConvolutionNoiseNode()
            {
                Title = "Sparse Convolution Noise";
                Rect = new Rect(10, 1850, 200, 200);
            }

            public override void Draw()
            {
                GUILayout.BeginArea(Rect, Title, GUI.skin.window);
                _scale = EditorGUILayout.FloatField("Scale", _scale);
                _kernelSize = EditorGUILayout.IntField("Kernel Size", _kernelSize);
                GUILayout.Box(_previewTexture, GUILayout.Width(180), GUILayout.Height(80));
                GUILayout.EndArea();
            }

            public override void Process()
            {
                GeneratePreviewTexture();
            }

            private void GeneratePreviewTexture()
            {
                int width = 180;
                int height = 80;
                _previewTexture = new Texture2D(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float value = Mathf.PerlinNoise(x * _scale / width, y * _scale / height);
                        _previewTexture.SetPixel(x, y, new Color(value, value, value));
                    }
                }
                _previewTexture.Apply();
            }
        }

        public sealed class GradientNoiseNode : Node
        {
            private float _scale = 1.0f;
            private float _amplitude = 1.0f;
            private float _frequency = 1.0f;
            private float2 _offset = new float2(0.0f, 0.0f);
            private Texture2D _previewTexture;

            public GradientNoiseNode()
            {
                Title = "Gradient Noise";
                Rect = new Rect(10, 2050, 200, 200);
            }

            public override void Draw()
            {
                GUILayout.BeginArea(Rect, Title, GUI.skin.window);
                _scale = EditorGUILayout.FloatField("Scale", _scale);
                _amplitude = EditorGUILayout.FloatField("Amplitude", _amplitude);
                _frequency = EditorGUILayout.FloatField("Frequency", _frequency);
                _offset = EditorGUILayout.Vector2Field("Offset", _offset);
                GUILayout.Box(_previewTexture, GUILayout.Width(180), GUILayout.Height(80));
                GUILayout.EndArea();
            }

            public override void Process()
            {
                GeneratePreviewTexture();
            }

            private void GeneratePreviewTexture()
            {
                int width = 180;
                int height = 80;
                _previewTexture = new Texture2D(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float value = Mathf.PerlinNoise(x * _scale / width, y * _scale / height);
                        _previewTexture.SetPixel(x, y, new Color(value, value, value));
                    }
                }
                _previewTexture.Apply();
            }
        }
    }
}
