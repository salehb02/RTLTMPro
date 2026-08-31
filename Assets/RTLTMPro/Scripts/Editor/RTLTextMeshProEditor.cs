using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

#if TMP_VERSION_2_1_0_OR_NEWER || UNITY_6000_0_OR_NEWER
using TMP_UiEditorPanel = TMPro.EditorUtilities.TMP_EditorPanelUI;
#else
using TMP_UiEditorPanel = TMPro.EditorUtilities.TMP_UiEditorPanel;
#endif

namespace RTLTMPro
{
    [CustomEditor(typeof(RTLTextMeshPro)), CanEditMultipleObjects]
    public class RTLTextMeshProEditor : TMP_UiEditorPanel
    {
        private SerializedProperty originalTextProp;
        private SerializedProperty forceFarsiNumbersProp;
        private SerializedProperty aramaicScriptProp;
        private SerializedProperty fixTagsProp;
        private SerializedProperty forceFixProp;
        private SerializedProperty updateTextOnEnableProp;


        private bool foldout;
        private RTLTextMeshPro tmpro;

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
            tmpro = (RTLTextMeshPro)target;

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(originalTextProp, new GUIContent("RTL Text Input Box"));

            ListenForZeroWidthNoJoiner();

            if (EditorGUI.EndChangeCheck())
                OnChanged();

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();

            DrawRtlSettings();

            if (m_HavePropertiesChanged)
                OnChanged();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRtlSettings()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 24);

            if (GUI.Button(rect, new GUIContent("<b>RTL Settings</b>"), TMP_UIStyleManager.sectionHeader))
                foldout = !foldout;

            GUI.Label(rect, (foldout ? k_UiStateLabel[0] : k_UiStateLabel[1]), TMP_UIStyleManager.rightLabel);
            if (foldout)
            {
                DrawOptions();

                if (GUILayout.Button("Re-Fix"))
                    m_HavePropertiesChanged = true;
            }
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
            EditorGUILayout.PropertyField(aramaicScriptProp, new GUIContent("Aramaic Script"));
            EditorGUILayout.PropertyField(forceFixProp, new GUIContent("Force Fix"));
            EditorGUILayout.PropertyField(forceFarsiNumbersProp, new GUIContent("Force Farsi Numbers"));
            EditorGUILayout.PropertyField(updateTextOnEnableProp, new GUIContent("Update Text OnEnable"));
            EditorGUILayout.PropertyField(fixTagsProp, new GUIContent("Fix Tags"));

            if (EditorGUI.EndChangeCheck())
                m_HavePropertiesChanged = true;
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