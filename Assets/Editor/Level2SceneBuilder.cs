using System.Collections.Generic;
using System.Linq;
using PerspectivePuzzle.Level1;
using PerspectivePuzzle.Level2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PerspectivePuzzle.EditorTools
{
    /// <summary>
    /// Rebuilds Assets/Scenes/Level2.unity from scratch. The diorama itself is generated at runtime by
    /// <see cref="Level2Game"/>; this only wires the camera, sun, character model, clips and the three
    /// material templates (so their shaders are guaranteed to ship in the player build).
    /// </summary>
    public static class Level2SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Level2.unity";
        const string MatFolder = "Assets/Art/Level2";

        [MenuItem("Tools/Perspective Puzzle/Rebuild Level 2 Scene")]
        public static void Menu()
        {
            Debug.Log(Build());
        }

        public static string Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art"))
                AssetDatabase.CreateFolder("Assets", "Art");
            if (!AssetDatabase.IsValidFolder(MatFolder))
                AssetDatabase.CreateFolder("Assets/Art", "Level2");

            Material lit = EnsureMaterial("L2_Lit", "Standard", m => { m.SetFloat("_Glossiness", 0.12f); m.color = Color.white; });
            Material vertex = EnsureMaterial("L2_Vertex", "Sprites/Default", m => m.color = Color.white);
            Material glow = EnsureMaterial("L2_Glow", "Unlit/Color", m => m.color = Color.white);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.99f, 0.56f, 0.5f);

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;

            var sys = new GameObject("_Level2");
            var game = sys.AddComponent<Level2Game>();
            sys.AddComponent<Level2Hud>();
            game.view = cam;
            game.sun = sun;
            game.bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterClips.IdleModel);
            game.idleClip = CharacterClips.FirstClip(CharacterClips.IdleModel);
            game.walkClip = CharacterClips.FirstClip(CharacterClips.WalkModel);
            game.carryClip = CharacterClips.FirstClip(CharacterClips.CarryModel);
            game.litTemplate = lit;
            game.vertexTemplate = vertex;
            game.glowTemplate = glow;

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureInBuild(ScenePath);
            return $"Level2 rebuilt: body={(game.bodyPrefab != null)} idle={(game.idleClip != null)} walk={(game.walkClip != null)} scenes={string.Join(",", EditorBuildSettings.scenes.Select(s => s.path))}";
        }

        static Material EnsureMaterial(string name, string shader, System.Action<Material> setup)
        {
            string path = $"{MatFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(shader));
                setup(m);
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                m.shader = Shader.Find(shader);
                setup(m);
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
            return m;
        }

        static void EnsureInBuild(string path)
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == path))
            {
                foreach (var s in list)
                    if (s.path == path)
                        s.enabled = true;
            }
            else
            {
                list.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
