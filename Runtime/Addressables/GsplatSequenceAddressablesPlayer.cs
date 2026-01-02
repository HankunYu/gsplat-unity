// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gsplat
{
    [DisallowMultipleComponent]
    public class GsplatSequenceAddressablesPlayer : MonoBehaviour
    {
        public GsplatRenderer renderer;

        [Range(0.1f, 120f)] public float framesPerSecond = 12f;
        public bool loop = true;

        [Min(0)] public int preloadAhead = 2;
        [Min(0)] public int preloadBehind = 1;
        [Min(0)] public int maxCachedFrames = 6;
        public bool holdLastFrameWhileLoading = true;

        public bool IsPlaying { get; private set; }

        public bool IsPaused { get; private set; }

        public int FrameIndex { get; private set; }

        private float _time;
        private bool _active;

        [Tooltip("Addressable references in playback order.")]
        public List<AssetReferenceT<GsplatAsset>> frameReferences = new();

        private readonly Dictionary<int, AsyncOperationHandle<GsplatAsset>> _mLoadedHandles = new();
        private readonly HashSet<int> _mInflight = new();

        private void OnEnable()
        {
            if (!renderer)
                renderer = GetComponent<GsplatRenderer>();
            _active = true;
        }

        private void OnDisable()
        {
            _active = false;
            ReleaseAll();
        }

        private void OnValidate()
        {
            if (!renderer)
                renderer = GetComponent<GsplatRenderer>();
            ClampCacheSettings();
        }

        private void Update()
        {
            if (!IsPlaying || IsPaused || frameReferences.Count == 0)
                return;

            float frameDuration = 1.0f / Mathf.Max(framesPerSecond, 0.01f);
            _time += Time.deltaTime;
            while (_time >= frameDuration)
            {
                _time -= frameDuration;
                AdvanceFrame();
                if (!IsPlaying)
                    break;
            }
        }

        public void Play()
        {
            ClampCacheSettings();
            if (frameReferences.Count == 0)
                return;
            IsPlaying = true;
            IsPaused = false;
            RequestFrame(FrameIndex);
            EnsurePrefetch();
            ApplyFrameIfReady(FrameIndex);
        }

        public void Pause()
        {
            if (!IsPlaying)
                return;
            IsPaused = true;
        }

        public void Resume()
        {
            if (!IsPlaying)
                return;
            IsPaused = false;
        }

        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            _time = 0f;
            FrameIndex = 0;
            ApplyFrameIfReady(FrameIndex);
        }

        public void SetFrame(int index)
        {
            if (frameReferences.Count == 0)
                return;
            FrameIndex = Mathf.Clamp(index, 0, frameReferences.Count - 1);
            RequestFrame(FrameIndex);
            EnsurePrefetch();
            ApplyFrameIfReady(FrameIndex);
        }

        public void ReloadFrames()
        {
            ReleaseAll();
            FrameIndex = Mathf.Clamp(FrameIndex, 0, Math.Max(frameReferences.Count - 1, 0));
            if (IsPlaying)
            {
                RequestFrame(FrameIndex);
                EnsurePrefetch();
                ApplyFrameIfReady(FrameIndex);
            }
        }

        private void AdvanceFrame()
        {
            if (frameReferences.Count == 0)
                return;

            int next = FrameIndex + 1;
            if (next >= frameReferences.Count)
            {
                if (loop)
                    next = 0;
                else
                {
                    Stop();
                    return;
                }
            }

            FrameIndex = next;
            RequestFrame(FrameIndex);
            EnsurePrefetch();
            ApplyFrameIfReady(FrameIndex);
        }

        private void RequestFrame(int index)
        {
            if (index < 0 || index >= frameReferences.Count)
                return;
            if (_mLoadedHandles.ContainsKey(index) || _mInflight.Contains(index))
                return;

            AssetReferenceT<GsplatAsset> reference = frameReferences[index];
            if (reference == null || !reference.RuntimeKeyIsValid())
                return;

            AsyncOperationHandle<GsplatAsset> handle = reference.LoadAssetAsync();
            _mInflight.Add(index);
            handle.Completed += op =>
            {
                _mInflight.Remove(index);
                if (!_active)
                {
                    if (op.IsValid())
                        Addressables.Release(op);
                    return;
                }
                if (!op.IsValid() || op.Status != AsyncOperationStatus.Succeeded)
                    return;
                _mLoadedHandles[index] = op;
                if (index == FrameIndex)
                    ApplyFrameIfReady(index);
                TrimCache();
            };
        }

        private void EnsurePrefetch()
        {
            if (frameReferences.Count == 0)
                return;

            int min = Mathf.Max(0, FrameIndex - preloadBehind);
            int max = Mathf.Min(frameReferences.Count - 1, FrameIndex + preloadAhead);
            for (int i = min; i <= max; i++)
                RequestFrame(i);

            TrimCache();
        }

        private void ApplyFrameIfReady(int index)
        {
            if (!renderer)
                return;
            if (_mLoadedHandles.TryGetValue(index, out AsyncOperationHandle<GsplatAsset> handle) && handle.IsValid() &&
                handle.Status == AsyncOperationStatus.Succeeded)
            {
                renderer.GsplatAsset = handle.Result;
            }
            else if (!holdLastFrameWhileLoading)
            {
                renderer.GsplatAsset = null;
            }
        }

        private void TrimCache()
        {
            ClampCacheSettings();
            if (frameReferences.Count == 0)
                return;

            int min = Mathf.Max(0, FrameIndex - preloadBehind);
            int max = Mathf.Min(frameReferences.Count - 1, FrameIndex + preloadAhead);

            List<int> toRelease = new List<int>();
            foreach (KeyValuePair<int, AsyncOperationHandle<GsplatAsset>> pair in _mLoadedHandles)
            {
                if (pair.Key < min || pair.Key > max)
                    toRelease.Add(pair.Key);
            }

            int maxAllowed = Math.Max(maxCachedFrames, (max - min) + 1);
            if (toRelease.Count == 0 && _mLoadedHandles.Count <= maxAllowed)
                return;

            foreach (int index in toRelease)
                ReleaseFrame(index);

            if (_mLoadedHandles.Count <= maxAllowed)
                return;

            int overflow = _mLoadedHandles.Count - maxAllowed;
            List<int> extra = new List<int>();
            foreach (KeyValuePair<int, AsyncOperationHandle<GsplatAsset>> pair in _mLoadedHandles)
            {
                if (pair.Key < min || pair.Key > max)
                    extra.Add(pair.Key);
            }

            extra.Sort((a, b) => Math.Abs(a - FrameIndex).CompareTo(Math.Abs(b - FrameIndex)));
            for (int i = 0; i < overflow && i < extra.Count; i++)
                ReleaseFrame(extra[i]);
        }

        private void ReleaseFrame(int index)
        {
            if (!_mLoadedHandles.TryGetValue(index, out AsyncOperationHandle<GsplatAsset> handle))
                return;
            if (handle.IsValid())
                Addressables.Release(handle);
            _mLoadedHandles.Remove(index);
        }

        private void ReleaseAll()
        {
            foreach (KeyValuePair<int, AsyncOperationHandle<GsplatAsset>> pair in _mLoadedHandles)
            {
                if (pair.Value.IsValid())
                    Addressables.Release(pair.Value);
            }
            _mLoadedHandles.Clear();
            _mInflight.Clear();
        }

        private void ClampCacheSettings()
        {
            int minCache = preloadAhead + preloadBehind + 1;
            if (maxCachedFrames < minCache)
                maxCachedFrames = minCache;
        }
    }
}
