using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniNoise.Editor
{
    public sealed class NodeBasedEditor : EditorWindow
    {
        private List<Nodes.Node> _nodes;
        private List<Connection> _connections;

        private Nodes.Node _selectedInNode;
        private Nodes.Node _selectedOutNode;

        [MenuItem("UniNoise/Node Editor")]
        public static void ShowWindow()
        {
            GetWindow<NodeBasedEditor>("Node Editor");
        }

        private void OnEnable()
        {
            _nodes = new List<Nodes.Node>();
            _connections = new List<Connection>();
        }

        private void OnGUI()
        {
            DrawGrid(20, 0.2f, Color.gray);
            DrawGrid(100, 0.4f, Color.gray);

            DrawToolbar();
            DrawNodes();
            DrawConnections();
            ProcessNodeEvents(Event.current);
        }

        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(gridSpacing * i, 0, 0), new Vector3(gridSpacing * i, position.height, 0f));
            }

            for (int j = 0; j < heightDivs; j++)
            {
                Handles.DrawLine(new Vector3(0, gridSpacing * j, 0), new Vector3(position.width, gridSpacing * j, 0f));
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Add Perlin Noise", EditorStyles.toolbarButton))
            {
                _nodes.Add(new Nodes.PerlinNoiseNode());
            }
            if (GUILayout.Button("Add Worley Noise", EditorStyles.toolbarButton))
            {
                _nodes.Add(new Nodes.WorleyNoiseNode());
            }
            if (GUILayout.Button("Add Simplex Noise", EditorStyles.toolbarButton))
            {
                _nodes.Add(new Nodes.SimplexNoiseNode());
            }
            if (GUILayout.Button("Add White Noise", EditorStyles.toolbarButton))
            {
                _nodes.Add(new Nodes.WhiteNoiseNode());
            }
            if (GUILayout.Button("Add Value Noise", EditorStyles.toolbarButton))
            {
                _nodes.Add(new Nodes.ValueNoiseNode());
            }
            if (GUILayout.Button("Add Wavelet Noise", EditorStyles.toolbarButton))
            {
                _nodes.Add(new Nodes.WaveletNoiseNode());
            }
            if (GUILayout.Button("Add Fractal Noise", EditorStyles.toolbarButton))
            {
                _nodes.Add(new Nodes.FractalNoiseNode());
            }
            if (GUILayout.Button("Add Gabor Noise", EditorStyles.toolbarButton))
            {
                _nodes.Add(new Nodes.GaborNoiseNode());
            }
            if (GUILayout.Button("Add Sparse Convolution Noise", EditorStyles.toolbarButton))
            {
                _nodes.Add(new Nodes.SparseConvolutionNoiseNode());
            }
            if (GUILayout.Button("Add Gradient Noise", EditorStyles.toolbarButton))
            {
                _nodes.Add(new Nodes.GradientNoiseNode());
            }
            GUILayout.EndHorizontal();
        }

        private void DrawNodes()
        {
            foreach (var node in _nodes)
            {
                node.Draw();
                node.DrawConnections();
            }
        }

        private void DrawConnections()
        {
            foreach (var connection in _connections)
            {
                connection.Draw();
            }
        }

        private void ProcessNodeEvents(Event e)
        {
            foreach (var node in _nodes)
            {
                if (node.ProcessEvents(e))
                {
                    if (_selectedInNode == null)
                    {
                        _selectedInNode = node;
                    }
                    else if (_selectedOutNode == null)
                    {
                        _selectedOutNode = node;
                        CreateConnection();
                    }
                    else
                    {
                        _selectedInNode = node;
                        _selectedOutNode = null;
                    }
                }
            }
        }

        private void CreateConnection()
        {
            if (_selectedInNode != null && _selectedOutNode != null)
            {
                var connection = new Connection
                {
                    InNode = _selectedInNode,
                    OutNode = _selectedOutNode
                };
                _connections.Add(connection);
                _selectedInNode = null;
                _selectedOutNode = null;
            }
        }

        private sealed class Connection
        {
            public Nodes.Node InNode;
            public Nodes.Node OutNode;

            public void Draw()
            {
                Handles.DrawLine(InNode.Rect.center, OutNode.Rect.center);
            }
        }
    }
}
