using System.Collections.Generic;
using UnityEngine;

namespace Gokoukotori.PoseTune.Editor
{
    public sealed class MenuPlan
    {
        public MenuControlPlan Root;
    }

    public sealed class MenuControlPlan
    {
        public string Label = "";
        public PoseTuneMenuControlType Type;
        public string Parameter = "";
        public float Value;
        public Texture2D Icon;
        public List<string> SubParameters = new();
        public List<MenuControlPlan> Children = new();
    }

    public enum PoseTuneMenuControlType
    {
        Button,
        Toggle,
        SubMenu,
        RadialPuppet
    }
}
