using UnityEngine;

namespace PerspectivePuzzle
{
    public static class ScreenUtils
    {
        public static bool IsFullscreen
        {
            get
            {
#if UNITY_EDITOR
                try
                {
                    var gvType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                    if (gvType != null)
                    {
                        var gv = UnityEditor.EditorWindow.GetWindow(gvType, false, null, false);
                        if (gv != null)
                            return gv.maximized;
                    }
                }
                catch { }
#endif
                return Screen.fullScreen;
            }
        }

        public static void ToggleFullscreen()
        {
#if UNITY_EDITOR
            try
            {
                var gvType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gvType != null)
                {
                    var gv = UnityEditor.EditorWindow.GetWindow(gvType, false, null, false);
                    if (gv != null)
                    {
                        gv.maximized = !gv.maximized;
                        return;
                    }
                }
            }
            catch { }
#endif
            Screen.fullScreen = !Screen.fullScreen;
        }
    }
}
