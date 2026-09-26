using System;
using Gokoukotori.PoseTune;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseTuneAssistantScopedInvalidation : IDisposable
    {
        private readonly Action _invalidate;
        private PoseTuneRoot _currentRoot;
        private bool _disposed;

        public PoseTuneAssistantScopedInvalidation(Action invalidate)
        {
            _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.hierarchyChanged += _invalidate;
            EditorApplication.projectChanged += _invalidate;
        }

        public void Track(PoseTuneRoot root)
        {
            _currentRoot = root;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.hierarchyChanged -= _invalidate;
            EditorApplication.projectChanged -= _invalidate;
            _currentRoot = null;
            _disposed = true;
        }

        private UndoPropertyModification[] OnPostprocessModifications(
            UndoPropertyModification[] modifications)
        {
            if (_currentRoot == null || modifications == null)
            {
                return modifications;
            }

            foreach (var modification in modifications)
            {
                var target = modification.currentValue?.target ?? modification.previousValue?.target;
                if (IsRelevantChange(target))
                {
                    _invalidate();
                    break;
                }
            }

            return modifications;
        }

        private void OnUndoRedo()
        {
            _invalidate();
        }

        private bool IsRelevantChange(UnityEngine.Object target)
        {
            if (target == null || _currentRoot == null)
            {
                return false;
            }

            if (EditorUtility.IsPersistent(target))
            {
                return true;
            }

            Transform targetTransform;
            if (target is Component component)
            {
                targetTransform = component.transform;
            }
            else if (target is GameObject gameObject)
            {
                targetTransform = gameObject.transform;
            }
            else
            {
                return false;
            }

            if (targetTransform == null)
            {
                return false;
            }

            var avatarDescriptor = _currentRoot.GetComponentInParent<VRCAvatarDescriptor>(true);
            var scope = avatarDescriptor != null ? avatarDescriptor.transform : _currentRoot.transform;
            return targetTransform == scope ||
                   targetTransform.IsChildOf(scope) ||
                   scope.IsChildOf(targetTransform);
        }
    }
}
