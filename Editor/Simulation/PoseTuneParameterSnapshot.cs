using System.Collections.Generic;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseTuneParameterSnapshot
    {
        private readonly Dictionary<string, float> floats = new();
        private readonly Dictionary<string, int> ints = new();
        private readonly Dictionary<string, bool> bools = new();

        public static PoseTuneParameterSnapshot Desktop()
        {
            var snapshot = new PoseTuneParameterSnapshot();
            snapshot.SetInt("VRMode", 0);
            snapshot.SetInt("TrackingType", 3);
            snapshot.SetFloat("Upright", 1f);
            snapshot.SetBool("Grounded", true);
            snapshot.SetBool("Seated", false);
            return snapshot;
        }

        public static PoseTuneParameterSnapshot ThreePointVr()
        {
            var snapshot = Desktop();
            snapshot.SetInt("VRMode", 1);
            snapshot.SetInt("TrackingType", 3);
            return snapshot;
        }

        public static PoseTuneParameterSnapshot FullBodyTracking()
        {
            var snapshot = ThreePointVr();
            snapshot.SetInt("TrackingType", 6);
            return snapshot;
        }

        public void SetFloat(string parameter, float value)
        {
            floats[parameter] = value;
        }

        public void SetInt(string parameter, int value)
        {
            ints[parameter] = value;
            floats[parameter] = value;
        }

        public void SetBool(string parameter, bool value)
        {
            bools[parameter] = value;
            floats[parameter] = value ? 1f : 0f;
            ints[parameter] = value ? 1 : 0;
        }

        public float Float(string parameter)
        {
            if (floats.TryGetValue(parameter, out var value))
            {
                return value;
            }

            if (ints.TryGetValue(parameter, out var intValue))
            {
                return intValue;
            }

            return bools.TryGetValue(parameter, out var boolValue) && boolValue ? 1f : 0f;
        }

        public int Int(string parameter)
        {
            if (ints.TryGetValue(parameter, out var value))
            {
                return value;
            }

            return UnityEngine.Mathf.RoundToInt(Float(parameter));
        }

        public bool Bool(string parameter)
        {
            if (bools.TryGetValue(parameter, out var value))
            {
                return value;
            }

            return Float(parameter) > 0.5f;
        }
    }
}
