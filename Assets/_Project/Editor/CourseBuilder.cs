using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SvoiPut.EditorTools
{
    /// <summary>
    /// Генератор всей сцены «Ржавый квартал»: материалы, крыши, цех, депо, игрок, системы и UI.
    /// Запуск: меню «Свой путь/Собрать игровую сцену» или -executeMethod из командной строки.
    /// </summary>
    public static partial class CourseBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string MaterialsFolder = "Assets/_Project/Materials";
        private const string ScenesFolder = "Assets/_Project/Scenes";

        // Городская палитра: бетон, кирпич, ржавчина, хром, графит.
        private static Material matConcrete;
        private static Material matConcreteDark;
        private static Material matBrick;
        private static Material matRust;
        private static Material matRustDark;
        private static Material matChrome;
        private static Material matGraphite;
        private static Material matAsphalt;
        private static Material matWindow;
        private static Material matWindowLit;
        private static Material matSignal;
        private static Material matDanger;
        private static Material matElectric;
        private static Material matSlab;
        private static Material matCheckpoint;
        private static Material matChromeGlow;
        private static Material matWet;
        private static Material matJacket;
        private static Material matJacketDark;
        private static Material matPants;
        private static Material matMask;
        private static Material matParticle;
        private static Material matSmoke;
        private static Material matSkybox;

        // Модели, собранные в Blender (Tools/Blender/build_models.py).
        private static GameObject modelWaterTower;
        private static GameObject modelACUnit;
        private static GameObject modelCrate;
        private static GameObject modelBarrel;
        private static GameObject modelStreetLamp;
        private static GameObject modelPatrolCar;
        private static GameObject modelTrainCar;
        private static GameObject modelSurvivor;
        private static GameObject modelMachine;

        private static Font worldFont;
        private static readonly List<Checkpoint> checkpoints = new List<Checkpoint>();

        [MenuItem("Свой путь/1. Собрать игровую сцену", false, 10)]
        public static void BuildSceneMenu()
        {
            BuildAll();
            EditorUtility.DisplayDialog("Свой путь", "Сцена собрана:\n" + ScenePath, "ОК");
        }

        public static void BuildAll()
        {
            try
            {
                Debug.Log("[SvoiPut] Начало генерации сцены...");
                EnsureFolder(ScenesFolder);
                ConfigurePlayerSettings();

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                worldFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (worldFont == null) worldFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (worldFont == null) Debug.LogWarning("[SvoiPut] Не найден встроенный шрифт для объёмных надписей.");

                BuildMaterials();
                LoadModels();
                SetupLighting();

                var systems = new GameObject("Systems");
                var manager = systems.AddComponent<GameManager>();
                var audio = systems.AddComponent<AudioDirector>();
                var ui = systems.AddComponent<UIManager>();

                var playerGo = CreatePlayer(out var player, out var timeSlow, out var playerVisual);
                var cameraGo = CreateCamera(playerGo.transform, out var playerCamera);
                player.cameraTransform = cameraGo.transform;
                timeSlow.playerCamera = playerCamera;
                playerVisual.controller = player;

                var levelRoot = new GameObject("Level").transform;
                var envRoot = new GameObject("Environment").transform;
                checkpoints.Clear();

                Transform startSpawn;
                BuildLevel(levelRoot, out startSpawn);
                BuildEnvironment(envRoot);

                manager.player = player;
                manager.playerCamera = playerCamera;
                manager.timeSlow = timeSlow;
                manager.ui = ui;
                manager.audio = audio;
                manager.startSpawn = startSpawn;
                manager.checkpoints = checkpoints.ToArray();

                var scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);

                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[SvoiPut] Сцена успешно собрана: " + ScenePath + " (чекпоинтов: " + checkpoints.Count + ")");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SvoiPut] Ошибка генерации сцены: " + exception);
                throw;
            }
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "SvoiPut";
            PlayerSettings.productName = "SvoiPut";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.usePlayerLog = true;

            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowDistance = 95f;
            QualitySettings.pixelLightCount = 8;
        }

        // ------------------------------------------------------------------ материалы

        private static void BuildMaterials()
        {
            if (AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.DeleteAsset(MaterialsFolder);
            }
            EnsureFolder(MaterialsFolder);

            Shader standard = Shader.Find("Standard");
            if (standard == null) throw new Exception("Не найден встроенный шейдер Standard (проект должен использовать Built-in Render Pipeline).");

            matConcrete = MakeMaterial("M_Concrete", standard, new Color(0.30f, 0.285f, 0.265f), 0.05f, 0.22f, Color.black, 0f);
            matConcreteDark = MakeMaterial("M_ConcreteDark", standard, new Color(0.155f, 0.15f, 0.145f), 0.05f, 0.18f, Color.black, 0f);
            matBrick = MakeMaterial("M_Brick", standard, new Color(0.265f, 0.135f, 0.10f), 0.05f, 0.2f, Color.black, 0f);
            matRust = MakeMaterial("M_Rust", standard, new Color(0.37f, 0.16f, 0.085f), 0.45f, 0.3f, Color.black, 0f);
            matRustDark = MakeMaterial("M_RustDark", standard, new Color(0.155f, 0.07f, 0.045f), 0.55f, 0.26f, Color.black, 0f);
            matChrome = MakeMaterial("M_Chrome", standard, new Color(0.60f, 0.63f, 0.65f), 0.92f, 0.8f, Color.black, 0f);
            matGraphite = MakeMaterial("M_Graphite", standard, new Color(0.085f, 0.095f, 0.105f), 0.55f, 0.42f, Color.black, 0f);
            matAsphalt = MakeMaterial("M_Asphalt", standard, new Color(0.08f, 0.085f, 0.09f), 0.12f, 0.2f, Color.black, 0f);
            matWindow = MakeMaterial("M_Window", standard, new Color(0.02f, 0.028f, 0.032f), 0.35f, 0.9f, Color.black, 0f);
            matWindowLit = MakeMaterial("M_WindowLit", standard, new Color(0.34f, 0.19f, 0.075f), 0.1f, 0.5f, new Color(1f, 0.62f, 0.3f), 1.05f);
            matSignal = MakeMaterial("M_Signal", standard, new Color(0.5f, 0.2f, 0.08f), 0.25f, 0.4f, new Color(1f, 0.45f, 0.12f), 1.5f);
            matDanger = MakeMaterial("M_Danger", standard, new Color(0.34f, 0.08f, 0.05f), 0.3f, 0.45f, new Color(1f, 0.18f, 0.06f), 2.2f);
            matElectric = MakeMaterial("M_Electric", standard, new Color(0.35f, 0.62f, 0.75f), 0.2f, 0.7f, new Color(0.7f, 0.92f, 1f), 2.8f);
            matSlab = MakeMaterial("M_Slab", standard, new Color(0.19f, 0.18f, 0.17f), 0.35f, 0.24f, new Color(0.55f, 0.24f, 0.08f), 0.015f);
            matCheckpoint = MakeMaterial("M_Checkpoint", standard, new Color(0.42f, 0.17f, 0.075f), 0.35f, 0.4f, new Color(1f, 0.42f, 0.12f), 1.2f);
            matChromeGlow = MakeMaterial("M_ChromeGlow", standard, new Color(0.64f, 0.67f, 0.69f), 0.85f, 0.75f, new Color(0.85f, 0.9f, 0.95f), 0.75f);
            matWet = MakeMaterial("M_Wet", standard, new Color(0.022f, 0.027f, 0.032f), 0.78f, 0.93f, Color.black, 0f);
            matJacket = MakeMaterial("M_Jacket", standard, new Color(0.31f, 0.145f, 0.085f), 0.15f, 0.42f, Color.black, 0f);
            matJacketDark = MakeMaterial("M_JacketDark", standard, new Color(0.16f, 0.075f, 0.045f), 0.2f, 0.35f, Color.black, 0f);
            matPants = MakeMaterial("M_Pants", standard, new Color(0.095f, 0.10f, 0.105f), 0.15f, 0.4f, Color.black, 0f);
            matMask = MakeMaterial("M_Mask", standard, new Color(0.50f, 0.48f, 0.44f), 0.1f, 0.55f, Color.black, 0f);

            var particleShader = LoadShader("SvoiPut/AdditiveParticles", "Assets/_Project/Shaders/AdditiveParticles.shader");
            matParticle = new Material(particleShader) { name = "M_Particles" };
            matParticle.SetColor("_Tint", Color.white);
            AssetDatabase.CreateAsset(matParticle, MaterialsFolder + "/M_Particles.mat");

            var smokeShader = LoadShader("SvoiPut/SmokeParticles", "Assets/_Project/Shaders/SmokeParticles.shader");
            matSmoke = new Material(smokeShader) { name = "M_Smoke" };
            matSmoke.SetColor("_Tint", new Color(0.34f, 0.32f, 0.30f, 1f));
            AssetDatabase.CreateAsset(matSmoke, MaterialsFolder + "/M_Smoke.mat");

            var skyShader = LoadShader("SvoiPut/CitySkybox", "Assets/_Project/Shaders/CitySkybox.shader");
            matSkybox = new Material(skyShader) { name = "M_CitySkybox" };
            matSkybox.SetColor("_TopColor", new Color(0.085f, 0.105f, 0.14f));
            matSkybox.SetColor("_HorizonColor", new Color(0.42f, 0.315f, 0.255f));
            matSkybox.SetColor("_GroundColor", new Color(0.045f, 0.047f, 0.05f));
            matSkybox.SetColor("_CloudColor", new Color(0.52f, 0.48f, 0.45f));
            matSkybox.SetColor("_SunColor", new Color(1f, 0.80f, 0.56f));
            matSkybox.SetVector("_SunDir", new Vector4(0.18f, 0.26f, 0.95f, 0f));
            matSkybox.SetFloat("_SunSize", 0.0032f);
            matSkybox.SetFloat("_CloudScale", 2.7f);
            matSkybox.SetFloat("_CloudCover", 0.5f);
            matSkybox.SetFloat("_Exposure", 1.02f);
            AssetDatabase.CreateAsset(matSkybox, MaterialsFolder + "/M_CitySkybox.mat");
        }

        private static Material MakeMaterial(string name, Shader shader, Color color, float metallic, float smoothness, Color emission, float emissionIntensity)
        {
            var material = new Material(shader) { name = name };
            material.SetColor("_Color", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            if (emissionIntensity > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission * emissionIntensity);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(material, MaterialsFolder + "/" + name + ".mat");
            return material;
        }

        private static Shader LoadShader(string shaderName, string assetPath)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            if (shader == null) throw new Exception("Не найден шейдер: " + shaderName);
            return shader;
        }

        // ------------------------------------------------------------------ модели Blender

        private static void LoadModels()
        {
            modelWaterTower = LoadModel("WaterTower");
            modelACUnit = LoadModel("ACUnit");
            modelCrate = LoadModel("Crate");
            modelBarrel = LoadModel("Barrel");
            modelStreetLamp = LoadModel("StreetLamp");
            modelPatrolCar = LoadModel("PatrolCar");
            modelTrainCar = LoadModel("TrainCar");
            modelSurvivor = LoadModel("Survivor");
            modelMachine = LoadModel("IndustrialMachine");
        }

        private static GameObject LoadModel(string name)
        {
            string path = "Assets/_Project/Models/" + name + ".fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[SvoiPut] Не найдена модель: " + path + ". Запустите Tools/Blender/build_models.py.");
            }
            return prefab;
        }

        /// <summary>Ставит FBX-модель в сцену, подменяет импортированные материалы на проектные.</summary>
        private static GameObject PlaceModel(GameObject prefab, Transform parent, string name, Vector3 position,
            float yaw = 0f, float scale = 1f, bool shadows = true)
        {
            if (prefab == null) return null;

            // FBX-корень несёт осевой поворот и офсет объекта — сохраняем их на вложенном инстансе,
            // а позицию/поворот/масштаб сцены задаём якорю.
            var anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = position;
            anchor.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            anchor.transform.localScale = Vector3.one * scale;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, anchor.transform);
            instance.name = name + "Model";
            instance.transform.localPosition = prefab.transform.localPosition;
            instance.transform.localRotation = prefab.transform.localRotation;
            instance.transform.localScale = prefab.transform.localScale;

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = ResolveModelMaterial(materials[i]);
                }
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = shadows;
            }
            return anchor;
        }

        private static Material ResolveModelMaterial(Material imported)
        {
            if (imported == null) return null;
            string key = imported.name;
            int dot = key.IndexOf('.');
            if (dot > 0) key = key.Substring(0, dot);

            switch (key)
            {
                case "M_Concrete": return matConcrete;
                case "M_ConcreteDark": return matConcreteDark;
                case "M_Brick": return matBrick;
                case "M_Rust": return matRust;
                case "M_RustDark": return matRustDark;
                case "M_Chrome": return matChrome;
                case "M_ChromeGlow": return matChromeGlow;
                case "M_Graphite": return matGraphite;
                case "M_Asphalt": return matAsphalt;
                case "M_Window": return matWindow;
                case "M_WindowLit": return matWindowLit;
                case "M_Signal": return matSignal;
                case "M_Danger": return matDanger;
                case "M_Jacket": return matJacket;
                case "M_JacketDark": return matJacketDark;
                case "M_Pants": return matPants;
                case "M_Mask": return matMask;
                case "M_Slab": return matSlab;
                default: return imported;
            }
        }


        // ------------------------------------------------------------------ освещение

        private static void SetupLighting()
        {
            RenderSettings.skybox = matSkybox;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.34f, 0.36f, 0.39f);
            RenderSettings.ambientEquatorColor = new Color(0.26f, 0.235f, 0.21f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.095f, 0.09f);
            RenderSettings.ambientIntensity = 1.1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.19f, 0.175f, 0.16f);
            RenderSettings.fogDensity = 0.0075f;
            RenderSettings.reflectionIntensity = 0.45f;

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.86f, 0.71f);
            sun.intensity = 1.28f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sunGo.transform.rotation = Quaternion.Euler(36f, -34f, 0f);

            var fillGo = new GameObject("FillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.46f, 0.55f, 0.68f);
            fill.intensity = 0.38f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(20f, 148f, 0f);
        }

        // ------------------------------------------------------------------ игрок

        private static GameObject CreatePlayer(out PlayerController player, out TimeSlowAbility timeSlow, out PlayerVisual visual)
        {
            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(0f, 0.12f, -1.5f);

            var controller = playerGo.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 50f;
            controller.stepOffset = 0.45f;
            controller.skinWidth = 0.03f;
            controller.minMoveDistance = 0f;

            player = playerGo.AddComponent<PlayerController>();
            timeSlow = playerGo.AddComponent<TimeSlowAbility>();
            visual = playerGo.AddComponent<PlayerVisual>();

            var visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(playerGo.transform, false);
            player.visualRoot = visualRoot;

            var rig = new GameObject("BodyRig").transform;
            rig.SetParent(visualRoot, false);

            // Выживший: модель из Blender, налобный фонарь и пыль при посадке.
            if (modelSurvivor != null)
            {
                PlaceModel(modelSurvivor, rig, "SurvivorModel", Vector3.zero, 0f, 1f);
            }
            else
            {
                Capsule(rig, "FallbackBody", new Vector3(0f, 0.9f, 0f), new Vector3(0.5f, 0.9f, 0.5f), matJacket);
            }

            visual.rig = rig;
            visual.thruster = null;
            visual.landPuff = CreateLandPuff(visualRoot);

            var lightGo = new GameObject("Headlamp");
            lightGo.transform.SetParent(rig, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.72f, 0.24f);
            lightGo.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
            var suitLight = lightGo.AddComponent<Light>();
            suitLight.type = LightType.Spot;
            suitLight.color = new Color(1f, 0.87f, 0.68f);
            suitLight.range = 18f;
            suitLight.spotAngle = 52f;
            suitLight.intensity = 1.15f;
            suitLight.shadows = LightShadows.None;
            visual.suitLight = suitLight;

            return playerGo;
        }

        private static ParticleSystem CreateLandPuff(Transform parent)
        {
            var ps = CreateParticleSystem(parent, "LandPuff", new Vector3(0f, 0.08f, 0f), new Color(0.56f, 0.50f, 0.44f, 0.55f),
                0f, 2.1f, 0.07f, 0.5f, false, ParticleSystemShapeType.Cone, Vector3.zero);
            var shape = ps.shape;
            shape.angle = 78f;
            shape.radius = 0.25f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.25f;
            main.gravityModifier = 0.18f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            return ps;
        }

        private static GameObject CreateCamera(Transform target, out PlayerCamera playerCamera)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 900f;
            cam.fieldOfView = 60f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            camGo.AddComponent<AudioListener>();

            playerCamera = camGo.AddComponent<PlayerCamera>();
            playerCamera.target = target;
            playerCamera.cam = cam;
            camGo.transform.position = target.position + new Vector3(0f, 3.2f, -6f);
            return camGo;
        }

        // ------------------------------------------------------------------ уровень

        private static void BuildLevel(Transform root, out Transform startSpawn)
        {
            var spawn = new GameObject("StartSpawn");
            spawn.transform.SetParent(root, false);
            spawn.transform.position = new Vector3(0f, 0.06f, -1.5f);
            spawn.transform.rotation = Quaternion.identity;
            startSpawn = spawn.transform;

            // ===== СТАРТ: крыша жилого дома =====
            Platform(root, "StartPlatform", new Vector3(0f, -0.5f, 2f), new Vector3(15f, 1f, 17f), matConcrete, matRust);
            Building(root, "StartBuilding", new Vector3(0f, -7f, 2f), new Vector3(15.5f, 14f, 17.5f), matBrick,
                true, true, true, false, 3101);
            AddRoofParapet(root, new Vector3(0f, 0f, 2f), new Vector2(15f, 17f), false, true);
            ACUnit(root, new Vector3(-5.7f, 0f, 6.6f), 18f);
            ACUnit(root, new Vector3(5.4f, 0f, -3.6f), -22f);
            Antenna(root, new Vector3(6.6f, 0f, -4.2f), 3.2f);
            WaterTower(root, new Vector3(5.5f, 0f, 6.2f), 0.78f);
            Crate(root, new Vector3(-5.9f, 0f, -4.3f), 1.1f, 16f);
            Puddle(root, new Vector3(-2.6f, 0.012f, 3.4f), new Vector2(2.8f, 1.7f), 14f);
            CreateStartArch(root, 0f, 6f);
            Chevrons(root, new Vector3(0f, 0.03f, -0.9f), 3, matSignal);
            SectorMarker(root, new Vector3(-6.7f, 3.1f, 10.2f), 15f, "01", "КРЫШИ", UIColors.Rust);

            var startLightGo = new GameObject("StartLight");
            startLightGo.transform.SetParent(root, false);
            startLightGo.transform.position = new Vector3(0f, 3.8f, 3f);
            var startLight = startLightGo.AddComponent<Light>();
            startLight.type = LightType.Point;
            startLight.color = new Color(1f, 0.66f, 0.38f);
            startLight.range = 16f;
            startLight.intensity = 1.8f;
            startLight.shadows = LightShadows.None;

            // ===== СЕКТОР 01: КРЫШИ (маршрут уходит влево) =====
            Platform(root, "Walkway01", new Vector3(-2.6f, -0.5f, 13f), new Vector3(6f, 1f, 7f), matConcrete, matRust);
            Building(root, "WalkwayBuilding", new Vector3(-2.6f, -8f, 13f), new Vector3(6.5f, 16f, 7.5f), matConcreteDark,
                false, false, true, false, 3102);
            Chevrons(root, new Vector3(-2.6f, 0.03f, 11.6f), 2, matSignal);
            RailingZ(root, -5.5f, 0.02f, 9.7f, 16.3f, 0.9f, matRustDark);
            RailingZ(root, 0.3f, 0.02f, 9.7f, 16.3f, 0.9f, matRustDark);

            // Обрушающиеся плиты: диагональ вправо.
            float[] tileX = { -2.2f, -1.0f, 0.2f, 1.4f, 2.6f };
            for (int i = 0; i < tileX.Length; i++)
            {
                float z = 19f + i * 2.75f;
                CreateVanishingTile(root, new Vector3(tileX[i], -0.2f, z), i);
                AddScaffold(root, new Vector3(tileX[i], -10.4f, z), new Vector3(3.2f, 0.4f, 2.4f), 10.2f);
            }

            Platform(root, "Landing01", new Vector3(2.9f, -0.5f, 35.8f), new Vector3(7f, 1f, 8f), matConcrete, matRust);
            Building(root, "LandingBuilding", new Vector3(2.9f, -7f, 35.8f), new Vector3(7.5f, 14f, 8.5f), matConcrete,
                true, true, true, false, 3103);
            ACUnit(root, new Vector3(5.3f, 0f, 34.2f), -12f);
            Crate(root, new Vector3(0.5f, 0f, 37.6f), 0.9f, -18f);
            Puddle(root, new Vector3(3.7f, 0.012f, 38.0f), new Vector2(2.2f, 1.3f), -8f);
            Chevrons(root, new Vector3(2.9f, 0.03f, 32.4f), 2, matSignal);

            // Вентилятор на крыше: проход по краю.
            Platform(root, "FanPlatform01", new Vector3(3f, -0.5f, 45.8f), new Vector3(11f, 1f, 8f), matConcrete, matRust);
            Building(root, "FanBuilding", new Vector3(3f, -6f, 45.8f), new Vector3(11.5f, 12f, 8.5f), matBrick,
                true, true, true, false, 3104);
            BuildFanHazard(root, new Vector3(3f, 0f, 45.8f), 3, 5.0f, 0.68f, 68f, 0f, "ВЕНТИЛЯТОР");
            Puddle(root, new Vector3(-1.2f, 0.012f, 42.6f), new Vector2(1.7f, 1.1f), 30f);

            // CP1
            Platform(root, "CheckpointPlatform01", new Vector3(-1.6f, -0.5f, 53.8f), new Vector3(9f, 1f, 8f), matConcrete, matChrome);
            Building(root, "CheckpointBuilding01", new Vector3(-1.6f, -5.5f, 53.8f), new Vector3(9.5f, 11f, 8.5f), matBrick,
                true, true, true, false, 3105);
            checkpoints.Add(CreateCheckpoint(root, new Vector3(-1.6f, 0f, 53.8f), 0));
            Antenna(root, new Vector3(2.0f, 0f, 51.2f), 2.8f);
            Crate(root, new Vector3(-5.2f, 0f, 51.4f), 1.0f, 24f);
            Puddle(root, new Vector3(0.9f, 0.012f, 55.8f), new Vector2(2.0f, 1.2f), -15f);

            // ===== СЕКТОР 02: ЦЕХ (маршрут уходит вправо) =====
            Platform(root, "FactoryEntry", new Vector3(-1.6f, -0.5f, 61f), new Vector3(6f, 1f, 6f), matConcrete, matDanger);
            CreateCrusher(root, new Vector3(-1.6f, 0f, 61f));
            AddSectionTrigger(root, new Vector3(-1.6f, 0f, 57.9f), "02 · ЦЕХ", "ПРЕСС");

            var platformA = Platform(root, "MovingPlatformA", new Vector3(1.4f, -0.25f, 66.5f), new Vector3(3.2f, 0.5f, 3.2f), matGraphite, matChrome);
            var moverA = platformA.AddComponent<MovingPlatform>();
            moverA.offset = new Vector3(0f, 0f, 9f);
            moverA.period = 5.2f;

            Platform(root, "LedgeB", new Vector3(4.0f, -0.5f, 79f), new Vector3(5f, 1f, 5f), matConcrete, matRust);
            Building(root, "LedgeBuilding", new Vector3(4.0f, -8f, 79f), new Vector3(5.5f, 16f, 5.5f), matConcreteDark,
                false, false, false, false, 3106);
            Crate(root, new Vector3(5.4f, 0f, 80.4f), 0.8f, 12f);
            Chevrons(root, new Vector3(4.0f, 0.03f, 77.4f), 2, matSignal);

            var liftPlatform = Platform(root, "LiftPlatform", new Vector3(3.8f, 0.15f, 84f), new Vector3(3f, 0.3f, 3f), matGraphite, matSignal);
            var lift = liftPlatform.AddComponent<MovingPlatform>();
            lift.offset = new Vector3(0f, 4.2f, 0f);
            lift.period = 5.6f;
            BuildLiftFrame(root, new Vector3(3.8f, 0f, 84f));

            Platform(root, "UpperFactory", new Vector3(0.6f, 4f, 89.8f), new Vector3(7f, 1f, 6f), matConcrete, matChrome);
            Chevrons(root, new Vector3(0.6f, 4.53f, 87.5f), 2, matChrome);
            RailingZ(root, -2.9f, 4.52f, 87.2f, 92.6f, 0.7f, matRustDark);
            RailingZ(root, 4.1f, 4.52f, 87.2f, 92.6f, 0.7f, matRustDark);

            // Электрический коридор с конвейером.
            Platform(root, "ArcBridge", new Vector3(0.6f, -0.5f, 97f), new Vector3(4f, 1f, 18f), matConcrete, matDanger);
            Building(root, "ArcBridgeSupport", new Vector3(0.6f, -8f, 97f), new Vector3(4.5f, 16f, 18.5f), matConcreteDark,
                false, false, false, false, 3107);
            CreateConveyor(root, new Vector3(0.6f, 0f, 99f));
            CreateArcGate(root, 0.6f, 94.8f, 0f);
            CreateArcGate(root, 0.6f, 98.5f, 1.1f);
            CreateArcGate(root, 0.6f, 102.2f, 2.2f);
            WorldSign(root, "ДУГИ", "ПАУЗА МЕЖДУ РАЗРЯДАМИ", new Vector3(3.9f, 3.1f, 90.6f), UIColors.Electric, 0.16f, -14f);
            RailingZ(root, -1.45f, 0.02f, 93.2f, 105.5f, 0.8f, matRustDark);
            RailingZ(root, 2.65f, 0.02f, 93.2f, 105.5f, 0.8f, matRustDark);

            Platform(root, "CheckpointPlatform02", new Vector3(-1.8f, -0.5f, 110.5f), new Vector3(9f, 1f, 8f), matConcrete, matChrome);
            Building(root, "CheckpointBuilding02", new Vector3(-1.8f, -5.5f, 110.5f), new Vector3(9.5f, 11f, 8.5f), matBrick,
                true, true, true, false, 3108);
            checkpoints.Add(CreateCheckpoint(root, new Vector3(-1.8f, 0f, 110.5f), 1));
            ACUnit(root, new Vector3(-5.4f, 0f, 108.0f), 90f);
            Puddle(root, new Vector3(0.6f, 0.012f, 112.3f), new Vector2(1.9f, 1.2f), 8f);

            // ===== СЕКТОР 03: ДЕПО =====
            Platform(root, "Arena", new Vector3(-0.6f, -0.5f, 121.5f), new Vector3(13f, 1f, 12f), matConcrete, matRust);
            Building(root, "ArenaBuilding", new Vector3(-0.6f, -7f, 121.5f), new Vector3(13.5f, 14f, 12.5f), matBrick,
                true, true, true, false, 3109);
            BuildPatrolCar(root, new Vector3(-5.9f, 0f, 121.5f), 10f, 3.8f);
            Puddle(root, new Vector3(-4.8f, 0.012f, 116.8f), new Vector2(2.8f, 1.8f), 22f);
            Crate(root, new Vector3(3.8f, 0f, 117.0f), 1.0f, 8f);
            Barrel(root, new Vector3(-5.4f, 0f, 125.6f), 1f, 20f);
            AddSectionTrigger(root, new Vector3(-0.6f, 0f, 114.8f), "03 · ДЕПО", "ПОЕЗД 03:07");
            WorldBanner(root, "03:07", new Vector3(6.4f, 9.6f, 126f), 90f, new Vector2(7f, 3.2f), UIColors.Rust);

            // Мост с турбиной.
            Platform(root, "Bridge03", new Vector3(0.4f, -0.5f, 131f), new Vector3(2.8f, 1f, 10f), matConcrete, matDanger);
            Building(root, "BridgeSupport", new Vector3(0.4f, -8f, 131f), new Vector3(3.2f, 16f, 10.5f), matConcreteDark,
                false, false, false, false, 3110);
            BuildFanHazard(root, new Vector3(0.4f, 0f, 131f), 3, 1.32f, 0.66f, 150f, 0.3f, "ТУРБИНА");
            RailingZ(root, -0.95f, 0.02f, 126.6f, 135.6f, 0.85f, matRustDark);
            RailingZ(root, 1.75f, 0.02f, 126.6f, 135.6f, 0.85f, matRustDark);
            WorldSign(root, "ХРОНО / SHIFT", "МИР 0.4× — ТЫ 1.0×", new Vector3(-2.9f, 3.3f, 128.5f), UIColors.Chrono, 0.15f, 16f);

            Platform(root, "CheckpointPlatform03", new Vector3(2.8f, -0.5f, 141.5f), new Vector3(9f, 1f, 8f), matConcrete, matChrome);
            Building(root, "CheckpointBuilding03", new Vector3(2.8f, -6f, 141.5f), new Vector3(9.5f, 12f, 8.5f), matBrick,
                true, true, true, false, 3111);
            checkpoints.Add(CreateCheckpoint(root, new Vector3(2.8f, 0f, 141.5f), 2));
            Crate(root, new Vector3(-0.4f, 0f, 138.9f), 0.9f, -22f);
            Puddle(root, new Vector3(5.4f, 0.012f, 143.7f), new Vector2(2.0f, 1.3f), 16f);

            var platformC = Platform(root, "MovingPlatformC", new Vector3(0.2f, -0.25f, 150f), new Vector3(3.4f, 0.5f, 3.4f), matGraphite, matChrome);
            var moverC = platformC.AddComponent<MovingPlatform>();
            moverC.offset = new Vector3(0f, 0f, 9f);
            moverC.period = 4.4f;

            Platform(root, "FinalPlatform", new Vector3(-2f, -0.5f, 164f), new Vector3(11f, 1f, 9f), matConcrete, matRust);
            Building(root, "FinalBuilding", new Vector3(-2f, -7f, 164f), new Vector3(11.5f, 14f, 9.5f), matBrick,
                true, true, true, false, 3112);
            Platform(root, "Step1", new Vector3(-2f, 0.2f, 165.9f), new Vector3(7f, 0.4f, 1.0f), matConcrete, matChrome);
            Platform(root, "Step2", new Vector3(-2f, 0.4f, 166.6f), new Vector3(7f, 0.8f, 1.0f), matConcrete, matChrome);
            Platform(root, "Podium", new Vector3(0f, 0.1f, 172f), new Vector3(12f, 2.2f, 10f), matConcrete, matRust);
            Chevrons(root, new Vector3(-2f, 0.03f, 160.6f), 2, matSignal);
            Crate(root, new Vector3(-5.8f, 0f, 160.3f), 1.0f, 12f);
            Barrel(root, new Vector3(1.6f, 0f, 160.7f), 1f, -8f);
            Puddle(root, new Vector3(-3.9f, 0.012f, 166.3f), new Vector2(2.2f, 1.4f), 34f);
            RailingZ(root, -4.3f, 1.22f, 168f, 175f, 0.85f, matRustDark);
            RailingZ(root, 4.3f, 1.22f, 168f, 175f, 0.85f, matRustDark);

            CreatePendulum(root, new Vector3(0.4f, 1.2f, 171f));
            WorldBanner(root, "03:07", new Vector3(-8.2f, 7.3f, 168.2f), -90f, new Vector2(3.4f, 4.4f), UIColors.Chrome);
            SectorMarker(root, new Vector3(-2f, 8.3f, 164.2f), 0f, "03", "ДЕПО", UIColors.Rust);

            Platform(root, "StationPlatform", new Vector3(0f, 0.2f, 178f), new Vector3(16f, 2f, 5f), matConcrete, matChrome);
            BuildTrain(root, new Vector3(0f, 1.05f, 182.1f));
            CreateFinish(root, new Vector3(0f, 0f, 180f));
            AddCourseLights(root);
        }

        private static GameObject Platform(Transform parent, string name, Vector3 center, Vector3 size, Material main, Material trim)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = center;

            var collider = root.AddComponent<BoxCollider>();
            collider.size = size;

            Box(root.transform, "Visual", Vector3.zero, size, main, false, true);
            AddEdgeStrips(root.transform, size, trim);
            return root;
        }

        private static void AddEdgeStrips(Transform parent, Vector3 size, Material material)
        {
            float y = size.y * 0.5f + 0.012f;
            float x = size.x * 0.5f - 0.07f;
            float z = size.z * 0.5f - 0.07f;

            Box(parent, "TrimX1", new Vector3(-x, y, 0f), new Vector3(0.08f, 0.03f, size.z - 0.24f), material, false, false);
            Box(parent, "TrimX2", new Vector3(x, y, 0f), new Vector3(0.08f, 0.03f, size.z - 0.24f), material, false, false);
            Box(parent, "TrimZ1", new Vector3(0f, y, -z), new Vector3(size.x - 0.24f, 0.03f, 0.08f), material, false, false);
            Box(parent, "TrimZ2", new Vector3(0f, y, z), new Vector3(size.x - 0.24f, 0.03f, 0.08f), material, false, false);
        }

        private static void Chevrons(Transform parent, Vector3 position, int count, Material material)
        {
            for (int i = 0; i < count; i++)
            {
                float z = position.z + i * 1.3f;
                var left = Box(parent, "ChevronL", new Vector3(position.x - 0.19f, position.y, z), new Vector3(0.1f, 0.025f, 0.6f), material, false, false);
                left.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
                var right = Box(parent, "ChevronR", new Vector3(position.x + 0.19f, position.y, z), new Vector3(0.1f, 0.025f, 0.6f), material, false, false);
                right.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
            }
        }

        private static void CreateStartArch(Transform parent, float x, float z)
        {
            var root = new GameObject("StartArch").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(x, 0f, z);
            Box(root, "PostL", new Vector3(-4.1f, 2.2f, 0f), new Vector3(0.42f, 4.4f, 0.55f), matRust, false);
            Box(root, "PostR", new Vector3(4.1f, 2.2f, 0f), new Vector3(0.42f, 4.4f, 0.55f), matRust, false);
            Box(root, "PostGlowL", new Vector3(-4.1f, 2.2f, -0.3f), new Vector3(0.08f, 3.6f, 0.03f), matSignal, false, false);
            Box(root, "PostGlowR", new Vector3(4.1f, 2.2f, -0.3f), new Vector3(0.08f, 3.6f, 0.03f), matSignal, false, false);
            Box(root, "Beam", new Vector3(0f, 4.55f, 0f), new Vector3(9f, 0.42f, 0.6f), matGraphite, false);
            Box(root, "BeamGlow", new Vector3(0f, 4.37f, -0.32f), new Vector3(7.8f, 0.05f, 0.03f), matChrome, false, false);
            Box(root, "BeamPlate", new Vector3(0f, 4.62f, -0.34f), new Vector3(3.0f, 0.62f, 0.04f), matRustDark, false, false);
            WorldText(root, "СТАРТ", new Vector3(0f, 4.62f, -0.375f), 0.30f, UIColors.ChromeBright);
        }

        private static void CreateVanishingTile(Transform parent, Vector3 center, int index)
        {
            var root = new GameObject("VanishingTile_" + index);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = center;

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(3.2f, 0.4f, 2.4f);

            var visual = Box(root.transform, "Visual", Vector3.zero, new Vector3(3.2f, 0.4f, 2.4f), matSlab, false, true);
            Box(root.transform, "StripeL", new Vector3(-1.5f, 0.215f, 0f), new Vector3(0.08f, 0.02f, 2.2f), matSignal, false, false);
            Box(root.transform, "StripeR", new Vector3(1.5f, 0.215f, 0f), new Vector3(0.08f, 0.02f, 2.2f), matSignal, false, false);
            Box(root.transform, "Grate", new Vector3(0f, 0.212f, 0.7f), new Vector3(1.8f, 0.014f, 0.9f), matRustDark, false, false);

            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.3f, 0f);
            trigger.size = new Vector3(3.1f, 2.5f, 2.3f);

            var tile = root.AddComponent<VanishingTile>();
            tile.tileRenderer = visual.GetComponent<Renderer>();
            tile.solidCollider = collider;
        }

        private static void CreateArcGate(Transform parent, float positionX, float z, float phase)
        {
            var root = new GameObject("ArcGate_" + z);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(positionX, 0f, z);

            Box(root.transform, "PostL", new Vector3(-2.35f, 1.7f, 0f), new Vector3(0.4f, 3.4f, 0.5f), matRust, false);
            Box(root.transform, "PostR", new Vector3(2.35f, 1.7f, 0f), new Vector3(0.4f, 3.4f, 0.5f), matRust, false);
            Box(root.transform, "TopBar", new Vector3(0f, 3.4f, 0f), new Vector3(5.1f, 0.32f, 0.5f), matGraphite, false);
            Box(root.transform, "PostGlowL", new Vector3(-2.35f, 1.7f, -0.27f), new Vector3(0.1f, 2.1f, 0.03f), matDanger, false, false);
            Box(root.transform, "PostGlowR", new Vector3(2.35f, 1.7f, -0.27f), new Vector3(0.1f, 2.1f, 0.03f), matDanger, false, false);
            Box(root.transform, "TopGlow", new Vector3(0f, 3.26f, -0.27f), new Vector3(4.4f, 0.045f, 0.03f), matDanger, false, false);
            Box(root.transform, "InsulatorL", new Vector3(-2.0f, 3.1f, 0f), new Vector3(0.18f, 0.36f, 0.18f), matChrome, false, false);
            Box(root.transform, "InsulatorR", new Vector3(2.0f, 3.1f, 0f), new Vector3(0.18f, 0.36f, 0.18f), matChrome, false, false);

            var beams = new List<Renderer>();
            for (int row = 0; row < 4; row++)
            {
                float y = 0.58f + row * 0.55f;
                for (int segment = 0; segment < 3; segment++)
                {
                    float x = -1.35f + segment * 1.35f;
                    float jitter = ((row + segment) % 2 == 0) ? 0.05f : -0.04f;
                    var beam = Box(root.transform, "Arc_" + row + "_" + segment,
                        new Vector3(x, y + jitter, jitter * 0.7f),
                        new Vector3(1.34f, 0.04f, 0.07f), matElectric, false, false);
                    beam.transform.localRotation = Quaternion.Euler(0f, 0f, segment == 1 ? -4f : 4f);
                    beams.Add(beam.GetComponent<Renderer>());
                }
            }

            var hazardGo = new GameObject("Hazard");
            hazardGo.transform.SetParent(root.transform, false);
            hazardGo.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            var hazardCollider = hazardGo.AddComponent<BoxCollider>();
            hazardCollider.size = new Vector3(4.3f, 3.2f, 0.55f);
            hazardCollider.isTrigger = true;
            hazardCollider.enabled = false;
            var hazard = hazardGo.AddComponent<Hazard>();
            hazard.reason = "ЭЛЕКТРИЧЕСКАЯ ДУГА";

            var lightGo = new GameObject("ArcLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.72f, 0.9f, 1f);
            light.range = 7f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.None;

            var laser = root.AddComponent<PulsingLaser>();
            laser.beams = beams.ToArray();
            laser.hazardCollider = hazardCollider;
            laser.glowLight = light;
            laser.phase = phase;
        }

        private static Checkpoint CreateCheckpoint(Transform parent, Vector3 position, int index)
        {
            var root = new GameObject("Checkpoint_" + (index + 1));
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.8f, 0f);
            trigger.size = new Vector3(4.6f, 3.6f, 3.6f);

            var glow = new List<Renderer>();

            var postL = Box(root.transform, "PostL", new Vector3(-1.8f, 1.45f, 0f), new Vector3(0.18f, 2.9f, 0.18f), matCheckpoint, false, false);
            var postR = Box(root.transform, "PostR", new Vector3(1.8f, 1.45f, 0f), new Vector3(0.18f, 2.9f, 0.18f), matCheckpoint, false, false);
            var top = Box(root.transform, "Top", new Vector3(0f, 2.9f, 0f), new Vector3(3.9f, 0.18f, 0.18f), matCheckpoint, false, false);
            glow.Add(postL.GetComponent<Renderer>());
            glow.Add(postR.GetComponent<Renderer>());
            glow.Add(top.GetComponent<Renderer>());

            var ring = new GameObject("Ring").transform;
            ring.SetParent(root.transform, false);
            ring.localPosition = new Vector3(0f, 0.03f, 0f);
            const int ringSegments = 16;
            for (int i = 0; i < ringSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / ringSegments;
                var segment = Box(ring, "RingSegment_" + i,
                    new Vector3(Mathf.Cos(angle) * 1.8f, 0f, Mathf.Sin(angle) * 1.8f),
                    new Vector3(0.7f, 0.025f, 0.14f), matCheckpoint, false, false);
                segment.transform.localRotation = Quaternion.Euler(0f, 90f - angle * Mathf.Rad2Deg, 0f);
                glow.Add(segment.GetComponent<Renderer>());
            }

            var icon = Box(root.transform, "Icon", new Vector3(0f, 3.35f, 0f), new Vector3(0.36f, 0.36f, 0.36f), matChromeGlow, false, false);
            icon.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
            glow.Add(icon.GetComponent<Renderer>());

            var lightGo = new GameObject("BeaconLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.45f, 0.15f);
            light.range = 9f;
            light.intensity = 2.2f;
            light.shadows = LightShadows.None;

            var particles = CreateParticleSystem(root.transform, "CheckpointParticles", new Vector3(0f, 0.15f, 0f),
                new Color(1f, 0.48f, 0.18f, 0.7f), 11f, 1.3f, 0.07f, 1.5f, true, ParticleSystemShapeType.Cone, Vector3.zero);
            var shape = particles.shape;
            shape.angle = 12f;
            shape.radius = 0.8f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var label = WorldText(root.transform, (index + 1).ToString("00"), new Vector3(0f, 4.15f, 0.05f), 0.36f,
                UIColors.Rust, true, 1.25f, 0.75f);

            var spawn = new GameObject("Spawn");
            spawn.transform.SetParent(root.transform, false);
            spawn.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            spawn.transform.localRotation = Quaternion.identity;

            var checkpoint = root.AddComponent<Checkpoint>();
            checkpoint.index = index;
            checkpoint.spawnPoint = spawn.transform;
            checkpoint.glowRenderers = glow.ToArray();
            checkpoint.beaconLight = light;
            checkpoint.icon = icon.transform;
            checkpoint.particles = particles;
            checkpoint.label = label;
            checkpoint.safeColor = UIColors.Rust;
            checkpoint.activeColor = UIColors.ChromeBright;
            return checkpoint;
        }

        private static void CreateFinish(Transform parent, Vector3 position)
        {
            var root = new GameObject("Finish");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            var ring = new GameObject("FinishRing").transform;
            ring.SetParent(root.transform, false);
            ring.localPosition = new Vector3(0f, 2.35f, -0.12f);
            for (int i = 0; i < 18; i++)
            {
                float angle = i * Mathf.PI * 2f / 18f;
                var segment = Box(ring, "Segment_" + i,
                    new Vector3(Mathf.Cos(angle) * 1.5f, Mathf.Sin(angle) * 1.5f, 0f),
                    new Vector3(0.13f, 0.42f, 0.1f), matChromeGlow, false, false);
                segment.transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
            }

            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.1f, 0f);
            trigger.size = new Vector3(2.6f, 2.3f, 1.4f);

            var particles = CreateParticleSystem(root.transform, "FinishParticles", new Vector3(0f, 2.35f, -0.1f),
                new Color(0.86f, 0.92f, 0.98f, 0.8f), 24f, 1.6f, 0.075f, 1.7f, true, ParticleSystemShapeType.Cone, Vector3.zero);
            var shape = particles.shape;
            shape.angle = 22f;
            shape.radius = 1.2f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var lightGo = new GameObject("FinishLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.4f, -0.4f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.82f, 0.62f);
            light.range = 13f;
            light.intensity = 3.2f;
            light.shadows = LightShadows.None;

            var finish = root.AddComponent<FinishZone>();
            finish.ring = ring;
            finish.particles = particles;
            finish.finishLight = light;

            WorldText(root.transform, "ФИНИШ", new Vector3(0f, 4.45f, -0.32f), 0.28f, UIColors.ChromeBright);
        }

        private static void AddCourseLights(Transform root)
        {
            Vector3[] positions =
            {
                new Vector3(0f, 4.8f, 8f),
                new Vector3(-2.6f, 4.8f, 14f),
                new Vector3(1.4f, 5.0f, 27f),
                new Vector3(2.9f, 4.8f, 36f),
                new Vector3(3.0f, 5.4f, 45.5f),
                new Vector3(-1.6f, 4.8f, 53.5f),
                new Vector3(-1.6f, 5.0f, 61f),
                new Vector3(3.8f, 5.2f, 79.5f),
                new Vector3(3.8f, 9.0f, 84f),
                new Vector3(0.6f, 9.2f, 90f),
                new Vector3(0.6f, 5.2f, 95.5f),
                new Vector3(0.6f, 5.2f, 100.5f),
                new Vector3(-1.8f, 4.8f, 110f),
                new Vector3(-0.6f, 4.8f, 121f),
                new Vector3(0.4f, 4.8f, 132.5f),
                new Vector3(2.8f, 4.8f, 141f),
                new Vector3(0.2f, 4.8f, 150.5f),
                new Vector3(-2f, 4.8f, 163f),
                new Vector3(0f, 7.2f, 171f),
                new Vector3(0f, 5.2f, 178f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var go = new GameObject("CourseLight_" + i);
                go.transform.SetParent(root, false);
                go.transform.localPosition = positions[i];
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = i % 2 == 0 ? new Color(1f, 0.62f, 0.34f) : new Color(0.78f, 0.84f, 0.9f);
                bool inside = positions[i].z > 57f && positions[i].z < 113f;
                light.range = inside ? 18f : 14f;
                light.intensity = inside ? 1.5f : 1.1f;
                light.shadows = LightShadows.None;
            }
        }

        // ------------------------------------------------------------------ окружение

        private static void BuildEnvironment(Transform parent)
        {
            BuildStreet(parent);
            BuildNearBuildings(parent);
            BuildCitySkyline(parent);
            BuildCrane(parent, new Vector3(11.5f, -11f, 43.5f), 21f, 12f);
            BuildSmokeStack(parent, new Vector3(-26f, -11f, 28f), 30f, 7101);
            BuildSmokeStack(parent, new Vector3(27f, -11f, 88f), 34f, 7102);
            BuildSmokeStack(parent, new Vector3(-30f, -11f, 158f), 28f, 7103);
            BuildFactoryHall(parent);
            BuildDepot(parent);

            WorldBanner(parent, "РЖАВЫЙ КВАРТАЛ", new Vector3(-11.75f, 10f, 28f), -90f, new Vector2(8.4f, 3.2f), UIColors.Rust);

            var dust = CreateParticleSystem(parent, "Dust", new Vector3(0f, 7f, 84f), new Color(0.62f, 0.56f, 0.5f, 0.20f),
                20f, 0.13f, 0.035f, 11f, true, ParticleSystemShapeType.Box, new Vector3(70f, 18f, 190f));
            var dustMain = dust.main;
            dustMain.maxParticles = 260;
            dustMain.gravityModifier = 0f;

            // Редкие искры у электрощитов.
            var sparks = CreateParticleSystem(parent, "Sparks", new Vector3(2.7f, 2.4f, 97f), new Color(0.8f, 0.93f, 1f, 0.9f),
                2.4f, 2.2f, 0.045f, 0.7f, true, ParticleSystemShapeType.Cone, Vector3.zero);
            var sparksShape = sparks.shape;
            sparksShape.angle = 26f;
            sparksShape.radius = 0.2f;
            sparksShape.rotation = new Vector3(-90f, 0f, 0f);
            var sparksMain = sparks.main;
            sparksMain.gravityModifier = 1.1f;

            MarkEnvironmentStatic(parent.gameObject);
        }

        private static void AddSectionTrigger(Transform parent, Vector3 position, string title, string message)
        {
            var go = new GameObject("SectionTrigger_" + title);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 2f, 0f);
            collider.size = new Vector3(11f, 4f, 1f);
            var trigger = go.AddComponent<SectionTrigger>();
            trigger.title = title;
            trigger.message = message;
        }

        // ------------------------------------------------------------------ общие хелперы

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider, bool shadows = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = shadows;
            if (!collider)
            {
                var box = go.GetComponent<Collider>();
                if (box != null) UnityEngine.Object.DestroyImmediate(box);
            }
            return go;
        }

        private static GameObject Sphere(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider, bool shadows = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = shadows;
            if (!collider)
            {
                var coll = go.GetComponent<Collider>();
                if (coll != null) UnityEngine.Object.DestroyImmediate(coll);
            }
            return go;
        }

        private static GameObject Capsule(Transform parent, string name, Vector3 position, Vector3 size, Material material, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            if (rotation.HasValue) go.transform.localRotation = rotation.Value;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            var coll = go.GetComponent<Collider>();
            if (coll != null) UnityEngine.Object.DestroyImmediate(coll);
            return go;
        }

        private static GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider, bool shadows = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = shadows;
            if (!collider)
            {
                var coll = go.GetComponent<Collider>();
                if (coll != null) UnityEngine.Object.DestroyImmediate(coll);
            }
            return go;
        }

        private static TextMesh WorldText(Transform parent, string text, Vector3 position, float characterSize, Color color,
            bool board = false, float boardWidth = 0f, float boardHeight = 0f)
        {
            if (board)
            {
                float width = boardWidth > 0f ? boardWidth : Mathf.Max(2.2f, text.Length * characterSize * 0.75f + 0.8f);
                float height = boardHeight > 0f ? boardHeight : characterSize * 2.2f;
                var back = Box(parent, "SignBoard", position + new Vector3(0f, 0f, 0.1f), new Vector3(width, height, 0.1f), matGraphite, false, false);
                back.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                Box(parent, "SignAccent", position + new Vector3(0f, height * 0.5f, 0.04f), new Vector3(width, 0.035f, 0.02f), matSignal, false, false).transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            var go = new GameObject("Text_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            // TextMesh читается со стороны -Z; трасса идёт в +Z, поэтому надписи смотрят на игрока.
            go.transform.localRotation = Quaternion.identity;

            var mesh = go.AddComponent<TextMesh>();
            mesh.font = worldFont;
            mesh.fontSize = 72;
            mesh.characterSize = characterSize * (10f / 72f);
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            mesh.text = text;
            mesh.richText = false;

            var renderer = go.GetComponent<MeshRenderer>();
            if (worldFont != null && worldFont.material != null) renderer.sharedMaterial = worldFont.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return mesh;
        }

        private static void WorldSign(Transform parent, string title, string subtitle, Vector3 position, Color color,
            float characterSize = 0.18f, float yaw = 0f)
        {
            float width = Mathf.Max(title.Length * characterSize * 0.62f, subtitle.Length * characterSize * 0.34f) + 0.8f;
            float height = characterSize * 3f;

            var root = new GameObject("Sign").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box(root, "SignBoard", new Vector3(0f, -0.02f, 0.12f), new Vector3(width, height, 0.1f), matGraphite, false, false);
            Box(root, "SignAccent", new Vector3(0f, height * 0.5f, 0.04f), new Vector3(width, 0.04f, 0.02f), AccentMaterial(color), false, false);
            WorldText(root, title, new Vector3(0f, characterSize * 0.85f, 0f), characterSize, color);
            WorldText(root, subtitle, new Vector3(0f, -characterSize * 0.9f, 0f), characterSize * 0.5f, new Color(0.62f, 0.66f, 0.68f));
        }

        private static Material AccentMaterial(Color color)
        {
            if (color.b > 0.85f && color.r < 0.85f && color.g > 0.7f) return matElectric;
            if (color.r > color.b * 1.9f && color.g < color.r * 0.62f) return matDanger;
            if (color.r > color.b * 1.35f) return matSignal;
            return matChrome;
        }

        private static ParticleSystem CreateParticleSystem(Transform parent, string name, Vector3 position, Color color,
            float rate, float speed, float size, float lifetime, bool loop, ParticleSystemShapeType shapeType, Vector3 boxScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = loop ? 5f : 0.3f;
            main.loop = loop;
            main.playOnAwake = loop;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = 300;
            main.gravityModifier = 0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = shapeType;
            if (shapeType == ParticleSystemShapeType.Box)
            {
                shape.scale = boxScale;
            }
            else
            {
                shape.radius = 0.35f;
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = matParticle;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        private static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parentPath = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parentPath) || string.IsNullOrEmpty(folderName)) return;
            EnsureFolder(parentPath);
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }
}
