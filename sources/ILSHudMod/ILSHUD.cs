// ILS HUD Mod — transparent overlay approach guidance
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ILSHudMod
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ILSHUD : MonoBehaviour
    {
        private struct Runway { public double lat, lon, alt, hdg, gs; public string label; }
        private List<Runway> rwys;
        private int sel;
        private bool show;
        private Texture2D bgTex, shadowTex;

        void Start()
        {
            rwys = new List<Runway>
            {
                new Runway { label="KSC 09", lat=-0.0485981, lon=-74.7244856, alt=67, hdg=90,  gs=3 },
                new Runway { label="KSC 27", lat=-0.0485981, lon=-74.5024856, alt=67, hdg=270, gs=3 },
                new Runway { label="KSC 18", lat=-0.051,     lon=-74.6135,    alt=67, hdg=180, gs=3 },
                new Runway { label="KSC 36", lat=-0.046,     lon=-74.6135,    alt=67, hdg=0,   gs=3 },
                new Runway { label="Island 09", lat=-1.5177, lon=-71.9658,    alt=133, hdg=90, gs=3 },
                new Runway { label="Island 27", lat=-1.5177, lon=-71.8658,    alt=133, hdg=270, gs=3 },
                new Runway { label="Desert 09", lat=-6.5650, lon=-144.0400,   alt=820, hdg=90, gs=3 }
            };
            bgTex = MakeTex(1, 1, new Color(0, 0, 0, 0.35f));
            shadowTex = MakeTex(1, 1, new Color(0, 0, 0, 0.55f));
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Backslash)) show = !show;
        }

        Texture2D MakeTex(int w, int h, Color c)
        {
            var t = new Texture2D(w, h);
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) t.SetPixel(x, y, c);
            t.Apply();
            return t;
        }

        void OnGUI()
        {
            if (!show) return;
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || !v.loaded) return;

            float w = Screen.width;
            float h = Screen.height;
            Runway r = rwys[sel];

            // Vessel data
            Vector3 sv = v.GetSrfVelocity();
            double spd = sv.magnitude;
            double alt = v.altitude - r.alt;
            double vs = v.verticalSpeed;
            double vHdg = (v.vesselTransform.eulerAngles.y + 360) % 360;
            Vector3d rPos = v.mainBody.GetWorldSurfacePosition(r.lat, r.lon, r.alt);
            Vector3d vPos = v.GetWorldPos3D();
            double dist = Vector3d.Distance(vPos, rPos);

            double hdgErr = vHdg - r.hdg;
            if (hdgErr > 180) hdgErr -= 360;
            if (hdgErr < -180) hdgErr += 360;
            double gsTgt = Math.Tan(r.gs * Math.PI / 180.0) * dist + r.alt;
            double gsErr = v.altitude - gsTgt;

            // === HUD OVERLAY ===

            // Top bar — runway selector and key info
            DrawRect(new Rect(w * 0.3f, 10, w * 0.4f, 60), bgTex);
            int nw = GUI.SelectionGrid(new Rect(w * 0.3f + 10, 15, w * 0.4f - 20, 22), sel,
                rwys.Select(x => x.label).ToArray(), 7);
            if (nw != sel && nw >= 0) sel = nw;

            GUI.Label(new Rect(w * 0.3f + 10, 40, w * 0.4f - 20, 22),
                "HDG " + r.hdg.ToString("F0") + "°  GS " + r.gs.ToString("F1") + "°  ELEV " + r.alt.ToString("F0") + "m",
                new GUIStyle(GUI.skin.label) { fontSize = 11, normal = new GUIStyleState { textColor = Color.gray } });

            // Left data panel
            DrawRect(new Rect(15, 80, 180, 140), bgTex);
            GUIStyle dt = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = new GUIStyleState { textColor = Color.white } };
            GUIStyle dg = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = new GUIStyleState { textColor = Color.gray } };

            GUI.Label(new Rect(25, 85, 160, 20), "DIST  " + (dist / 1000).ToString("F1") + " km", dt);
            GUI.Label(new Rect(25, 105, 160, 20), "ALT   " + alt.ToString("F0") + " m AGL", dt);
            GUI.Label(new Rect(25, 125, 160, 20), "SPD   " + spd.ToString("F1") + " m/s", dt);
            GUI.Label(new Rect(25, 145, 160, 20), "VS    " + vs.ToString("F1") + " m/s", dt);
            GUI.Label(new Rect(25, 165, 160, 20), "HDG   " + vHdg.ToString("F0") + "°", dt);
            GUI.Label(new Rect(25, 185, 160, 15), "RWY: " + r.label, dg);

            // === CROSSHAIR — center screen ===
            float cx = w / 2f;
            float cy = h / 2f + 30;
            float sz = 70;
            float gap = 15;

            // Background box behind crosshair
            DrawRect(new Rect(cx - sz - gap, cy - sz - gap, (sz + gap) * 2, (sz + gap) * 2), shadowTex);

            // Crosshair circles and lines
            Color crossCol = new Color(0.3f, 0.8f, 1f, 0.7f);
            DrawCircle(cx, cy, sz, crossCol, 1);

            // Center dot
            DrawRect(new Rect(cx - 2, cy - 2, 4, 4), Color.green);

            // Localizer diamond (horizontal)
            float locOff = Mathf.Clamp((float)(hdgErr / 10.0), -1, 1) * sz;
            Color locCol = Math.Abs(hdgErr) < 2 ? Color.green : (Math.Abs(hdgErr) < 5 ? Color.yellow : Color.red);
            DrawDiamond(cx + locOff, cy, locCol, 8, 2);

            // Glideslope diamond (vertical)
            float gsOff = Mathf.Clamp((float)(gsErr / 100.0), -1, 1) * sz;
            Color gsCol = Math.Abs(gsErr) < 20 ? Color.green : (Math.Abs(gsErr) < 50 ? Color.yellow : Color.red);
            DrawDiamond(cx, cy + gsOff, gsCol, 8, 2);

            // Labels
            GUIStyle lab = new GUIStyle(GUI.skin.label) { fontSize = 10, normal = new GUIStyleState { textColor = Color.gray } };
            GUI.Label(new Rect(cx + sz + 8, cy - 8, 30, 15), "LOC", lab);
            GUI.Label(new Rect(cx - 15, cy - sz - 20, 30, 15), "GS", lab);

            // Error values bottom
            GUI.Label(new Rect(cx - sz, cy + sz + 8, sz, 15),
                hdgErr.ToString("F1") + "°", new GUIStyle(GUI.skin.label) { fontSize = 10, normal = new GUIStyleState { textColor = locCol } });
            GUI.Label(new Rect(cx + 5, cy + sz + 8, sz, 15),
                gsErr.ToString("F0") + "m", new GUIStyle(GUI.skin.label) { fontSize = 10, normal = new GUIStyleState { textColor = gsCol } });

            // Compass-like heading strip at bottom
            DrawRect(new Rect(w * 0.25f, h - 55, w * 0.5f, 40), bgTex);
            float tapeW = w * 0.45f;
            float tapeCX = w / 2f;
            GUIStyle hdgStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = Color.white } };
            GUIStyle hdgSmall = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = Color.gray } };

            for (int deg = -60; deg <= 60; deg += 5)
            {
                float px = tapeCX + (deg / 60f) * tapeW;
                if (px < w * 0.25f || px > w * 0.75f) continue;
                double a = (vHdg + deg + 360) % 360;
                if (deg % 10 == 0)
                {
                    DrawLine(new Vector2(px, h - 50), new Vector2(px, h - 44), Color.white, 1);
                    string lbl = a.ToString("F0");
                    GUI.Label(new Rect(px - 15, h - 40, 30, 15), lbl, (deg == 0) ? hdgStyle : hdgSmall);
                }
                else
                {
                    DrawLine(new Vector2(px, h - 50), new Vector2(px, h - 47), Color.gray, 1);
                }
            }
            // Heading bug at runway heading
            float bugPx = tapeCX + (float)((r.hdg - vHdg + 360) % 360);
            if (bugPx > w * 0.25f && bugPx < w * 0.75f)
            {
                // Clamp to visible range
                float dAng = (float)((r.hdg - vHdg + 540) % 360 - 180);
                bugPx = tapeCX + (dAng / 60f) * tapeW;
                if (Math.Abs(dAng) < 60)
                    DrawDiamond(bugPx, h - 45, Color.yellow, 5, 2);
            }

            // Bottom-right — toggle hint
            GUI.Label(new Rect(w - 130, h - 22, 120, 18), "HUD: \\ toggles",
                new GUIStyle(GUI.skin.label) { fontSize = 10, normal = new GUIStyleState { textColor = new Color(0.5f, 0.5f, 0.5f) } });
        }

        void DrawRect(Rect r, Texture2D t) { GUI.DrawTexture(r, t); }
        void DrawRect(Rect r, Color c) { var t = MakeTex(1, 1, c); GUI.DrawTexture(r, t); Destroy(t, 0.1f); }

        void DrawLine(Vector2 a, Vector2 b, Color c, float w)
        {
            Color s = GUI.color;
            GUI.color = c;
            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            float len = Vector2.Distance(a, b);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, a);
            GUI.DrawTexture(new Rect(a.x, a.y - w / 2, len, w), shadowTex);
            GUI.DrawTexture(new Rect(a.x, a.y - w / 2, len, w), MakeTex(1, 1, c));
            GUI.matrix = m;
            GUI.color = s;
        }

        void DrawCircle(float cx, float cy, float r, Color c, float w)
        {
            int segs = 32;
            for (int i = 0; i < segs; i++)
            {
                float a1 = i * 2 * Mathf.PI / segs;
                float a2 = (i + 1) * 2 * Mathf.PI / segs;
                DrawLine(new Vector2(cx + Mathf.Cos(a1) * r, cy + Mathf.Sin(a1) * r),
                         new Vector2(cx + Mathf.Cos(a2) * r, cy + Mathf.Sin(a2) * r), c, w);
            }
        }

        void DrawDiamond(float cx, float cy, Color c, float s, float w)
        {
            DrawLine(new Vector2(cx, cy - s), new Vector2(cx + s, cy), c, w);
            DrawLine(new Vector2(cx + s, cy), new Vector2(cx, cy + s), c, w);
            DrawLine(new Vector2(cx, cy + s), new Vector2(cx - s, cy), c, w);
            DrawLine(new Vector2(cx - s, cy), new Vector2(cx, cy - s), c, w);
        }
    }
}
