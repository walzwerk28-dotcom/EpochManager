using UnityEngine;
using UnityEngine.Rendering;

namespace EpochCity
{
    /// <summary>Hilfen für URP-Materialien (Lit und Unlit), zur Laufzeit und im Editor nutzbar.</summary>
    public static class MaterialUtil
    {
        /// <summary>
        /// Stellt ein URP-Material auf transparent (Alpha-Blending) um und schaltet seinen Schattenwurf ab.
        /// Entspricht der Einstellung „Surface Type: Transparent“ im Inspector.
        /// </summary>
        public static void SetTransparent(Material m)
        {
            if (m == null) return;
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
        }

        public static void SetColor(Material m, Color color)
        {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            else if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        }
    }
}
