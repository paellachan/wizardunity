// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityCommon;
using UnityEngine;

namespace Naninovel.UI
{
    public class DebugInfoGUI : MonoBehaviour
    {
        private const int windowId = 0;

        public static KeyCode PreviousKey { get; set; } = KeyCode.LeftArrow;
        public static KeyCode NextKey { get; set; } = KeyCode.RightArrow;
        public static KeyCode PlayKey { get; set; } = KeyCode.UpArrow;
        public static KeyCode StopKey { get; set; } = KeyCode.DownArrow;

        private static bool initialized, show;
        private static Rect windowRect = new Rect(20, 20, 250, 125);
        private static EngineVersion version;
        private static ScriptPlayer player;
        private static AudioManager audioManager;
        private static string lastActionInfo, lastAutoVoiceName;

        public static void Toggle ()
        {
            if (!initialized)
            {
                Engine.CreateObject<DebugInfoGUI>(nameof(DebugInfoGUI));
                initialized = true;
            }
            show = !show;
        }

        private void Awake ()
        {
            version = EngineVersion.LoadFromResources();
            player = Engine.GetService<ScriptPlayer>();
            audioManager = Engine.GetService<AudioManager>();
        }

        private void OnEnable ()
        {
            player.OnCommandExecutionStart += HandleActionExecuted;
        }

        private void OnDisable ()
        {
            player.OnCommandExecutionStart -= HandleActionExecuted;
        }

        private void Update ()
        {
            if (!show) return;

            if (Input.GetKeyDown(PreviousKey)) player.RewindToPreviousCommandAsync(resumePlayback: false).WrapAsync();
            if (Input.GetKeyDown(NextKey)) player.RewindToNextCommandAsync(resumePlayback: false).WrapAsync();
            if (Input.GetKeyDown(PlayKey)) player.Play();
            if (Input.GetKeyDown(StopKey)) player.Stop();
        }

        private void OnGUI ()
        {
            if (!show) return;

            windowRect = GUI.Window(windowId, windowRect, DrawWindow, 
                string.IsNullOrEmpty(lastActionInfo) ? $"Naninovel ver. {version.Version}" : lastActionInfo);
        }

        private void DrawWindow (int windowID)
        {
            if (player.PlayedCommand != null)
            {
                if (!string.IsNullOrEmpty(lastAutoVoiceName))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Auto Voice: ");
                    GUILayout.TextField(lastAutoVoiceName);
                    GUILayout.EndHorizontal();
                }

                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<< previous")) player.RewindToPreviousCommandAsync(resumePlayback: false).WrapAsync();
                if (GUILayout.Button("next >>")) player.RewindToNextCommandAsync(resumePlayback: false).WrapAsync();
                GUILayout.EndHorizontal();
                if (!player.IsPlaying && GUILayout.Button("Play")) player.Play();
                if (player.IsPlaying && GUILayout.Button("Stop")) player.Stop();
                if (GUILayout.Button("Close Window")) show = false;
            }

            GUI.DragWindow();
        }

        private void HandleActionExecuted (Commands.Command command)
        {
            if (command is null) return;

            lastActionInfo = $"{player.PlayedScript.Name} #{player.PlayedCommand.LineIndex}.{player.PlayedCommand.InlineIndex}";

            if (audioManager != null && audioManager.AutoVoicingEnabled && command is Commands.PrintText printAction)
                lastAutoVoiceName = $"{player.PlayedScript.Name}/{printAction.LineNumber}.{printAction.InlineIndex}";
        }
    }
}
