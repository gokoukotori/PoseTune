using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    [Serializable]
    internal sealed class KawaiiMigrationMotionManifestEntry
    {
        public string poseGlobalObjectId = "";
        public string assetPath = "";
        public string motionType = "";
    }

    [Serializable]
    internal sealed class KawaiiMigrationSourceManifestEntry
    {
        public string globalObjectId = "";
        public string hierarchyPath = "";
        public string previousTag = "";
        public bool previousActive;
        public string disposition = "";
    }

    [Serializable]
    internal sealed class KawaiiMigrationOptionsSnapshot
    {
        public bool createNewPoseTuneRoot;
        public string existingRootGlobalObjectId = "";
        public bool preserveSourceParameterNames;
        public bool preserveExplicitMenuValues;
        public bool preserveInitialPose;
        public bool preserveCustomIcons;
        public bool preserveDisabledPosesAsDisabled;
        public string footHeightMode = "";
        public string blendTreeMode = "";
        public string rootRecenterMode = "";
        public string rotationMode = "";
        public string adjustmentMode = "";
        public string motionTimeMode = "";
        public string overrideImportMode = "";
        public string poseSpaceMode = "";
        public string targetLayerMode = "";
        public string selectionSyncMode = "";
        public bool addTrackingPolicy;
        public bool enableAutoContextSwitch;
        public bool disableWhenFullBodyTracking;
        public string sourceDisposition = "";
        public bool confirmSharedSourceObjectMutation;
        public bool dryRunOnly;

        public static KawaiiMigrationOptionsSnapshot Capture(KawaiiMigrationOptions options)
        {
            options ??= KawaiiMigrationOptions.Default();
            return new KawaiiMigrationOptionsSnapshot
            {
                createNewPoseTuneRoot = options.createNewPoseTuneRoot,
                existingRootGlobalObjectId = options.existingRoot != null
                    ? GlobalObjectId.GetGlobalObjectIdSlow(options.existingRoot).ToString()
                    : "",
                preserveSourceParameterNames = options.preserveSourceParameterNames,
                preserveExplicitMenuValues = options.preserveExplicitMenuValues,
                preserveInitialPose = options.preserveInitialPose,
                preserveCustomIcons = options.preserveCustomIcons,
                preserveDisabledPosesAsDisabled = options.preserveDisabledPosesAsDisabled,
                footHeightMode = options.footHeightMode.ToString(),
                blendTreeMode = options.blendTreeMode.ToString(),
                rootRecenterMode = options.rootRecenterMode.ToString(),
                rotationMode = options.rotationMode.ToString(),
                adjustmentMode = options.adjustmentMode.ToString(),
                motionTimeMode = options.motionTimeMode.ToString(),
                overrideImportMode = options.overrideImportMode.ToString(),
                poseSpaceMode = options.poseSpaceMode.ToString(),
                targetLayerMode = options.targetLayerMode.ToString(),
                selectionSyncMode = options.selectionSyncMode.ToString(),
                addTrackingPolicy = options.addTrackingPolicy,
                enableAutoContextSwitch = options.enableAutoContextSwitch,
                disableWhenFullBodyTracking = options.disableWhenFullBodyTracking,
                sourceDisposition = options.sourceDisposition.ToString(),
                confirmSharedSourceObjectMutation = options.confirmSharedSourceObjectMutation,
                dryRunOnly = options.dryRunOnly
            };
        }
    }

    internal sealed class KawaiiMigrationManifest : ScriptableObject
    {
        public int schemaVersion = 2;
        public string runGuid = "";
        public string avatarGlobalObjectId = "";
        public string rootGlobalObjectId = "";
        public string optionsSummary = "";
        public KawaiiMigrationOptionsSnapshot options = new();
        public string manifestAssetPath = "";
        public List<string> createdAssetPaths = new();
        public List<KawaiiMigrationMotionManifestEntry> motions = new();
        public List<KawaiiMigrationSourceManifestEntry> sources = new();
    }
}
