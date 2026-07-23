using System.Collections.Generic;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Gokoukotori.PoseTune.Editor
{
    internal static class ExpressionMenuParameterSearch
    {
        public static bool Contains(VRCExpressionsMenu root, string parameterName)
        {
            if (root == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return false;
            }

            var visited = new HashSet<VRCExpressionsMenu> { root };
            var pending = new Stack<VRCExpressionsMenu>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var menu = pending.Pop();
                if (menu == null || menu.controls == null)
                {
                    continue;
                }

                foreach (var control in menu.controls)
                {
                    if (control == null)
                    {
                        continue;
                    }

                    if (control.parameter != null && control.parameter.name == parameterName)
                    {
                        return true;
                    }

                    if (control.subParameters != null)
                    {
                        foreach (var parameter in control.subParameters)
                        {
                            if (parameter != null && parameter.name == parameterName)
                            {
                                return true;
                            }
                        }
                    }

                    if (control.subMenu != null && visited.Add(control.subMenu))
                    {
                        pending.Push(control.subMenu);
                    }
                }
            }

            return false;
        }
    }
}
