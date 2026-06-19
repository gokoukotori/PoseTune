using System;
using UnityEngine;

namespace Gokoukotori.PoseTune
{
    [Serializable]
    public sealed class StableComponentGuid
    {
        [SerializeField] private string value = "";

        public string Value
        {
            get
            {
                Ensure();
                return value;
            }
            set => this.value = value;
        }

        public void Regenerate()
        {
            value = Guid.NewGuid().ToString("N");
        }

        public void Ensure()
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Regenerate();
            }
        }
    }
}
