using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SvoiPut.EditorTools
{
    /// <summary>Промышленные испытания, вагоны, цех и депо «Ржавого квартала».</summary>
    public static partial class CourseBuilder
    {
        // ------------------------------------------------------------------ опасности

        private static void BuildFanHazard(Transform parent, Vector3 position, int blades, float length, float height, float speed, float phase, string reason)
        {
            var root = new GameObject("FanHazard").transform;
            root.SetParent(parent, false);
            root.localPosition = position;

            Cylinder(root, "Housing", new Vector3(0f, 0.035f, 0f), new Vector3(length * 2.02f, 0.035f, length * 2.02f), matGraphite, false, false);
            const int ringSegments = 28;
            for (int i = 0; i < ringSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / ringSegments;
                float radius = length + 0.14f;
                var seg = Box(root, "Rim_" + i,
                    new Vector3(Mathf.Cos(angle) * radius, 0.11f, Mathf.Sin(angle) * radius),
                    new Vector3(0.62f, 0.14f, 0.18f), i % 3 == 0 ? matDanger : matRust, false, false);
                seg.transform.localRotation = Quaternion.Euler(0f, 90f - angle * Mathf.Rad2Deg, 0f);
            }

            Cylinder(root, "Hub", new Vector3(0f, height, 0f), new Vector3(1.05f, 0.34f, 1.05f), matRustDark, false);
            Cylinder(root, "HubCap", new Vector3(0f, height + 0.33f, 0f), new Vector3(0.55f, 0.06f, 0.55f), matChrome, false, false);

            var rotor = new GameObject("Rotor").transform;
            rotor.SetParent(root, false);
            rotor.localPosition = new Vector3(0f, height, 0f);

            for (int b = 0; b < blades; b++)
            {
                var bladeRoot = new GameObject("BladeRoot_" + b).transform;
                bladeRoot.SetParent(rotor, false);
                bladeRoot.localRotation = Quaternion.Euler(0f, b * 360f / blades, 0f);

                float center = length * 0.5f;
                Box(bladeRoot, "Blade", new Vector3(0f, 0f, center), new Vector3(0.52f, 0.18f, length), matRust, false, false);
                Box(bladeRoot, "BladeEdge", new Vector3(0f, 0.105f, center), new Vector3(0.56f, 0.045f, length * 0.84f), matSignal, false, false);
                Box(bladeRoot, "BladeTip", new Vector3(0f, 0f, length - 0.18f), new Vector3(0.62f, 0.24f, 0.38f), matDanger, false, false);

                var hazardGo = new GameObject("BladeHazard");
                hazardGo.transform.SetParent(bladeRoot, false);
                hazardGo.transform.localPosition = new Vector3(0f, 0f, center);
                var collider = hazardGo.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.58f, 1.2f, length);
                collider.isTrigger = true;
                var hazard = hazardGo.AddComponent<Hazard>();
                hazard.reason = reason;
            }

            var lightGo = new GameObject("FanLight");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.localPosition = new Vector3(0f, height + 0.6f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.5f, 0.18f);
            light.range = 9f;
            light.intensity = 1.3f;
            light.shadows = LightShadows.None;

            var sweeper = root.gameObject.AddComponent<RotatingSweeper>();
            sweeper.rotor = rotor;
            sweeper.speed = speed;
            sweeper.startAngle = phase;
            sweeper.hubLight = light;
        }

        private static void CreateCrusher(Transform rootParent, Vector3 position)
        {
            var root = new GameObject("IndustrialPress").transform;
            root.SetParent(rootParent, false);
            root.localPosition = position;

            Box(root, "PostL", new Vector3(-2.8f, 2.2f, 0f), new Vector3(0.5f, 4.4f, 1.0f), matRust, false);
            Box(root, "PostR", new Vector3(2.8f, 2.2f, 0f), new Vector3(0.5f, 4.4f, 1.0f), matRust, false);
            Box(root, "TopBeam", new Vector3(0f, 4.45f, 0f), new Vector3(6.2f, 0.7f, 1.1f), matGraphite, false);
            Box(root, "TopRust", new Vector3(0f, 4.86f, 0f), new Vector3(6.0f, 0.12f, 0.9f), matRustDark, false, false);
            Box(root, "BackBrace", new Vector3(0f, 3.6f, 0.65f), new Vector3(5.4f, 0.3f, 0.16f), matRustDark, false, false);

            var glow = new List<Renderer>();
            glow.Add(Box(root, "PostGlowL", new Vector3(-2.53f, 2.2f, -0.53f), new Vector3(0.08f, 3.4f, 0.03f), matSignal, false, false).GetComponent<Renderer>());
            glow.Add(Box(root, "PostGlowR", new Vector3(2.53f, 2.2f, -0.53f), new Vector3(0.08f, 3.4f, 0.03f), matSignal, false, false).GetComponent<Renderer>());

            var head = new GameObject("HeadGroup").transform;
            head.SetParent(root, false);
            head.localPosition = new Vector3(0f, 3.2f, 0f);

            Box(head, "Head", Vector3.zero, new Vector3(5.4f, 0.9f, 1.6f), matGraphite, false);
            Box(head, "Plate", new Vector3(0f, -0.5f, 0f), new Vector3(5.5f, 0.12f, 1.7f), matRust, false, false);
            Box(head, "Piston", new Vector3(0f, 1.65f, 0f), new Vector3(0.55f, 1.65f, 0.55f), matChrome, false, false);
            Box(head, "GuideL", new Vector3(-2.2f, 0.95f, 0.35f), new Vector3(0.16f, 1.9f, 0.16f), matChrome, false, false);
            Box(head, "GuideR", new Vector3(2.2f, 0.95f, 0.35f), new Vector3(0.16f, 1.9f, 0.16f), matChrome, false, false);

            for (int i = 0; i < 4; i++)
            {
                float y = -0.3f + i * 0.22f;
                glow.Add(Box(head, "StripeF_" + i, new Vector3(0f, y, -0.82f), new Vector3(4.9f, 0.11f, 0.04f), matSignal, false, false).GetComponent<Renderer>());
                glow.Add(Box(head, "StripeB_" + i, new Vector3(0f, y, 0.82f), new Vector3(4.9f, 0.11f, 0.04f), matSignal, false, false).GetComponent<Renderer>());
            }

            var hazardGo = new GameObject("PressHazard");
            hazardGo.transform.SetParent(head, false);
            var hazardCollider = hazardGo.AddComponent<BoxCollider>();
            hazardCollider.size = new Vector3(5.4f, 1.25f, 1.7f);
            hazardCollider.isTrigger = true;
            hazardCollider.enabled = false;
            var hazard = hazardGo.AddComponent<Hazard>();
            hazard.reason = "ПРЕСС";

            var warningGo = new GameObject("WarningLight");
            warningGo.transform.SetParent(root, false);
            warningGo.transform.localPosition = new Vector3(0f, 5.15f, 0f);
            var warningLight = warningGo.AddComponent<Light>();
            warningLight.type = LightType.Point;
            warningLight.color = new Color(1f, 0.5f, 0.12f);
            warningLight.range = 10f;
            warningLight.intensity = 1.0f;
            warningLight.shadows = LightShadows.None;
            Box(root, "WarningCage", new Vector3(0f, 5.05f, 0f), new Vector3(0.4f, 0.4f, 0.4f), matGraphite, false, false);
            Sphere(root, "WarningBulb", new Vector3(0f, 5.15f, 0f), new Vector3(0.22f, 0.22f, 0.22f), matSignal, false, false);

            var dust = CreateParticleSystem(root, "PressDust", new Vector3(0f, 0.12f, 0f),
                new Color(0.58f, 0.52f, 0.46f, 0.65f), 0f, 2.8f, 0.09f, 0.8f, false, ParticleSystemShapeType.Cone, Vector3.zero);
            var dustShape = dust.shape;
            dustShape.angle = 72f;
            dustShape.radius = 2.1f;
            dustShape.rotation = new Vector3(-90f, 0f, 0f);
            var dustMain = dust.main;
            dustMain.loop = false;
            dustMain.playOnAwake = false;
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var impactGo = new GameObject("ImpactLight");
            impactGo.transform.SetParent(root, false);
            impactGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var impactLight = impactGo.AddComponent<Light>();
            impactLight.type = LightType.Point;
            impactLight.color = new Color(1f, 0.38f, 0.12f);
            impactLight.range = 9f;
            impactLight.intensity = 0f;
            impactLight.shadows = LightShadows.None;

            var press = root.gameObject.AddComponent<CrusherPiston>();
            press.headGroup = head;
            press.hazardCollider = hazardCollider;
            press.warningLight = warningLight;
            press.glowRenderers = glow.ToArray();
            press.impactParticles = dust;
            press.impactLight = impactLight;
            press.topY = 3.2f;
            press.bottomY = 0.62f;
            press.upTime = 1.35f;
            press.warnTime = 0.6f;
            press.slamTime = 0.1f;
            press.downTime = 0.75f;
            press.riseTime = 1.05f;
        }

        private static void BuildLiftFrame(Transform parent, Vector3 position)
        {
            var root = new GameObject("LiftFrame").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            float[] xs = { -1.9f, 1.9f };
            float[] zs = { -1.9f, 1.9f };
            for (int i = 0; i < 4; i++)
            {
                Box(root, "Post_" + i, new Vector3(xs[i % 2], 4.1f, zs[i / 2]), new Vector3(0.18f, 8.2f, 0.18f), matRust, false);
            }
            Box(root, "TopX1", new Vector3(0f, 8.2f, -1.9f), new Vector3(4.0f, 0.2f, 0.2f), matRustDark, false, false);
            Box(root, "TopX2", new Vector3(0f, 8.2f, 1.9f), new Vector3(4.0f, 0.2f, 0.2f), matRustDark, false, false);
            Box(root, "TopZ1", new Vector3(-1.9f, 8.2f, 0f), new Vector3(0.2f, 0.2f, 4.0f), matRustDark, false, false);
            Box(root, "TopZ2", new Vector3(1.9f, 8.2f, 0f), new Vector3(0.2f, 0.2f, 4.0f), matRustDark, false, false);
            Cylinder(root, "Sheave", new Vector3(0f, 8.1f, 0f), new Vector3(1.0f, 0.12f, 1.0f), matChrome, false, false);
            Pipe(root, "HoistCable", new Vector3(0f, 8.0f, 0f), new Vector3(0f, 4.6f, 0f), 0.035f, matGraphite);
            Box(root, "SafetyStripe1", new Vector3(-1.9f, 0.12f, 1.9f), new Vector3(0.5f, 0.2f, 0.5f), matSignal, false, false);
            Box(root, "SafetyStripe2", new Vector3(1.9f, 0.12f, -1.9f), new Vector3(0.5f, 0.2f, 0.5f), matSignal, false, false);
        }

        private static void CreateConveyor(Transform parent, Vector3 position)
        {
            var root = new GameObject("ConveyorBelt").transform;
            root.SetParent(parent, false);
            root.localPosition = position;

            Box(root, "Frame", new Vector3(0f, 0.02f, 0f), new Vector3(3.4f, 0.2f, 7.2f), matGraphite, false, false);
            Box(root, "Belt", new Vector3(0f, 0.11f, 0f), new Vector3(3.0f, 0.06f, 6.8f), matRustDark, false, false);
            Box(root, "SideRailL", new Vector3(-1.62f, 0.16f, 0f), new Vector3(0.12f, 0.2f, 7.1f), matRust, false, false);
            Box(root, "SideRailR", new Vector3(1.62f, 0.16f, 0f), new Vector3(0.12f, 0.2f, 7.1f), matRust, false, false);
            Box(root, "WarnL", new Vector3(-1.69f, 0.16f, 0f), new Vector3(0.02f, 0.1f, 6.8f), matSignal, false, false);
            Box(root, "WarnR", new Vector3(1.69f, 0.16f, 0f), new Vector3(0.02f, 0.1f, 6.8f), matSignal, false, false);

            var chevrons = new Transform[8];
            for (int i = 0; i < chevrons.Length; i++)
            {
                float z = -3.2f + i * 0.92f;
                var bar = Box(root, "Chevron_" + i, new Vector3(0f, 0.155f, z), new Vector3(2.45f, 0.018f, 0.2f), matSignal, false, false);
                chevrons[i] = bar.transform;
            }

            var solid = root.gameObject.AddComponent<BoxCollider>();
            solid.size = new Vector3(3.2f, 0.14f, 7.0f);
            solid.center = new Vector3(0f, 0.07f, 0f);
            var trigger = root.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3.2f, 2.2f, 7.1f);
            trigger.center = new Vector3(0f, 1.1f, 0f);

            var lightGo = new GameObject("BeltLight");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.7f, -3.3f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.3f);
            light.range = 8f;
            light.intensity = 1.0f;
            light.shadows = LightShadows.None;

            var belt = root.gameObject.AddComponent<ConveyorBelt>();
            belt.chevrons = chevrons;
            belt.beltLight = light;
            belt.beltLength = 7f;
            belt.beltSpeed = 2.8f;
            belt.pushDirection = Vector3.back;
            belt.pushStrength = 2.35f;
        }

        private static void CreatePendulum(Transform parent, Vector3 position)
        {
            var root = new GameObject("Pendulum").transform;
            root.SetParent(parent, false);
            root.localPosition = position;

            Box(root, "PostL", new Vector3(-4.6f, 2.8f, 0f), new Vector3(0.42f, 5.6f, 0.42f), matRust, false);
            Box(root, "PostR", new Vector3(4.6f, 2.8f, 0f), new Vector3(0.42f, 5.6f, 0.42f), matRust, false);
            Box(root, "TopBeam", new Vector3(0f, 5.7f, 0f), new Vector3(9.7f, 0.5f, 0.55f), matGraphite, false);
            Box(root, "BeamStripe", new Vector3(0f, 5.7f, -0.29f), new Vector3(8.9f, 0.09f, 0.03f), matSignal, false, false);
            Box(root, "BraceL", new Vector3(-3.9f, 4.9f, 0f), new Vector3(1.8f, 0.22f, 0.22f), matRustDark, false, false).transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            Box(root, "BraceR", new Vector3(3.9f, 4.9f, 0f), new Vector3(1.8f, 0.22f, 0.22f), matRustDark, false, false).transform.localRotation = Quaternion.Euler(0f, 0f, 35f);

            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(0f, 5.35f, 0f);

            var arm = new GameObject("Arm").transform;
            arm.SetParent(pivot, false);

            Cylinder(arm, "Chain", new Vector3(0f, -2.1f, 0f), new Vector3(0.1f, 2.1f, 0.1f), matGraphite, false, false);
            for (int i = 0; i < 7; i++)
            {
                float y = -0.65f - i * 0.52f;
                var link = Box(arm, "Link_" + i, new Vector3(0f, y, 0f), new Vector3(0.18f, 0.1f, 0.28f), matChrome, false, false);
                link.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
            }

            var ball = new GameObject("Ball").transform;
            ball.SetParent(arm, false);
            ball.localPosition = new Vector3(0f, -4.35f, 0f);
            Sphere(ball, "WreckingBall", Vector3.zero, new Vector3(1.7f, 1.7f, 1.7f), matRustDark, false);
            Cylinder(ball, "Band", Vector3.zero, new Vector3(1.76f, 0.12f, 1.76f), matChrome, false, false);
            Sphere(ball, "BallRust", new Vector3(0.65f, 0.3f, -0.35f), new Vector3(0.5f, 0.35f, 0.4f), matRust, false, false);

            var hazardGo = new GameObject("BallHazard");
            hazardGo.transform.SetParent(ball, false);
            var collider = hazardGo.AddComponent<SphereCollider>();
            collider.radius = 0.95f;
            collider.isTrigger = true;
            var hazard = hazardGo.AddComponent<Hazard>();
            hazard.reason = "УДАР ГРУЗА";

            var glowGo = new GameObject("BallGlow");
            glowGo.transform.SetParent(arm, false);
            glowGo.transform.localPosition = new Vector3(0f, -4.35f, 0f);
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.42f, 0.14f);
            glow.range = 8f;
            glow.intensity = 1.6f;
            glow.shadows = LightShadows.None;

            var swing = root.gameObject.AddComponent<SwingingHazard>();
            swing.arm = arm;
            swing.ball = ball;
            swing.glow = glow;
            swing.amplitude = 56f;
            swing.period = 2.7f;
            swing.ballSpin = 38f;

            WorldText(root, "ГРУЗ", new Vector3(0f, 6.02f, -0.32f), 0.24f, UIColors.Rust);
        }

        private static void BuildPatrolCar(Transform parent, Vector3 position, float travel, float period)
        {
            var root = new GameObject("PatrolCar").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            var mover = root.gameObject.AddComponent<MovingPlatform>();
            mover.offset = new Vector3(travel, 0f, 0f);
            mover.period = period;

            if (modelPatrolCar != null)
            {
                PlaceModel(modelPatrolCar, root, "PatrolCarModel", Vector3.zero, 180f, 1f);
            }
            else
            {
                BuildCar(root, Vector3.zero, 0f, true, matRust);
            }

            var hazardGo = new GameObject("CarHazard");
            hazardGo.transform.SetParent(root, false);
            hazardGo.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            var collider = hazardGo.AddComponent<BoxCollider>();
            collider.size = new Vector3(4.8f, 2.0f, 2.3f);
            collider.isTrigger = true;
            var hazard = hazardGo.AddComponent<Hazard>();
            hazard.reason = "МАШИНА";

            var lightGo = new GameObject("CarLight");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.localPosition = new Vector3(2.4f, 0.95f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.5f);
            light.range = 9f;
            light.intensity = 1.4f;
            light.shadows = LightShadows.None;
        }

        private static GameObject BuildCar(Transform parent, Vector3 position, float yaw, bool lit, Material body)
        {
            var root = new GameObject("Car").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box(root, "Lower", new Vector3(0f, 0.72f, 0f), new Vector3(4.6f, 0.65f, 2.0f), body, false);
            Box(root, "Cabin", new Vector3(-0.2f, 1.32f, 0f), new Vector3(2.4f, 0.62f, 1.8f), body, false);
            Box(root, "Hood", new Vector3(1.75f, 0.97f, 0f), new Vector3(1.3f, 0.28f, 1.9f), body, false, false);
            Box(root, "Trunk", new Vector3(-1.95f, 0.98f, 0f), new Vector3(0.9f, 0.26f, 1.9f), body, false, false);
            Box(root, "BumperF", new Vector3(2.33f, 0.6f, 0f), new Vector3(0.14f, 0.24f, 2.0f), matChrome, false, false);
            Box(root, "BumperB", new Vector3(-2.33f, 0.6f, 0f), new Vector3(0.14f, 0.24f, 2.0f), matChrome, false, false);
            Box(root, "WindowL", new Vector3(-0.2f, 1.35f, -0.92f), new Vector3(1.85f, 0.42f, 0.04f), matWindow, false, false);
            Box(root, "WindowR", new Vector3(-0.2f, 1.35f, 0.92f), new Vector3(1.85f, 0.42f, 0.04f), matWindow, false, false);
            Box(root, "Windshield", new Vector3(1.02f, 1.35f, 0f), new Vector3(0.05f, 0.44f, 1.58f), matWindow, false, false);
            Box(root, "RearGlass", new Vector3(-1.42f, 1.35f, 0f), new Vector3(0.05f, 0.42f, 1.58f), matWindow, false, false);
            Box(root, "RustDoor", new Vector3(0.35f, 0.78f, -1.01f), new Vector3(0.9f, 0.45f, 0.02f), matRust, false, false);

            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -1.5f : 1.5f;
                float z = (i < 2) ? -1.02f : 1.02f;
                var wheel = Cylinder(root, "Wheel_" + i, new Vector3(x, 0.4f, z), new Vector3(0.65f, 0.16f, 0.65f), matGraphite, false, false);
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Cylinder(root, "Hub_" + i, new Vector3(x, 0.4f, z), new Vector3(0.3f, 0.17f, 0.3f), matChrome, false, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            Box(root, "HeadL", new Vector3(2.32f, 0.8f, -0.58f), new Vector3(0.06f, 0.2f, 0.32f), lit ? matSignal : matWindow, false, false);
            Box(root, "HeadR", new Vector3(2.32f, 0.8f, 0.58f), new Vector3(0.06f, 0.2f, 0.32f), lit ? matSignal : matWindow, false, false);
            Box(root, "TailL", new Vector3(-2.32f, 0.8f, -0.58f), new Vector3(0.06f, 0.18f, 0.28f), matDanger, false, false);
            Box(root, "TailR", new Vector3(-2.32f, 0.8f, 0.58f), new Vector3(0.06f, 0.18f, 0.28f), matDanger, false, false);

            if (lit)
            {
                var lightGo = new GameObject("Headlight");
                lightGo.transform.SetParent(root, false);
                lightGo.transform.localPosition = new Vector3(2.5f, 0.85f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.78f, 0.5f);
                light.range = 9f;
                light.intensity = 1.5f;
                light.shadows = LightShadows.None;
            }
            return root.gameObject;
        }

        // ------------------------------------------------------------------ поезд и станция

        private static void BuildTrain(Transform parent, Vector3 position)
        {
            var root = new GameObject("LastTrain").transform;
            root.SetParent(parent, false);
            root.localPosition = position;

            if (modelTrainCar != null)
            {
                // Модель из Blender: дверь смотрит в -Y, после поворота на 180° — на игрока.
                PlaceModel(modelTrainCar, root, "TrainCarModel", Vector3.zero, 180f, 1f);

                var modelLightGo = new GameObject("InteriorLight");
                modelLightGo.transform.SetParent(root, false);
                modelLightGo.transform.localPosition = new Vector3(0f, 2.35f, 0.3f);
                var modelLight = modelLightGo.AddComponent<Light>();
                modelLight.type = LightType.Point;
                modelLight.color = new Color(1f, 0.78f, 0.55f);
                modelLight.range = 8f;
                modelLight.intensity = 2.2f;
                modelLight.shadows = LightShadows.None;

                var doorLightGo = new GameObject("DoorLight");
                doorLightGo.transform.SetParent(root, false);
                doorLightGo.transform.localPosition = new Vector3(0f, 2.2f, -2.1f);
                var doorLight = doorLightGo.AddComponent<Light>();
                doorLight.type = LightType.Point;
                doorLight.color = new Color(1f, 0.72f, 0.45f);
                doorLight.range = 7f;
                doorLight.intensity = 1.6f;
                doorLight.shadows = LightShadows.None;

                WorldText(root, "03", new Vector3(-6.4f, 1.52f, -1.60f), 0.5f, UIColors.ChromeBright);
                return;
            }

            Box(root, "Underframe", new Vector3(0f, 0.18f, 0f), new Vector3(18f, 0.35f, 3.0f), matGraphite, false);
            Box(root, "Floor", new Vector3(0f, 0.42f, 0f), new Vector3(17.8f, 0.1f, 2.8f), matConcreteDark, false, false);
            Box(root, "SidePanelL", new Vector3(-5.05f, 1.55f, -1.46f), new Vector3(7.9f, 2.3f, 0.14f), matRustDark, false);
            Box(root, "SidePanelR", new Vector3(5.05f, 1.55f, -1.46f), new Vector3(7.9f, 2.3f, 0.14f), matRustDark, false);
            Box(root, "SideRear", new Vector3(0f, 1.55f, 1.46f), new Vector3(18f, 2.3f, 0.14f), matRustDark, false);
            Box(root, "UpperBand", new Vector3(0f, 2.95f, -1.46f), new Vector3(18.05f, 0.5f, 0.14f), matRust, false);
            Box(root, "UpperBandRear", new Vector3(0f, 2.95f, 1.46f), new Vector3(18.05f, 0.5f, 0.14f), matRust, false);
            Box(root, "Roof", new Vector3(0f, 3.28f, 0f), new Vector3(18.3f, 0.24f, 3.1f), matGraphite, false);
            Box(root, "RoofRustL", new Vector3(0f, 3.24f, -1.48f), new Vector3(18.1f, 0.1f, 0.2f), matRust, false, false);
            Box(root, "RoofRustR", new Vector3(0f, 3.24f, 1.48f), new Vector3(18.1f, 0.1f, 0.2f), matRust, false, false);

            // Открытая дверь вагона.
            Box(root, "DoorPostL", new Vector3(-1.18f, 1.55f, -1.56f), new Vector3(0.13f, 2.4f, 0.12f), matChrome, false, false);
            Box(root, "DoorPostR", new Vector3(1.18f, 1.55f, -1.56f), new Vector3(0.13f, 2.4f, 0.12f), matChrome, false, false);
            Box(root, "DoorTop", new Vector3(0f, 2.72f, -1.56f), new Vector3(2.5f, 0.13f, 0.12f), matChrome, false, false);
            Box(root, "Threshold", new Vector3(0f, 0.46f, -1.56f), new Vector3(2.4f, 0.09f, 0.24f), matChrome, false, false);
            Box(root, "DoorGlowL", new Vector3(-1.26f, 1.55f, -1.63f), new Vector3(0.05f, 2.2f, 0.03f), matChromeGlow, false, false);
            Box(root, "DoorGlowR", new Vector3(1.26f, 1.55f, -1.63f), new Vector3(0.05f, 2.2f, 0.03f), matChromeGlow, false, false);
            Box(root, "DoorGlowTop", new Vector3(0f, 2.79f, -1.63f), new Vector3(2.4f, 0.05f, 0.03f), matChromeGlow, false, false);
            var doorL = Box(root, "DoorLeafL", new Vector3(-1.7f, 1.55f, -1.66f), new Vector3(0.9f, 2.3f, 0.07f), matRust, false, false);
            doorL.transform.localRotation = Quaternion.Euler(0f, -14f, 0f);
            var doorR = Box(root, "DoorLeafR", new Vector3(1.7f, 1.55f, -1.66f), new Vector3(0.9f, 2.3f, 0.07f), matRust, false, false);
            doorR.transform.localRotation = Quaternion.Euler(0f, 14f, 0f);

            // Вид через открытую дверь.
            Box(root, "InteriorBack", new Vector3(0f, 1.6f, 1.34f), new Vector3(17.4f, 2.4f, 0.1f), matGraphite, false, false);
            Box(root, "InteriorCeil", new Vector3(0f, 2.76f, 0f), new Vector3(17.4f, 0.08f, 2.7f), matGraphite, false, false);
            Box(root, "BenchL", new Vector3(-0.85f, 0.85f, 0.72f), new Vector3(1.3f, 0.4f, 0.5f), matRust, false, false);
            Box(root, "BenchR", new Vector3(0.85f, 0.85f, 0.72f), new Vector3(1.3f, 0.4f, 0.5f), matRust, false, false);
            Box(root, "InteriorGlow", new Vector3(0f, 2.3f, 0.2f), new Vector3(2.0f, 0.08f, 0.5f), matChromeGlow, false, false);

            var interiorLightGo = new GameObject("InteriorLight");
            interiorLightGo.transform.SetParent(root, false);
            interiorLightGo.transform.localPosition = new Vector3(0f, 2.35f, 0.3f);
            var interiorLight = interiorLightGo.AddComponent<Light>();
            interiorLight.type = LightType.Point;
            interiorLight.color = new Color(1f, 0.78f, 0.55f);
            interiorLight.range = 7f;
            interiorLight.intensity = 1.8f;
            interiorLight.shadows = LightShadows.None;

            // Окна и номер.
            for (int i = 0; i < 8; i++)
            {
                float x = -7.3f + i * 2.05f;
                if (Mathf.Abs(x) < 1.8f) continue;
                Box(root, "Window_" + i, new Vector3(x, 2.95f, -1.545f), new Vector3(1.25f, 0.28f, 0.04f), matWindowLit, false, false);
            }
            Box(root, "NumberPlate", new Vector3(-6.4f, 1.52f, -1.555f), new Vector3(1.5f, 0.66f, 0.04f), matGraphite, false, false);
            WorldText(root, "03", new Vector3(-6.4f, 1.52f, -1.59f), 0.5f, UIColors.ChromeBright);

            // Тележки и колёса.
            for (int b = 0; b < 2; b++)
            {
                float bx = b == 0 ? -5.9f : 5.9f;
                Box(root, "Bogie_" + b, new Vector3(bx, 0.08f, 0f), new Vector3(2.7f, 0.45f, 2.2f), matGraphite, false, false);
                for (int i = 0; i < 4; i++)
                {
                    float wx = bx + (i % 2 == 0 ? -0.8f : 0.8f);
                    float wz = (i < 2) ? -1.08f : 1.08f;
                    var wheel = Cylinder(root, "Wheel", new Vector3(wx, -0.08f, wz), new Vector3(0.55f, 0.14f, 0.55f), matRustDark, false, false);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }

            Box(root, "Vent", new Vector3(4.2f, 3.48f, 0f), new Vector3(1.8f, 0.22f, 1.0f), matRustDark, false, false);
            Box(root, "Vent2", new Vector3(-3.4f, 3.48f, 0.4f), new Vector3(1.2f, 0.2f, 0.8f), matGraphite, false, false);
            Box(root, "PantoBase", new Vector3(0.8f, 3.48f, 0f), new Vector3(1.6f, 0.16f, 1.3f), matGraphite, false, false);
            var pantoA = Box(root, "PantoA", new Vector3(0.35f, 3.95f, 0f), new Vector3(0.07f, 1.1f, 0.07f), matChrome, false, false);
            pantoA.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            var pantoB = Box(root, "PantoB", new Vector3(1.25f, 3.95f, 0f), new Vector3(0.07f, 1.1f, 0.07f), matChrome, false, false);
            pantoB.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            Box(root, "PantoBar", new Vector3(0.8f, 4.48f, 0f), new Vector3(1.9f, 0.07f, 0.14f), matChrome, false, false);
        }

        // ------------------------------------------------------------------ цех и депо

        private static void BuildFactoryHall(Transform parent)
        {
            var root = new GameObject("FactoryHall").transform;
            root.SetParent(parent, false);
            float mid = 85f;
            float length = 56f;

            Box(root, "Floor", new Vector3(0f, -10.85f, mid), new Vector3(20.6f, 0.3f, length), matConcreteDark, false, false);
            Box(root, "WallL", new Vector3(-10.3f, 0.6f, mid), new Vector3(0.55f, 22.9f, length), matBrick, false);
            Box(root, "WallR", new Vector3(10.3f, 0.6f, mid), new Vector3(0.55f, 22.9f, length), matBrick, false);
            Box(root, "Roof", new Vector3(0f, 12.3f, mid), new Vector3(21.2f, 0.5f, length + 1f), matGraphite, false);
            Box(root, "Skylight", new Vector3(0f, 12.06f, mid), new Vector3(6.0f, 0.08f, length - 4f), matWindow, false, false);

            // Фасад с проёмом.
            Box(root, "FrontSideL", new Vector3(-7.7f, 0.6f, 56.9f), new Vector3(5.6f, 22.9f, 0.55f), matBrick, false);
            Box(root, "FrontSideR", new Vector3(7.7f, 0.6f, 56.9f), new Vector3(5.6f, 22.9f, 0.55f), matBrick, false);
            Box(root, "FrontTop", new Vector3(0f, 9.75f, 56.9f), new Vector3(21.2f, 5.2f, 0.55f), matBrick, false);
            Box(root, "FrontBeam", new Vector3(0f, 7.15f, 56.7f), new Vector3(10.6f, 0.5f, 0.65f), matRust, false);
            Box(root, "RearSideL", new Vector3(-7.7f, 0.6f, 113.05f), new Vector3(5.6f, 22.9f, 0.55f), matBrick, false);
            Box(root, "RearSideR", new Vector3(7.7f, 0.6f, 113.05f), new Vector3(5.6f, 22.9f, 0.55f), matBrick, false);
            Box(root, "RearTop", new Vector3(0f, 9.75f, 113.05f), new Vector3(21.2f, 5.2f, 0.55f), matBrick, false);
            Box(root, "RearBeam", new Vector3(0f, 7.15f, 113.25f), new Vector3(10.6f, 0.5f, 0.65f), matRust, false);

            for (int i = 0; i < 9; i++)
            {
                float z = 60f + i * 6.4f;
                Box(root, "PilasterL", new Vector3(-10.0f, 0.6f, z), new Vector3(0.5f, 22.9f, 0.7f), matRustDark, false, false);
                Box(root, "PilasterR", new Vector3(10.0f, 0.6f, z), new Vector3(0.5f, 22.9f, 0.7f), matRustDark, false, false);
                Box(root, "Truss", new Vector3(0f, 10.7f, z), new Vector3(20.2f, 0.36f, 0.36f), matRust, false, false);
                Box(root, "TrussDropL", new Vector3(-5.0f, 11.4f, z), new Vector3(0.22f, 1.2f, 0.22f), matRustDark, false, false);
                Box(root, "TrussDropR", new Vector3(5.0f, 11.4f, z), new Vector3(0.22f, 1.2f, 0.22f), matRustDark, false, false);
            }

            for (int i = 0; i < 7; i++)
            {
                float z = 61f + i * 7.2f;
                Box(root, "SideWindowL", new Vector3(-10.62f, 8.6f, z), new Vector3(0.06f, 2.0f, 3.4f), i % 3 == 0 ? matWindowLit : matWindow, false, false);
                Box(root, "SideWindowR", new Vector3(10.62f, 8.6f, z), new Vector3(0.06f, 2.0f, 3.4f), i % 3 == 1 ? matWindowLit : matWindow, false, false);
            }

            Pipe(root, "WallPipeL", new Vector3(-9.7f, 7.2f, 58f), new Vector3(-9.7f, 7.2f, 112f), 0.14f, matRustDark);
            Pipe(root, "WallPipeR", new Vector3(9.7f, 6.3f, 58f), new Vector3(9.7f, 6.3f, 112f), 0.11f, matRust);
            for (int i = 0; i < 4; i++)
            {
                float z = 64f + i * 12f;
                Pipe(root, "PipeDropL", new Vector3(-9.7f, 7.2f, z), new Vector3(-9.7f, 2.0f, z), 0.12f, matRustDark);
                Pipe(root, "PipeDropR", new Vector3(9.7f, 6.3f, z + 4f), new Vector3(9.7f, 1.6f, z + 4f), 0.1f, matRust);
            }

            AddHangingLamp(root, new Vector3(0f, 7.6f, 63f), 1.4f, true);
            AddHangingLamp(root, new Vector3(0f, 7.6f, 75f), 1.4f, true);
            AddHangingLamp(root, new Vector3(0f, 7.6f, 87f), 1.4f, true);
            AddHangingLamp(root, new Vector3(0f, 7.6f, 99f), 1.4f, true);
            AddHangingLamp(root, new Vector3(0f, 7.6f, 108f), 1.3f, true);

            // Станки вдоль стен.
            var rng = new System.Random(7711);
            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = 61f + i * 6.2f;
                float x = side * (7.2f + (float)rng.NextDouble() * 0.5f);
                if (modelMachine != null)
                {
                    float yaw = side < 0f ? 90f : -90f;
                    yaw += (float)(rng.NextDouble() * 10.0 - 5.0);
                    PlaceModel(modelMachine, root, "Machine_" + i, new Vector3(x, -10.7f, z), yaw,
                        0.92f + (float)rng.NextDouble() * 0.16f);
                }
                else
                {
                    float y = -1.0f - (float)rng.NextDouble() * 1.5f;
                    Box(root, "Machine", new Vector3(x, y, z),
                        new Vector3(1.7f + (float)rng.NextDouble(), 2.2f + (float)rng.NextDouble() * 2.2f, 2.4f), matGraphite, false);
                }

                Cylinder(root, "Tank", new Vector3(side * 8.6f, -9.4f, z + 2.1f),
                    new Vector3(2.2f, 2.0f, 2.2f), i % 2 == 0 ? matRust : matRustDark, false);
                Pipe(root, "MachinePipe", new Vector3(side * 8.6f, -7.4f, z + 2.1f),
                    new Vector3(side * 8.6f, -3.4f, z + 2.1f), 0.09f, matChrome);
            }
            Crate(root, new Vector3(-7.0f, -10.7f, 106f), 1.4f, 12f);
            Barrel(root, new Vector3(7.2f, -10.7f, 105.4f), 1.3f, 40f);
            Puddle(root, new Vector3(4.6f, -10.68f, 70f), new Vector2(3.0f, 2.0f), 20f);
        }

        private static void BuildDepot(Transform parent)
        {
            var root = new GameObject("Depot").transform;
            root.SetParent(parent, false);
            float mid = 177.75f;

            Box(root, "WallL", new Vector3(-10.6f, 0.6f, mid), new Vector3(0.55f, 22.9f, 27f), matBrick, false);
            Box(root, "WallR", new Vector3(10.6f, 0.6f, mid), new Vector3(0.55f, 22.9f, 27f), matBrick, false);
            Box(root, "BackWall", new Vector3(0f, 0.6f, 191f), new Vector3(21.7f, 22.9f, 0.6f), matBrick, false);
            Box(root, "Roof", new Vector3(0f, 11.15f, mid), new Vector3(21.8f, 0.5f, 28f), matGraphite, false);
            Box(root, "BackWindowBand", new Vector3(0f, 7.4f, 190.65f), new Vector3(18.0f, 2.2f, 0.06f), matWindow, false, false);
            for (int i = 0; i < 7; i++)
            {
                Box(root, "BackMullion", new Vector3(-9f + i * 3f, 7.4f, 190.6f), new Vector3(0.14f, 2.4f, 0.08f), matRustDark, false, false);
            }
            for (int i = 0; i < 4; i++)
            {
                float z = 168f + i * 6f;
                Box(root, "Truss", new Vector3(0f, 10.4f, z), new Vector3(20.4f, 0.4f, 0.4f), matRust, false, false);
                Box(root, "TrussDropL", new Vector3(-5f, 10.95f, z), new Vector3(0.22f, 0.9f, 0.22f), matRustDark, false, false);
                Box(root, "TrussDropR", new Vector3(5f, 10.95f, z), new Vector3(0.22f, 0.9f, 0.22f), matRustDark, false, false);
            }

            Box(root, "Canopy", new Vector3(0f, 6.7f, 176.2f), new Vector3(15f, 0.25f, 9.5f), matRust, false);
            Box(root, "CanopyEdge", new Vector3(0f, 6.55f, 171.55f), new Vector3(15f, 0.3f, 0.18f), matChrome, false, false);
            for (int i = 0; i < 3; i++)
            {
                float z = 172f + i * 5f;
                Box(root, "ColumnL", new Vector3(-7.3f, 3.95f, z), new Vector3(0.34f, 5.5f, 0.34f), matRust, false);
                Box(root, "ColumnR", new Vector3(7.3f, 3.95f, z), new Vector3(0.34f, 5.5f, 0.34f), matRust, false);
            }
            AddHangingLamp(root, new Vector3(-3.8f, 5.8f, 174.2f), 1.4f, true);
            AddHangingLamp(root, new Vector3(3.8f, 5.8f, 178.2f), 1.4f, true);

            // Часы депо.
            var clock = new GameObject("DepotClock").transform;
            clock.SetParent(root, false);
            clock.localPosition = new Vector3(6.4f, 5.7f, 178.6f);
            Box(clock, "Board", Vector3.zero, new Vector3(2.3f, 1.1f, 0.12f), matGraphite, false, false);
            Box(clock, "Edge", new Vector3(0f, 0.58f, -0.02f), new Vector3(2.4f, 0.06f, 0.06f), matChrome, false, false);
            WorldText(clock, "03:07", new Vector3(0f, 0f, -0.07f), 0.34f, UIColors.ChromeBright);
            Pipe(root, "ClockRod", new Vector3(6.4f, 5.8f, 178.6f), new Vector3(6.4f, 6.5f, 178.6f), 0.05f, matRustDark);

            // Путь и шпалы.
            Box(root, "TrackBed", new Vector3(0f, 0.35f, 182.4f), new Vector3(23f, 0.25f, 4.4f), matAsphalt, false, false);
            for (int i = 0; i < 15; i++)
            {
                float x = -10.5f + i * 1.5f;
                Box(root, "Sleeper", new Vector3(x, 0.50f, 182.2f), new Vector3(0.42f, 0.12f, 3.2f), matGraphite, false, false);
            }
            Box(root, "RailL", new Vector3(0f, 0.63f, 181.2f), new Vector3(23f, 0.12f, 0.14f), matChrome, false, false);
            Box(root, "RailR", new Vector3(0f, 0.63f, 182.7f), new Vector3(23f, 0.12f, 0.14f), matChrome, false, false);

            // Задняя вывеска.
            var sign = new GameObject("DepotSign").transform;
            sign.SetParent(root, false);
            sign.localPosition = new Vector3(0f, 8.6f, 190.6f);
            Box(sign, "Board", Vector3.zero, new Vector3(9.2f, 2.0f, 0.12f), matGraphite, false, false);
            Box(sign, "Line", new Vector3(0f, 0.95f, -0.05f), new Vector3(9.4f, 0.07f, 0.08f), matSignal, false, false);
            WorldText(sign, "ДЕПО", new Vector3(0f, 0f, -0.08f), 0.72f, UIColors.Rust);

            // Боковые окна и фонари.
            for (int i = 0; i < 5; i++)
            {
                float z = 167f + i * 5f;
                Box(root, "SideWindowL", new Vector3(-10.92f, 7.2f, z), new Vector3(0.06f, 2.4f, 2.6f), i % 2 == 0 ? matWindowLit : matWindow, false, false);
                Box(root, "SideWindowR", new Vector3(10.92f, 7.2f, z), new Vector3(0.06f, 2.4f, 2.6f), i % 2 == 1 ? matWindowLit : matWindow, false, false);
            }

            Crate(root, new Vector3(-8.2f, 1.2f, 173.5f), 1.2f, 22f);
            Barrel(root, new Vector3(8.4f, 1.2f, 172.6f), 1.1f, -30f);
            Crate(root, new Vector3(8.1f, 1.2f, 169.4f), 1.0f, -12f);
            Puddle(root, new Vector3(-5.6f, 1.212f, 170.4f), new Vector2(2.2f, 1.2f), 14f);
        }
    }
}
