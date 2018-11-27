using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;

namespace QJBT.Editor
{
    public class EditorNode
    {
        private Rect rect;

        //public GUIStyle style;
        //Vector2 _position;

        float _width = 150;
        float _height = 50;
        bool _isSelected = false;
        //bool _isDragged = false;
        public static readonly GUIStyle _defaultStyle;
        public static readonly GUIStyle _selectedStyle;
        public static readonly GUIStyle _runningStyle;
        Node _node;

        static EditorNode()
        {
            _defaultStyle = new GUIStyle();
            _defaultStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
            _defaultStyle.border = new RectOffset(12, 12, 12, 12);
            _defaultStyle.alignment = TextAnchor.MiddleCenter;
            _defaultStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f);
            _defaultStyle.clipping = TextClipping.Clip;

            _selectedStyle = new GUIStyle();
            _selectedStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
            _selectedStyle.border = new RectOffset(12, 12, 12, 12);
            _selectedStyle.alignment = TextAnchor.MiddleCenter;
            _selectedStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f);
            _selectedStyle.clipping = TextClipping.Clip;

            _runningStyle = new GUIStyle();
            _runningStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node2 on.png") as Texture2D;
            _runningStyle.border = new RectOffset(12, 12, 12, 12);
            _runningStyle.alignment = TextAnchor.MiddleCenter;
            _runningStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f);
            _runningStyle.clipping = TextClipping.Clip;
        }

        public EditorNode(Node node)
        {
            _node = node;
            Style = _defaultStyle;
            rect = new Rect(Node.EditorPosition.x, Node.EditorPosition.y, _width, _height);
        }

        public Node Node
        {
            get
            {
                return _node;
            }
        }


        public Vector2 Position
        {
            get
            {
                return Node.EditorPosition;
            }
            set
            {
                Node.EditorPosition = value;
                rect.position = Node.EditorPosition;
            }
        }

        public Rect Rect
        {
            get
            {
                return rect;
            }
        }

        public GUIStyle Style
        {
            get;
            set;
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                if (_isSelected)
                {
                    Style = _selectedStyle;
                }
                else
                {
                    Style = _defaultStyle;
                }
            }
        }

        public void Drag(Vector2 delta)
        {
            Position += delta;
        }

        public void Draw(Matrix4x4 matrix)
        {
            GUI.Box(rect.Transform(matrix), Node.Name, Node.IsActive ? GetNodeStyle() : Style);
        }

        GUIStyle GetNodeStyle()
        {
            if (Node.ActiveStatus == Status.Success)
            {
                return BehaviourTreeEditor._greenStyle;
            }
            else if (Node.ActiveStatus == Status.Failure)
            {
                return BehaviourTreeEditor._redStyle;
            }
            else
            {
                return _runningStyle;
            }
        }

        public void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    //if (e.button == 0)
                    //{
                    //    if (rect.Contains(e.mousePosition))
                    //    {
                    //        //_isDragged = true;
                    //        GUI.changed = true;
                    //        IsSelected = true;
                    //        e.Use();
                    //    }
                    //    else
                    //    {
                    //        GUI.changed = true;
                    //        IsSelected = false;
                    //    }
                    //}

                    if (e.button == 1 && rect.Contains(e.mousePosition))
                    {
                        IsSelected = true;
                        GUI.changed = true;
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0 && _isSelected)
                    {
                        Node.EditorPositionChanged();
                    }
                    else if (e.button == 1 && rect.Contains(e.mousePosition))
                    {
                        ProcessContextMenu(e.mousePosition);
                        e.Use();
                    }
                    //_isDragged = false;
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && _isSelected)
                    {
                        Drag(e.delta);
                        //e.Use();
                        GUI.changed = true;
                    }
                    break;
            }

        }

        void ProcessContextMenu(Vector2 mousePos)
        {
            GenericMenu genericMenu = new GenericMenu();
            foreach (var pair in NodeFactory.RegisteredNodes)
            {
                if (BehaviourTreeEditor.Instance)
                {
                    genericMenu.AddItem(new GUIContent("Add/" + pair.Value), false, () => { BehaviourTreeEditor.Instance.CreateNewNode(pair.Key, Node); });
                }
            }

            if (BehaviourTreeEditor.Instance && BehaviourTreeEditor.Instance.GetParent(Node) != null)
            {
                genericMenu.AddItem(new GUIContent("Delete"), false, () => { BehaviourTreeEditor.Instance.DeleteNode(Node); });
                genericMenu.AddItem(new GUIContent("Set New Parent"), false, () => { BehaviourTreeEditor.Instance.IsSettingNewParent = true; });
            }
            genericMenu.ShowAsContext();
        }
    }

    public class Connection
    {
        public EditorNode _start;
        public EditorNode _end;

        public Connection(EditorNode start, EditorNode end)
        {
            _start = start;
            _end = end;
        }

        public void Draw(Matrix4x4 matrix)
        {
            var point0 = _start.Rect.center.Transform(matrix);
            var point4 = _end.Rect.center.Transform(matrix);
            var point1 = (_start.Rect.center + Vector2.up * 50.0f).Transform(matrix);
            var point3 = (_end.Rect.center).Transform(matrix);
            var point2 = new Vector3(point3.x, point1.y);
            Handles.DrawPolyLine(
                point0,
                point1,
                point2,
                point3,
                point4);

        }

    }



    public class BehaviourTreeEditor : EditorWindow
    {

        Dictionary<EditorNode, List<EditorNode>> _editorNodesHierarchy = new Dictionary<EditorNode, List<EditorNode>>();

        List<Connection> _connections = new List<Connection>();

        //public static Dictionary<string, string> _registeredNodeDisplayNames = new Dictionary<string, string>();

        public static GUIStyle _greenStyle;

        public static GUIStyle _redStyle;

        public static GUIStyle _textSyle;

        public static GUIStyle _selectionBoxStyle;

        BehaviourTreeController _tree;

        static BehaviourTreeEditor _instance;

        const float kZoomMin = 0.1f;
        const float kZoomMax = 10.0f;

        float _zoom = 1.0f;
        Vector2 _zoomCoordsOrigin = Vector2.zero;
        Rect _selectionBox;
        Vector2 _selectionStart;
        Vector2 _selectionEnd;

        Vector2 _selectionBoxStart;
        Vector2 _selectionBoxEnd;

        bool _isSelecting = false;

        string _treeName = "";

        Rect ZoomArea
        {
            get
            {
                return new Rect(0.0f, 0.0f, position.width, position.height);
            }
        }

        public bool IsSettingNewParent
        {
            get;
            set;
        }

        public void CreateNewNode(string key, Node parent)
        {
            Node newNode = NodeFactory.CreateNode(key, _tree);
            newNode.EditorPosition = parent.EditorPosition + 100.0f * Vector2.up;
            parent.AddChild(newNode);
            UpdateNodesHierarchy();
        }

        public void DeleteNode(Node node)
        {
            _tree.DeleteNode(node);
            UpdateNodesHierarchy();
        }

        public Node GetParent(Node node)
        {
            return _tree.GetParent(node);
        }

        void UpdateNodesHierarchy()
        {
            if (_tree != null)
            {
                _editorNodesHierarchy.Clear();
                _connections.Clear();

                var nodesHierarchy = _tree.GetNodesHierarchy();
                var nodesToEditorNodesMapping = new Dictionary<Node, EditorNode>();

                foreach (var pair in nodesHierarchy)
                {
                    nodesToEditorNodesMapping.Add(pair.Key, new EditorNode(pair.Key));
                }

                foreach (var pair in nodesHierarchy)
                {
                    var parentEditorNode = nodesToEditorNodesMapping[pair.Key];
                    var childEditorNodes = new List<EditorNode>();
                    foreach (var childNode in pair.Value)
                    {
                        childEditorNodes.Add(nodesToEditorNodesMapping[childNode]);
                    }
                    _editorNodesHierarchy.Add(parentEditorNode, childEditorNodes);
                }

                foreach (var pair in _editorNodesHierarchy)
                {
                    if (pair.Value.Count > 0)
                    {
                        foreach (var childNode in pair.Value)
                        {
                            var connection = new Connection(pair.Key, childNode);
                            _connections.Add(connection);
                        }
                    }
                }
                Repaint();
            }
        }

        public static BehaviourTreeEditor Instance   
        {
            get
            {
                if (_instance == null)
                {
                    var windows = Resources.FindObjectsOfTypeAll<BehaviourTreeEditor>();
                    if (windows.Length > 0)
                        _instance = windows[0];
                }
                return _instance;              
            }
        }


        [MenuItem("Window/QJBT Behaviour Tree Editor")]  
        private static void OpenWindow()
        {
            BehaviourTreeEditor window = GetWindow<BehaviourTreeEditor>(typeof(SceneView));
            _instance = window;
            window.titleContent = new GUIContent("Behaviour Tree Editor");
            window.Show();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is GameObject)
            {
                var go = Selection.activeObject as GameObject;
                var bt = go.GetComponent<BehaviourTree>();
                if (bt != null && bt.BehaviourTreeController != null && bt.BehaviourTreeController != _tree)
                {
                    Initialize(bt.BehaviourTreeController);
                }
            }
        }

        void Initialize(BehaviourTreeController tree)
        {
            _tree = tree;
            if (_tree)
            {
                _treeName = tree.name;
                BuildVisualTree();
                _tree.TreeStatusChanged += OnTreeStatusChanged;
                if (_editorNodesHierarchy.Count > 0)
                {
                    Rect? rect = null;
                    foreach (var pair in _editorNodesHierarchy)
                    {
                        if (rect == null)
                        {
                            rect = pair.Key.Rect;
                        }
                        else
                        {
                            rect = rect.Value.Encapsulate(pair.Key.Rect);
                        }
                    }
                    if (_zoom != 0.0f)
                    {
                        _zoomCoordsOrigin = rect.Value.center - new Vector2(ZoomArea.center.x / _zoom, ZoomArea.center.y / _zoom);
                    }
                }
            }
            //_serializedTree = new UnityEditor.SerializedObject(_tree);
        }

        void BuildVisualTree()
        {
            if (_tree != null)
            {
                _editorNodesHierarchy.Clear();
                _connections.Clear();

                var nodesHierarchy = _tree.GetNodesHierarchy();
                var nodesToEditorNodesMapping = new Dictionary<Node, EditorNode>();

                foreach (var pair in nodesHierarchy)
                {
                    nodesToEditorNodesMapping.Add(pair.Key, new EditorNode(pair.Key));
                }

                foreach (var pair in nodesHierarchy)
                {
                    var parentEditorNode = nodesToEditorNodesMapping[pair.Key];
                    var childEditorNodes = new List<EditorNode>();
                    foreach (var childNode in pair.Value)
                    {
                        childEditorNodes.Add(nodesToEditorNodesMapping[childNode]);
                    }
                    _editorNodesHierarchy.Add(parentEditorNode, childEditorNodes);
                }

                foreach (var pair in _editorNodesHierarchy)
                {
                    if (pair.Value.Count > 0)
                    {
                        foreach (var childNode in pair.Value)
                        {
                            var connection = new Connection(pair.Key, childNode);
                            _connections.Add(connection);
                        }
                    }
                }
                Repaint();
            }
        }

        void OnTreeStatusChanged()
        {
            Repaint();
        }


        private void OnEnable()
        {
            //AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            _greenStyle = new GUIStyle();
            _greenStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node3.png") as Texture2D;
            _greenStyle.border = new RectOffset(12, 12, 12, 12);
            _greenStyle.alignment = TextAnchor.MiddleCenter;
            _greenStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f);
            _greenStyle.clipping = TextClipping.Clip;

            _redStyle = new GUIStyle();
            _redStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node6.png") as Texture2D;
            _redStyle.border = new RectOffset(12, 12, 12, 12);
            _redStyle.alignment = TextAnchor.MiddleCenter;
            _redStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f);
            _redStyle.clipping = TextClipping.Clip;

            _textSyle = new GUIStyle();
            _textSyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f);
            _textSyle.clipping = TextClipping.Clip;

            _selectionBoxStyle = new GUIStyle();
            _selectionBoxStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/selectionrect.png") as Texture2D;
        }

        //void OnDisable()
        //{
        //    AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        //}

        //public void OnBeforeAssemblyReload()
        //{
        //    Debug.Log("Before Assembly Reload");
        //}

        [UnityEditor.Callbacks.DidReloadScripts]   
        public static void OnAfterAssemblyReload()
        {
            if (Instance && Instance._tree)
            {
                Instance.BuildVisualTree();
                Instance._tree.TreeStatusChanged += Instance.OnTreeStatusChanged;
            }
        }


        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            if (Selection.activeObject is BehaviourTreeController)
            {
                OpenWindow();
                Instance.Initialize(Selection.activeObject as BehaviourTreeController);

                //Instance.wantsMouseMove = true;
                return true; //catch open file
            }
            return false;
        }

        private void OnGUI()
        {
            //var mat = GUI.matrix

            EditorZoomArea.Begin(_zoom, ZoomArea);

            var e = new Event(Event.current);

            e.mousePosition = e.mousePosition.Transform(Matrix4x4.TRS(_zoomCoordsOrigin, Quaternion.identity, Vector3.one));

            if (IsSettingNewParent)
            {
                var selectedNodes = GetSelectedNodes();
                if (selectedNodes.Count > 0)
                {
                    var matrix = Matrix4x4.TRS(-_zoomCoordsOrigin, Quaternion.identity, Vector3.one);
                    Handles.DrawLine(selectedNodes[0].Rect.center.Transform(matrix), e.mousePosition.Transform(matrix));
                }
            }

            DrawConnections();

            DrawNodes();

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (IsSettingNewParent)
                        {
                            EditorNode clickedNode = null;
                            foreach (var pair in _editorNodesHierarchy)
                            {
                                if (pair.Key.Rect.Contains(e.mousePosition))
                                {
                                    clickedNode = pair.Key;
                                    break;
                                }
                            }

                            if (clickedNode != null)
                            {
                                var selectedNodes = GetSelectedNodes();
                                if (selectedNodes.Count > 0)
                                {
                                    _tree.SetNewParent(selectedNodes[0].Node, clickedNode.Node);
                                    UpdateNodesHierarchy();
                                }
                            }
                            e.Use();
                            Event.current.Use();
                        }
                        else
                        {
                            _selectionStart = e.mousePosition;
                            var selectedNodes = GetSelectedNodes();
                            bool cancelSingleSelection = false;
                            foreach (var node in selectedNodes)
                            {
                                if (node.Rect.Contains(e.mousePosition))
                                {
                                    e.Use();
                                    Event.current.Use();
                                    cancelSingleSelection = true;
                                    break;
                                }
                            }

                            if (!cancelSingleSelection)
                            {
                                var found = false;
                                foreach (var pair in _editorNodesHierarchy)
                                {
                                    pair.Key.IsSelected = false;
                                    if (!found)
                                    {
                                        if (pair.Key.Rect.Contains(e.mousePosition))
                                        {
                                            pair.Key.IsSelected = true;
                                            found = true;
                                            e.Use();
                                            Event.current.Use();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (e.button == 1)
                    {
                        foreach (var pair in _editorNodesHierarchy)
                        {
                            if (pair.Key.Rect.Contains(e.mousePosition))
                            {
                                foreach (var pair1 in _editorNodesHierarchy)
                                {
                                    pair1.Key.IsSelected = false;
                                }
                                break;
                            }
                        }
                    }
                    IsSettingNewParent = false;
                    break;

                case EventType.MouseUp:
                    if (_isSelecting)
                    {
                        _selectionEnd = e.mousePosition;
                    }
                    break;

            }

            ProcessNodeEvents(e);

            //DrawZoomArea();

            EditorZoomArea.End();

            GUI.Label(new Rect(10.0f, 10.0f, 300.0f, 35.0f), _treeName, _textSyle);

            GUI.Box(new Rect(10.0f, 45.0f, 105.0f, 35.0f), "Running", EditorNode._runningStyle);

            GUI.Box(new Rect(10.0f, 80.0f, 105.0f, 35.0f), "Success", _greenStyle);

            GUI.Box(new Rect(10.0f, 115.0f, 105.0f, 35.0f), "Failure", _redStyle);

            if (_isSelecting && (_selectionBoxEnd - _selectionBoxStart).sqrMagnitude > 25.0f)
            {
                float minX, maxX;
                if (_selectionBoxStart.x > _selectionBoxEnd.x)
                {
                    maxX = _selectionBoxStart.x;
                    minX = _selectionBoxEnd.x;
                }
                else
                {
                    minX = _selectionBoxStart.x;
                    maxX = _selectionBoxEnd.x;
                }

                float minY, maxY;
                if (_selectionBoxStart.y > _selectionBoxEnd.y)
                {
                    maxY = _selectionBoxStart.y;
                    minY = _selectionBoxEnd.y;
                }
                else
                {
                    minY = _selectionBoxStart.y;
                    maxY = _selectionBoxEnd.y;
                }

                _selectionBox.min = new Vector2(minX, minY);
                _selectionBox.max = new Vector2(maxX, maxY);
                GUI.Box(_selectionBox, "", _selectionBoxStyle);
            }

            ProcessEvents(Event.current);

            if (GUI.changed || IsSettingNewParent)
            {
                Repaint();
            }
            if (_tree != null)
            {
                EditorUtility.SetDirty(_tree);
            }

        }

        void DrawNodes()
        {
            if (_editorNodesHierarchy != null)
            {
                foreach (var pair in _editorNodesHierarchy)
                {
                    pair.Key.Draw(Matrix4x4.TRS(-_zoomCoordsOrigin, Quaternion.identity, Vector3.one));
                }
            }
        }

        void DrawConnections()
        {
            foreach (var conn in _connections)
            {
                conn.Draw(Matrix4x4.TRS(-_zoomCoordsOrigin, Quaternion.identity, Vector3.one));
            }
        }

        void ProcessNodeEvents(Event e)
        {
            foreach (var pair in _editorNodesHierarchy)
            {
                pair.Key.ProcessEvents(e);
            }
        }

        List<EditorNode> GetSelectedNodes()
        {
            var list = new List<EditorNode>();
            foreach (var pair in _editorNodesHierarchy)
            {
                if (pair.Key.IsSelected)
                {
                    list.Add(pair.Key);
                }
            }
            return list;
        }

        void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.ScrollWheel:
                    Vector2 screenCoordsMousePos = Event.current.mousePosition;
                    Vector2 delta = Event.current.delta;
                    Vector2 zoomCoordsMousePos = ConvertScreenCoordsToZoomCoords(screenCoordsMousePos);
                    float zoomDelta = -delta.y / 150.0f;
                    float oldZoom = _zoom;
                    _zoom += zoomDelta;
                    _zoom = Mathf.Clamp(_zoom, kZoomMin, kZoomMax);
                    _zoomCoordsOrigin += (zoomCoordsMousePos - _zoomCoordsOrigin) - (oldZoom / _zoom) * (zoomCoordsMousePos - _zoomCoordsOrigin);
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (e.button == 2)
                    {
                        Vector2 d = Event.current.delta;
                        d /= _zoom;
                        _zoomCoordsOrigin -= d;
                    }
                    else if (_isSelecting)
                    {
                        _selectionBoxEnd = e.mousePosition;
                    }
                    GUI.changed = true;
                    break;

                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        _selectionBoxStart = e.mousePosition;
                        _selectionBoxEnd = e.mousePosition;
                        _isSelecting = true;
                    }
                    break;

                case EventType.MouseUp:
                    if (_isSelecting)
                    {
                        _isSelecting = false;
                        SelectNodes();
                    }
                    GUI.changed = true;
                    break;

                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Delete)
                    {
                        DeleteSelectedNodes();
                    }
                    break;
            }
        }

        void DeleteSelectedNodes()
        {
            var selectedNodes = GetSelectedNodes();
            foreach (var node in selectedNodes)
            {
                _tree.DeleteNode(node.Node);
            }
            UpdateNodesHierarchy();
        }

        void SelectNodes()
        {
            float minX, maxX;
            if (_selectionStart.x > _selectionEnd.x)
            {
                maxX = _selectionStart.x;
                minX = _selectionEnd.x;
            }
            else
            {
                minX = _selectionStart.x;
                maxX = _selectionEnd.x;
            }

            float minY, maxY;
            if (_selectionStart.y > _selectionEnd.y)
            {
                maxY = _selectionStart.y;
                minY = _selectionEnd.y;
            }
            else
            {
                minY = _selectionStart.y;
                maxY = _selectionEnd.y;
            }

            var rect = new Rect();
            rect.min = new Vector2(minX, minY);
            rect.max = new Vector2(maxX, maxY);

            foreach (var pair in _editorNodesHierarchy)
            {
                if (rect.Overlaps(pair.Key.Rect))
                {
                    pair.Key.IsSelected = true;
                }
            }
        }

        private Vector2 ConvertScreenCoordsToZoomCoords(Vector2 screenCoords)
        {
            return (screenCoords - ZoomArea.TopLeft()) / _zoom + _zoomCoordsOrigin;
        }

    }

    // Helper Rect extension methods
    public static class Extensions
    {
        public static Rect Transform(this Rect rect, Matrix4x4 matrix)
        {
            var min = rect.min.Transform(matrix);
            var max = rect.max.Transform(matrix);
            return new Rect(min, max - min);
        }

        public static Vector2 Transform(this Vector2 vec, Matrix4x4 matrix)
        {
            // var v4 = new Vector4(vec.x, vec.y, 0.0f, 0.0f);
            return matrix.MultiplyPoint(vec);
            //return new Vector2(newVec.x, newVec.y);
        }

        public static Vector2 TopLeft(this Rect rect)
        {
            return new Vector2(rect.xMin, rect.yMin);
        }

        //public static Rect ScaleSizeBy(this Rect rect, float scale)
        //{
        //    return rect.ScaleSizeBy(scale, rect.center);
        //}

        public static Rect ScaleSizeBy(this Rect rect, float scale, Vector2 pivotPoint)
        {
            Rect result = rect;
            result.x -= pivotPoint.x;
            result.y -= pivotPoint.y;
            result.xMin *= scale;
            result.xMax *= scale;
            result.yMin *= scale;
            result.yMax *= scale;
            result.x += pivotPoint.x;
            result.y += pivotPoint.y;
            return result;
        }

        public static Rect Encapsulate(this Rect r, Rect rect)
        {
            r.max = new Vector2(rect.max.x > r.max.x ? rect.max.x : r.max.x, rect.max.y > r.max.y ? rect.max.y : r.max.y);
            r.min = new Vector2(rect.min.x < r.min.x ? rect.min.x : r.min.x, rect.min.y < r.min.y ? rect.min.y : r.min.y);
            return r;
        }

        //public static Rect ScaleSizeBy(this Rect rect, Vector2 scale)
        //{
        //    return rect.ScaleSizeBy(scale, rect.center);
        //}

        //public static Rect ScaleSizeBy(this Rect rect, Vector2 scale, Vector2 pivotPoint)
        //{
        //    Rect result = rect;
        //    result.x -= pivotPoint.x;
        //    result.y -= pivotPoint.y;
        //    result.xMin *= scale.x;
        //    result.xMax *= scale.x;
        //    result.yMin *= scale.y;
        //    result.yMax *= scale.y;
        //    result.x += pivotPoint.x;
        //    result.y += pivotPoint.y;
        //    return result;
        //}
    }

    public class EditorZoomArea
    {
        private const float kEditorWindowTabHeight = 21.0f;
        public static Rect _clippedArea;
        public static Matrix4x4 _zoomedAreaGuiMatrix;
        private static Matrix4x4 _prevGuiMatrix;   

        public static Rect Begin(float zoomScale, Rect screenCoordsArea)
        {
            GUI.EndGroup();        // End the group Unity begins automatically for an EditorWindow to clip out the window tab. This allows us to draw outside of the size of the EditorWindow.
            Rect clippedArea = screenCoordsArea.ScaleSizeBy(1.0f / zoomScale, screenCoordsArea.TopLeft());
            clippedArea.y += kEditorWindowTabHeight;
            GUI.BeginGroup(clippedArea);
            _prevGuiMatrix = GUI.matrix;
            Matrix4x4 translation = Matrix4x4.TRS(clippedArea.TopLeft(), Quaternion.identity, Vector3.one);
            Matrix4x4 scale = Matrix4x4.Scale(new Vector3(zoomScale, zoomScale, 1.0f));
            GUI.matrix = translation * scale * translation.inverse * GUI.matrix;
            _zoomedAreaGuiMatrix = GUI.matrix;
            _clippedArea = clippedArea;
            return clippedArea;
        }

        public static void End()
        {
            GUI.matrix = _prevGuiMatrix;
            GUI.EndGroup();
            GUI.BeginGroup(new Rect(0.0f, kEditorWindowTabHeight, Screen.width, Screen.height));
        }
    }

}