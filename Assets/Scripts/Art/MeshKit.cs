using System.Collections.Generic;
using UnityEngine;

namespace TukTuk.Art
{
    /// <summary>
    /// Generatore di mesh lisce: scatole con spigoli arrotondati e solidi di rotazione
    /// (cupole, torri, corpi, ruote). Le normali sono analitiche, quindi niente spigoli vivi.
    /// Usato dall'editor per creare i modelli, e a runtime quando serve.
    /// </summary>
    public class MeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<int> Triangles = new List<int>();
        /// <summary>Centro di riferimento per orientare i triangoli (forme convesse).</summary>
        public Vector3 Center;

        public int Add(Vector3 p, Vector3 n, Vector2 uv)
        {
            Vertices.Add(p);
            Normals.Add(n.normalized);
            Uvs.Add(uv);
            return Vertices.Count - 1;
        }

        /// <summary>Aggiunge un triangolo orientato verso l'esterno.</summary>
        public void Tri(int a, int b, int c)
        {
            Vector3 pa = Vertices[a], pb = Vertices[b], pc = Vertices[c];
            Vector3 centroid = (pa + pb + pc) / 3f;
            Vector3 cross = Vector3.Cross(pb - pa, pc - pa);
            if (Vector3.Dot(cross, centroid - Center) < 0f)
            {
                Triangles.Add(a); Triangles.Add(c); Triangles.Add(b);
            }
            else
            {
                Triangles.Add(a); Triangles.Add(b); Triangles.Add(c);
            }
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (Vertices.Count > 60000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(Vertices);
            m.SetNormals(Normals);
            m.SetUVs(0, Uvs);
            m.SetTriangles(Triangles, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Unisce un'altra mesh in questa, con trasformazione.</summary>
        public void Append(MeshBuilder other, Matrix4x4 m)
        {
            int offset = Vertices.Count;
            for (int i = 0; i < other.Vertices.Count; i++)
            {
                Vertices.Add(m.MultiplyPoint3x4(other.Vertices[i]));
                Normals.Add(m.MultiplyVector(other.Normals[i]).normalized);
                Uvs.Add(other.Uvs[i]);
            }
            for (int i = 0; i < other.Triangles.Count; i++)
                Triangles.Add(other.Triangles[i] + offset);
        }
    }

    public static class MeshKit
    {
        /// <summary>Scatola con spigoli arrotondati (raggio r), dimensioni size, centrata nell'origine.</summary>
        public static MeshBuilder RoundedBox(Vector3 size, float radius, int seg = 10, float uvScale = 0.25f)
        {
            var b = new MeshBuilder { Center = Vector3.zero };
            Vector3 h = size * 0.5f;
            float r = Mathf.Min(radius, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.99f);
            Vector3 inner = h - Vector3.one * r;

            Vector3[] faceNormal =
            {
                Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back,
            };

            for (int f = 0; f < 6; f++)
            {
                int start = b.Vertices.Count;
                for (int i = 0; i <= seg; i++)
                {
                    for (int j = 0; j <= seg; j++)
                    {
                        float s = -1f + 2f * i / seg;
                        float t = -1f + 2f * j / seg;
                        Vector3 p = FacePoint(f, s, t, h);
                        Vector3 q = new Vector3(
                            Mathf.Clamp(p.x, -inner.x, inner.x),
                            Mathf.Clamp(p.y, -inner.y, inner.y),
                            Mathf.Clamp(p.z, -inner.z, inner.z));
                        Vector3 d = p - q;
                        Vector3 n;
                        Vector3 pos;
                        if (d.sqrMagnitude > 1e-10f)
                        {
                            n = d.normalized;
                            pos = q + n * r;
                        }
                        else
                        {
                            n = faceNormal[f];
                            pos = p;
                        }
                        Vector2 uv = new Vector2(pos.x + pos.y + pos.z, pos.y - pos.x + pos.z) * uvScale;
                        b.Add(pos, n, uv);
                    }
                }
                int row = seg + 1;
                for (int i = 0; i < seg; i++)
                {
                    for (int j = 0; j < seg; j++)
                    {
                        int a = start + i * row + j;
                        int c = start + (i + 1) * row + j;
                        int d2 = start + i * row + j + 1;
                        int e = start + (i + 1) * row + j + 1;
                        b.Tri(a, c, d2);
                        b.Tri(d2, c, e);
                    }
                }
            }
            return b;
        }

        private static Vector3 FacePoint(int f, float s, float t, Vector3 h)
        {
            switch (f)
            {
                case 0: return new Vector3(h.x, s * h.y, t * h.z);
                case 1: return new Vector3(-h.x, s * h.y, t * h.z);
                case 2: return new Vector3(s * h.x, h.y, t * h.z);
                case 3: return new Vector3(s * h.x, -h.y, t * h.z);
                case 4: return new Vector3(s * h.x, t * h.y, h.z);
                default: return new Vector3(s * h.x, t * h.y, -h.z);
            }
        }

        /// <summary>
        /// Solido di rotazione attorno all'asse Y. Il profilo è una lista di (raggio, altezza) dal basso in alto.
        /// Le normali seguono la tangente del profilo, quindi le curve risultano lisce.
        /// </summary>
        public static MeshBuilder Lathe(IList<Vector2> profile, int seg = 24, float uvScale = 0.25f)
        {
            var b = new MeshBuilder();
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in profile)
            {
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }
            b.Center = new Vector3(0f, (minY + maxY) * 0.5f, 0f);

            int rings = profile.Count;
            for (int k = 0; k < rings; k++)
            {
                Vector2 prev = profile[Mathf.Max(0, k - 1)];
                Vector2 next = profile[Mathf.Min(rings - 1, k + 1)];
                Vector2 tan = next - prev;
                // Normale 2D verso l'esterno (profilo dal basso verso l'alto).
                Vector2 n2 = new Vector2(tan.y, -tan.x);
                if (n2.sqrMagnitude < 1e-10f) n2 = Vector2.right;
                n2.Normalize();

                for (int i = 0; i <= seg; i++)
                {
                    float a = 2f * Mathf.PI * i / seg;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    Vector3 pos = new Vector3(profile[k].x * ca, profile[k].y, profile[k].x * sa);
                    Vector3 n = new Vector3(n2.x * ca, n2.y, n2.x * sa);
                    Vector2 uv = new Vector2((float)i / seg * 2f * Mathf.PI * profile[k].x, profile[k].y) * uvScale;
                    b.Add(pos, n, uv);
                }
            }

            int row = seg + 1;
            for (int k = 0; k < rings - 1; k++)
            {
                for (int i = 0; i < seg; i++)
                {
                    int a = k * row + i;
                    int c = (k + 1) * row + i;
                    int d = k * row + i + 1;
                    int e = (k + 1) * row + i + 1;
                    b.Tri(a, c, d);
                    b.Tri(d, c, e);
                }
            }
            return b;
        }

        /// <summary>Profilo di un ellissoide (mezzo per la cupola, intero per il bulbo): raggi rx, ry.</summary>
        public static List<Vector2> EllipsoidProfile(float rx, float ry, int steps = 16, bool halfTop = false)
        {
            var list = new List<Vector2>();
            int to = halfTop ? steps / 2 : steps;
            for (int i = 0; i <= to; i++)
            {
                float th = Mathf.PI * i / steps; // 0 = polo superiore
                float y = ry * Mathf.Cos(th);
                float x = rx * Mathf.Sin(th);
                list.Add(new Vector2(x, y));
            }
            // Dal basso verso l'alto.
            list.Reverse();
            return list;
        }

        /// <summary>Cilindro con spigoli smussati (torre, minareto, colonna).</summary>
        public static List<Vector2> RoundedCylinderProfile(float radius, float height, float round = 0.2f, int steps = 4)
        {
            var list = new List<Vector2>();
            float r = Mathf.Min(round, radius * 0.9f);
            list.Add(new Vector2(0f, 0f));
            list.Add(new Vector2(radius - r, 0f));
            for (int i = 0; i <= steps; i++)
            {
                float a = -Mathf.PI * 0.5f + Mathf.PI * 0.5f * i / steps;
                list.Add(new Vector2(radius - r + Mathf.Cos(a) * r, r + Mathf.Sin(a) * r));
            }
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.PI * 0.5f * i / steps;
                list.Add(new Vector2(radius - r + Mathf.Cos(a) * r, height - r + Mathf.Sin(a) * r));
            }
            list.Add(new Vector2(0f, height));
            return list;
        }
    }
}
