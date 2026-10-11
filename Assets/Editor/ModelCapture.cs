#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PerspectivePuzzle.Level2;

namespace PerspectivePuzzle.Editor
{
    public static class ModelCapture
    {
        const string ScenePath = "Assets/Scenes/Level2.unity";
        const string ArtifactDir = @"C:\Users\ADMIN\.gemini\antigravity-ide\brain\f8aa9cb6-0857-45e7-8aec-9b4d2e9b36ae";
        const string ProjectOutputDir = @"E:\Tsinghua_Mihoyo\Tsinghua-miHoYo-GameJam\Screenshots_NoUI";

        [MenuItem("GameJam/Capture Level 2 Models (No UI)")]
        public static void CaptureBothAngles()
        {
            Debug.Log("[ModelCapture] Starting capture of Level 2 model angles (No UI)...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = Object.FindAnyObjectByType<Level2Game>();
            if (game == null)
            {
                Debug.LogError("[ModelCapture] Could not find Level2Game in scene!");
                return;
            }

            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);
            if (!Directory.Exists(ProjectOutputDir)) Directory.CreateDirectory(ProjectOutputDir);

            // ==========================================
            // ANGLE 1: WorldYaw = 90 deg (Diorama overview - Framed Beauty Shot)
            // ==========================================
            Debug.Log("[ModelCapture] Setting up Angle 1 (WorldYaw = 90)...");
            game.SetupForScreenshot(
                worldYaw: 90f,
                bridgeYaw: 0f,
                liftHeight: 3.0f,
                mood: 0.05f,
                playerNodeName: "u2",
                orthoSize: 7.2f,
                customFocus: new Vector3(0f, 1.6f, 0f)
            );

            string file1_artifact = Path.Combine(ArtifactDir, "angle_1_diorama_90deg.png");
            string file1_proj = Path.Combine(ProjectOutputDir, "angle_1_diorama_90deg.png");
            CaptureCameraToPNG(game.View, file1_artifact, 1920, 1080);
            File.Copy(file1_artifact, file1_proj, true);
            Debug.Log($"[ModelCapture] Angle 1 saved to: {file1_artifact}");

            // ==========================================
            // ANGLE 2: WorldYaw = 0 deg (Seam / Pagoda connection - Framed Beauty Shot)
            // ==========================================
            Debug.Log("[ModelCapture] Setting up Angle 2 (WorldYaw = 0)...");
            game.SetupForScreenshot(
                worldYaw: 0f,
                bridgeYaw: 0f,
                liftHeight: 3.0f,
                mood: 0.25f,
                playerNodeName: "c4",
                orthoSize: 7.2f,
                customFocus: new Vector3(0f, 1.6f, 0f)
            );

            string file2_artifact = Path.Combine(ArtifactDir, "angle_2_pagoda_seam_0deg.png");
            string file2_proj = Path.Combine(ProjectOutputDir, "angle_2_pagoda_seam_0deg.png");
            CaptureCameraToPNG(game.View, file2_artifact, 1920, 1080);
            File.Copy(file2_artifact, file2_proj, true);
            Debug.Log($"[ModelCapture] Angle 2 saved to: {file2_artifact}");

            // Wide shots for full sky/sun backdrop overview
            game.SetupForScreenshot(
                worldYaw: 90f,
                bridgeYaw: 0f,
                liftHeight: 3.0f,
                mood: 0.05f,
                playerNodeName: "u2",
                orthoSize: 9.6f,
                customFocus: new Vector3(0f, 0.4f, 0f)
            );
            string file1_wide_art = Path.Combine(ArtifactDir, "angle_1_wide.png");
            string file1_wide_proj = Path.Combine(ProjectOutputDir, "angle_1_wide.png");
            CaptureCameraToPNG(game.View, file1_wide_art, 1920, 1080);
            File.Copy(file1_wide_art, file1_wide_proj, true);

            game.SetupForScreenshot(
                worldYaw: 0f,
                bridgeYaw: 0f,
                liftHeight: 3.0f,
                mood: 0.25f,
                playerNodeName: "c4",
                orthoSize: 9.6f,
                customFocus: new Vector3(0f, 0.4f, 0f)
            );
            string file2_wide_art = Path.Combine(ArtifactDir, "angle_2_wide.png");
            string file2_wide_proj = Path.Combine(ProjectOutputDir, "angle_2_wide.png");
            CaptureCameraToPNG(game.View, file2_wide_art, 1920, 1080);
            File.Copy(file2_wide_art, file2_wide_proj, true);

            Debug.Log("[ModelCapture] Capture complete successfully!");
        }

        static void CaptureCameraToPNG(Camera cam, string outputPath, int width, int height)
        {
            if (cam == null)
            {
                Debug.LogError("[ModelCapture] Camera is null!");
                return;
            }

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8
            };

            RenderTexture prevRT = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevRT;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(outputPath, bytes);
        }
    }
}
#endif
