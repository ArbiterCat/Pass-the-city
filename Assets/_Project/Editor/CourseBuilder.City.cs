using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SvoiPut.EditorTools
{
    /// <summary>Городские дома, крыши, улица, краны и дым — окружение «Ржавого квартала».</summary>
    public static partial class CourseBuilder
    {
        // ------------------------------------------------------------------ дома и крыши

        private static void Building(Transform parent, string name, Vector3 center, Vector3 size, Material wall,
            bool windowsXPos, bool windowsXNeg, bool windowsZPos, bool windowsZNeg, int seed, bool shadows = true)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = center;

            Box(root, "Body", Vector3.zero, size, wall, false, shadows);
            Box(root, "Foundation", new Vector3(0f, -size.y * 0.5f + 0.45f, 0f),
                new Vector3(size.x + 0.15f, 0.9f, size.z + 0.15f), matConcreteDark, false, false);
            Box(root, "RoofCap", new Vector3(0f, size.y * 0.5f + 0.08f, 0f),
                new Vector3(size.x + 0.35f, 0.16f, size.z + 0.35f), matConcreteDark, false, false);

            int floors = Mathf.Clamp(Mathf.FloorToInt((size.y - 2.2f) / 3.1f), 2, 7);
            if (windowsXPos) WindowGrid(root, 1, size, floors, seed + 1);
            if (windowsXNeg) WindowGrid(root, 2, size, floors, seed + 2);
            if (windowsZPos) WindowGrid(root, 3, size, floors, seed + 3);
            if (windowsZNeg) WindowGrid(root, 4, size, floors, seed + 4);
        }

        private static void WindowGrid(Transform root, int face, Vector3 size, int floors, int seed)
        {
            var rng = new System.Random(seed);
            bool alongZ = face == 1 || face == 2;
            float width = alongZ ? size.z : size.x;
            int columns = Mathf.Clamp(Mathf.FloorToInt(width / 2.8f), 2, 4);
            float step = width * 0.82f / Mathf.Max(1, columns - 1);
            float floorStep = size.y / (floors + 1.2f);

            for (int f = 0; f < floors; f++)
            {
                float y = -size.y * 0.5f + floorStep * (f + 1.2f);
                for (int c = 0; c < columns; c++)
                {
                    if (rng.NextDouble() < 0.14) continue;
                    float along = -width * 0.41f + step * c;
                    bool lit = rng.NextDouble() < 0.16;
                    Material material = lit ? matWindowLit : matWindow;
                    Vector3 position;
                    Vector3 boxSize;
                    if (alongZ)
                    {
                        position = new Vector3((face == 1 ? 1f : -1f) * (size.x * 0.5f + 0.03f), y, along);
                        boxSize = new Vector3(0.06f, 1.05f, Mathf.Min(0.9f, step * 0.62f));
                    }
                    else
                    {
                        position = new Vector3(along, y, (face == 3 ? 1f : -1f) * (size.z * 0.5f + 0.03f));
                        boxSize = new Vector3(Mathf.Min(0.9f, step * 0.62f), 1.05f, 0.06f);
                    }
                    Box(root, "Window", position, boxSize, material, false, false);
                }
            }
        }

        private static void AddRoofParapet(Transform parent, Vector3 center, Vector2 footprint, bool front = true, bool back = true)
        {
            float x = footprint.x * 0.5f - 0.12f;
            float z = footprint.y * 0.5f - 0.12f;
            float y = center.y + 0.24f;
            Box(parent, "ParapetL", new Vector3(center.x - x, y, center.z), new Vector3(0.24f, 0.48f, footprint.y), matConcreteDark, false, false);
            Box(parent, "ParapetR", new Vector3(center.x + x, y, center.z), new Vector3(0.24f, 0.48f, footprint.y), matConcreteDark, false, false);
            if (back)
            {
                Box(parent, "ParapetB", new Vector3(center.x, y, center.z - z), new Vector3(footprint.x, 0.48f, 0.24f), matConcreteDark, false, false);
            }
            if (front)
            {
                Box(parent, "ParapetF", new Vector3(center.x, y, center.z + z), new Vector3(footprint.x, 0.48f, 0.24f), matConcreteDark, false, false);
            }
            Box(parent, "ParapetCapL", new Vector3(center.x - x, y + 0.27f, center.z), new Vector3(0.30f, 0.06f, footprint.y + 0.06f), matRust, false, false);
            Box(parent, "ParapetCapR", new Vector3(center.x + x, y + 0.27f, center.z), new Vector3(0.30f, 0.06f, footprint.y + 0.06f), matRust, false, false);
        }

        private static void ACUnit(Transform parent, Vector3 position, float yaw)
        {
            PlaceModel(modelACUnit, parent, "ACUnit", position, yaw, 1f);
        }

        private static void Antenna(Transform parent, Vector3 position, float height)
        {
            var root = new GameObject("Antenna").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            Cylinder(root, "Mast", new Vector3(0f, height * 0.5f, 0f), new Vector3(0.09f, height * 0.5f, 0.09f), matRustDark, false);
            for (int i = 0; i < 3; i++)
            {
                float y = height * (0.48f + i * 0.14f);
                var bar = Box(root, "Arm_" + i, new Vector3(0f, y, 0f), new Vector3(0.8f - i * 0.15f, 0.045f, 0.045f), matChrome, false, false);
                bar.transform.localRotation = Quaternion.Euler(0f, i * 55f, 0f);
            }
            Sphere(root, "Beacon", new Vector3(0f, height + 0.08f, 0f), new Vector3(0.16f, 0.16f, 0.16f), matDanger, false, false);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2f / 3f;
                Cable(root, new Vector3(0f, height * 0.85f, 0f),
                    new Vector3(Mathf.Cos(angle) * 0.9f, 0f, Mathf.Sin(angle) * 0.9f), 0.05f, 0.012f, 4);
            }
        }

        private static void WaterTower(Transform parent, Vector3 position, float scale)
        {
            PlaceModel(modelWaterTower, parent, "WaterTower", position, 0f, scale);
        }


        // ------------------------------------------------------------------ мелочи крыш

        private static void Crate(Transform parent, Vector3 position, float size, float yaw)
        {
            if (modelCrate != null)
            {
                PlaceModel(modelCrate, parent, "Crate", position, yaw, size);
                return;
            }

            var root = new GameObject("Crate").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box(root, "Box", new Vector3(0f, size * 0.5f, 0f), new Vector3(size, size, size), matRust, false);
            Box(root, "Lid", new Vector3(0f, size + 0.03f, 0f), new Vector3(size + 0.06f, 0.06f, size + 0.06f), matRustDark, false, false);
        }

        private static void Barrel(Transform parent, Vector3 position, float scale, float yaw, bool tipped = false)
        {
            if (modelBarrel != null)
            {
                var instance = PlaceModel(modelBarrel, parent, "Barrel", position, yaw, scale);
                if (tipped && instance != null)
                {
                    instance.transform.localPosition = position + Vector3.up * 0.3f;
                    instance.transform.localRotation = Quaternion.Euler(0f, yaw, 90f);
                }
                return;
            }

            var root = new GameObject("Barrel").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localScale = Vector3.one * scale;
            root.localRotation = tipped ? Quaternion.Euler(0f, yaw, 90f) : Quaternion.Euler(0f, yaw, 0f);
            if (tipped) root.localPosition = position + Vector3.up * 0.3f;
            Cylinder(root, "Body", new Vector3(0f, 0.44f, 0f), new Vector3(0.6f, 0.44f, 0.6f), matRust, false);
        }

        private static void StreetLamp(Transform parent, Vector3 position, float yaw)
        {
            if (modelStreetLamp != null)
            {
                PlaceModel(modelStreetLamp, parent, "StreetLamp", position, yaw, 1f);
                return;
            }

            var lampRoot = new GameObject("StreetLamp").transform;
            lampRoot.SetParent(parent, false);
            lampRoot.localPosition = position;
            lampRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Cylinder(lampRoot, "Post", new Vector3(0f, 3.0f, 0f), new Vector3(0.18f, 3.0f, 0.18f), matRustDark, false, false);
            Box(lampRoot, "Arm", new Vector3(1.0f, 6.0f, 0f), new Vector3(1.8f, 0.12f, 0.12f), matRustDark, false, false);
            Box(lampRoot, "Head", new Vector3(1.9f, 5.86f, 0f), new Vector3(0.7f, 0.16f, 0.34f), matSignal, false, false);
        }

        private static void Puddle(Transform parent, Vector3 position, Vector2 size, float yaw)
        {
            var root = new GameObject("Puddle").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box(root, "Water", Vector3.zero, new Vector3(size.x, 0.012f, size.y), matWet, false, false);
            Box(root, "WaterEdge", new Vector3(0.25f, 0.004f, 0.2f), new Vector3(size.x * 0.55f, 0.008f, size.y * 0.5f), matWet, false, false);
        }

        private static void Rubble(Transform parent, Vector3 position, float spread, int count, System.Random rng)
        {
            for (int i = 0; i < count; i++)
            {
                float x = position.x + ((float)rng.NextDouble() - 0.5f) * spread;
                float z = position.z + ((float)rng.NextDouble() - 0.5f) * spread;
                float s = 0.15f + (float)rng.NextDouble() * 0.45f;
                var piece = Box(parent, "Rubble", new Vector3(x, position.y + s * 0.3f, z),
                    new Vector3(s, s * 0.35f, s * (0.6f + (float)rng.NextDouble())), matConcreteDark, false, false);
                piece.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 30f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 24f);
            }
        }

        private static void Pipe(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material material, bool shadows = false)
        {
            Vector3 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.02f) return;
            var go = Cylinder(parent, name, (a + b) * 0.5f, new Vector3(radius * 2f, length * 0.5f, radius * 2f), material, false, shadows);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        }

        private static void Cable(Transform parent, Vector3 a, Vector3 b, float sag, float radius = 0.02f, int segments = 8)
        {
            Vector3 previous = a;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = Vector3.Lerp(a, b, t) - Vector3.up * (sag * 4f * t * (1f - t));
                Pipe(parent, "Cable", previous, point, radius, matGraphite);
                previous = point;
            }
        }

        private static void RailingZ(Transform parent, float x, float y, float z0, float z1, float height, Material material)
        {
            int posts = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(z1 - z0) / 2.4f));
            for (int i = 0; i <= posts; i++)
            {
                float z = Mathf.Lerp(z0, z1, i / (float)posts);
                Box(parent, "RailPost", new Vector3(x, y + height * 0.5f, z), new Vector3(0.07f, height, 0.07f), material, false, false);
            }
            float mid = (z0 + z1) * 0.5f;
            Box(parent, "RailTop", new Vector3(x, y + height, mid), new Vector3(0.08f, 0.08f, Mathf.Abs(z1 - z0)), material, false, false);
            Box(parent, "RailMid", new Vector3(x, y + height * 0.52f, mid), new Vector3(0.05f, 0.05f, Mathf.Abs(z1 - z0)), material, false, false);
        }

        private static void RailingX(Transform parent, float z, float y, float x0, float x1, float height, Material material, bool endRail = false)
        {
            int posts = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(x1 - x0) / 2.4f));
            for (int i = 0; i <= posts; i++)
            {
                float x = Mathf.Lerp(x0, x1, i / (float)posts);
                Box(parent, "RailPost", new Vector3(x, y + height * 0.5f, z), new Vector3(0.07f, height, 0.07f), material, false, false);
            }
            float mid = (x0 + x1) * 0.5f;
            Box(parent, "RailTop", new Vector3(mid, y + height, z), new Vector3(Mathf.Abs(x1 - x0), 0.08f, 0.08f), material, false, false);
            if (endRail)
            {
                Box(parent, "RailEnd", new Vector3(mid, y + height * 0.52f, z), new Vector3(Mathf.Abs(x1 - x0), 0.05f, 0.05f), material, false, false);
            }
        }

        private static void AddScaffold(Transform parent, Vector3 bottomPosition, Vector3 footprint, float height)
        {
            var root = new GameObject("Scaffold").transform;
            root.SetParent(parent, false);
            root.localPosition = bottomPosition;
            float x = footprint.x * 0.5f - 0.08f;
            float z = footprint.z * 0.5f - 0.08f;
            for (int i = 0; i < 4; i++)
            {
                float px = (i % 2 == 0) ? -x : x;
                float pz = (i < 2) ? -z : z;
                Box(root, "Leg_" + i, new Vector3(px, height * 0.5f, pz), new Vector3(0.1f, height, 0.1f), matRustDark, false, false);
            }
            for (float y = 2.0f; y < height; y += 2.6f)
            {
                Box(root, "BraceX", new Vector3(0f, y, -z), new Vector3(footprint.x - 0.1f, 0.07f, 0.07f), matRust, false, false);
                Box(root, "BraceX2", new Vector3(0f, y, z), new Vector3(footprint.x - 0.1f, 0.07f, 0.07f), matRust, false, false);
                Box(root, "BraceZ", new Vector3(-x, y, 0f), new Vector3(0.07f, 0.07f, footprint.z - 0.1f), matRust, false, false);
                Box(root, "BraceZ2", new Vector3(x, y, 0f), new Vector3(0.07f, 0.07f, footprint.z - 0.1f), matRust, false, false);
            }
            Box(root, "TopPlate", new Vector3(0f, height, 0f), new Vector3(footprint.x + 0.2f, 0.1f, footprint.z + 0.2f), matGraphite, false, false);
        }

        // ------------------------------------------------------------------ вывески

        private static void SectorMarker(Transform parent, Vector3 position, float yaw, string number, string name, Color color)
        {
            var root = new GameObject("SectorMarker_" + number).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box(root, "Board", new Vector3(0f, 0f, 0.1f), new Vector3(3.6f, 2.5f, 0.12f), matGraphite, false, false);
            Box(root, "FrameTop", new Vector3(0f, 1.23f, 0.02f), new Vector3(3.7f, 0.08f, 0.06f), AccentMaterial(color), false, false);
            Box(root, "FrameBottom", new Vector3(0f, -1.23f, 0.02f), new Vector3(3.7f, 0.08f, 0.06f), matRust, false, false);
            Box(root, "Stripe", new Vector3(-1.52f, 0f, 0.02f), new Vector3(0.12f, 2.4f, 0.06f), AccentMaterial(color), false, false);
            WorldText(root, number, new Vector3(0.25f, 0.42f, -0.01f), 0.86f, color);
            WorldText(root, name, new Vector3(0.25f, -0.72f, -0.01f), 0.24f, UIColors.Chrome);
            Box(root, "Bolt1", new Vector3(-1.6f, 1.08f, -0.02f), new Vector3(0.08f, 0.08f, 0.04f), matChrome, false, false);
            Box(root, "Bolt2", new Vector3(1.6f, 1.08f, -0.02f), new Vector3(0.08f, 0.08f, 0.04f), matChrome, false, false);
        }

        private static void WorldBanner(Transform parent, string text, Vector3 position, float yaw, Vector2 size, Color color)
        {
            var root = new GameObject("Banner").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box(root, "Board", new Vector3(0f, 0f, 0.08f), new Vector3(size.x, size.y, 0.16f), matGraphite, false, false);
            Box(root, "FrameTop", new Vector3(0f, size.y * 0.5f, 0f), new Vector3(size.x + 0.1f, 0.1f, 0.1f), matRust, false, false);
            Box(root, "FrameBottom", new Vector3(0f, -size.y * 0.5f, 0f), new Vector3(size.x + 0.1f, 0.1f, 0.1f), matRust, false, false);
            Box(root, "FrameL", new Vector3(-size.x * 0.5f, 0f, 0f), new Vector3(0.1f, size.y, 0.1f), matRust, false, false);
            Box(root, "FrameR", new Vector3(size.x * 0.5f, 0f, 0f), new Vector3(0.1f, size.y, 0.1f), matRust, false, false);
            float characterSize = Mathf.Min(size.y * 0.3f, size.x / Mathf.Max(4f, text.Length * 0.62f));
            WorldText(root, text, new Vector3(0f, 0f, -0.01f), characterSize, color);
        }

        private static void AddHangingLamp(Transform parent, Vector3 position, float intensity, bool warm)
        {
            var root = new GameObject("HangingLamp").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            Box(root, "Rod", new Vector3(0f, 0.55f, 0f), new Vector3(0.05f, 1.1f, 0.05f), matGraphite, false, false);
            Cylinder(root, "Shade", new Vector3(0f, 0.05f, 0f), new Vector3(0.8f, 0.14f, 0.8f), matRust, false, false);
            Sphere(root, "Bulb", new Vector3(0f, -0.08f, 0f), new Vector3(0.24f, 0.24f, 0.24f), warm ? matSignal : matChromeGlow, false, false);
            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = warm ? new Color(1f, 0.65f, 0.36f) : new Color(0.8f, 0.86f, 0.92f);
            light.range = 12f;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        // ------------------------------------------------------------------ улица и город

        private static void BuildStreet(Transform parent)
        {
            var root = new GameObject("Street").transform;
            root.SetParent(parent, false);
            Box(root, "Road", new Vector3(0f, -11f, 100f), new Vector3(13f, 0.2f, 260f), matAsphalt, false, false);
            Box(root, "SidewalkL", new Vector3(-8.3f, -10.85f, 100f), new Vector3(3.4f, 0.3f, 260f), matConcreteDark, false, false);
            Box(root, "SidewalkR", new Vector3(8.3f, -10.85f, 100f), new Vector3(3.4f, 0.3f, 260f), matConcreteDark, false, false);
            Box(root, "CurbL", new Vector3(-6.6f, -10.78f, 100f), new Vector3(0.3f, 0.34f, 260f), matConcrete, false, false);
            Box(root, "CurbR", new Vector3(6.6f, -10.78f, 100f), new Vector3(0.3f, 0.34f, 260f), matConcrete, false, false);

            for (float z = -18f; z < 228f; z += 6f)
            {
                Box(root, "LaneMark", new Vector3(0f, -10.885f, z), new Vector3(0.16f, 0.01f, 2.4f), matChrome, false, false);
            }

            var rng = new System.Random(44071);
            float[] carZ = { -4f, 14f, 30f, 58f, 84f, 118f, 146f, 172f, 204f };
            for (int i = 0; i < carZ.Length; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * (2.4f + (float)rng.NextDouble() * 1.2f);
                var car = BuildCar(parent, new Vector3(x, -11f, carZ[i]), (float)rng.NextDouble() * 360f, false,
                    i % 3 == 0 ? matRustDark : (i % 3 == 1 ? matGraphite : matRust));
                car.name = "WreckedCar_" + i;
            }

            float[] lampZ = { 4f, 32f, 60f, 88f, 116f, 144f, 172f, 200f };
            for (int i = 0; i < lampZ.Length; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float yaw = side < 0f ? 180f : 0f;
                StreetLamp(root, new Vector3(side * 8.2f, -10.7f, lampZ[i]), yaw);

                var glowGo = new GameObject("LampGlow");
                glowGo.transform.SetParent(root, false);
                glowGo.transform.localPosition = new Vector3(side * 6.28f, -4.84f, lampZ[i]);
                var glow = glowGo.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = new Color(1f, 0.62f, 0.32f);
                glow.range = 11f;
                glow.intensity = 0.85f;
                glow.shadows = LightShadows.None;
            }

            Puddle(root, new Vector3(-3.2f, -10.885f, 12f), new Vector2(3.6f, 2.2f), 12f);
            Puddle(root, new Vector3(2.8f, -10.885f, 72f), new Vector2(4.0f, 2.6f), -18f);
            Puddle(root, new Vector3(-2.6f, -10.885f, 152f), new Vector2(3.2f, 2.0f), 34f);
            Rubble(root, new Vector3(-4.4f, -10.8f, 42f), 5f, 9, rng);
            Rubble(root, new Vector3(4.6f, -10.8f, 132f), 4f, 7, rng);

            for (int i = 0; i < 3; i++)
            {
                var lightGo = new GameObject("StreetGlow_" + i);
                lightGo.transform.SetParent(root, false);
                lightGo.transform.localPosition = new Vector3(i % 2 == 0 ? -5f : 5f, -6.2f, 24f + i * 72f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.6f, 0.3f);
                light.range = 20f;
                light.intensity = 0.75f;
                light.shadows = LightShadows.None;
            }
        }

        private static void BuildNearBuildings(Transform parent)
        {
            var rng = new System.Random(90211);
            float[] leftZ = { -12f, 20f, 44f, 118f, 146f, 196f, 222f };
            float[] rightZ = { 2f, 24f, 47f, 122f, 150f, 198f };
            for (int i = 0; i < leftZ.Length; i++)
            {
                float height = 16f + (i * 7) % 18f;
                float width = 12f + (i % 2) * 2f;
                float depth = 14f + (i % 3) * 2f;
                float x = -18.5f + (i % 2) * 0.7f;
                Building(parent, "HouseL_" + i, new Vector3(x, -11f + height * 0.5f, leftZ[i]),
                    new Vector3(width, height, depth), i % 3 == 0 ? matBrick : matConcrete,
                    true, false, false, true, 5200 + i);
                if (i == 1)
                {
                    float roof = -11f + height;
                    WaterTower(parent, new Vector3(x, roof, leftZ[i]), 1.0f);
                }
            }
            for (int i = 0; i < rightZ.Length; i++)
            {
                float height = 15f + (i * 9) % 20f;
                float width = 12f + (i % 3) * 2f;
                float depth = 15f + (i % 2) * 3f;
                float x = 18.2f - (i % 2) * 0.6f;
                Building(parent, "HouseR_" + i, new Vector3(x, -11f + height * 0.5f, rightZ[i]),
                    new Vector3(width, height, depth), i % 2 == 0 ? matConcreteDark : matBrick,
                    false, true, false, true, 5400 + i);
                if (i == 3)
                {
                    float roof = -11f + height;
                    Antenna(parent, new Vector3(x, roof, rightZ[i]), 4.5f);
                }
            }

            // Водостоки и кабели между домами.
            Pipe(parent, "WallPipeL", new Vector3(-11.7f, 6f, 6f), new Vector3(-11.7f, 0.5f, 6f), 0.12f, matRustDark);
            Pipe(parent, "WallPipeR", new Vector3(11.8f, 8f, 30f), new Vector3(11.8f, 1f, 30f), 0.12f, matRustDark);
            Cable(parent, new Vector3(-11.6f, 9f, 8f), new Vector3(11.6f, 7f, 22f), 1.4f, 0.022f, 10);
            Cable(parent, new Vector3(-11.6f, 7.4f, 38f), new Vector3(11.8f, 8.6f, 50f), 1.7f, 0.022f, 10);
            Cable(parent, new Vector3(-11.6f, 8.2f, 118f), new Vector3(11.6f, 6.6f, 132f), 1.3f, 0.022f, 10);
            Cable(parent, new Vector3(-11.6f, 7.0f, 148f), new Vector3(11.6f, 8.0f, 160f), 1.5f, 0.022f, 10);

            // Ржавые вывески на фасадах.
            var signRoot = new GameObject("FacadeSign").transform;
            signRoot.SetParent(parent, false);
            signRoot.localPosition = new Vector3(11.85f, 9.4f, 18f);
            signRoot.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Box(signRoot, "Board", Vector3.zero, new Vector3(3.8f, 1.3f, 0.12f), matRustDark, false, false);
            WorldText(signRoot, "ЦЕХ 02", new Vector3(0f, 0f, -0.08f), 0.36f, UIColors.Rust);
        }

        private static void BuildCitySkyline(Transform parent)
        {
            var rng = new System.Random(61803);
            for (int i = 0; i < 15; i++)
            {
                float z = -20f + i * 16f + (float)rng.NextDouble() * 6f;
                float x = -34f - (float)rng.NextDouble() * 30f;
                float height = 18f + (float)rng.NextDouble() * 34f;
                float width = 10f + (float)rng.NextDouble() * 9f;
                float depth = 11f + (float)rng.NextDouble() * 10f;
                Building(parent, "SkylineL_" + i, new Vector3(x, -11f + height * 0.5f, z),
                    new Vector3(width, height, depth), i % 4 == 0 ? matBrick : (i % 3 == 0 ? matGraphite : matConcreteDark),
                    true, false, false, false, 6100 + i, false);
            }
            for (int i = 0; i < 15; i++)
            {
                float z = -14f + i * 16f + (float)rng.NextDouble() * 6f;
                float x = 33f + (float)rng.NextDouble() * 32f;
                float height = 16f + (float)rng.NextDouble() * 38f;
                float width = 10f + (float)rng.NextDouble() * 10f;
                float depth = 12f + (float)rng.NextDouble() * 9f;
                Building(parent, "SkylineR_" + i, new Vector3(x, -11f + height * 0.5f, z),
                    new Vector3(width, height, depth), i % 4 == 0 ? matBrick : (i % 3 == 0 ? matGraphite : matConcreteDark),
                    false, true, false, false, 6300 + i, false);
            }
            for (int i = 0; i < 9; i++)
            {
                float x = -58f + i * 14f + (float)rng.NextDouble() * 5f;
                float z = 238f + (float)rng.NextDouble() * 34f;
                float height = 22f + (float)rng.NextDouble() * 42f;
                float width = 12f + (float)rng.NextDouble() * 10f;
                float depth = 12f + (float)rng.NextDouble() * 10f;
                Building(parent, "SkylineFar_" + i, new Vector3(x, -11f + height * 0.5f, z),
                    new Vector3(width, height, depth), i % 2 == 0 ? matConcreteDark : matGraphite,
                    false, false, false, true, 6500 + i, false);
            }
        }

        // ------------------------------------------------------------------ кран и трубы

        private static void BuildCrane(Transform parent, Vector3 basePosition, float topY, float jibLength)
        {
            var root = new GameObject("Crane").transform;
            root.SetParent(parent, false);
            root.localPosition = basePosition;
            float mast = Mathf.Max(10f, topY - basePosition.y);
            float half = 0.9f;
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -half : half;
                float z = (i < 2) ? -half : half;
                Box(root, "Leg_" + i, new Vector3(x, mast * 0.5f, z), new Vector3(0.17f, mast, 0.17f), matRust, false);
            }
            int levels = Mathf.FloorToInt(mast / 3.2f);
            for (int i = 1; i < levels; i++)
            {
                float y = i * 3.2f;
                Box(root, "BraceX", new Vector3(0f, y, -half), new Vector3(half * 2f, 0.1f, 0.1f), matRustDark, false, false);
                Box(root, "BraceX2", new Vector3(0f, y, half), new Vector3(half * 2f, 0.1f, 0.1f), matRustDark, false, false);
                Box(root, "BraceZ", new Vector3(-half, y, 0f), new Vector3(0.1f, 0.1f, half * 2f), matRustDark, false, false);
                Box(root, "BraceZ2", new Vector3(half, y, 0f), new Vector3(0.1f, 0.1f, half * 2f), matRustDark, false, false);
            }

            float top = mast;
            Box(root, "Cab", new Vector3(0.2f, top + 0.8f, 0f), new Vector3(2.2f, 1.5f, 2.0f), matRust, false);
            Box(root, "CabWindow", new Vector3(0.2f, top + 0.95f, -1.02f), new Vector3(1.5f, 0.7f, 0.08f), matWindow, false, false);
            Box(root, "Peak", new Vector3(0f, top + 2.2f, 0f), new Vector3(0.18f, 2.8f, 0.18f), matRustDark, false, false);

            float jibY = top + 1.6f;
            Box(root, "JibBottom", new Vector3(-jibLength * 0.5f + 1f, jibY, 0f), new Vector3(jibLength, 0.22f, 0.45f), matRust, false, false);
            Box(root, "JibTop", new Vector3(-jibLength * 0.5f + 1f, jibY + 0.75f, 0f), new Vector3(jibLength, 0.12f, 0.18f), matRustDark, false, false);
            for (int i = 0; i < 6; i++)
            {
                float x = 1f - i * (jibLength / 6f);
                var brace = Box(root, "JibBrace_" + i, new Vector3(x, jibY + 0.36f, 0f), new Vector3(0.1f, 0.95f, 0.1f), matRustDark, false, false);
                brace.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 30f : -30f);
            }
            Box(root, "CounterJib", new Vector3(jibLength * 0.3f, jibY, 0f), new Vector3(jibLength * 0.6f, 0.25f, 0.5f), matRust, false, false);
            Box(root, "Counterweight", new Vector3(jibLength * 0.5f, jibY - 0.75f, 0f), new Vector3(1.7f, 1.3f, 1.4f), matConcreteDark, false);
            Box(root, "CounterweightRust", new Vector3(jibLength * 0.5f, jibY - 0.15f, 0.72f), new Vector3(1.5f, 0.1f, 0.02f), matRust, false, false);

            Vector3 tip = new Vector3(1f - jibLength, jibY, 0f);
            Pipe(root, "CableFront", new Vector3(0f, top + 3.4f, 0f), tip, 0.025f, matGraphite);
            Pipe(root, "CableBack", new Vector3(0f, top + 3.4f, 0f), new Vector3(jibLength * 0.5f, jibY, 0f), 0.025f, matGraphite);
            Pipe(root, "HookCable", new Vector3(tip.x + 3f, jibY, 0f), new Vector3(tip.x + 3f, jibY - 9f, 0f), 0.022f, matGraphite);
            Box(root, "Hook", new Vector3(tip.x + 3f, jibY - 9.3f, 0f), new Vector3(0.4f, 0.55f, 0.4f), matChrome, false, false);
            Box(root, "Trolley", new Vector3(tip.x + 3f, jibY - 0.15f, 0f), new Vector3(0.8f, 0.3f, 0.6f), matGraphite, false, false);
            Sphere(root, "CraneBeacon", new Vector3(0f, top + 3.55f, 0f), new Vector3(0.22f, 0.22f, 0.22f), matDanger, false, false);
        }

        private static void BuildSmokeStack(Transform parent, Vector3 basePosition, float height, int seed)
        {
            var root = new GameObject("SmokeStack_" + seed).transform;
            root.SetParent(parent, false);
            root.localPosition = basePosition;
            Cylinder(root, "Stack", new Vector3(0f, height * 0.5f, 0f), new Vector3(4f, height * 0.5f, 4f), matBrick, false);
            for (int i = 0; i < 4; i++)
            {
                float y = height * (0.25f + i * 0.18f);
                Cylinder(root, "Band_" + i, new Vector3(0f, y, 0f), new Vector3(4.15f, 0.12f, 4.15f), matRustDark, false, false);
            }
            Cylinder(root, "Rim", new Vector3(0f, height, 0f), new Vector3(4.3f, 0.3f, 4.3f), matGraphite, false, false);
            Sphere(root, "Beacon", new Vector3(0f, height + 0.45f, 0f), new Vector3(0.28f, 0.28f, 0.28f), matDanger, false, false);
            CreateSmoke(root, "Smoke", new Vector3(0f, height + 0.3f, 0f), 1.0f, 5.5f);
            var rng = new System.Random(seed);
            for (int i = 0; i < 3; i++)
            {
                float angle = (float)rng.NextDouble() * 360f;
                Box(root, "LadderRung", new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 2.05f, 4f + i * 6f, Mathf.Sin(angle * Mathf.Deg2Rad) * 2.05f),
                    new Vector3(0.5f, 0.06f, 0.06f), matChrome, false, false);
            }
        }

        private static void CreateSmoke(Transform parent, string name, Vector3 position, float size, float rate)
        {
            var ps = CreateParticleSystem(parent, name, position, new Color(0.33f, 0.31f, 0.30f, 0.42f),
                rate, 0.8f, size, 8f, true, ParticleSystemShapeType.Cone, Vector3.zero);
            var shape = ps.shape;
            shape.angle = 12f;
            shape.radius = 0.9f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var main = ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 1.2f, size * 2.8f);
            main.gravityModifier = -0.012f;
            main.maxParticles = 90;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = matSmoke;
        }

        private static void MarkEnvironmentStatic(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
            }
        }
    }
}
