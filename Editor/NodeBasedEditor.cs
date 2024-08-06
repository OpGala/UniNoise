using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniNoise.Editor
{
      public sealed class NodeBasedEditor : EditorWindow
      {
            private List<Connection> _connections;
            private List<Nodes.Node> _nodes;

            private Nodes.Node _selectedInNode;
            private Nodes.Node _selectedOutNode;

            private void OnEnable()
            {
                  this._nodes = new List<Nodes.Node>();
                  this._connections = new List<Connection>();
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

            [MenuItem("UniNoise/Node Editor")]
            public static void ShowWindow()
            {
                  GetWindow<NodeBasedEditor>("Node Editor");
            }

            private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
            {
                  int widthDivs = Mathf.CeilToInt(this.position.width / gridSpacing);
                  int heightDivs = Mathf.CeilToInt(this.position.height / gridSpacing);

                  Handles.BeginGUI();
                  Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

                  for (int i = 0; i < widthDivs; i++) Handles.DrawLine(new Vector3(gridSpacing * i, 0, 0), new Vector3(gridSpacing * i, this.position.height, 0f));

                  for (int j = 0; j < heightDivs; j++) Handles.DrawLine(new Vector3(0, gridSpacing * j, 0), new Vector3(this.position.width, gridSpacing * j, 0f));

                  Handles.color = Color.white;
                  Handles.EndGUI();
            }

            private void DrawToolbar()
            {
                  GUILayout.BeginHorizontal(EditorStyles.toolbar);
                  if (GUILayout.Button("Add Perlin Noise", EditorStyles.toolbarButton)) this._nodes.Add(new Nodes.PerlinNoiseNode());

                  if (GUILayout.Button("Add Worley Noise", EditorStyles.toolbarButton)) this._nodes.Add(new Nodes.WorleyNoiseNode());

                  if (GUILayout.Button("Add Simplex Noise", EditorStyles.toolbarButton)) this._nodes.Add(new Nodes.SimplexNoiseNode());

                  if (GUILayout.Button("Add White Noise", EditorStyles.toolbarButton)) this._nodes.Add(new Nodes.WhiteNoiseNode());

                  if (GUILayout.Button("Add Value Noise", EditorStyles.toolbarButton)) this._nodes.Add(new Nodes.ValueNoiseNode());

                  if (GUILayout.Button("Add Wavelet Noise", EditorStyles.toolbarButton)) this._nodes.Add(new Nodes.WaveletNoiseNode());

                  if (GUILayout.Button("Add Fractal Noise", EditorStyles.toolbarButton)) this._nodes.Add(new Nodes.FractalNoiseNode());

                  if (GUILayout.Button("Add Gabor Noise", EditorStyles.toolbarButton)) this._nodes.Add(new Nodes.GaborNoiseNode());

                  if (GUILayout.Button("Add Sparse Convolution Noise", EditorStyles.toolbarButton)) this._nodes.Add(new Nodes.SparseConvolutionNoiseNode());

                  if (GUILayout.Button("Add Gradient Noise", EditorStyles.toolbarButton)) this._nodes.Add(new Nodes.GradientNoiseNode());

                  GUILayout.EndHorizontal();
            }

            private void DrawNodes()
            {
                  foreach (Nodes.Node node in this._nodes)
                  {
                        node.Draw();
                        node.DrawConnections();
                  }
            }

            private void DrawConnections()
            {
                  foreach (Connection connection in this._connections) connection.Draw();
            }

            private void ProcessNodeEvents(Event e)
            {
                  foreach (Nodes.Node node in this._nodes)
                  {
                        if (node.ProcessEvents(e))
                        {
                              if (this._selectedInNode == null)
                                    this._selectedInNode = node;
                              else if (this._selectedOutNode == null)
                              {
                                    this._selectedOutNode = node;
                                    CreateConnection();
                              }
                              else
                              {
                                    this._selectedInNode = node;
                                    this._selectedOutNode = null;
                              }
                        }
                  }
            }

            private void CreateConnection()
            {
                  if (this._selectedInNode != null && this._selectedOutNode != null)
                  {
                        var connection = new Connection
                        {
                              InNode = this._selectedInNode,
                              OutNode = this._selectedOutNode
                        };
                        this._connections.Add(connection);
                        this._selectedInNode = null;
                        this._selectedOutNode = null;
                  }
            }

            private sealed class Connection
            {
                  public Nodes.Node InNode;
                  public Nodes.Node OutNode;

                  public void Draw()
                  {
                        Handles.DrawLine(this.InNode.Rect.center, this.OutNode.Rect.center);
                  }
            }
      }
}