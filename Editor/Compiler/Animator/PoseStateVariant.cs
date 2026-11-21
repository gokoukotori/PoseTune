using System.Collections.Generic;
using UnityEditor.Animations;

namespace Gokoukotori.PoseTune.Editor
{
    internal enum PoseStateVariantKind
    {
        Base,
        Desktop,
        Vr,
        FullBody
    }

    /// <summary>
    /// One state variant of a pose: its static facts, planned by <see cref="PoseStateVariantRules.Plan"/>, and
    /// the tracking data and states compiled for it.
    /// </summary>
    internal sealed class PoseStateVariant
    {
        public PoseStateVariantKind Kind { get; set; }
        /// <summary>State name suffix, see <see cref="PoseStateNaming"/>.</summary>
        public string Suffix { get; set; }
        /// <summary>Whether the variant's states apply the pose's pose space settings.</summary>
        public bool AppliesPoseSpace { get; set; }
        public bool IsFullBody => Kind == PoseStateVariantKind.FullBody;
        /// <summary>X offset of the pose state from the pose's column.</summary>
        public int StateX { get; set; }
        /// <summary>X offset of the handoff state from the pose's column.</summary>
        public int HandoffX { get; set; }
        /// <summary>X offset of the exclusive commit state from the pose's column.</summary>
        public int CommitX { get; set; }

        public TrackingPolicyData Policy { get; set; }
        public int VoteId { get; set; }
        public AnimatorState State { get; set; }
        public AnimatorState Handoff { get; set; }
        /// <summary>Exclusive commit state, created lazily by the first manual entry that needs it.</summary>
        public AnimatorState Commit { get; set; }
    }

    internal sealed class PoseStateVariants
    {
        public PoseStateVariants(
            IReadOnlyList<PoseStateVariant> all,
            bool needsPoseSpaceVrVariant,
            bool needsDesktopVariant)
        {
            All = all;
            NeedsPoseSpaceVrVariant = needsPoseSpaceVrVariant;
            NeedsDesktopVariant = needsDesktopVariant;
        }

        /// <summary>The pose's variants in creation order: Base, Desktop, VR, FBT (only those that exist).</summary>
        public IReadOnlyList<PoseStateVariant> All { get; }
        public bool NeedsPoseSpaceVrVariant { get; }
        public bool NeedsDesktopVariant { get; }

        /// <summary>The variant of the given kind, or null when the pose has none.</summary>
        public PoseStateVariant Get(PoseStateVariantKind kind)
        {
            foreach (var variant in All)
            {
                if (variant.Kind == kind)
                {
                    return variant;
                }
            }

            return null;
        }
    }
}
