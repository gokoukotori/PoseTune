using System;
using UnityEditor;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    [FilePath("ProjectSettings/Packages/com.gokoukotori.posetune/settings.json", FilePathAttribute.Location.ProjectFolder)]
    public sealed class PoseTuneEditorProjectSettings : ScriptableSingleton<PoseTuneEditorProjectSettings>
    {
        [SerializeField] private bool enableSelectedPosePreview = true;

        public static event Action<bool> EnableSelectedPosePreviewChanged;

        public static bool EnableSelectedPosePreview
        {
            get => instance.enableSelectedPosePreview;
            set
            {
                if (instance.enableSelectedPosePreview == value)
                {
                    return;
                }

                instance.enableSelectedPosePreview = value;
                instance.Save(true);
                EnableSelectedPosePreviewChanged?.Invoke(value);
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            _ = instance;
        }
    }
}
