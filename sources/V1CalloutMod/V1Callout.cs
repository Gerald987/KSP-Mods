// V1 Takeoff Callout Mod for Kerbal Space Program
// Provides audible + visual V1, VR, V2 callouts during takeoff

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace V1CalloutMod
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class V1Callout : MonoBehaviour
    {
        private static Dictionary<string, double[]> categories = new Dictionary<string, double[]>
        {
            { "ultralight", new double[] { 0, 2, 35, 40, 45 } },
            { "light",      new double[] { 2, 10, 50, 55, 60 } },
            { "medium",     new double[] { 10, 30, 65, 70, 75 } },
            { "heavy",      new double[] { 30, 100, 85, 90, 95 } },
            { "superheavy", new double[] { 100, 999, 100, 105, 110 } }
        };

        private double v1, vr, v2;
        private bool v1Called, vrCalled, v2Called;
        private bool calloutsActive;
        private double startAlt;
        private AudioSource audioSource;
        private bool initialized;

        private float msgTimer;
        private string msg;
        private GUIStyle msgStyle;
        private GUIStyle infoStyle;
        private bool stylesReady;

        void Start()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0.8f;
        }

        private double GetGroundSpeed(Vessel v)
        {
            return v.GetSrfVelocity().magnitude;
        }

        private double GetMaxThrust(Vessel v)
        {
            double thrust = 0;
            foreach (Part p in v.parts)
            {
                if (!p.Modules.OfType<ModuleEngines>().Any() && 
                    !p.Modules.OfType<ModuleEnginesFX>().Any()) continue;
                foreach (var eng in p.Modules.OfType<ModuleEngines>())
                {
                    if (eng.EngineIgnited) thrust += eng.maxThrust;
                }
                foreach (var eng in p.Modules.OfType<ModuleEnginesFX>())
                {
                    if (eng.EngineIgnited) thrust += eng.maxThrust;
                }
            }
            return thrust;
        }

        void Update()
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || v.parts == null || !v.loaded) return;

            double spd = GetGroundSpeed(v);

            if (!initialized)
            {
                if (v.situation == Vessel.Situations.PRELAUNCH ||
                    (v.situation == Vessel.Situations.LANDED && 
                     v.ctrlState.mainThrottle > 0.01f && spd > 1.0))
                {
                    CalcSpeeds(v);
                    initialized = true;
                    calloutsActive = true;
                }
                return;
            }

            if (!calloutsActive) return;

            double agl = v.altitude - startAlt;
            double vs = v.verticalSpeed;

            if (!v1Called && spd >= v1)
            {
                v1Called = true;
                Bing(880);
                ShowMsg("V1  —  DECISION SPEED", Color.yellow);
            }

            if (v1Called && !vrCalled && spd >= vr && agl < 50)
            {
                vrCalled = true;
                Bing(660);
                ShowMsg("ROTATE  —  VR", Color.green);
            }

            if (vrCalled && !v2Called && agl > 5 && vs > 1.5)
            {
                v2Called = true;
                Bing(440);
                ShowMsg("V2  —  POSITIVE RATE", Color.cyan);
                calloutsActive = false;
            }

            if (v.situation == Vessel.Situations.LANDED && spd < 1 && !v2Called)
            {
                initialized = false;
                calloutsActive = false;
                v1Called = false;
                vrCalled = false;
                v2Called = false;
            }
        }

        void CalcSpeeds(Vessel v)
        {
            double mass = v.GetTotalMass();
            double thrust = GetMaxThrust(v);
            double twr = thrust / (mass * 9.81);
            startAlt = v.altitude;

            string catName = "superheavy";
            foreach (var kvp in categories)
            {
                if (mass >= kvp.Value[0] && mass < kvp.Value[1])
                {
                    catName = kvp.Key;
                    break;
                }
            }

            double[] cat = categories[catName];
            v1 = cat[2];
            vr = cat[3];
            v2 = cat[4];

            double twFactor = Math.Max(0.8, Math.Min(1.2, twr));
            v1 *= twFactor;
            vr *= twFactor;
            v2 *= twFactor;

            double altFactor = 1 + (startAlt / 10000);
            v1 *= altFactor;
            vr *= altFactor;
            v2 *= altFactor;

            Debug.Log("[V1Callout] " + catName + " mass=" + mass.ToString("F1") + 
                "t TWR=" + twr.ToString("F2") + " V1=" + v1.ToString("F1") + 
                " VR=" + vr.ToString("F1") + " V2=" + v2.ToString("F1"));
        }

        void Bing(float freq)
        {
            int sr = 44100;
            int len = sr * 40 / 1000;
            AudioClip clip = AudioClip.Create("bing", len, 1, sr, false);
            float[] s = new float[len];
            for (int i = 0; i < len; i++)
                s[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sr) * 0.5f;
            clip.SetData(s, 0);
            audioSource.PlayOneShot(clip);
        }

        void ShowMsg(string text, Color c)
        {
            msg = text;
            msgTimer = 3.0f;
            ScreenMessages.PostScreenMessage(text, 3.0f, ScreenMessageStyle.UPPER_CENTER);
        }

        void OnGUI()
        {
            if (msgTimer <= 0) return;

            if (!stylesReady)
            {
                msgStyle = new GUIStyle(GUI.skin.label);
                msgStyle.fontSize = 28;
                msgStyle.fontStyle = FontStyle.Bold;
                msgStyle.alignment = TextAnchor.MiddleCenter;

                infoStyle = new GUIStyle(GUI.skin.label);
                infoStyle.fontSize = 16;
                infoStyle.alignment = TextAnchor.MiddleCenter;
                stylesReady = true;
            }

            float x = Screen.width / 2f;
            float a = Mathf.Min(1f, msgTimer / 1f);
            Color c = msgTimer > 2f ? Color.yellow : (msgTimer > 1f ? Color.green : Color.cyan);
            c.a = a;
            msgStyle.normal.textColor = c;
            GUI.Label(new Rect(x - 200, Screen.height * 0.35f, 400, 60), msg, msgStyle);

            if (calloutsActive)
            {
                Vessel v = FlightGlobals.ActiveVessel;
                if (v != null)
                {
                    double spd = GetGroundSpeed(v);
                    infoStyle.normal.textColor = new Color(1, 1, 1, a);
                    string info = "Speed: " + spd.ToString("F1") + " m/s";
                    info += "  |  V1: " + v1.ToString("F1") + "  VR: " + vr.ToString("F1") + "  V2: " + v2.ToString("F1");
                    GUI.Label(new Rect(x - 250, Screen.height * 0.35f + 70, 500, 30), info, infoStyle);
                }
            }

            msgTimer -= Time.deltaTime;
        }
    }
}
