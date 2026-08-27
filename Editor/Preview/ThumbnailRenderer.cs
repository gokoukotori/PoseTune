using System.IO;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    public static class ThumbnailRenderer
    {
        public static Texture2D GenerateThumbnail(PoseClip pose, string folder, PoseTuneRoot root = null)
        {
            if (pose == null)
            {
                return null;
            }

            root ??= pose.GetComponentInParent<PoseTuneRoot>(true);
            if (root != null && !root.enableIconGeneration)
            {
                return null;
            }

            if (root != null && root.questLowMemoryMode)
            {
                return null;
            }

            if (!TryGetThumbnailAssetPath(pose, folder, out var path))
            {
                PoseTuneLog.Error("thumbnail生成には保存済みSceneまたはPrefab上のPoseClipが必要です。", pose);
                return null;
            }

            PoseTuneProjectAssetUtility.EnsureFolder(folder);
            var size = Mathf.Clamp(root != null ? root.previewSettings.thumbnailSize : 256, 64, 1024);
            var texture = RenderPose(pose, root, size) ?? CreateFallbackTexture(size, root);
            return PersistThumbnail(pose, path, texture);
        }

        internal static Texture2D PersistThumbnail(PoseClip pose, string path, Texture2D texture)
        {
            var transferTemporaryToCaller = false;
            try
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (imported != null)
                {
                    if (pose != null)
                    {
                        pose.customIcon = imported;
                        EditorUtility.SetDirty(pose);
                    }

                    return imported;
                }

                transferTemporaryToCaller = true;
                return texture;
            }
            finally
            {
                if (!transferTemporaryToCaller && texture != null && !EditorUtility.IsPersistent(texture))
                {
                    Object.DestroyImmediate(texture);
                }
            }
        }

        public static bool TryGetThumbnailAssetPath(PoseClip pose, string folder, out string path)
        {
            return new PoseTuneIconCacheService().TryGetThumbnailAssetPath(pose, folder, out path);
        }

        private static Texture2D RenderPose(PoseClip pose, PoseTuneRoot root, int size)
        {
            if (pose == null)
            {
                return null;
            }

            var avatar = root != null ? root.GetComponentInParent<VRCAvatarDescriptor>(true) : null;
            if (avatar == null)
            {
                return null;
            }

            GameObject clone = null;
            Camera camera = null;
            Light light = null;
            RenderTexture renderTexture = null;
            var ownsAnimationMode = false;
            try
            {
                clone = Object.Instantiate(avatar.gameObject);
                clone.hideFlags = HideFlags.HideAndDontSave;
                clone.transform.position = new Vector3(10000f, 10000f, 10000f);
                clone.transform.rotation = Quaternion.identity;

                var sampleClip = PoseClipPreparationService.PrepareClipForSampling(pose, pose.displayName + "_Thumbnail");
                if (sampleClip == null)
                {
                    return null;
                }

                var samplingStarted = false;
                try
                {
                    if (!AnimationMode.InAnimationMode())
                    {
                        AnimationMode.StartAnimationMode();
                        ownsAnimationMode = true;
                    }

                    AnimationMode.BeginSampling();
                    samplingStarted = true;
                    AnimationMode.SampleAnimationClip(clone, sampleClip, 0f);
                }
                finally
                {
                    if (samplingStarted)
                    {
                        AnimationMode.EndSampling();
                    }

                    PoseClipPreparationService.ReleasePreparedClipForSampling(sampleClip);
                }

                var bounds = CalculateBounds(clone);
                if (bounds.size == Vector3.zero)
                {
                    return null;
                }

                var rig = new GameObject("PoseTune Thumbnail Camera");
                rig.hideFlags = HideFlags.HideAndDontSave;
                camera = rig.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.Color;
                camera.backgroundColor = root != null ? root.previewSettings.backgroundColor : new Color(0.08f, 0.08f, 0.08f, 1f);
                camera.fieldOfView = 28f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.transform.position = CalculateCameraPosition(bounds, pose.cameraOffset);
                camera.transform.LookAt(CalculateLookAtTarget(bounds, pose.cameraOffset));

                light = new GameObject("PoseTune Thumbnail Light").AddComponent<Light>();
                light.gameObject.hideFlags = HideFlags.HideAndDontSave;
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

                renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();

                var previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = renderTexture;
                    var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                    texture.Apply();
                    return texture;
                }
                finally
                {
                    RenderTexture.active = previous;
                }
            }
            finally
            {
                if (ownsAnimationMode && AnimationMode.InAnimationMode())
                {
                    AnimationMode.StopAnimationMode();
                }

                if (renderTexture != null)
                {
                    if (camera != null && camera.targetTexture == renderTexture)
                    {
                        camera.targetTexture = null;
                    }

                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }

                if (camera != null)
                {
                    Object.DestroyImmediate(camera.gameObject);
                }

                if (light != null)
                {
                    Object.DestroyImmediate(light.gameObject);
                }

                if (clone != null)
                {
                    Object.DestroyImmediate(clone);
                }
            }
        }

        public static Vector3 CalculateCameraPosition(Bounds bounds, Vector3 cameraOffset)
        {
            return bounds.center +
                   new Vector3(0f, bounds.extents.y * 0.15f, -Mathf.Max(1.5f, bounds.extents.magnitude * 2.8f)) +
                   cameraOffset;
        }

        private static Vector3 CalculateLookAtTarget(Bounds bounds, Vector3 cameraOffset)
        {
            return bounds.center + Vector3.up * bounds.extents.y * 0.2f + cameraOffset;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            var initialized = false;
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private static Texture2D CreateFallbackTexture(int size, PoseTuneRoot root)
        {
            var color = root != null ? root.previewSettings.backgroundColor : new Color(0.08f, 0.08f, 0.08f, 1f);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
