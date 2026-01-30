using Azathrix.EzInput.Settings;
using UnityEditor;
using UnityEngine;

namespace Azathrix.EzInput.Editor
{
    [CustomEditor(typeof(EzInputSettings))]
    public class EzInputSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _inputActionAsset;
        private SerializedProperty _defaultControlScheme;
        private SerializedProperty _autoCreatePlayerInput;
        private SerializedProperty _defaultMap;
        private SerializedProperty _debugLog;

        private bool _generalFoldout = true;
        private bool _debugFoldout = true;

        private void OnEnable()
        {
            _inputActionAsset = serializedObject.FindProperty("inputActionAsset");
            _defaultControlScheme = serializedObject.FindProperty("defaultControlScheme");
            _autoCreatePlayerInput = serializedObject.FindProperty("autoCreatePlayerInput");
            _defaultMap = serializedObject.FindProperty("defaultMap");
            _debugLog = serializedObject.FindProperty("debugLog");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawGeneralSection();
            EditorGUILayout.Space(8);
            DrawDebugSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneralSection()
        {
            _generalFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_generalFoldout, "基础配置");
            if (_generalFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_inputActionAsset, new GUIContent("InputAction 资源"));
                EditorGUILayout.PropertyField(_defaultControlScheme, new GUIContent("默认控制方案"));
                EditorGUILayout.PropertyField(_autoCreatePlayerInput, new GUIContent("自动创建 PlayerInput"));
                EditorGUILayout.PropertyField(_defaultMap, new GUIContent("默认 Map"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawDebugSection()
        {
            _debugFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_debugFoldout, "调试");
            if (_debugFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_debugLog, new GUIContent("输出调试日志"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
