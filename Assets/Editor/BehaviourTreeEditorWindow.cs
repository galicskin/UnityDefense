// Assets/BehaviourTreeKit/Editor/BehaviourTreeEditorWindow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace BehaviourTreeKit.Editor
{
    public class BehaviourTreeEditorWindow : EditorWindow
    {
        private BTAsset _asset;
        private Vector2 _scroll;
        private BTNode _selected;

        private GUIStyle _nodeStyle;
        private GUIStyle _portStyle;

        private const float NODE_W = 240f;
        private const float NODE_H = 130f;   // 고정 높이

        // 포트 드래그 링크 상태
        private bool _isLinking;
        private BTNode _linkFrom;

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
                fixedWidth = 12,
                fixedHeight = 12,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
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

            var rect = GUILayoutUtility.GetRect(position.width, position.height - 24f);
            _scroll = GUI.BeginScrollView(rect, _scroll, new Rect(0, 0, 4000, 3000));

            // 1) 노드 윈도우
            BeginWindows();
            foreach (var n in nodes.ToArray())
            {
                if (n == null) continue;
                var id = n.GetInstanceID();
                n.editorPosition.width = NODE_W;
                n.editorPosition.height = NODE_H;
                n.editorPosition = GUI.Window(id, n.editorPosition, _ => DrawNodeWindow(n), n.name ?? n.GetType().Name);
            }
            EndWindows();

            // 2) 연결선
            DrawConnections(nodes);

            // 3) 드래그 중 임시 선 (항상 Repaint)
            if (_isLinking && _linkFrom != null)
            {
                Handles.BeginGUI();
                var from = GetBottomPortPos(_linkFrom);
                var mouse = Event.current.mousePosition;
                DrawConnection(from, mouse, true);
                Handles.EndGUI();
                Repaint();
            }

            // 4) 드래그 종료로 연결 확정 (세로 위치로 부모/자식 자동 결정)
            if (_isLinking && Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                var target = GetNodeAtPosition(Event.current.mousePosition);
                if (target != null && target != _linkFrom)
                {
                    var fromY = _linkFrom.editorPosition.center.y;
                    var toY = target.editorPosition.center.y;

                    BTNode parent, child;
                    if (fromY > toY) { parent = target; child = _linkFrom; } // 아래→위: 위쪽이 부모
                    else { parent = _linkFrom; child = target; } // 위→아래: 위쪽이 부모

                    if (!parent.children.Contains(child))
                    {
                        parent.children.Add(child);
                        EditorUtility.SetDirty(parent);
                        EditorUtility.SetDirty(_asset);
                        AssetDatabase.SaveAssets();
                    }
                }
                _isLinking = false; _linkFrom = null;
                Event.current.Use();
            }
            // 취소(Esc/우클릭)
            if (_isLinking && ((Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                || (Event.current.type == EventType.MouseDown && Event.current.button == 1)))
            {
                _isLinking = false; _linkFrom = null; Event.current.Use();
            }

            GUI.EndScrollView();

            DrawInspectorPanel(nodes);

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
            void Add<T>() where T : BTNode
                => menu.AddItem(new GUIContent(typeof(T).Name), false, () => CreateNode<T>());

            Add<SequenceNode>();
            Add<SelectorNode>();
            Add<InverterNode>();
            Add<ActionNode>();
            Add<WaitNode>();

            menu.ShowAsContext();
        }

        private void CreateNode<T>() where T : BTNode
        {
            var n = ScriptableObject.CreateInstance<T>();
            n.name = typeof(T).Name;
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

            // 절대좌표 UI
            var w = node.editorPosition.width;
            var h = node.editorPosition.height;

            GUI.Label(new Rect(8, 6, w - 16, 18), node.GetType().Name, EditorStyles.boldLabel);

            GUI.Label(new Rect(8, 28, 70, 18), "Comment");
            node.comment = EditorGUI.TextField(new Rect(80, 28, w - 88, 18), node.comment);

            if (GUI.Button(new Rect(8, h - 30, w - 16, 22), "Remove Node"))
            {
                RemoveNode(node);
                GUIUtility.ExitGUI();
            }

            // 포트 (Top=입력, Bottom=출력)
            var topRect = new Rect(w * 0.5f - 6f, -6f, 12f, 12f);
            var bottomRect = new Rect(w * 0.5f - 6f, h - 6f, 12f, 12f);

            // 시각용
            GUI.Button(topRect, "c", _portStyle);
            GUI.Button(bottomRect, "p", _portStyle);

            // ★ MouseDown으로 즉시 드래그 시작 (GUI.Button 대신 직접 체크)
            var e = Event.current;
            Debug.Log($"{e.type}, {e.button}, {bottomRect}");
            if (e.type == EventType.MouseDown && e.button == 0 && bottomRect.Contains(e.mousePosition))
            {
                
                _isLinking = true; _linkFrom = node; e.Use(); Repaint();
            }

            // 창 이동(제목줄)
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void RemoveNode(BTNode node)
        {
            var nodes = SafeNodes();
            if (node == null) return;

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

        private void DrawInspectorPanel(List<BTNode> nodes)
        {
            EditorGUILayout.Space();
            if (_selected == null) return;
            if (!nodes.Contains(_selected)) { _selected = null; return; }

            using (new GUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);
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
    }
}
#endif
