// Copyright (c) 2025 Yize Wu
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gsplat
{
    public enum GsplatFrameSource
    {
        ManualList,
        AssetDatabasePath,
        ResourcesPath
    }

    [DisallowMultipleComponent]
    public class GsplatSequencePlayer : MonoBehaviour
    {
        public GsplatRenderer Renderer;
        public GsplatFrameSource FrameSource = GsplatFrameSource.ManualList;
        public bool SortByName = true;

        [Tooltip("Used when FrameSource is ManualList.")]
        public List<GsplatAsset> Frames = new();

        [Tooltip("Used when FrameSource is AssetDatabasePath (Editor only).")]
        public string AssetSearchPath = "Assets/GsplatFrames";

        [Tooltip("Used when FrameSource is ResourcesPath (runtime).")]
        public string ResourcesPath = "GsplatFrames";

        [Range(0.1f, 120f)] public float FramesPerSecond = 12f;
        public bool Loop = true;

        public bool IsPlaying => m_playing;
        public bool IsPaused => m_paused;
        public int FrameIndex => m_frameIndex;

        bool m_playing;
        bool m_paused;
        float m_time;
        int m_frameIndex;

        void OnEnable()
        {
            if (!Renderer)
                Renderer = GetComponent<GsplatRenderer>();
            ReloadFrames();
        }

        void OnValidate()
        {
            if (!Renderer)
                Renderer = GetComponent<GsplatRenderer>();
            ReloadFrames();
        }

        void Update()
        {
            if (!m_playing || m_paused || Frames.Count == 0)
                return;

            var frameDuration = 1.0f / Mathf.Max(FramesPerSecond, 0.01f);
            m_time += Time.deltaTime;
            while (m_time >= frameDuration)
            {
                m_time -= frameDuration;
                AdvanceFrame();
                if (!m_playing)
                    break;
            }
        }

        public void Play()
        {
            ReloadFrames();
            if (Frames.Count == 0)
                return;
            m_playing = true;
            m_paused = false;
            ApplyFrame();
        }

        public void Pause()
        {
            if (!m_playing)
                return;
            m_paused = true;
        }

        public void Resume()
        {
            if (!m_playing)
                return;
            m_paused = false;
        }

        public void Stop()
        {
            m_playing = false;
            m_paused = false;
            m_time = 0f;
            m_frameIndex = 0;
            ApplyFrame();
        }

        public void SetFrame(int index)
        {
            if (Frames.Count == 0)
                return;
            m_frameIndex = Mathf.Clamp(index, 0, Frames.Count - 1);
            ApplyFrame();
        }

        public void ReloadFrames()
        {
            if (FrameSource == GsplatFrameSource.ManualList)
            {
                CleanupFrames();
                ApplyFrame();
                return;
            }

#if UNITY_EDITOR
            if (FrameSource == GsplatFrameSource.AssetDatabasePath)
            {
                Frames = GsplatSequenceUtils.LoadFromAssetDatabase(AssetSearchPath);
                CleanupFrames();
                ApplyFrame();
                return;
            }
#endif

            if (FrameSource == GsplatFrameSource.ResourcesPath)
            {
                Frames = GsplatSequenceUtils.LoadFromResources(ResourcesPath);
                CleanupFrames();
                ApplyFrame();
            }
        }

        void AdvanceFrame()
        {
            if (Frames.Count == 0)
                return;

            var next = m_frameIndex + 1;
            if (next >= Frames.Count)
            {
                if (Loop)
                    next = 0;
                else
                {
                    Stop();
                    return;
                }
            }

            m_frameIndex = next;
            ApplyFrame();
        }

        void ApplyFrame()
        {
            if (!Renderer || Frames.Count == 0)
                return;
            m_frameIndex = Mathf.Clamp(m_frameIndex, 0, Frames.Count - 1);
            Renderer.GsplatAsset = Frames[m_frameIndex];
        }

        void CleanupFrames()
        {
            Frames.RemoveAll(frame => frame == null);
            if (SortByName)
                Frames.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            m_frameIndex = Mathf.Clamp(m_frameIndex, 0, Math.Max(Frames.Count - 1, 0));
        }
    }

    static class GsplatSequenceUtils
    {
#if UNITY_EDITOR
        public static List<GsplatAsset> LoadFromAssetDatabase(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return new List<GsplatAsset>();

            var guids = UnityEditor.AssetDatabase.FindAssets("t:GsplatAsset", new[] { assetPath });
            var assets = new List<GsplatAsset>(guids.Length);
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GsplatAsset>(path);
                if (asset != null)
                    assets.Add(asset);
            }
            return assets;
        }
#endif

        public static List<GsplatAsset> LoadFromResources(string resourcesPath)
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
                return new List<GsplatAsset>();
            return new List<GsplatAsset>(Resources.LoadAll<GsplatAsset>(resourcesPath));
        }
    }
}
