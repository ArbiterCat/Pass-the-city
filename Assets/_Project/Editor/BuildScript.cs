using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SvoiPut.EditorTools
{
    /// <summary>Сборка готовой Windows-версии: сцена генерируется заново, затем собирается exe.</summary>
    public static class BuildScript
    {
        private const string BuildFolder = "Build";
        private const string ExecutableName = "SvoiPut.exe";

        [MenuItem("Свой путь/2. Собрать Windows-сборку", false, 20)]
        public static void BuildWindowsMenu()
        {
            string executable = BuildWindows();
            EditorUtility.RevealInFinder(executable);
        }

        [MenuItem("Свой путь/3. Открыть игровую сцену", false, 30)]
        public static void OpenGameScene()
        {
            if (!File.Exists(CourseBuilder.ScenePath))
            {
                CourseBuilder.BuildAll();
            }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(CourseBuilder.ScenePath, OpenSceneMode.Single);
        }

        /// <summary>Точка входа для командной строки: -executeMethod SvoiPut.EditorTools.BuildScript.BuildWindows</summary>
        public static string BuildWindows()
        {
            CourseBuilder.BuildAll();

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildDirectory = Path.Combine(projectRoot, BuildFolder);
            Directory.CreateDirectory(buildDirectory);
            string executablePath = Path.Combine(buildDirectory, ExecutableName);

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { CourseBuilder.ScenePath },
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log("[SvoiPut] Сборка Windows: " + executablePath);
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception("[SvoiPut] Сборка не удалась: " + summary.result +
                                    ", ошибок: " + summary.totalErrors +
                                    ", размер: " + summary.totalSize + " байт");
            }

            Debug.Log("[SvoiPut] Сборка успешна: " + executablePath +
                      " (размер: " + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " МБ, " +
                      "время: " + summary.totalTime.TotalSeconds.ToString("0") + " c)");
            return executablePath;
        }
    }
}
