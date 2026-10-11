using System.Collections.Generic;
using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>Utilità condivise dai spawner: primitive colorate, corsie, pulizia degli oggetti dietro.</summary>
    public static class Prims
    {
        /// <summary>Crea una primitiva senza collider, colorata con lo shader toon.</summary>
        public static GameObject Create(Material toon, Transform parent, PrimitiveType type, string name,
                                        Vector3 localPos, Vector3 scale, Color color,
                                        Texture2D tex = null, Vector2 tiling = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            ToonUtil.Paint(go.GetComponent<Renderer>(), toon, color, tex, tiling);
            Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Coordinata X del centro di una corsia (0 = centro strada).</summary>
        public static float LaneX(int lane)
        {
            return (lane - (TukTukController.LaneCount - 1) * 0.5f) * CityLayout.LaneWidth;
        }

        /// <summary>Distrugge e rimuove dalla lista gli oggetti che il tuk-tuk si è lasciato alle spalle.</summary>
        public static void RemoveBehind<T>(List<T> list, float playerZ) where T : Component
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null || list[i].transform.position.z < playerZ - 20f)
                {
                    if (list[i] != null) Object.Destroy(list[i].gameObject);
                    list.RemoveAt(i);
                }
            }
        }
    }
}
