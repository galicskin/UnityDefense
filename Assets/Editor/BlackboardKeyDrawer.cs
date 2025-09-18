// Assets/BehaviourTreeKit/Editor/BlackboardKeyDrawer.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace BehaviourTreeKit
{
    [CustomPropertyDrawer(typeof(BlackboardKey))]
    public class BlackboardKeyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // key + type + value = 3 라인 (+ 패딩)
            return (EditorGUIUtility.singleLineHeight + 4f) * 3f + 4f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var keyProp = property.FindPropertyRelative("key");
            var typeProp = property.FindPropertyRelative("type");
            var boolProp = property.FindPropertyRelative("boolValue");
            var intProp = property.FindPropertyRelative("intValue");
            var floatProp = property.FindPropertyRelative("floatValue");
            var vecProp = property.FindPropertyRelative("vector3Value");
            var objProp = property.FindPropertyRelative("objectValue");

            var r = position; r.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(r, keyProp, new GUIContent("Key"));
            r.y += r.height + 4f;

            EditorGUI.PropertyField(r, typeProp, new GUIContent("Type"));
            r.y += r.height + 4f;

            var vt = (BlackboardKey.ValueType)typeProp.enumValueIndex;
            switch (vt)
            {
                case BlackboardKey.ValueType.Bool:
                    EditorGUI.PropertyField(r, boolProp, new GUIContent("Value"));
                    break;
                case BlackboardKey.ValueType.Int:
                    EditorGUI.PropertyField(r, intProp, new GUIContent("Value"));
                    break;
                case BlackboardKey.ValueType.Float:
                    EditorGUI.PropertyField(r, floatProp, new GUIContent("Value"));
                    break;
                case BlackboardKey.ValueType.Vector3:
                    EditorGUI.PropertyField(r, vecProp, new GUIContent("Value"));
                    break;
                case BlackboardKey.ValueType.Object:
                    EditorGUI.PropertyField(r, objProp, new GUIContent("Value"));
                    break;
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
