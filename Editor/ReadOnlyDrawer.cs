#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Concept.UI
{

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                // Altura do foldout
                float height = EditorGUIUtility.singleLineHeight;
                if (property.isExpanded)
                {
                    // Adiciona a altura de cada elemento
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        height += EditorGUI.GetPropertyHeight(property.GetArrayElementAtIndex(i), true) + 2;
                    }
                }
                return height;
            }
            else
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                // Desenha o foldout
                property.isExpanded = EditorGUI.Foldout(
                    new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                    property.isExpanded, label, true);

                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    float y = position.y + EditorGUIUtility.singleLineHeight;

                    // Desenha cada elemento da array
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        var element = property.GetArrayElementAtIndex(i);
                        float elementHeight = EditorGUI.GetPropertyHeight(element, true);
                        var elementRect = new Rect(position.x, y, position.width, elementHeight);

                        // Desenha o elemento como readonly
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUI.PropertyField(elementRect, element, new GUIContent($"Element {i}"), true);
                        EditorGUI.EndDisabledGroup();

                        y += elementHeight + 2;
                    }
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                // Para propriedades n�o-array, desenha como readonly
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(position, property, label, true);
                EditorGUI.EndDisabledGroup();
            }
        }
    }

}
#endif