// Assets/BehaviourTreeKit/Editor/BehaviourTreeEditorWindow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace BehaviourTreeKit.Editor
{
    public class BehaviourTreeEditorWindow : EditorWindow
    {
        private BTAsset _asset;
        private Vector2 _scroll;
        private BTNode _selected;

        private GUIStyle _nodeStyle;
        private GUIStyle _portStyle;
        private GUIStyle _orderLabelStyle;

        private const float NODE_W = 240f;
        private const float NODE_H = 130f;   // 고정 높이
        private const float SIDEBAR_W = 320f;

        // 포트 선택(입력/출력) 상태로 2-클릭 배선
        private struct PortSel { public BTNode node; public bool isOutput; }
        private PortSel? _wiring;  // null이면 배선 아님

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
        }

        // ───────────────────────────────────────────────────────────────────────
        // null 안전 보정
        private List<BTNode> SafeNodes()
        {
            if (_asset == null) return s_emptyNodes;
            if (_asset.nodes == null) { _asset.nodes = new List<BTNode>(); EditorUtility.SetDirty(_asset); }
            _asset.nodes.RemoveAll(n => n == null);
            foreach (var n in _asset.nodes)
            {
                if (n != null && n.children == null)
                {
                    n.children = new List<BTNode>();
                    EditorUtility.SetDirty(n);
                }
            }
            return _asset.nodes;
        }
        private static readonly List<BTNode> s_emptyNodes = new List<BTNode>();
        private static IEnumerable<BTNode> SafeChildren(BTNode n)
        {
            if (n == null || n.children == null) yield break;
            foreach (var c in n.children) if (c != null) yield return c;
        }
        // ───────────────────────────────────────────────────────────────────────

        // 포트 위치(Top=입력, Bottom=출력)
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
            var nodes = SafeNodes();
            for (int i = nodes.Count - 1; i >= 0; --i)
            {
                var n = nodes[i];
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

            var nodes = SafeNodes();

            // ── 레이아웃: 왼쪽 그래프 + 오른쪽 사이드바 ─────────────────────────
            EditorGUILayout.BeginHorizontal();
            {
                // 왼쪽: 그래프 영역(스크롤)
                var graphRect = GUILayoutUtility.GetRect(position.width - SIDEBAR_W, position.height - 24f);
                _scroll = GUI.BeginScrollView(graphRect, _scroll, new Rect(0, 0, 4000, 3000));

                // 1) 노드 윈도우
                BeginWindows();
                foreach (var n in nodes.ToArray())
                {
                    if (n == null) continue;
                    var id = n.GetInstanceID();
                    n.editorPosition.width = NODE_W;
                    n.editorPosition.height = NODE_H;
                    n.editorPosition = GUI.Window(id, n.editorPosition, _ => DrawNodeWindow(n), "");
                }
                EndWindows();

                // 2) 연결선
                DrawConnections(nodes);

                // 3) 임시 선(포트 1회 클릭 상태)
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

                // 취소(Esc/우클릭)
                if (_wiring.HasValue && ((Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                    || (Event.current.type == EventType.MouseDown && Event.current.button == 1)))
                {
                    _wiring = null; Event.current.Use(); Repaint();
                }

                GUI.EndScrollView();

                // 오른쪽: 사이드바(Blackboard + Node Inspector)
                EditorGUILayout.BeginVertical(GUILayout.Width(SIDEBAR_W));
                {
                    DrawBlackboardPanel();   // ← 여기서 BTAsset의 Blackboard를 연결/생성/편집
                    GUILayout.Space(6);
                    DrawInspectorPanel(nodes);
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

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _asset = (BTAsset)EditorGUILayout.ObjectField(_asset, typeof(BTAsset), false, GUILayout.Width(280));
            if (GUILayout.Button("New", EditorStyles.toolbarButton)) CreateNewAsset();
            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton)) ShowAddNodeMenu();
            if (GUILayout.Button("Set Root: Selected", EditorStyles.toolbarButton))
            {
                if (_selected != null) _asset.root = _selected;
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
        }

        private void ShowAddNodeMenu()
        {
            if (_asset == null)
            {
                EditorUtility.DisplayDialog("No Asset", "Create or open a tree asset first.", "OK");
                return;
            }

            var menu = new GenericMenu();

            // 기본 노드
            void Add<T>() where T : BTNode
                => menu.AddItem(new GUIContent(typeof(T).Name), false, () => CreateNode(typeof(T)));

            Add<SequenceNode>();
            Add<SelectorNode>();
            Add<InverterNode>();
            Add<WaitNode>();

            // ActionNode 파생 자동 등록 
            var actionTypes = BTTypeUtil.GetConcreteActionNodeTypes();
            if (actionTypes.Count > 0)
            {
                menu.AddSeparator("");
                foreach (var t in actionTypes)
                {
                    var path = BTTypeUtil.GetMenuPathForAction(t); // "Action/..."
                    menu.AddItem(new GUIContent(path), false, () => CreateNode(t));
                }
            }

            menu.ShowAsContext();
        }

        private void CreateNode(Type nodeType)
        {
            if (nodeType == null || !typeof(BTNode).IsAssignableFrom(nodeType)) return;

            var n = ScriptableObject.CreateInstance(nodeType) as BTNode;
            n.name = nodeType.Name;
            n.editorPosition.position = _scroll + new Vector2(
                100 + UnityEngine.Random.Range(-20, 20),
                100 + UnityEngine.Random.Range(-20, 20));

            AssetDatabase.AddObjectToAsset(n, _asset);
            SafeNodes().Add(n);
            if (_asset.root == null) _asset.root = n;

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(_asset);
            Selection.activeObject = n;
            _selected = n;
        }
        private void CreateNode<T>() where T : BTNode => CreateNode(typeof(T));

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

            // Root 노드면 배경 강조
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

            // 포트 (Top=입력, Bottom=출력)
            var portSize = 20f;
            var topRect = new Rect((w - portSize) * 0.5f, 0, portSize, portSize);
            var bottomRect = new Rect((w - portSize) * 0.5f, h - portSize, portSize, portSize);

            EditorGUIUtility.AddCursorRect(topRect, MouseCursor.Link);
            EditorGUIUtility.AddCursorRect(bottomRect, MouseCursor.Link);

            if (GUI.Button(topRect, "c", _portStyle)) HandlePortClick(node, isOutput: false);
            if (GUI.Button(bottomRect, "p", _portStyle)) HandlePortClick(node, isOutput: true);

            // 부모에서 내 순서를 찾아 숫자 + < > 버튼 표시
            var parent = FindParentOf(node);
            if (parent != null && parent.children != null)
            {
                int idx = parent.children.IndexOf(node);
                if (idx >= 0)
                {
                    float pad = 4f;
                    var labelRect = new Rect(topRect.xMax + pad, 26f, 60f, topRect.height);
                    GUI.Label(labelRect, " order : " + (idx + 1).ToString(), _orderLabelStyle);

                    var leftRect = new Rect(labelRect.xMax + 2f, 26f, 20f, topRect.height);
                    var rightRect = new Rect(leftRect.xMax + 2f, 26f, 20f, topRect.height);

                    if (GUI.Button(leftRect, "<")) MoveChildOrder(node, toFront: true);
                    if (GUI.Button(rightRect, ">")) MoveChildOrder(node, toFront: false);
                }
            }

            if (node is ActionNode)
            {
                node.GetType();
                GUI.Label(new Rect(w * 0.6f, h - 25, w * 0.35f, 22), $" {node.GetType()} : ActionNode");
            }

            // 창 드래그
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

            // 동일 포트 재클릭 → 취소
            if (from.node == node && from.isOutput == isOutput) { _wiring = null; Repaint(); return; }

            // 입력↔출력이 아니면 취소
            if (from.isOutput == isOutput) { _wiring = null; Repaint(); return; }

            // 부모→자식 결정 (출력→입력 방향)
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
            var nodes = SafeNodes();
            foreach (var p in nodes)
            {
                if (p == null || p.children == null) continue;
                if (p.children.Contains(child)) return p;
            }
            return null;
        }

        private void RemoveNode(BTNode node)
        {
            var nodes = SafeNodes();
            if (node == null) return;

            if (_selected == node) _selected = null;
            if (_wiring.HasValue && _wiring.Value.node == node) _wiring = null;

            foreach (var n in nodes)
            {
                if (n == null || n.children == null) continue;
                n.children.RemoveAll(c => c == null || c == node);
                EditorUtility.SetDirty(n);
            }

            nodes.Remove(node);
            if (_asset.root == node) _asset.root = nodes.FirstOrDefault();

            DestroyImmediate(node, true);
            AssetDatabase.SaveAssets();
        }

        // ───────────────────────────────────────────────────────────────────────
        // Blackboard 패널 (한 세트처럼 연결/편집)
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

                // 인라인 편집
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

            if (_asset.blackboardTemplate != null) return; // 이미 있음

            var bb = ScriptableObject.CreateInstance<Blackboard>();
            bb.name = "Blackboard";
            AssetDatabase.AddObjectToAsset(bb, _asset);
            _asset.blackboardTemplate = bb;

            EditorUtility.SetDirty(bb);
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            Repaint();
        }
        // ───────────────────────────────────────────────────────────────────────

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

        private void MoveChildOrder(BTNode child, bool toFront)
        {
            var parent = FindParentOf(child);
            if (parent == null || parent.children == null) return;

            int idx = parent.children.IndexOf(child);
            if (idx < 0) return;

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
    }
}
#endif
