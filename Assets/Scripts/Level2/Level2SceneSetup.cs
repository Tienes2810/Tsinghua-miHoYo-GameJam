#if UNITY_EDITOR
using System.Collections.Generic;
using PerspectivePuzzle.Level1;
using PerspectivePuzzle.Movement;
using PerspectivePuzzle.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PerspectivePuzzle.Level2
{
    public static class Level2SceneSetup
    {
        const string Level2ScenePath = "Assets/Scenes/Level2.unity";
        const string ConnectModelPath = "Assets/Art/Models/Connect/connect/connect1.fbx";
        const string ConnectTexturePath = "Assets/Art/Models/Connect/connect/basecolor.png";

        [MenuItem("PerspectivePuzzle/Setup Level 2 Scene")]
        public static string Setup()
        {
            var scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
                return "Failed to open scene " + Level2ScenePath;

            var buildingRoot = GameObject.Find("BuildingRoot");
            if (buildingRoot == null)
                return "BuildingRoot not found!";

            var rotator = buildingRoot.GetComponent<BuildingRotator>();
            var playerGo = GameObject.Find("Player");
            var playerMover = playerGo != null ? playerGo.GetComponent<PlayerMovement>() : null;
            var cameraRig = GameObject.Find("CameraRig");
            var rig = cameraRig != null ? cameraRig.GetComponent<IsoRig>() : null;
            var cam = cameraRig != null ? cameraRig.GetComponent<Camera>() : null;
            var routeGo = GameObject.Find("Route");
            var ribbon = routeGo != null ? routeGo.GetComponent<RouteRibbon>() : null;

            // 1. Configure Sunset Lighting & Camera
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.18f, 0.13f, 0.24f, 1f);
            }

            var sunGo = GameObject.Find("Sun");
            if (sunGo != null)
            {
                var sunLight = sunGo.GetComponent<Light>();
                if (sunLight != null)
                {
                    sunLight.color = new Color(1.0f, 0.70f, 0.40f);
                    sunLight.intensity = 1.35f;
                }
                sunGo.transform.rotation = Quaternion.Euler(38f, -48f, 0f);
            }
            RenderSettings.ambientLight = new Color(0.55f, 0.42f, 0.48f);

            // 2. Locate Path Nodes
            var nodes = GameObject.Find("Nodes");
            PathNode nodeA = nodes.transform.Find("A")?.GetComponent<PathNode>();
            PathNode nodeB = nodes.transform.Find("B")?.GetComponent<PathNode>();
            PathNode nodeC = nodes.transform.Find("C")?.GetComponent<PathNode>();
            PathNode nodeF = nodes.transform.Find("F")?.GetComponent<PathNode>();
            PathNode nodeG = nodes.transform.Find("G")?.GetComponent<PathNode>();
            PathNode nodeM = nodes.transform.Find("M")?.GetComponent<PathNode>();
            PathNode nodePole = nodes.transform.Find("Pole")?.GetComponent<PathNode>();

            // 3. Create or Update Spatial Bridge (connect1.fbx)
            var existingBridge = buildingRoot.transform.Find("SpatialBridge");
            if (existingBridge != null)
                Object.DestroyImmediate(existingBridge.gameObject);

            var bridgeGo = new GameObject("SpatialBridge");
            bridgeGo.transform.SetParent(buildingRoot.transform, false);

            Vector3 posC = nodeC != null ? nodeC.transform.localPosition : Vector3.zero;
            Vector3 posF = nodeF != null ? nodeF.transform.localPosition : Vector3.zero;
            Vector3 bridgeCenter = (posC + posF) * 0.5f + new Vector3(0f, 0.2f, 0f);
            bridgeGo.transform.localPosition = bridgeCenter;

            GameObject connectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConnectModelPath);
            if (connectPrefab != null)
            {
                var modelInst = (GameObject)PrefabUtility.InstantiatePrefab(connectPrefab, bridgeGo.transform);
                modelInst.name = "BridgeMesh";
                modelInst.transform.localPosition = Vector3.zero;
                modelInst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                modelInst.transform.localScale = Vector3.one * 0.015f;

                Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ConnectTexturePath);
                if (baseTex != null)
                {
                    Material bridgeMat = new Material(Shader.Find("Standard"));
                    bridgeMat.mainTexture = baseTex;
                    bridgeMat.color = new Color(0.95f, 0.9f, 0.85f);
                    foreach (var r in modelInst.GetComponentsInChildren<Renderer>())
                        r.material = bridgeMat;
                }
            }
            else
            {
                // Fallback geometry
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "BridgeMesh";
                cube.transform.SetParent(bridgeGo.transform, false);
                cube.transform.localScale = new Vector3(1.2f, 0.2f, 2.5f);
            }

            // Collider and PathNode on bridge
            var bridgeCol = bridgeGo.GetComponent<BoxCollider>();
            if (bridgeCol == null)
                bridgeCol = bridgeGo.AddComponent<BoxCollider>();
            bridgeCol.size = new Vector3(1.8f, 0.8f, 2.4f);

            var bridgeNode = bridgeGo.GetComponent<PathNode>();
            if (bridgeNode == null)
                bridgeNode = bridgeGo.AddComponent<PathNode>();

            // 4. Create Lanterns
            var existingL1 = buildingRoot.transform.Find("Lantern1");
            if (existingL1 != null) Object.DestroyImmediate(existingL1.gameObject);
            var existingL2 = buildingRoot.transform.Find("Lantern2");
            if (existingL2 != null) Object.DestroyImmediate(existingL2.gameObject);
            var existingL3 = buildingRoot.transform.Find("Lantern3");
            if (existingL3 != null) Object.DestroyImmediate(existingL3.gameObject);

            Vector3 l1Pos = (nodeB != null ? nodeB.transform.localPosition : Vector3.zero) + new Vector3(0.6f, 0.5f, 0f);
            LanternProp lantern1 = LanternProp.Create(buildingRoot.transform, "Lantern1", l1Pos, 1);

            Vector3 l2Pos = (nodeF != null ? nodeF.transform.localPosition : Vector3.zero) + new Vector3(-0.6f, 0.5f, 0f);
            LanternProp lantern2 = LanternProp.Create(buildingRoot.transform, "Lantern2", l2Pos, 2);

            Vector3 l3Pos = (nodePole != null ? nodePole.transform.localPosition : Vector3.zero) + new Vector3(0.7f, 0.5f, 0f);
            LanternProp lantern3 = LanternProp.Create(buildingRoot.transform, "Lantern3", l3Pos, 3);

            // 5. Create Sunset Bell
            var existingBell = buildingRoot.transform.Find("SunsetBell");
            if (existingBell != null) Object.DestroyImmediate(existingBell.gameObject);

            var bellGo = new GameObject("SunsetBell");
            bellGo.transform.SetParent(buildingRoot.transform, false);
            Vector3 bellPos = (nodePole != null ? nodePole.transform.localPosition : Vector3.zero) + new Vector3(0f, 1.4f, 0f);
            bellGo.transform.localPosition = bellPos;

            var bellCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bellCyl.name = "BellCylinder";
            bellCyl.transform.SetParent(bellGo.transform, false);
            bellCyl.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
            Object.DestroyImmediate(bellCyl.GetComponent<Collider>());
            var bellMat = new Material(Shader.Find("Standard"));
            bellMat.color = new Color(0.85f, 0.68f, 0.28f);
            bellCyl.GetComponent<Renderer>().material = bellMat;

            // 6. Setup _Level2 System Controller
            var oldLevel1Go = GameObject.Find("_Level1");
            if (oldLevel1Go != null)
                Object.DestroyImmediate(oldLevel1Go);

            var existingLevel2Go = GameObject.Find("_Level2");
            if (existingLevel2Go != null)
                Object.DestroyImmediate(existingLevel2Go);

            var level2Go = new GameObject("_Level2");
            var board = level2Go.AddComponent<RotationBoard>();
            board.Bind(buildingRoot.transform, playerMover);

            var presenter = level2Go.AddComponent<Level2Presenter>();
            var director = level2Go.AddComponent<Level2Director>();
            var input = level2Go.AddComponent<Level2Input>();
            input.view = cam;
            input.director = director;
            input.rotator = rotator;

            director.Bind(
                playerMover,
                board,
                rotator,
                presenter,
                rig,
                ribbon,
                nodeA,
                nodeB,
                nodeC,
                bridgeNode,
                nodeF,
                nodeM != null ? nodeM : nodeG,
                nodePole,
                lantern1,
                lantern2,
                lantern3,
                bridgeGo,
                bellGo.transform
            );

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Level2ScenePath);
            return "SUCCESS: Level 2 Scene successfully configured!";
        }
    }
}
#endif
