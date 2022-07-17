using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Benchmark
{
    [DisallowMultipleComponent]
    public class BenchmarkTool : MonoBehaviour
    {
        private const float WindowWidth = 0.34f;
        private const float AnimationSpeed = 1f;
        private const int ReferenceResolution = 800;

        private static readonly IReadOnlyList<(string file, string name)> BuiltinMeshFiles = new (string, string)[]
        {
            ("Cube.fbx", "Cube"),
            ("Sphere.fbx", "Sphere"),
            ("Capsule.fbx", "Capsule"),
            ("Quad.fbx", "Quad")
        };

        public Mesh[] customMeshes;
        public Material[] materials;

        private Mesh[] builtinMeshes;
        private bool staticBatching;
        private bool combineMeshes;
        private float spacing = 2f;
        private Vector3Int count = new Vector3Int(10, 1, 10);
        private float scale = 1f;
        private string createdInfo = "<None>";
        private MaterialGUI activeMaterial;
        private MaterialGUI[] materialGUIs;

        private bool drawToolsPanel = true;
        private float windowOffset;
        private Vector2 scroll;

        private float guiScale;
        private float scaledScreenWidth;
        private float scaledScreenHeight;

        private LogType lastLogType = LogType.Log;
        private string lastLogMessage;

        private Color errorColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        private Color warningColor = new Color(0.8f, 0.6f, 0.2f, 0.8f);
        private Color fpsBoxColor = new Color(0f, 0f, 0f, 0.5f);

        private GUIStyle fpsTextStyle;
        private GUIStyle logTextStyle;
        private GUIContent fpsLabel = new GUIContent();
        private Vector2 fpsPosition = new Vector2(60f, 0f);
        private int fps;
        private int avgfps;
        private float avgTime;

        private Vector2Int initResolution;

        private Transform root;

        private void Awake()
        {
            initResolution = new Vector2Int(Screen.width, Screen.height);

            if (materials != null)
            {
                materialGUIs = new MaterialGUI[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                    materialGUIs[i] = new MaterialGUI(new Material(materials[i]));

                if (materials != null && materials.Length > 0)
                    activeMaterial = materialGUIs[0];
            }
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;

            logTextStyle = new GUIStyle {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            logTextStyle.normal.textColor = Color.white;

            fpsTextStyle = new GUIStyle {
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(5, 5, 5, 5)
            };
            fpsTextStyle.normal.textColor = Color.white;

            builtinMeshes = new Mesh[BuiltinMeshFiles.Count];
            for (int i = 0; i < builtinMeshes.Length; i++)
                builtinMeshes[i] = Resources.GetBuiltinResource<Mesh>(BuiltinMeshFiles[i].file);
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void Update()
        {
            fps = (int)(1f / Time.unscaledDeltaTime);
            avgTime += (Time.unscaledDeltaTime - avgTime) * 0.03f;
            avgfps = (int)(1f / avgTime);
        }

        private IEnumerator SlideToolsWindow(bool display)
        {
            var state = 0f;
            var initValue = windowOffset;
            var target = display ? 0f : WindowWidth;
            if (display)
                drawToolsPanel = true;
            while (state < 1f)
            {
                state += AnimationSpeed * Time.deltaTime / WindowWidth;
                windowOffset = Mathf.Lerp(initValue, target, CubicInterpolate01(1f, 3f, state));
                yield return null;
            }
            drawToolsPanel = display;
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            lastLogType = type;
            lastLogMessage = logString;
        }

        private void OnGUI()
        {
            guiScale = Screen.width / (float)ReferenceResolution;
            scaledScreenWidth = Screen.width / guiScale;
            scaledScreenHeight = Screen.height / guiScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, guiScale));

            DrawFPS();
            DrawLog();
            DrawToolsWindow();
        }

        private void DrawFPS()
        {
            GUI.color = fpsBoxColor;
            fpsLabel.text = $" fps: {fps} avg fps: {avgfps}";
            var rect = new Rect(fpsPosition, fpsTextStyle.CalcSize(fpsLabel));
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, fpsLabel, fpsTextStyle);
        }

        private void DrawLog()
        {
            if (lastLogType == LogType.Log)
                return;

            const float logHeight = 0.06f;
            var logRect = new Rect(0, scaledScreenHeight * (1 - logHeight), scaledScreenWidth, scaledScreenHeight * logHeight);
            GUI.color = lastLogType switch
            {
                LogType.Warning => warningColor,
                _ => errorColor
            };
            GUI.DrawTexture(logRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(logRect, lastLogMessage, logTextStyle);
        }

        private void DrawToolsWindow()
        {
            var rect = new Rect(scaledScreenWidth * (1 - WindowWidth) + windowOffset * scaledScreenWidth, 0, scaledScreenWidth * WindowWidth, scaledScreenHeight);
            float sideButtonWidth = scaledScreenWidth * 0.025f;
            float sideButtonHeight = scaledScreenHeight * 0.6f;
            if (GUI.Button(new Rect(rect.xMin - sideButtonWidth, scaledScreenHeight * 0.5f - sideButtonHeight * 0.5f, sideButtonWidth, sideButtonHeight), drawToolsPanel ? ">" : "<"))
                StartCoroutine(SlideToolsWindow(!drawToolsPanel));

            if (!drawToolsPanel)
                return;

            GUILayout.BeginArea(rect);
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Benchmark Tool");

            scroll = GUILayout.BeginScrollView(scroll);
            DrawGeneralPanel();
            DrawGeometryPanel();
            DrawMaterialsPanel();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawGeneralPanel()
        {
            GUILayout.BeginVertical("Box");
            GUILayout.Label("General");
            GUILayout.Label($"Resolution: {Screen.currentResolution}");
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Set Scale");
                if (GUILayout.Button("x1.0")) ScaleResolution(1f);
                if (GUILayout.Button("x0.7")) ScaleResolution(0.7f);
                if (GUILayout.Button("x0.5")) ScaleResolution(0.5f);
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"FPS Limit: {Application.targetFrameRate}");
                if (GUILayout.Button("Default")) Application.targetFrameRate = -1;
                if (GUILayout.Button("60")) Application.targetFrameRate = 60;
                if (GUILayout.Button("30")) Application.targetFrameRate = 30;
                if (GUILayout.Button("20")) Application.targetFrameRate = 20;
            }
            GUILayout.EndVertical();
        }

        private void DrawGeometryPanel()
        {
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Generate Geometry");
            staticBatching = GUILayout.Toggle(staticBatching, "Static Batching");
            combineMeshes = GUILayout.Toggle(combineMeshes, "Combine Meshes");
            GUIUtils.Vector3IntField("Count", ref count);
            GUIUtils.Slider("Spacing", 1f, 10f, ref spacing);
            GUIUtils.FloatField("Scale", ref scale);
            GUILayout.Label("Create:");
            for (int i = 0; i < builtinMeshes.Length; i++)
            {
                if (GUILayout.Button(BuiltinMeshFiles[i].name)) GenerateMeshes(builtinMeshes[i]);
            }

            if (customMeshes != null)
            {
                foreach (var mesh in customMeshes)
                {
                    if (mesh == null) continue;
                    if (GUILayout.Button(mesh.name)) GenerateMeshes(mesh);
                }
            }

            GUILayout.Label("Created Info:");
            GUILayout.Label(createdInfo);
            GUILayout.EndVertical();
        }

        private void DrawMaterialsPanel()
        {
            if (materialGUIs == null) return;

            GUILayout.BeginVertical("Box");
            GUILayout.Label("Materials");
            for (int i = 0; i < materialGUIs.Length; i++)
            {
                var isActive = activeMaterial == materialGUIs[i];
                if (GUILayout.Toggle(isActive, materialGUIs[i].Material.name, "Button") != isActive)
                {
                    activeMaterial = materialGUIs[i];
                    if (root != null)
                    {
                        foreach (var rend in root.GetComponentsInChildren<MeshRenderer>())
                            rend.sharedMaterial = activeMaterial.Material;
                    }
                }
            }

            if (activeMaterial != null)
            {
                GUILayout.Label($"Active Material: {activeMaterial.Material.name}");
                activeMaterial.OnGUI();
            }

            GUILayout.EndVertical();
        }

        private void ScaleResolution(float scale)
        {
            Screen.SetResolution((int)(initResolution.x * scale), (int)(initResolution.y * scale), true);
        }

        private IEnumerable<Vector3> GetPositions()
        {
            var offset = (count.x - 1) * spacing * 0.5f;
            for (int x = 0; x < count.x; x++)
            {
                var posX = x * spacing - offset;
                for (int y = 0; y < count.y; y++)
                {
                    var posY = y * spacing;
                    for (int z = 0; z < count.z; z++)
                        yield return new Vector3(posX, posY, z * spacing);
                }
            }
        }

        private void InitRoot()
        {
            if (root == null)
            {
                root = new GameObject("ObjectsRoot").transform;
                return;
            }

            for (int i = root.childCount - 1; i >= 0 ; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        private void GenerateMeshes(Mesh mesh)
        {
            InitRoot();
            var totalMeshes = count.x * count.y * count.z;
            var totalVerts = totalMeshes * mesh.vertexCount;
            var meshScale = new Vector3(scale, scale, scale);

            if (combineMeshes)
            {
                var meshName = $"Combined_{mesh.name}";
                var groupVertices = 0;
                var instances = new List<CombineInstance>();
                var combinedMesh = new Mesh() { name = meshName };
                CreateMeshRenderer(combinedMesh, activeMaterial?.Material);
                var groupCount = 1;

                foreach (var pos in GetPositions())
                {
                    if (ushort.MaxValue - groupVertices < mesh.vertexCount)
                    {
                        combinedMesh.CombineMeshes(instances.ToArray());

                        instances.Clear();
                        combinedMesh = new Mesh() { name = meshName };
                        CreateMeshRenderer(combinedMesh, activeMaterial?.Material);
                        groupVertices = 0;
                        groupCount++;
                    }

                    groupVertices += mesh.vertexCount;
                    instances.Add(new CombineInstance
                    {
                        mesh = mesh,
                        transform = Matrix4x4.TRS(pos, Quaternion.identity, meshScale)
                    });
                }

                combinedMesh.CombineMeshes(instances.ToArray());
                totalMeshes = groupCount;
            }
            else
            {
                foreach (var pos in GetPositions())
                {
                    var rend = CreateMeshRenderer(mesh, activeMaterial?.Material);
                    rend.transform.position = pos;
                    rend.transform.localScale = meshScale;
                }
            }

            if (staticBatching)
                StaticBatchingUtility.Combine(root.gameObject);

            createdInfo = $"Objects {totalMeshes}\n";
            createdInfo += $"Total Verts {totalVerts}\n";
            createdInfo += $"Combined {combineMeshes}\n";
            createdInfo += $"Batching {staticBatching}";
        }

        private MeshRenderer CreateMeshRenderer(Mesh mesh, Material material)
        {
            var obj = new GameObject(mesh.name);
            obj.transform.SetParent(root);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = obj.AddComponent<MeshRenderer>();
            rend.sharedMaterial = material;
            return rend;
        }

        private static float CubicInterpolate01(float t1, float t2, float v)
        {
            float vv = v * v;
            float a0 = t2 - t1 - 1f;
            float a1 = 1f - a0;
            float a2 = t1 - 1f;
            return a0 * v * vv + a1 * vv + a2 * v;
        }

    #if UNITY_EDITOR
        [MenuItem("GameObject/Create Benchmark Tool")]
        private static void CreateTool()
        {
            var obj = new GameObject("Benchmark Tool");
            obj.AddComponent<BenchmarkTool>();
            Selection.activeGameObject = obj;
        }
    #endif
    }
}
