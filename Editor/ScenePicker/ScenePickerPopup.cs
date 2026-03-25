#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ConceptFactory.UIEssentials.Editor
{
    public sealed class ScenePickerEntry
    {
        public ScenePickerEntry(string name, string path, bool inBuildSettings, bool isMissing)
        {
            Name = name;
            Path = path;
            InBuildSettings = inBuildSettings;
            IsMissing = isMissing;
        }

        public string Name { get; }

        public string Path { get; }

        public bool InBuildSettings { get; }

        public bool IsMissing { get; }
    }

    public sealed class ScenePickerPopup : PopupWindowContent
    {
        private const string UxmlPath = "Packages/UIEssentials/Editor/ScenePicker/ScenePickerPopup.uxml";
        private const string UssPath = "Packages/UIEssentials/Editor/ScenePicker/ScenePickerPopup.uss";

        private readonly string _selectedSceneName;
        private readonly Action<ScenePickerEntry> _onSelected;
        private readonly List<ScenePickerEntry> _buildScenes = new();
        private readonly List<ScenePickerEntry> _projectScenes = new();

        public ScenePickerPopup(string selectedSceneName, Action<ScenePickerEntry> onSelected)
        {
            _selectedSceneName = selectedSceneName;
            _onSelected = onSelected;
            LoadScenes();
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(420f, 360f);
        }

        public override void OnOpen()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            if (editorWindow == null)
            {
                return;
            }

            VisualElement root = editorWindow.rootVisualElement;
            root.Clear();

            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);

            VisualElement contentRoot = visualTree != null ? visualTree.CloneTree() : CreateFallbackRoot();
            if (styleSheet != null)
            {
                root.styleSheets.Clear();
                root.styleSheets.Add(styleSheet);
            }

            root.Add(contentRoot);
            ApplyRootStyles(root, contentRoot);

            Label title = contentRoot.Q<Label>("titleLabel");
            if (title != null)
            {
                title.text = "Select Default Scene";
                ApplyTitleStyles(title);
            }

            Foldout buildFoldout = contentRoot.Q<Foldout>("buildScenesFoldout");
            Foldout projectFoldout = contentRoot.Q<Foldout>("projectScenesFoldout");
            VisualElement buildList = contentRoot.Q<VisualElement>("buildScenesList");
            VisualElement projectList = contentRoot.Q<VisualElement>("projectScenesList");

            if (buildFoldout != null)
            {
                buildFoldout.text = $"Build Scenes ({_buildScenes.Count})";
                buildFoldout.value = true;
            }

            if (projectFoldout != null)
            {
                projectFoldout.text = $"Project Scenes ({_projectScenes.Count})";
                projectFoldout.value = true;
            }

            PopulateList(buildList, _buildScenes, "No scenes were found in Build Settings.");
            PopulateList(projectList, _projectScenes, "No extra scenes were found in the project.");
        }

        private void PopulateList(VisualElement container, List<ScenePickerEntry> scenes, string emptyMessage)
        {
            if (container == null)
            {
                return;
            }

            container.Clear();

            if (scenes.Count == 0)
            {
                var emptyLabel = new Label(emptyMessage);
                emptyLabel.AddToClassList("scene-picker-empty");
                container.Add(emptyLabel);
                return;
            }

            for (int i = 0; i < scenes.Count; i++)
            {
                container.Add(CreateSceneItem(scenes[i], i));
            }
        }

        private VisualElement CreateSceneItem(ScenePickerEntry scene, int index)
        {
            bool isSelected = string.Equals(scene.Name, _selectedSceneName, StringComparison.OrdinalIgnoreCase);

            var item = new VisualElement();
            item.AddToClassList("scene-picker-item");
            ApplyItemStyles(item, index, isSelected, scene.IsMissing);

            if (index % 2 == 0)
            {
                item.AddToClassList("scene-picker-item-alt");
            }

            if (isSelected)
            {
                item.AddToClassList("scene-picker-item-current");
            }

            if (scene.IsMissing)
            {
                item.AddToClassList("scene-picker-item-missing");
            }

            var headerRow = new VisualElement();
            headerRow.AddToClassList("scene-picker-item-header");
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            item.Add(headerRow);

            var title = new Label(isSelected ? $"{scene.Name} (current)" : scene.Name);
            title.AddToClassList("scene-picker-item-title");
            title.style.color = new Color(0.86f, 0.90f, 0.92f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.flexGrow = 1f;
            headerRow.Add(title);

            if (scene.IsMissing)
            {
                var missingLabel = new Label("Missing");
                missingLabel.AddToClassList("scene-picker-item-badge");
                missingLabel.style.marginLeft = 8f;
                missingLabel.style.paddingLeft = 6f;
                missingLabel.style.paddingRight = 6f;
                missingLabel.style.paddingTop = 1f;
                missingLabel.style.paddingBottom = 1f;
                missingLabel.style.fontSize = 9f;
                missingLabel.style.color = new Color(1f, 0.89f, 0.89f);
                missingLabel.style.backgroundColor = new Color(0.70f, 0.27f, 0.27f, 0.4f);
                missingLabel.style.borderTopLeftRadius = 8f;
                missingLabel.style.borderTopRightRadius = 8f;
                missingLabel.style.borderBottomLeftRadius = 8f;
                missingLabel.style.borderBottomRightRadius = 8f;
                headerRow.Add(missingLabel);
            }

            var path = new Label(scene.Path.Replace('\\', '/'));
            path.AddToClassList("scene-picker-item-path");
            path.style.marginTop = 3f;
            path.style.marginLeft = 1f;
            path.style.fontSize = 10f;
            path.style.color = new Color(0.82f, 0.82f, 0.82f, 0.72f);
            item.Add(path);

            item.RegisterCallback<MouseEnterEvent>(_ =>
            {
                item.AddToClassList("scene-picker-item-hover");
                ApplyHoverStyles(item, true, isSelected, scene.IsMissing);
            });
            item.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                item.RemoveFromClassList("scene-picker-item-hover");
                ApplyHoverStyles(item, false, isSelected, scene.IsMissing);
            });
            item.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }

                _onSelected?.Invoke(scene);
                editorWindow?.Close();
                evt.StopImmediatePropagation();
            });

            return item;
        }

        private static void ApplyRootStyles(VisualElement windowRoot, VisualElement contentRoot)
        {
            if (windowRoot != null)
            {
                windowRoot.style.backgroundColor = new Color(0.19f, 0.17f, 0.17f);
            }

            if (contentRoot != null)
            {
                contentRoot.style.paddingLeft = 8f;
                contentRoot.style.paddingRight = 8f;
                contentRoot.style.paddingTop = 8f;
                contentRoot.style.paddingBottom = 8f;
                contentRoot.style.minHeight = 340f;
                contentRoot.style.backgroundColor = new Color(0.19f, 0.17f, 0.17f);
            }
        }

        private static void ApplyTitleStyles(Label title)
        {
            if (title == null)
            {
                return;
            }

            title.style.fontSize = 13f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6f;
            title.style.color = new Color(0.86f, 0.90f, 0.92f);
        }

        private static void ApplyItemStyles(VisualElement item, int index, bool isSelected, bool isMissing)
        {
            if (item == null)
            {
                return;
            }

            item.style.flexDirection = FlexDirection.Column;
            item.style.paddingLeft = 8f;
            item.style.paddingRight = 8f;
            item.style.paddingTop = 6f;
            item.style.paddingBottom = 6f;
            item.style.marginBottom = 2f;
            item.style.borderTopLeftRadius = 4f;
            item.style.borderTopRightRadius = 4f;
            item.style.borderBottomLeftRadius = 4f;
            item.style.borderBottomRightRadius = 4f;
            item.style.borderLeftWidth = 1f;
            item.style.borderRightWidth = 1f;
            item.style.borderTopWidth = 1f;
            item.style.borderBottomWidth = 1f;

            Color borderColor = new Color(1f, 1f, 1f, 0.06f);
            Color backgroundColor = index % 2 == 0
                ? new Color(1f, 1f, 1f, 0.055f)
                : new Color(1f, 1f, 1f, 0.035f);

            if (isSelected)
            {
                backgroundColor = new Color(0.24f, 0.44f, 0.73f, 0.22f);
                borderColor = new Color(0.35f, 0.59f, 0.90f, 0.45f);
            }
            else if (isMissing)
            {
                backgroundColor = new Color(0.65f, 0.29f, 0.29f, 0.14f);
            }

            item.style.backgroundColor = backgroundColor;
            item.style.borderLeftColor = borderColor;
            item.style.borderRightColor = borderColor;
            item.style.borderTopColor = borderColor;
            item.style.borderBottomColor = borderColor;
        }

        private static void ApplyHoverStyles(VisualElement item, bool hovered, bool isSelected, bool isMissing)
        {
            if (item == null)
            {
                return;
            }

            if (!hovered)
            {
                ApplyItemStyles(item, item.ClassListContains("scene-picker-item-alt") ? 0 : 1, isSelected, isMissing);
                return;
            }

            if (!isSelected)
            {
                item.style.backgroundColor = new Color(0.33f, 0.49f, 0.67f, 0.18f);
                Color hoverBorder = new Color(0.41f, 0.57f, 0.77f, 0.28f);
                item.style.borderLeftColor = hoverBorder;
                item.style.borderRightColor = hoverBorder;
                item.style.borderTopColor = hoverBorder;
                item.style.borderBottomColor = hoverBorder;
            }
        }

        private static VisualElement CreateFallbackRoot()
        {
            var root = new VisualElement();
            root.name = "scenePickerRoot";
            root.AddToClassList("scene-picker-root");

            var title = new Label("Select Default Scene");
            title.name = "titleLabel";
            title.AddToClassList("scene-picker-title");
            root.Add(title);

            var scrollView = new ScrollView();
            scrollView.name = "sceneScroll";
            scrollView.AddToClassList("scene-picker-scroll");

            var buildFoldout = new Foldout { name = "buildScenesFoldout", value = true };
            var buildList = new VisualElement { name = "buildScenesList" };
            buildList.AddToClassList("scene-picker-list");
            buildFoldout.Add(buildList);
            scrollView.Add(buildFoldout);

            var projectFoldout = new Foldout { name = "projectScenesFoldout", value = true };
            var projectList = new VisualElement { name = "projectScenesList" };
            projectList.AddToClassList("scene-picker-list");
            projectFoldout.Add(projectList);
            scrollView.Add(projectFoldout);

            root.Add(scrollView);
            return root;
        }

        private void LoadScenes()
        {
            var buildScenePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
            for (int i = 0; i < buildScenes.Length; i++)
            {
                EditorBuildSettingsScene buildScene = buildScenes[i];
                if (buildScene == null || string.IsNullOrWhiteSpace(buildScene.path))
                {
                    continue;
                }

                string sceneName = Path.GetFileNameWithoutExtension(buildScene.path);
                bool isMissing = AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScene.path) == null;
                _buildScenes.Add(new ScenePickerEntry(sceneName, buildScene.path, true, isMissing));
                buildScenePaths.Add(buildScene.path);
            }

            string[] sceneGuids = AssetDatabase.FindAssets("t:SceneAsset");
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                if (string.IsNullOrWhiteSpace(path) || buildScenePaths.Contains(path))
                {
                    continue;
                }

                string sceneName = Path.GetFileNameWithoutExtension(path);
                _projectScenes.Add(new ScenePickerEntry(sceneName, path, false, false));
            }

            _buildScenes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            _projectScenes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
#endif
