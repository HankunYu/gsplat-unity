// Copyright (c) 2025 Yize Wu
// SPDX-License-Identifier: MIT

using UnityEditor;
using UnityEngine;

namespace Gsplat
{
    [CustomEditor(typeof(GsplatSequencePlayer))]
    public class GsplatSequencePlayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var player = (GsplatSequencePlayer)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play"))
                    player.Play();

                var pauseLabel = player.IsPaused ? "Resume" : "Pause";
                if (GUILayout.Button(pauseLabel))
                {
                    if (player.IsPaused)
                        player.Resume();
                    else
                        player.Pause();
                }

                if (GUILayout.Button("Stop"))
                    player.Stop();
            }

            if (GUILayout.Button("Reload Frames"))
                player.ReloadFrames();
        }
    }
}
