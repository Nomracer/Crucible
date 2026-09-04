using System.IO;
using Crucible.Diagnostics;
using Crucible.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crucible.Editor
{
    /// <summary>
    /// Builds the sandbox scene from code.
    ///
    /// A scene assembled by hand is a binary blob that nobody can review and that quietly rots when
    /// a field is renamed. Generating it means the setup is readable, diffable, and reproducible by
    /// anyone who clones the repo — including the reviewer.
    /// </summary>
    public static class SandboxSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        private const string ShaderPath = "Assets/_Project/Art/Shaders/GridUnlit.shader";

        [MenuItem("Crucible/Build Sandbox Scene")]
        public static void Build()
        {
            Shader gridShader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (gridShader == null)
            {
                Debug.LogError($"Grid shader not found at {ShaderPath}. Scene not built.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera();
            MeshRenderer quad = CreateQuad();
            StatsOverlay overlay = CreateOverlay();

            var gameObject = new GameObject("Game");
            var game = gameObject.AddComponent<CrucibleGame>();

            var serialized = new SerializedObject(game);
            serialized.FindProperty("_camera").objectReferenceValue = camera;
            serialized.FindProperty("_quad").objectReferenceValue = quad;
            serialized.FindProperty("_gridShader").objectReferenceValue = gridShader;
            serialized.FindProperty("_overlay").objectReferenceValue = overlay;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            AddToBuildSettings();

            Debug.Log($"Sandbox scene built at {ScenePath}");
        }

        private static Camera CreateCamera()
        {
            var gameObject = new GameObject("Camera");
            var camera = gameObject.AddComponent<Camera>();

            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            // Matches the empty-cell colour, so the letterbox edge is invisible.
            camera.backgroundColor = new Color32(10, 10, 12, 255);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            gameObject.tag = "MainCamera";
            return camera;
        }

        private static MeshRenderer CreateQuad()
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Grid";

            // The primitive ships with a collider. There is no physics in this project.
            Object.DestroyImmediate(quad.GetComponent<Collider>());

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = false;

            return renderer;
        }

        private static StatsOverlay CreateOverlay()
        {
            var canvasObject = new GameObject("Overlay Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();

            var labelObject = new GameObject("Stats");
            labelObject.transform.SetParent(canvasObject.transform, worldPositionStays: false);

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 22f;
            label.color = new Color32(200, 200, 210, 255);
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;
            label.richText = false;

            var rect = label.rectTransform;
            // Anchored to the top-left inside the safe area margin. Real safe-area handling arrives
            // with the UI in M8; this is far enough in to clear a notch on the devices we test on.
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -80f);
            rect.sizeDelta = new Vector2(520f, 220f);

            var overlay = canvasObject.AddComponent<StatsOverlay>();
            var serialized = new SerializedObject(overlay);
            serialized.FindProperty("_label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return overlay;
        }

        private static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var scene in scenes)
            {
                if (scene.path == ScenePath)
                {
                    return;
                }
            }

            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(updated, 0);
            updated[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = updated;
        }
    }
}
