#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Concept.Editor
{
    public class PathPickerBinding : IBinding
    {
        private SerializedProperty m_Property;
        private TextField m_TextField;
        private Button m_Button;

        public PathPickerBinding(SerializedProperty property, TextField textField, Button button)
        {
            m_Property = property;
            m_TextField = textField;
            m_Button = button;

            m_TextField.value = m_Property.stringValue;
            m_TextField.RegisterValueChangedCallback(OnValueChanged);
            m_Button.clicked += OnButtonClicked;
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            m_Property.stringValue = evt.newValue;
            m_Property.serializedObject.ApplyModifiedProperties();
        }

        private void OnButtonClicked()
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder",
                GetInitialPath(m_Property.stringValue), "");

            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Converte para caminho relativo
                string projectPath = Application.dataPath.Replace("/Assets", "");
                if (selectedPath.StartsWith(projectPath))
                {
                    selectedPath = selectedPath.Substring(projectPath.Length + 1);
                }

                m_Property.stringValue = selectedPath;
                m_TextField.value = selectedPath;
                m_Property.serializedObject.ApplyModifiedProperties();
            }
        }

        private string GetInitialPath(string currentPath)
        {
            string projectRoot = System.IO.Path.GetFullPath(Application.dataPath);

            if (!string.IsNullOrEmpty(currentPath))
            {
                string testPath = currentPath;

                if (!System.IO.Path.IsPathRooted(currentPath))
                {
                    testPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(projectRoot, "..", currentPath));
                }

                if (System.IO.Directory.Exists(testPath))
                    return testPath;
            }

            return projectRoot;
        }

        public void PreUpdate()
        {
            // Não necessário
        }

        public void Update()
        {
            // Não necessário
        }

        public void Release()
        {
            m_TextField.UnregisterValueChangedCallback(OnValueChanged);
            m_Button.clicked -= OnButtonClicked;
        }
    }
}
#endif