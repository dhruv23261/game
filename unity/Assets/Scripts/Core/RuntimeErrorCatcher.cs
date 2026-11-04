using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MobControl.Core
{
    /// <summary>
    /// Global Error Catcher and On-Screen Log Viewer for Android Standalone Builds.
    /// Intercepts unhandled exceptions and error logs before any scene or script loads,
    /// preventing abrupt crashes and displaying the error clearly on the phone screen.
    /// </summary>
    public class RuntimeErrorCatcher : MonoBehaviour
    {
        private static RuntimeErrorCatcher s_instance;
        private static readonly List<LogEntry> s_entries = new List<LogEntry>();
        private static bool s_hasCriticalError = false;
        private static bool s_showGui = false;
        private Vector2 scrollPos = Vector2.zero;
        private static string logFilePath = "";

        private struct LogEntry
        {
            public string condition;
            public string stackTrace;
            public LogType type;
            public string time;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void PreloadInit()
        {
            if (s_instance != null) return;

            GameObject catcherObj = new GameObject("[RuntimeErrorCatcher]");
            DontDestroyOnLoad(catcherObj);
            s_instance = catcherObj.AddComponent<RuntimeErrorCatcher>();

            logFilePath = Path.Combine(Application.persistentDataPath, "game_runtime_log.txt");

            Application.logMessageReceived += OnLogMessageReceived;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Debug.Log($"[RuntimeErrorCatcher] Active. Persistent log path: {logFilePath}");
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            LogEntry entry = new LogEntry
            {
                condition = condition,
                stackTrace = stackTrace,
                type = type,
                time = DateTime.Now.ToString("HH:mm:ss")
            };

            s_entries.Add(entry);
            if (s_entries.Count > 60) s_entries.RemoveAt(0);

            if (type == LogType.Exception || type == LogType.Error)
            {
                s_hasCriticalError = true;
                if (!Application.isEditor)
                {
                    s_showGui = true;
                }
            }

            try
            {
                if (!string.IsNullOrEmpty(logFilePath))
                {
                    File.AppendAllText(logFilePath, $"[{entry.time}][{type}] {condition}\n{stackTrace}\n");
                }
            }
            catch { }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            s_hasCriticalError = true;
            s_showGui = true;
            string msg = $"[CRITICAL UNHANDLED EXCEPTION] {e.ExceptionObject}";
            LogEntry entry = new LogEntry
            {
                condition = msg,
                stackTrace = "",
                type = LogType.Exception,
                time = DateTime.Now.ToString("HH:mm:ss")
            };
            s_entries.Add(entry);

            try
            {
                if (!string.IsNullOrEmpty(logFilePath))
                {
                    File.AppendAllText(logFilePath, $"[{entry.time}][UNHANDLED] {msg}\n");
                }
            }
            catch { }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                s_showGui = !s_showGui;
            }
            if (Input.touchCount >= 4 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                s_showGui = !s_showGui;
            }
        }

        private void OnGUI()
        {
            if (!s_showGui) return;

            GUI.depth = -9999;
            float boxW = Screen.width - 20;
            float boxH = Screen.height * 0.70f;
            Rect boxRect = new Rect(10, 60, boxW, boxH);

            GUI.Box(boxRect, s_hasCriticalError ? "⚠ RUNTIME ERROR / EXCEPTION DETECTED ⚠" : "DIAGNOSTIC LOG CONSOLE");

            GUILayout.BeginArea(new Rect(20, 95, boxW - 20, boxH - 45));
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.width / 28, 14, 24),
                fontStyle = FontStyle.Bold
            };

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.width / 34, 12, 20),
                wordWrap = true
            };

            for (int i = s_entries.Count - 1; i >= 0; i--)
            {
                var entry = s_entries[i];
                if (entry.type == LogType.Exception || entry.type == LogType.Error)
                {
                    titleStyle.normal.textColor = Color.red;
                    textStyle.normal.textColor = Color.yellow;
                }
                else if (entry.type == LogType.Warning)
                {
                    titleStyle.normal.textColor = new Color(1f, 0.6f, 0.1f);
                    textStyle.normal.textColor = Color.white;
                }
                else
                {
                    titleStyle.normal.textColor = Color.cyan;
                    textStyle.normal.textColor = Color.white;
                }

                GUILayout.Label($"[{entry.time}] {entry.type}: {entry.condition}", titleStyle);
                if (!string.IsNullOrEmpty(entry.stackTrace))
                {
                    GUILayout.Label(entry.stackTrace, textStyle);
                }
                GUILayout.Space(6);
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CLEAR", GUILayout.Height(40)))
            {
                s_entries.Clear();
                s_hasCriticalError = false;
            }
            if (GUILayout.Button("RESUME GAME", GUILayout.Height(40)))
            {
                s_showGui = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}
