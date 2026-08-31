using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace RTLTMPro
{
    [CustomEditor(typeof(RTLTextMeshPro3D)), CanEditMultipleObjects]
    public class RTLTextMeshPro3DEditor : TMP_EditorPanel
    {
        private SerializedProperty originalTextProp;
        private SerializedProperty forceFarsiNumbersProp;
        private SerializedProperty aramaicScriptProp;
        private SerializedProperty fixTagsProp;
        private SerializedProperty forceFixProp;
        private SerializedProperty updateTextOnEnableProp;

        private bool foldout;
        private RTLTextMeshPro3D tmpro;

        protected override void OnEnable()
        {
            base.OnEnable();
            foldout = true;
            forceFarsiNumbersProp = serializedObject.FindProperty("forceFarsiNumbers");
            aramaicScriptProp = serializedObject.FindProperty("aramaicScript");
            fixTagsProp = serializedObject.FindProperty("fixTags");
            forceFixProp = serializedObject.FindProperty("forceFix");
            originalTextProp = serializedObject.FindProperty("originalText");
            updateTextOnEnableProp = serializedObject.FindProperty("updateTextOnEnable");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            tmpro = (RTLTextMeshPro3D)target;

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(originalTextProp, new GUIContent("RTL Text Input Box"));

            ListenForZeroWidthNoJoiner();

            if (EditorGUI.EndChangeCheck())
                OnChanged();

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();

            foldout = EditorGUILayout.Foldout(foldout, "RTL Settings", TMP_UIStyleManager.boldFoldout);
            if (foldout)
            {
                DrawOptions();

                if (GUILayout.Button("Re-Fix"))
                    m_HavePropertiesChanged = true;

                if (EditorGUI.EndChangeCheck())
                    m_HavePropertiesChanged = true;
            }

            if (m_HavePropertiesChanged)
                OnChanged();

            serializedObject.ApplyModifiedProperties();
        }

        protected void OnChanged()
        {
            tmpro.UpdateText();
            m_HavePropertiesChanged = false;
            m_TextComponent.havePropertiesChanged = true;
            m_TextComponent.ComputeMarginSize();
            EditorUtility.SetDirty(target);
        }

        protected virtual void DrawOptions()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PrefixLabel(new GUIContent("Aramaic Script", "Select the script to use"));
            aramaicScriptProp.enumValueIndex = (int)(AramaicScript)EditorGUILayout.EnumPopup((AramaicScript)aramaicScriptProp.enumValueIndex);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            forceFixProp.boolValue = GUILayout.Toggle(forceFixProp.boolValue, new GUIContent("Force Fix"));
            forceFarsiNumbersProp.boolValue = GUILayout.Toggle(forceFarsiNumbersProp.boolValue, new GUIContent("Force Farsi Numbers"));
            updateTextOnEnableProp.boolValue = GUILayout.Toggle(updateTextOnEnableProp.boolValue, new GUIContent("Update Text OnEnable"));

            if (tmpro.richText)
                fixTagsProp.boolValue = GUILayout.Toggle(fixTagsProp.boolValue, new GUIContent("FixTags"));

            EditorGUILayout.EndHorizontal();
        }

        protected virtual void ListenForZeroWidthNoJoiner()
        {
            var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);

            bool shortcutPressed = (Event.current.modifiers & EventModifiers.Control) != 0 &&
                                   (Event.current.modifiers & EventModifiers.Shift) != 0 &&
                                   Event.current.type == EventType.KeyUp &&
                                   Event.current.keyCode == KeyCode.Alpha2;

            if (!shortcutPressed) return;

            originalTextProp.stringValue = originalTextProp.stringValue.Insert(editor.cursorIndex, ((char)SpecialCharacters.ZeroWidthNoJoiner).ToString());
            editor.selectIndex++;
            editor.cursorIndex++;
            Event.current.Use();
            Repaint();
        }
    }
}