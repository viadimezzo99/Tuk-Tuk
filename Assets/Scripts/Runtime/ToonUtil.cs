using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Applica il materiale toon con colore, texture e tiling tramite MaterialPropertyBlock.
    /// </summary>
    public static class ToonUtil
    {
        public static void Paint(Renderer rend, Material mat, Color color,
                                 Texture2D tex = null, Vector2 tiling = default)
        {
            if (tiling == default) tiling = Vector2.one;

            rend.sharedMaterial = mat;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetTexture("_MainTex", tex != null ? tex : ProceduralTextures.White);
            block.SetVector("_MainTex_ST", new Vector4(tiling.x, tiling.y, 0f, 0f));
            rend.SetPropertyBlock(block);
        }
    }
}
