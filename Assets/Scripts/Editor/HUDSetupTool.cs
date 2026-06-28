using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using LivingAbyss.UI;

namespace LivingAbyss.Editor
{
    // Menu: Living Abyss > Setup HUD
    // Creates the Symbiosis HUD Canvas hierarchy and wires all references.
    public static class HUDSetupTool
    {
        [MenuItem("Living Abyss/Setup HUD")]
        public static void Setup()
        {
            var canvasGO = GameObject.Find("HUD_Canvas") ?? CreateCanvas();
            var panel    = BuildSymbiosisPanel(canvasGO.transform);
            Selection.activeGameObject = panel;
            Debug.Log("[HUDSetupTool] Done — press Play to see the HUD.");
        }

        // ── Canvas ────────────────────────────────────────────────────────────

        private static GameObject CreateCanvas()
        {
            var go     = new GameObject("HUD_Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            // Scale so 1 canvas unit = 1 game pixel (320×180 → 1280×720 at 4×).
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(320f, 180f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.Expand;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        // ── Symbiosis panel ───────────────────────────────────────────────────

        private static GameObject BuildSymbiosisPanel(Transform parent)
        {
            // Remove stale panel if the tool is run twice.
            var old = parent.Find("Symbiosis_Panel");
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);

            // Panel root: bottom-left anchor, center-bottom pivot so breathing
            // expands the shape upward (not into the screen corner).
            var panelGO = new GameObject("Symbiosis_Panel");
            panelGO.transform.SetParent(parent, false);

            var rt = panelGO.AddComponent<RectTransform>();
            rt.anchorMin         = Vector2.zero;
            rt.anchorMax         = Vector2.zero;
            rt.pivot             = new Vector2(0.5f, 0f);
            rt.anchoredPosition  = new Vector2(9f, 6f);  // 4px from left, 6px from bottom
            rt.sizeDelta         = new Vector2(10f, 30f); // 10×30 game pixels

            panelGO.AddComponent<CanvasGroup>();
            var hud = panelGO.AddComponent<SymbiosisHUD>();

            // Background image — outline oval, dark underlay
            var bgImg = AddImage(panelGO, "Symb_Bg");
            bgImg.color            = new Color(0.07f, 0.04f, 0.11f, 0.88f);
            bgImg.raycastTarget    = false;

            // Fill image — solid oval, zone color, fills from bottom
            var fillImg = AddImage(panelGO, "Symb_Fill");
            fillImg.type           = Image.Type.Filled;
            fillImg.fillMethod     = Image.FillMethod.Vertical;
            fillImg.fillOrigin     = (int)Image.OriginVertical.Bottom;
            fillImg.fillAmount     = 0.5f;
            fillImg.color          = new Color(0.243f, 0.620f, 0.420f); // MID zone
            fillImg.raycastTarget  = false;

            // Wire _bg and _fill into the HUD component via SerializedObject.
            var so = new SerializedObject(hud);
            so.FindProperty("_bg").objectReferenceValue   = bgImg;
            so.FindProperty("_fill").objectReferenceValue = fillImg;
            so.ApplyModifiedPropertiesWithoutUndo();

            return panelGO;
        }

        // Creates an Image child stretched to fill its parent.
        private static Image AddImage(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go.AddComponent<Image>();
        }
    }
}
