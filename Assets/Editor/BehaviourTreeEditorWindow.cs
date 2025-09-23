// Assets/BehaviourTreeKit/Editor/BehaviourTreeEditorWindow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;

namespace BehaviourTreeKit.Editor
{
    // Path helpers for "<TreeName>_Nodes"
    internal static class BTAssetPaths
    {
        public static string GetAssetPath(UnityEngine.Object obj)
        {
            return AssetDatabase.GetAssetPath(obj);
        }

        public static string GetDirOf(UnityEngine.Object obj)
        {
            var p = GetAssetPath(obj);
            if (string.IsNullOrEmpty(p)) return "Assets";
            return Path.GetDirectoryName(p).Replace("\\", "/");
        }

        public static string GetTreeName(UnityEngine.Object obj)
        {
            var p = GetAssetPath(obj);
            return Path.GetFileNameWithoutExtension(p);
        }

        public static string GetNodeFolderPath(UnityEngine.Object asset, string suffix = "_Nodes")
        {
            var baseDir = GetDirOf(asset);
            var treeName = GetTreeName(asset);
            return baseDir + "/" + treeName + suffix;
        }

        public static string EnsureNodeFolder(UnityEngine.Object asset, string suffix = "_Nodes")
        {
            var baseDir = GetDirOf(asset);
            var treeName = GetTreeName(asset);
            var nodeDir = baseDir + "/" + treeName + suffix;
            if (!AssetDatabase.IsValidFolder(nodeDir))
            {
                AssetDatabase.CreateFolder(baseDir, treeName + suffix);
            }
            return nodeDir;
        }

        public static string UniqueAssetPath(string dir, string fileNameNoExt)
        {
            return AssetDatabase.GenerateUniqueAssetPath(dir + "/" + fileNameNoExt + ".asset");
        }
    }

    // Behaviour Tree Editor Window
    public partial class BehaviourTreeEditorWindow : EditorWindow
    {
        private static bool s_NodesDirty = false;

        private BTAsset _asset;
        private Vector2 _scroll;
        private Vector2 _blackboardScroll;
        private BTNode _selected;

        private GUIStyle _nodeStyle;
        private GUIStyle _portStyle;
        private GUIStyle _orderLabelStyle;

        private const float NODE_W = 240f;
        private const float NODE_H = 130f;
        private const float SIDEBAR_W = 320f;

        private struct PortSel { public BTNode node; public bool isOutput; }
        private PortSel? _wiring;

        private readonly List<BTNode> _nodesCache = new();

        [MenuItem("Tools/Behaviour Tree Editor")]
        public static void Open()
        {
            GetWindow<BehaviourTreeEditorWindow>("Behaviour Tree");
        }

        private void OnEnable()
        {
            _nodeStyle = new GUIStyle("box")
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                padding = new RectOffset(6, 6, 6, 6)
            };
            _portStyle = new GUIStyle("button")
            {
                fixedWidth = 20,
                fixedHeight = 20,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            _orderLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };

            // auto refresh hooks
            EditorApplication.projectChanged += OnProjectChangedAuto;
            EditorApplication.hierarchyChanged += OnProjectChangedAuto;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChangedAuto;
            EditorApplication.hierarchyChanged -= OnProjectChangedAuto;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnFocus()
        {
            s_NodesDirty = true;
        }

        private void OnInspectorUpdate()
        {
            if (s_NodesDirty)
            {
                s_NodesDirty = false;
                RefreshNodesFromFolder();
                Repaint();
            }
        }

        private void OnProjectChangedAuto()
        {
            s_NodesDirty = true;
        }

        private void OnAfterAssemblyReload()
        {
            s_NodesDirty = true;
        }

        private void OnUndoRedo()
        {
            s_NodesDirty = true;
        }

        // Scan nodes in folder and sync cache and _asset.nodes
        private void RefreshNodesFromFolder()
        {
            _nodesCache.Clear();
            if (_asset == null) return;

            var nodeDir = BTAssetPaths.GetNodeFolderPath(_asset);
            if (AssetDatabase.IsValidFolder(nodeDir))
            {
                var guids = AssetDatabase.FindAssets("t:BTNode", new[] { nodeDir });
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var n = AssetDatabase.LoadAssetAtPath<BTNode>(path);
                    if (n == null) continue;

                    // ensure rect not zero
                    if (n.editorPosition.width < 10f || n.editorPosition.height < 10f)
                    {
                        var pos = _scroll + new Vector2(
                            100 + UnityEngine.Random.Range(-20, 20),
                            100 + UnityEngine.Random.Range(-20, 20));
                        n.editorPosition = new Rect(pos.x, pos.y, NODE_W, NODE_H);
                        EditorUtility.SetDirty(n);
                    }

                    _nodesCache.Add(n);
                }
            }

            if (_asset.nodes == null) _asset.nodes = new List<BTNode>();
            _asset.nodes.RemoveAll(x => x == null);
            foreach (var n in _nodesCache)
                if (!_asset.nodes.Contains(n)) _asset.nodes.Add(n);
            _asset.nodes.RemoveAll(x => !_nodesCache.Contains(x));

            if (_asset.root != null && !_nodesCache.Contains(_asset.root))
                _asset.root = null;

            EditorUtility.SetDirty(_asset);
        }

        private static IEnumerable<BTNode> SafeChildren(BTNode n)
        {
            if (n == null || n.children == null) yield break;
            foreach (var c in n.children) if (c != null) yield return c;
        }

        private float GetExtraHeight(BTNode bTNode)
        {
            if (bTNode is not ActionNode) return 0f;
            ActionNode actionNode = bTNode as ActionNode;
            return actionNode.BlackboardKeys.Count * 22f;
        }

        private Vector2 GetTopPortPos(BTNode n)
        {
            var r = n.editorPosition;
            return new Vector2(r.center.x, r.yMin - 6f);
        }

        private Vector2 GetBottomPortPos(BTNode n)
        {
            var r = n.editorPosition;
            return new Vector2(r.center.x, r.yMax + 6f);
        }

        private BTNode GetNodeAtPosition(Vector2 p)
        {
            for (int i = _nodesCache.Count - 1; i >= 0; --i)
            {
                var n = _nodesCache[i];
                if (n != null && n.editorPosition.Contains(p)) return n;
            }
            return null;
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_asset == null)
            {
                EditorGUILayout.HelpBox("Create or open a Behaviour Tree asset.", MessageType.Info);
                return;
            }

            // light path: rely on auto refresh; still safe to ensure cache on first draw
            if (_nodesCache.Count == 0) RefreshNodesFromFolder();
            var nodes = _nodesCache;

            EditorGUILayout.BeginHorizontal();
            {
                var graphRect = GUILayoutUtility.GetRect(position.width - SIDEBAR_W, position.height - 24f);
                _scroll = GUI.BeginScrollView(graphRect, _scroll, new Rect(0, 0, 4000, 3000));

                BeginWindows();
                foreach (var n in nodes.ToArray())
                {
                    if (n == null) continue;
                    var id = n.GetInstanceID();
                    n.editorPosition.width = NODE_W;
                    float extra = GetExtraHeight(n);
                    n.editorPosition.height = NODE_H + extra;
                    n.editorPosition = GUI.Window(id, n.editorPosition, _ => DrawNodeWindow(n), "");
                }
                EndWindows();

                DrawConnections(nodes);

                if (_wiring.HasValue)
                {
                    Handles.BeginGUI();
                    var w = _wiring.Value;
                    var from = w.isOutput ? GetBottomPortPos(w.node) : GetTopPortPos(w.node);
                    var mouse = Event.current.mousePosition;
                    DrawConnection(from, mouse, true);
                    Handles.EndGUI();
                    Repaint();
                }

                if (_wiring.HasValue && ((Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                    || (Event.current.type == EventType.MouseDown && Event.current.button == 1)))
                {
                    _wiring = null; Event.current.Use(); Repaint();
                }

                GUI.EndScrollView();

                EditorGUILayout.BeginVertical(GUILayout.Width(SIDEBAR_W));
                {
                    using (var scroll = new EditorGUILayout.ScrollViewScope(
                               _blackboardScroll,
                               GUILayout.Width(SIDEBAR_W),
                               GUILayout.ExpandHeight(true)))
                    {
                        _blackboardScroll = scroll.scrollPosition;
                        DrawBlackboardPanel();
                        GUILayout.Space(6);
                        DrawInspectorPanel(nodes);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_asset);
                foreach (var n in nodes) if (n != null) EditorUtility.SetDirty(n);
            }
        }

        // Toolbar
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _asset = (BTAsset)EditorGUILayout.ObjectField(_asset, typeof(BTAsset), false, GUILayout.Width(280));

            if (GUILayout.Button("New", EditorStyles.toolbarButton))
                CreateNewAsset();

            using (new EditorGUI.DisabledScope(_asset == null))
            {
                if (GUILayout.Button("Create Node Folder", EditorStyles.toolbarButton))
                {
                    var nodeDir = BTAssetPaths.EnsureNodeFolder(_asset);
                    AssetDatabase.Refresh();
                    Debug.Log("[BT] Node folder ready: " + nodeDir);
                    s_NodesDirty = true;
                }

                if (GUILayout.Button("Add Node", EditorStyles.toolbarButton))
                    ShowAddNodeMenu();

                if (GUILayout.Button("Open Folder", EditorStyles.toolbarButton))
                {
                    var nodeDir = BTAssetPaths.GetNodeFolderPath(_asset);
                    if (AssetDatabase.IsValidFolder(nodeDir))
                        EditorUtility.RevealInFinder(nodeDir);
                    else
                        EditorUtility.DisplayDialog("No Folder", "Create Node Folder first.", "OK");
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    RefreshNodesFromFolder();
                    Repaint();
                }

                if (GUILayout.Button("Set Root: Selected", EditorStyles.toolbarButton))
                {
                    if (_selected != null && _nodesCache.Contains(_selected))
                    {
                        _asset.root = _selected;
                        EditorUtility.SetDirty(_asset);
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Behaviour Tree", "NewBehaviourTree", "asset", "Select save path");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<BTAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _asset = asset;
            s_NodesDirty = true;
        }

        // Add Node menu
        private void ShowAddNodeMenu()
        {
            if (_asset == null)
            {
                EditorUtility.DisplayDialog("No Asset", "Create or open a tree asset first.", "OK");
                return;
            }

            var menu = new GenericMenu();

            var nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
                })
                .Where(t => typeof(BTNode).IsAssignableFrom(t)
                            && t.IsClass
                            && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToList();

            foreach (var t in nodeTypes)
            {
                var display = "Create New/" + t.Name;
                menu.AddItem(new GUIContent(display), false, () => CreateNodeAssetInFolder(t));
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Add Selected Nodes (Move)"), false, AddSelectedNodesFromProject_Move);
            menu.AddItem(new GUIContent("Add Selected Nodes (Duplicate)"), false, AddSelectedNodesFromProject_Duplicate);

            menu.ShowAsContext();
        }

        // Create node asset in tree folder
        private void CreateNodeAssetInFolder(Type nodeType)
        {
            if (_asset == null || nodeType == null || !typeof(BTNode).IsAssignableFrom(nodeType)) return;

            var nodeDir = BTAssetPaths.EnsureNodeFolder(_asset);
            Undo.RegisterCompleteObjectUndo(_asset, "Create BT Node Asset");

            var n = ScriptableObject.CreateInstance(nodeType) as BTNode;
            n.name = nodeType.Name;

            // initialize rect
            var pos = _scroll + new Vector2(
                100 + UnityEngine.Random.Range(-20, 20),
                100 + UnityEngine.Random.Range(-20, 20));
            n.editorPosition = new Rect(pos.x, pos.y, NODE_W, NODE_H);

            var path = BTAssetPaths.UniqueAssetPath(nodeDir, nodeType.Name);
            AssetDatabase.CreateAsset(n, path);

            EditorUtility.SetDirty(n);
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _selected = n;
            Selection.activeObject = n;

            s_NodesDirty = true;
            Repaint();
        }

        // Move selected BTNode assets into tree folder
        private void AddSelectedNodesFromProject_Move()
        {
            if (_asset == null) return;
            var nodeDir = BTAssetPaths.EnsureNodeFolder(_asset);

            var selected = Selection.objects.OfType<BTNode>().ToList();
            if (selected.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No BTNode Selected",
                    "Select one or more BTNode assets in the Project view.",
                    "OK");
                return;
            }

            Undo.RegisterCompleteObjectUndo(_asset, "Move BT Nodes To Folder");

            foreach (var n in selected)
            {
                var srcPath = AssetDatabase.GetAssetPath(n);
                if (string.IsNullOrEmpty(srcPath)) continue;

                var dstPath = BTAssetPaths.UniqueAssetPath(nodeDir, Path.GetFileNameWithoutExtension(srcPath));
                var result = AssetDatabase.MoveAsset(srcPath, dstPath);
                if (!string.IsNullOrEmpty(result))
                    Debug.LogError("[BT] Move failed: " + result);

                // position and dirty
                var pos = _scroll + new Vector2(
                    100 + UnityEngine.Random.Range(-20, 20),
                    100 + UnityEngine.Random.Range(-20, 20));
                n.editorPosition = new Rect(pos.x, pos.y, NODE_W, NODE_H);
                EditorUtility.SetDirty(n);
            }

            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            s_NodesDirty = true;
            Repaint();
        }

        // Duplicate selected BTNode assets into tree folder
        private void AddSelectedNodesFromProject_Duplicate()
        {
            if (_asset == null) return;
            var nodeDir = BTAssetPaths.EnsureNodeFolder(_asset);

            var selected = Selection.objects.OfType<BTNode>().ToList();
            if (selected.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No BTNode Selected",
                    "Select one or more BTNode assets in the Project view.",
                    "OK");
                return;
            }

            Undo.RegisterCompleteObjectUndo(_asset, "Duplicate BT Nodes To Folder");

            foreach (var n in selected)
            {
                var newNode = Instantiate(n);
                newNode.name = n.name;

                var path = BTAssetPaths.UniqueAssetPath(nodeDir, newNode.name);
                AssetDatabase.CreateAsset(newNode, path);

                var pos = _scroll + new Vector2(
                    100 + UnityEngine.Random.Range(-20, 20),
                    100 + UnityEngine.Random.Range(-20, 20));
                newNode.editorPosition = new Rect(pos.x, pos.y, NODE_W, NODE_H);
                EditorUtility.SetDirty(newNode);
            }

            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            s_NodesDirty = true;
            Repaint();
        }

        // Draw connections
        private void DrawConnections(List<BTNode> nodes)
        {
            if (nodes == null) return;
            Handles.BeginGUI();
            foreach (var n in nodes)
            {
                if (n == null) continue;
                foreach (var c in SafeChildren(n))
                {
                    var from = GetBottomPortPos(n);
                    var to = GetTopPortPos(c);
                    DrawConnection(from, to, n == _selected);
                }
            }
            Handles.EndGUI();
        }

        private void DrawConnection(Vector2 start, Vector2 end, bool highlight = false)
        {
            Vector3 s = new Vector3(start.x, start.y, 0);
            Vector3 e = new Vector3(end.x, end.y, 0);
            Vector3 startTan = s + Vector3.up * 40f;
            Vector3 endTan = e - Vector3.up * 40f;

            var col = highlight ? new Color(1f, 0.8f, 0.2f, 1f) : new Color(1f, 1f, 1f, 0.9f);
            Handles.DrawBezier(s, e, startTan, endTan, col, null, highlight ? 3f : 2f);
            Handles.DrawSolidDisc(e, Vector3.forward, 2.5f);
        }

        private void DrawNodeWindow(BTNode node)
        {
            if (node == null) return;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                _selected = node;

            var w = node.editorPosition.width;
            var h = node.editorPosition.height;

            bool isRoot = (_asset != null && _asset.root == node);

            var bgColor = GUI.backgroundColor;
            if (isRoot) GUI.backgroundColor = new Color(1.0f, 1.8f, 0.2f);
            GUI.Box(new Rect(0, 0, w, h), GUIContent.none, _nodeStyle);
            GUI.backgroundColor = bgColor;

            string title = node.GetType().Name;
            if (isRoot) title = "[ROOT] " + title;
            GUI.Label(new Rect(8, 26, w - 16, 18), title, EditorStyles.boldLabel);

            GUI.Label(new Rect(8, 48, 70, 18), "Comment");
            node.comment = EditorGUI.TextField(new Rect(80, 48, w - 88, 18), node.comment);

            if (GUI.Button(new Rect(w * 0.6f, h - 50, w * 0.35f, 22), "Remove"))
            {
                RemoveNode(node);
                GUIUtility.ExitGUI();
            }

            var portSize = 20f;
            var topRect = new Rect((w - portSize) * 0.5f, 0, portSize, portSize);
            var bottomRect = new Rect((w - portSize) * 0.5f, h - portSize, portSize, portSize);

            EditorGUIUtility.AddCursorRect(topRect, MouseCursor.Link);
            EditorGUIUtility.AddCursorRect(bottomRect, MouseCursor.Link);

            if (GUI.Button(topRect, "c", _portStyle)) HandlePortClick(node, isOutput: false);
            if (GUI.Button(bottomRect, "p", _portStyle)) HandlePortClick(node, isOutput: true);

            var parent = FindParentOf(node);
            if (parent != null && parent.children != null)
            {
                int idx = parent.children.IndexOf(node);
                if (idx >= 0)
                {
                    float pad = 4f;
                    var labelRect = new Rect(topRect.xMax + pad, 26f, 80f, topRect.height);
                    GUI.Label(labelRect, " order : " + (idx + 1).ToString(), _orderLabelStyle);

                    var leftRect = new Rect(labelRect.xMax + 2f, 26f, 20f, topRect.height);
                    var rightRect = new Rect(leftRect.xMax + 2f, 26f, 20f, topRect.height);

                    if (GUI.Button(leftRect, "<")) MoveChildOrder(node, toFront: true);
                    if (GUI.Button(rightRect, ">")) MoveChildOrder(node, toFront: false);
                }
            }

            if (node is ActionNode)
            {
                float heightInterval = 22f;
                Rect infoRect = new Rect(8, 48 + heightInterval, 60f, 22);
                ActionNode actionNode = node as ActionNode;

                Dictionary<(string, BlackboardKey.ValueType), BlackboardKey> changedBlackboardKeys = new();

                foreach (var blackboardKey in actionNode.BlackboardKeys)
                {
                    string fieldName = blackboardKey.Key.Item1;
                    BlackboardKey.ValueType valueType = blackboardKey.Key.Item2;

                    infoRect.width = fieldName.Length * 7f;
                    GUI.Label(infoRect, fieldName);

                    var valueKeyList = _asset.blackboardTemplate != null
                        ? _asset.blackboardTemplate.GetKeyList(valueType)
                        : new List<string>();
                    string[] displayOptions = valueKeyList.ToArray();

                    int curIndex;
                    if (blackboardKey.Value == null)
                    {
                        curIndex = -1;
                    }
                    else
                    {
                        BlackboardKey selectedBlackboardKey =
                            _asset.blackboardTemplate != null
                                ? _asset.blackboardTemplate.GetRefBlackboardKey(valueType, blackboardKey.Value.key)
                                : null;

                        if (selectedBlackboardKey == actionNode.BlackboardKeys[blackboardKey.Key])
                            curIndex = valueKeyList.IndexOf(blackboardKey.Value.key);
                        else
                            curIndex = -1;
                    }

                    curIndex = EditorGUI.Popup(
                        new Rect(w - 68f, infoRect.y, 60f, 22f),
                        curIndex,
                        displayOptions
                    );
                    infoRect.y += heightInterval;

                    if (curIndex == -1)
                    {
                        changedBlackboardKeys[(fieldName, valueType)] = null;
                        continue;
                    }

                    var blackboardFieldName = displayOptions[curIndex];
                    changedBlackboardKeys[(fieldName, valueType)] =
                        _asset.blackboardTemplate != null
                        ? _asset.blackboardTemplate.GetRefBlackboardKey(valueType, blackboardFieldName)
                        : null;
                }

                foreach (var changedBlackboardKey in changedBlackboardKeys)
                {
                    actionNode.BlackboardKeys[changedBlackboardKey.Key] = changedBlackboardKey.Value;
                }
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void HandlePortClick(BTNode node, bool isOutput)
        {
            if (_wiring == null)
            {
                _wiring = new PortSel { node = node, isOutput = isOutput };
                Repaint(); return;
            }

            var from = _wiring.Value;

            if (from.node == node && from.isOutput == isOutput) { _wiring = null; Repaint(); return; }
            if (from.isOutput == isOutput) { _wiring = null; Repaint(); return; }

            BTNode parent, child;
            if (from.isOutput) { parent = from.node; child = node; }
            else { parent = node; child = from.node; }

            if (IsAncestor(child, parent))
            {
                ShowNotification(new GUIContent("Cycle not allowed"));
                _wiring = null; Repaint(); return;
            }

            var existingParent = FindParentOf(child);
            if (existingParent != null && existingParent != parent)
            {
                ShowNotification(new GUIContent("Child already has a parent"));
                _wiring = null; Repaint(); return;
            }

            if (!parent.children.Contains(child))
            {
                Undo.RegisterCompleteObjectUndo(parent, "Connect BT Child");
                parent.children.Add(child);
                EditorUtility.SetDirty(parent);
                EditorUtility.SetDirty(_asset);
                AssetDatabase.SaveAssets();
            }

            _wiring = null; Repaint();
        }

        private bool IsAncestor(BTNode potentialAncestor, BTNode node)
        {
            if (potentialAncestor == null || node == null) return false;
            foreach (var c in SafeChildren(potentialAncestor))
            {
                if (c == node) return true;
                if (IsAncestor(c, node)) return true;
            }
            return false;
        }

        private BTNode FindParentOf(BTNode child)
        {
            if (child == null) return null;
            foreach (var p in _nodesCache)
            {
                if (p == null || p.children == null) continue;
                if (p.children.Contains(child)) return p;
            }
            return null;
        }

        private void MoveChildOrder(BTNode child, bool toFront)
        {
            var parent = FindParentOf(child);
            if (parent == null || parent.children == null) return;

            int idx = parent.children.IndexOf(child);
            if (idx < 0) return;

            Undo.RegisterCompleteObjectUndo(parent, "Reorder BT Child");

            parent.children.RemoveAt(idx);

            if (toFront)
                parent.children.Insert(Mathf.Max(0, idx - 1), child);
            else
                parent.children.Insert(Mathf.Min(parent.children.Count, idx + 1), child);

            EditorUtility.SetDirty(parent);
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        private void RemoveNode(BTNode node)
        {
            if (node == null || _asset == null) return;

            var nodes = _nodesCache;

            Undo.RegisterCompleteObjectUndo(_asset, "Remove BT Node");

            foreach (var n in nodes)
            {
                if (n == null || n.children == null) continue;
                if (n.children.Contains(node))
                {
                    Undo.RegisterCompleteObjectUndo(n, "Disconnect BT Child");
                    n.children.RemoveAll(c => c == null || c == node);
                    EditorUtility.SetDirty(n);
                }
            }

            if (_asset.root == node) _asset.root = null;

            var path = AssetDatabase.GetAssetPath(node);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(node, true);
            }

            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            s_NodesDirty = true;

            _selected = null;
            Selection.activeObject = _asset;
        }

        private void DrawBlackboardPanel()
        {
            using (new GUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Blackboard", EditorStyles.boldLabel);

                var bb = _asset.blackboardTemplate;
                var newBB = (Blackboard)EditorGUILayout.ObjectField("Asset", bb, typeof(Blackboard), false);
                if (newBB != bb)
                {
                    _asset.blackboardTemplate = newBB;
                    EditorUtility.SetDirty(_asset);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Embedded", GUILayout.Width(140)))
                {
                    CreateEmbeddedBlackboard();
                }
                EditorGUI.BeginDisabledGroup(_asset.blackboardTemplate == null);
                if (GUILayout.Button("Ping", GUILayout.Width(60)))
                {
                    EditorGUIUtility.PingObject(_asset.blackboardTemplate);
                }
                EditorGUI.EndDisabledGroup();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (_asset.blackboardTemplate != null)
                {
                    var soBB = new SerializedObject(_asset.blackboardTemplate);
                    soBB.Update();
                    var entriesProp = soBB.FindProperty("entries");
                    if (entriesProp != null)
                    {
                        EditorGUILayout.PropertyField(entriesProp, true);
                    }
                    soBB.ApplyModifiedProperties();
                }
                else
                {
                    EditorGUILayout.HelpBox("Assign a Blackboard asset or create an embedded one.", MessageType.Info);
                }
            }
        }

        private void CreateEmbeddedBlackboard()
        {
            if (_asset == null) return;
            if (_asset.blackboardTemplate != null) return;

            var bb = ScriptableObject.CreateInstance<Blackboard>();
            bb.name = "Blackboard";

            // Note: we allow Blackboard as sub-asset; nodes are not embedded
            AssetDatabase.AddObjectToAsset(bb, _asset);
            _asset.blackboardTemplate = bb;

            EditorUtility.SetDirty(bb);
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        private void DrawInspectorPanel(List<BTNode> nodes)
        {
            using (new GUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Node Inspector", EditorStyles.boldLabel);

                if (_selected == null) { EditorGUILayout.LabelField("Select a node."); return; }
                if (!nodes.Contains(_selected)) { _selected = null; EditorGUILayout.LabelField("Select a node."); return; }

                var so = new SerializedObject(_selected);
                so.Update();

                var it = so.GetIterator();
                bool enterChildren = true;
                while (it.NextVisible(enterChildren))
                {
                    if (it.name == nameof(BTNode.editorPosition) || it.name == nameof(BTNode.children))
                    {
                        enterChildren = false; continue;
                    }
                    EditorGUILayout.PropertyField(it, true);
                    enterChildren = false;
                }
                so.ApplyModifiedProperties();
            }
        }

        // helper for assetpostprocessor
        internal static void MarkNodesDirty() { s_NodesDirty = true; }
    }

    // asset postprocessor to auto refresh when BTNode assets change
    class BTNodeFolderWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool any = false;

            bool HasBTNode(string path)
            {
                if (string.IsNullOrEmpty(path)) return false;
                var obj = AssetDatabase.LoadAssetAtPath<BTNode>(path);
                return obj != null;
            }

            foreach (var p in importedAssets) { if (HasBTNode(p)) { any = true; break; } }
            if (!any) foreach (var p in deletedAssets) { if (p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) { any = true; break; } }
            if (!any) foreach (var p in movedAssets) { if (HasBTNode(p)) { any = true; break; } }
            if (!any) foreach (var p in movedFromAssetPaths) { if (p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) { any = true; break; } }

            if (any && EditorWindow.HasOpenInstances<BehaviourTreeKit.Editor.BehaviourTreeEditorWindow>())
            {
                BehaviourTreeKit.Editor.BehaviourTreeEditorWindow.MarkNodesDirty();
            }
        }
    }
}
#endif
