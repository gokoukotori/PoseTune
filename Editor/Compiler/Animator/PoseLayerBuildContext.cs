using System.Collections.Generic;
using Gokoukotori.PoseTune;
using UnityEditor.Animations;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    /// <summary>Everything shared by the poses of one action pose layer while it is built.</summary>
    internal sealed class PoseLayerBuildContext
    {
        public AnimatorBuildResult Result { get; set; }
        public AnimatorControllerLayer Layer { get; set; }
        public PoseGraph Graph { get; set; }
        public PoseGroupDefinition Group { get; set; }
        public HashSet<string> DuplicateStateBaseNames { get; set; }
        /// <summary>Parameters an exclusive group's commit state resets.</summary>
        public List<string> ExclusiveResetTargets { get; set; }
        /// <summary>Per-pose active parameters guarding manual re-entry; empty when no commit guard is needed.</summary>
        public List<string> PoseActiveParameters { get; set; }
        /// <summary>The layer's group active parameter.</summary>
        public string ActiveParameter { get; set; }
        public AnimationClip HandoffHold { get; set; }
        public PoseSelectionPlan PoseSelection { get; set; }

        public PoseTuneRoot Root => Graph.RootComponent;

        /// <summary>With shared exclusive pose IDs the commit states' exclusive reset drivers are local only.</summary>
        public bool ResetLocalOnly =>
            Graph.RootComponent.poseSelectionSyncMode == PoseSelectionSyncMode.SharedExclusivePoseId;
    }
}
