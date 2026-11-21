using System.Collections.Generic;
using System.Linq;
using Gokoukotori.PoseTune;
using Gokoukotori.PoseTune.Editor.Compiler.Validation;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseMotionPreparationContext
    {
        public GameObject AvatarRoot;
        public PoseTuneRoot RootComponent;
        public AnimatorBuildResult BuildResult;
        public bool SanitizeUnsupportedSourceCurves;

        public static PoseMotionPreparationContext Empty()
        {
            return new PoseMotionPreparationContext();
        }

        public static PoseMotionPreparationContext FromGraph(PoseGraph graph, AnimatorBuildResult result)
        {
            return new PoseMotionPreparationContext
            {
                AvatarRoot = graph != null ? graph.AvatarRoot : null,
                RootComponent = graph != null ? graph.RootComponent : null,
                BuildResult = result,
                SanitizeUnsupportedSourceCurves = true
            };
        }
    }

    internal sealed class PoseMotionPreparationResult
    {
        public Motion Motion;
        public List<Object> GeneratedAssets = new();
    }

    internal static class PoseMotionPreparationService
    {
        public static PoseMotionPreparationResult PrepareMotion(
            PoseDefinition pose,
            string name,
            PoseMotionPreparationContext context)
        {
            context ??= PoseMotionPreparationContext.Empty();
            var source = pose?.SourceMotion != null ? pose.SourceMotion : pose?.Clip;
            if (pose == null || source == null)
            {
                return new PoseMotionPreparationResult { Motion = null };
            }

            if (source is BlendTree tree)
            {
                var prepareLeafClips = PosePreparedClipBuilder.RequiresPreparedMotion(pose) ||
                                       context.SanitizeUnsupportedSourceCurves &&
                                       MotionTreeCloneUtility.EnumerateMotions(tree)
                                           .OfType<AnimationClip>()
                                           .Any(PoseTuneCurveBindingPolicy.HasUnsupportedCurves);
                var cloned = (BlendTree)MotionTreeCloneUtility.Clone(
                    tree,
                    name + "_Motion",
                    new List<Object>(),
                    (clip, cloneName) => prepareLeafClips
                        ? PosePreparedClipBuilder.ClonePreparedClip(
                            pose,
                            clip,
                            cloneName,
                            context.SanitizeUnsupportedSourceCurves)
                        : MotionTreeCloneUtility.CloneClip(clip, cloneName),
                    ChildMotionName);
                var assets = MotionTreeCloneUtility.EnumerateMotions(cloned).Cast<Object>().ToList();
                return new PoseMotionPreparationResult
                {
                    Motion = cloned,
                    GeneratedAssets = assets
                };
            }

            if (source is not AnimationClip clip)
            {
                return new PoseMotionPreparationResult { Motion = source };
            }

            if (!PosePreparedClipBuilder.RequiresPreparedMotion(pose) &&
                (!context.SanitizeUnsupportedSourceCurves ||
                 !PoseTuneCurveBindingPolicy.HasUnsupportedCurves(clip)))
            {
                return new PoseMotionPreparationResult { Motion = clip };
            }

            var generated = PosePreparedClipBuilder.ClonePreparedClip(
                pose,
                name,
                context.SanitizeUnsupportedSourceCurves);
            return new PoseMotionPreparationResult
            {
                Motion = generated,
                GeneratedAssets = { generated }
            };
        }

        private static string ChildMotionName(Motion motion, string parentName, int index)
        {
            return motion != null ? motion.name : "Motion";
        }
    }
}
