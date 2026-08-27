using System;
using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal interface IKawaiiMigrationAssetStore
    {
        IReadOnlyCollection<PoseClip> BakedPoses { get; }
        Motion PersistPreparedMotion(PoseClip pose, PoseMotionPreparationResult prepared);
        void CommitManifest(IEnumerable<MonoBehaviour> sources, KawaiiMigrationOptions options);
        void Rollback();
    }

    internal sealed class KawaiiMigrationAssetStore : IKawaiiMigrationAssetStore
    {
        private readonly GameObject avatarRoot;
        private readonly KawaiiMigrationReport report;
        private readonly string runGuid = Guid.NewGuid().ToString("N");
        private readonly List<string> createdAssetPaths = new();
        private readonly List<string> createdFolderPaths = new();
        private readonly List<Object> generatedObjects = new();
        private readonly HashSet<PoseClip> bakedPoses = new();
        private readonly KawaiiMigrationManifest manifest;

        public KawaiiMigrationAssetStore(
            GameObject avatarRoot,
            PoseTuneRoot root,
            KawaiiMigrationReport report)
        {
            this.avatarRoot = avatarRoot != null ? avatarRoot : throw new ArgumentNullException(nameof(avatarRoot));
            root = root != null ? root : throw new ArgumentNullException(nameof(root));
            this.report = report ?? throw new ArgumentNullException(nameof(report));
            if (!PoseTuneObjectIdentity.TryGetPersistentId(avatarRoot.transform, out var avatarGlobalObjectId) ||
                !PoseTuneObjectIdentity.TryGetPersistentId(root, out var rootGlobalObjectId))
            {
                throw new InvalidOperationException(
                    "Kawaii migration requires saved Scene or Prefab objects with valid GlobalObjectIds.");
            }

            manifest = ScriptableObject.CreateInstance<KawaiiMigrationManifest>();
            manifest.name = "PoseTune Kawaii Migration Manifest";
            manifest.runGuid = runGuid;
            manifest.avatarGlobalObjectId = avatarGlobalObjectId;
            manifest.rootGlobalObjectId = rootGlobalObjectId;
        }

        public IReadOnlyCollection<PoseClip> BakedPoses => bakedPoses;

        public Motion PersistPreparedMotion(PoseClip pose, PoseMotionPreparationResult prepared)
        {
            if (pose == null)
            {
                throw new ArgumentNullException(nameof(pose));
            }

            if (prepared?.Motion == null)
            {
                throw new InvalidOperationException("BakeAtMigration did not produce a Motion.");
            }

            bakedPoses.Add(pose);
            var rootMotion = prepared.Motion;
            var transientAssets = (prepared.GeneratedAssets ?? new List<Object>())
                .Where(asset => asset != null)
                .Distinct()
                .ToList();
            if (!EditorUtility.IsPersistent(rootMotion) && !transientAssets.Contains(rootMotion))
            {
                transientAssets.Insert(0, rootMotion);
            }

            generatedObjects.AddRange(transientAssets);
            var expectedAssets = transientAssets
                .Where(asset => asset != rootMotion)
                .Select(asset => new ExpectedAsset(asset.GetType(), asset.name))
                .ToList();
            if (EditorUtility.IsPersistent(rootMotion))
            {
                if (transientAssets.Any(asset => !EditorUtility.IsPersistent(asset)))
                {
                    throw new InvalidOperationException("A persistent baked root Motion contains unpersisted generated assets.");
                }

                AddMotionEntry(pose, rootMotion, AssetDatabase.GetAssetPath(rootMotion));
                return rootMotion;
            }

            if (rootMotion is not AnimationClip && rootMotion is not BlendTree)
            {
                throw new InvalidOperationException("BakeAtMigration produced an unsupported Motion type: " + rootMotion.GetType().FullName);
            }

            foreach (var asset in transientAssets.Where(asset => !EditorUtility.IsPersistent(asset)))
            {
                asset.hideFlags = HideFlags.None;
            }

            if (!KawaiiMigrationAssetPathUtility.TryMotionsPath(avatarRoot, runGuid, out var folder) ||
                !KawaiiMigrationAssetPathUtility.TryMotionFileName(
                    pose,
                    pose.displayName,
                    rootMotion is AnimationClip,
                    out var fileName))
            {
                throw new InvalidOperationException(
                    "Kawaii migration could not resolve persistent GlobalObjectIds for generated Motion assets.");
            }

            EnsureTrackedFolder(folder);
            var isClip = rootMotion is AnimationClip;
            var candidate = folder + "/" + fileName;
            var path = AssetDatabase.GenerateUniqueAssetPath(candidate);
            AssetDatabase.CreateAsset(rootMotion, path);
            createdAssetPaths.Add(path);

            var childAssets = transientAssets
                .Where(asset => asset != rootMotion && !EditorUtility.IsPersistent(asset))
                .ToList();
            if (isClip && childAssets.Count > 0)
            {
                throw new InvalidOperationException("An AnimationClip bake unexpectedly produced additional generated assets.");
            }

            foreach (var child in childAssets)
            {
                AssetDatabase.AddObjectToAsset(child, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var persisted = isClip
                ? (Motion)AssetDatabase.LoadAssetAtPath<AnimationClip>(path)
                : AssetDatabase.LoadAssetAtPath<BlendTree>(path);
            if (persisted == null || !EditorUtility.IsPersistent(persisted))
            {
                throw new InvalidOperationException("Failed to reload the persisted BakeAtMigration Motion: " + path);
            }

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var expected in expectedAssets)
            {
                if (!allAssets.Any(asset => asset != null &&
                                            asset.GetType() == expected.Type &&
                                            string.Equals(asset.name, expected.Name, StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException("A generated bake subasset was not persisted: " + expected.Name);
                }
            }

            AddMotionEntry(pose, persisted, path);
            report.Created(persisted, persisted is BlendTree ? "BlendTreeAsset" : "ClipAsset");
            return persisted;
        }

        public void CommitManifest(IEnumerable<MonoBehaviour> sources, KawaiiMigrationOptions options)
        {
            options ??= KawaiiMigrationOptions.Default();
            manifest.optionsSummary = KawaiiMigrationOptionSupport.Summary(options);
            manifest.options = KawaiiMigrationOptionsSnapshot.Capture(options);
            var seenObjects = new HashSet<int>();
            foreach (var source in sources ?? Enumerable.Empty<MonoBehaviour>())
            {
                if (source == null || !seenObjects.Add(source.gameObject.GetInstanceID()))
                {
                    continue;
                }

                manifest.sources.Add(new KawaiiMigrationSourceManifestEntry
                {
                    globalObjectId = PersistentIdOrThrow(source),
                    hierarchyPath = HierarchyPath(source.transform),
                    previousTag = source.gameObject.tag,
                    previousActive = source.gameObject.activeSelf,
                    disposition = options.sourceDisposition.ToString()
                });
            }

            if (!KawaiiMigrationAssetPathUtility.TryReportsPath(avatarRoot, runGuid, out var folder))
            {
                throw new InvalidOperationException(
                    "Kawaii migration could not resolve the Avatar GlobalObjectId for the manifest path.");
            }

            EnsureTrackedFolder(folder);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/Migration_{timestamp}_{runGuid}.asset");
            manifest.manifestAssetPath = path;
            manifest.createdAssetPaths = createdAssetPaths.Concat(new[] { path }).ToList();
            AssetDatabase.CreateAsset(manifest, path);
            createdAssetPaths.Add(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (!EditorUtility.IsPersistent(AssetDatabase.LoadAssetAtPath<KawaiiMigrationManifest>(path)))
            {
                throw new InvalidOperationException("Failed to persist the Kawaii migration manifest: " + path);
            }

            report.Created(manifest, "MigrationManifest");
        }

        public void Rollback()
        {
            var failures = new List<string>();
            for (var index = createdAssetPaths.Count - 1; index >= 0; index--)
            {
                var path = createdAssetPaths[index];
                if (string.IsNullOrWhiteSpace(path) || !AssetExists(path))
                {
                    continue;
                }

                var deleted = AssetDatabase.DeleteAsset(path);
                if (!deleted || AssetExists(path))
                {
                    failures.Add($"asset '{path}' (DeleteAsset={deleted}, remains={AssetExists(path)})");
                }
            }

            createdAssetPaths.Clear();
            foreach (var generated in generatedObjects.Where(asset => asset != null && !EditorUtility.IsPersistent(asset)).Distinct())
            {
                Object.DestroyImmediate(generated);
            }

            if (manifest != null && !EditorUtility.IsPersistent(manifest))
            {
                Object.DestroyImmediate(manifest);
            }

            foreach (var folder in createdFolderPaths
                         .Where(path => !string.IsNullOrWhiteSpace(path))
                         .Distinct(StringComparer.Ordinal)
                         .OrderByDescending(path => path.Count(character => character == '/')))
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                var remainingChildren = AssetDatabase.FindAssets("", new[] { folder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => !string.IsNullOrEmpty(path) &&
                                   !string.Equals(path, folder, StringComparison.Ordinal))
                    .ToArray();
                if (remainingChildren.Length > 0)
                {
                    failures.Add($"folder '{folder}' is not empty");
                    continue;
                }

                var deleted = AssetDatabase.DeleteAsset(folder);
                if (!deleted || AssetDatabase.IsValidFolder(folder))
                {
                    failures.Add($"folder '{folder}' (DeleteAsset={deleted}, remains={AssetDatabase.IsValidFolder(folder)})");
                }
            }

            createdFolderPaths.Clear();
            AssetDatabase.SaveAssets();
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Kawaii migration rollback left generated paths: " + string.Join("; ", failures));
            }
        }

        private void EnsureTrackedFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parts = assetPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !string.Equals(parts[0], "Assets", StringComparison.Ordinal))
            {
                throw new ArgumentException("Kawaii migration asset path must be under Assets: " + assetPath);
            }

            var current = "Assets";
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    var guid = AssetDatabase.CreateFolder(current, parts[index]);
                    if (string.IsNullOrEmpty(guid) || !AssetDatabase.IsValidFolder(next))
                    {
                        throw new InvalidOperationException("Failed to create Kawaii migration folder: " + next);
                    }

                    createdFolderPaths.Add(next);
                }

                current = next;
            }
        }

        private static bool AssetExists(string assetPath)
        {
            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null;
        }

        private void AddMotionEntry(PoseClip pose, Motion motion, string path)
        {
            manifest.motions.Add(new KawaiiMigrationMotionManifestEntry
            {
                poseGlobalObjectId = PersistentIdOrThrow(pose),
                assetPath = path ?? "",
                motionType = motion != null ? motion.GetType().FullName : ""
            });
        }

        private static string PersistentIdOrThrow(Object target)
        {
            if (PoseTuneObjectIdentity.TryGetPersistentId(target, out var id))
            {
                return id;
            }

            throw new InvalidOperationException(
                "Kawaii migration requires saved Scene or Prefab objects with valid GlobalObjectIds.");
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                names.Push(current.name);
            }

            return string.Join("/", names);
        }

        private readonly struct ExpectedAsset
        {
            public readonly Type Type;
            public readonly string Name;

            public ExpectedAsset(Type type, string name)
            {
                Type = type;
                Name = name;
            }
        }
    }
}
