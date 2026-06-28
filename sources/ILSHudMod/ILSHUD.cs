// ILS HUD Mod for Kerbal Space Program
// Heads-up display with localizer & glideslope indicators for runway approaches

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ILSHudMod
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ILSHUD : MonoBehaviour
    {
        private static Dictionary<string, double[]> runways = new Dictionary<string, double[]>
        {
            { "KSC 09",   new double[] { -0.0485981, -74.7244856, 67, 90, 3.0 } },
            { "KSC 27",   new double[] { -0.0485981, -74.5024856, 67, 270, 3.0 } },
            { "KSC 18",   new double[] { -0.051,     -74.6135,    67, 180, 3.0 } },
            { "KSC 36",   new double[] { -0.046,     -74.6135,    67, 0, 3.0 } },
            { "Island 09", new double[] { -1.5177,   -71.9658,    133, 90, 3.0 } },
            { "Island 27", new double[] { -1.5177,   -71.8658,    133, 270, 3.0 } },
            { "Desert 09", new double[] { -6.5650,   -144.0400,   820, 90, 3.0 } }
        };

        private bool hudVisible;
        private string selectedRunway;
        private double rwyLat, rwyLon, rwyAlt, rwyHdg, glideSlope;
        private Rect hudRect;
        private string[] rwyNames;
        private int selectedIdx;
        private GUIStyle labelStyle, titleStyle, smallStyle;
        private bool stylesInit;

        void Start()
        {
            rwyNames = runways.Keys.ToArray();
            selectedIdx = 0;
            SelectRunway(0);
            hudRect = new Rect(Screen.width - 290, 60, 270, 380);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                hudVisible = !hudVisible;
            }
        }

        void SelectRunway(int idx)
        {
            if (idx < 0 || idx >= rwyNames.Length) return;
            selectedIdx = idx;
            selectedRunway = rwyNames[idx];
            double[] d = runways[selectedRunway];
            rwyLat = d[0]; rwyLon = d[1]; rwyAlt = d[2]; rwyHdg = d[3]; glideSlope = d[4];
        }

        void OnGUI()
        {
            if (!hudVisible) return;
            if (!stylesInit) InitStyles();

            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || !v.loaded) return;

            hudRect = GUI.Window(4281, hudRect, DrawWindow, "ILS HUD");
        }

        void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, hudRect.width, 22));
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null) return;

            float x = 10;
            float y = 28;
            float w = hudRect.width - 20;

            GUI.Label(new Rect(x, y, 60, 20), "Runway:", labelStyle);
            int newIdx = GUI.SelectionGrid(new Rect(x + 65, y, w - 65, 20), selectedIdx, rwyNames, 1);
            if (newIdx >= 0 && newIdx != selectedIdx) SelectRunway(newIdx);
            y += 25;

            Vector3 srfVel = v.GetSrfVelocity();
            double groundSpd = srfVel.magnitude;
            double altMSL = v.altitude;
            double altAGL = altMSL - rwyAlt;
            double vs = v.verticalSpeed;

            CelestialBody body = v.mainBody;
            Vector3d rwyPos = body.GetWorldSurfacePosition(rwyLat, rwyLon, rwyAlt);
            Vector3d vPos = v.GetWorldPos3D();
            double dist = Vector3d.Distance(vPos, rwyPos);

            double vHeading = (v.vesselTransform.eulerAngles.y + 360) % 360;
            double hdgErr = vHeading - rwyHdg;
            if (hdgErr > 180) hdgErr -= 360;
            if (hdgErr < -180) hdgErr += 360;

            double gsTargetAlt = Math.Tan(glideSlope * Math.PI / 180.0) * dist + rwyAlt;
            double gsErr = altMSL - gsTargetAlt;

            GUI.Label(new Rect(x, y, w, 20),
                "Dist: " + (dist / 1000).ToString("F1") + " km  Spd: " + groundSpd.ToString("F1") + " m/s", labelStyle);
            y += 20;
            GUI.Label(new Rect(x, y, w, 20),
                "Alt AGL: " + altAGL.ToString("F0") + "m  VS: " + vs.ToString("F1") + " m/s", labelStyle);
            y += 20;
            GUI.Label(new Rect(x, y, w, 20),
                "HDG: " + vHeading.ToString("F0") + "°  RWY: " + rwyHdg.ToString("F0") + "°", labelStyle);
            y += 22;

            float cx = w / 2 + x;
            float crossY = y + 5;
            float crossSize = 55;

            GUI.Box(new Rect(x, crossY, w, crossSize * 2 + 40), "");

            DrawLine(new Vector2(cx - crossSize, crossY + crossSize), new Vector2(cx + crossSize, crossY + crossSize), Color.white, 1);
            DrawLine(new Vector2(cx, crossY + 10), new Vector2(cx, crossY + crossSize * 2 - 10), Color.white, 1);

            float locX = cx + Mathf.Clamp((float)(hdgErr / 15.0), -1, 1) * crossSize;
            DrawDiamond(locX, crossY + crossSize, Color.yellow, 6);

            float gsY = crossY + crossSize - Mathf.Clamp((float)(gsErr / 150.0), -1, 1) * crossSize;
            DrawDiamond(cx, gsY, Color.yellow, 6);

            GUI.Label(new Rect(cx - 15, crossY + 5, 40, 15), "GS", smallStyle);
            GUI.Label(new Rect(cx + crossSize + 5, crossY + crossSize - 8, 40, 15), "LOC", smallStyle);

            float locY = crossY + crossSize * 2 + 10;
            Color locCol = Math.Abs(hdgErr) < 2 ? Color.green : (Math.Abs(hdgErr) < 5 ? Color.yellow : Color.red);
            Color gsCol = Math.Abs(gsErr) < 30 ? Color.green : (Math.Abs(gsErr) < 80 ? Color.yellow : Color.red);

            GUI.Label(new Rect(x + 10, locY, 120, 20), "LOC: " + hdgErr.ToString("F1") + "°", labelStyle);
            GUI.Label(new Rect(x + 10, locY + 15, 120, 20), "GS: " + gsErr.ToString("F0") + "m", labelStyle);

            GUI.backgroundColor = locCol;
            GUI.Box(new Rect(x + 120, locY + 2, 8, 14), "");
            GUI.backgroundColor = gsCol;
            GUI.Box(new Rect(x + 120, locY + 17, 8, 14), "");
            GUI.backgroundColor = Color.white;

            GUI.Label(new Rect(x, locY + 35, w, 15), "Press ] to toggle", smallStyle);
        }

        void DrawLine(Vector2 a, Vector2 b, Color c, float w)
        {
            Color saved = GUI.color;
            GUI.color = c;
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            float len = Vector2.Distance(a, b);
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - w / 2, len, w), GetWhiteTex());
            GUI.matrix = m;
            GUI.color = saved;
        }

        void DrawDiamond(float cx, float cy, Color c, float size)
        {
            Color saved = GUI.color;
            GUI.color = c;
            DrawLine(new Vector2(cx - size, cy), new Vector2(cx, cy - size), c, 2);
            DrawLine(new Vector2(cx, cy - size), new Vector2(cx + size, cy), c, 2);
            DrawLine(new Vector2(cx + size, cy), new Vector2(cx, cy + size), c, 2);
            DrawLine(new Vector2(cx, cy + size), new Vector2(cx - size, cy), c, 2);
            GUI.color = saved;
        }

        Texture2D _whiteTex;
        Texture2D GetWhiteTex()
        {
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(1, 1);
                _whiteTex.SetPixel(0, 0, Color.white);
                _whiteTex.Apply();
            }
            return _whiteTex;
        }

        void InitStyles()
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 12;
            labelStyle.normal.textColor = Color.white;

            titleStyle = new GUIStyle(GUI.skin.box);
            titleStyle.fontSize = 14;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.cyan;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.fontSize = 10;
            smallStyle.normal.textColor = Color.gray;

            stylesInit = true;
        }
    }
}
