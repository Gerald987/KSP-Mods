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
        private bool active;
        private double startAlt;
        private AudioSource audioSrc;

        private float msgTimer;
        private string msgText;
        private GUIStyle bigStyle;
        private GUIStyle infoStyle;
        private bool ready;

        void Start()
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.volume = 1.0f;
        }

        private double GroundSpeed(Vessel v)
        {
            return v.GetSrfVelocity().magnitude;
        }

        private double TotalThrust(Vessel v)
        {
            double thrust = 0;
            foreach (Part p in v.parts)
            {
                foreach (var eng in p.Modules.OfType<ModuleEngines>())
                    thrust += eng.maxThrust;
                foreach (var eng in p.Modules.OfType<ModuleEnginesFX>())
                    thrust += eng.maxThrust;
            }
            return thrust;
        }

        void Update()
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || v.parts == null || !v.loaded) return;

            double spd = GroundSpeed(v);

            // Auto-arm: throttle up + rolling on ground
            if (!active)
            {
                if (v.ctrlState.mainThrottle > 0.05f && spd > 2.0 &&
                    (v.situation == Vessel.Situations.LANDED || v.situation == Vessel.Situations.PRELAUNCH))
                {
                    Init(v);
                    active = true;
                    Debug.Log("[V1Callout] Armed — throttle=" + v.ctrlState.mainThrottle + " spd=" + spd);
                }
                return;
            }

            double agl = v.altitude - startAlt;
            double vs = v.verticalSpeed;

            if (!v1Called && spd >= v1)
            {
                v1Called = true;
                Beep(880);
                Show("V1  —  DECISION SPEED", 3f);
            }

            if (!vrCalled && v1Called && spd >= vr && agl < 50)
            {
                vrCalled = true;
                Beep(660);
                Show("ROTATE  —  VR", 3f);
            }

            if (!v2Called && vrCalled && agl > 5 && vs > 1.0)
            {
                v2Called = true;
                Beep(440);
                Show("V2  —  POSITIVE RATE", 3f);
                active = false;
            }

            // Reset if stopped before takeoff
            if (spd < 0.5 && !v2Called)
            {
                active = false;
                v1Called = vrCalled = v2Called = false;
            }
        }

        void Init(Vessel v)
        {
            double mass = v.GetTotalMass();
            double thrust = TotalThrust(v);
            double twr = thrust / (mass * 9.81);
            startAlt = v.altitude;

            string cat = "superheavy";
            foreach (var kvp in categories)
                if (mass >= kvp.Value[0] && mass < kvp.Value[1]) { cat = kvp.Key; break; }

            double[] c = categories[cat];
            v1 = c[2]; vr = c[3]; v2 = c[4];

            double twF = Math.Max(0.8, Math.Min(1.2, twr));
            v1 *= twF; vr *= twF; v2 *= twF;

            double altF = 1 + (startAlt / 10000);
            v1 *= altF; vr *= altF; v2 *= altF;

            Debug.Log("[V1Callout] " + cat + " mass=" + mass.ToString("F1") +
                "t thrust=" + thrust.ToString("F0") + "kN TWR=" + twr.ToString("F2") +
                " V1=" + v1.ToString("F1") + " VR=" + vr.ToString("F1") + " V2=" + v2.ToString("F1"));
        }

        void Beep(float freq)
        {
            int sr = 44100;
            int len = sr * 80 / 1000;
            AudioClip clip = AudioClip.Create("b", len, 1, sr, false);
            float[] s = new float[len];
            for (int i = 0; i < len; i++)
                s[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sr) * 0.4f;
            clip.SetData(s, 0);
            audioSrc.PlayOneShot(clip);
        }

        void Show(string text, float dur)
        {
            msgText = text;
            msgTimer = dur;
            ScreenMessages.PostScreenMessage(text, dur, ScreenMessageStyle.UPPER_CENTER);
        }

        void OnGUI()
        {
            if (msgTimer <= 0) return;
            if (!ready)
            {
                bigStyle = new GUIStyle(GUI.skin.label);
                bigStyle.fontSize = 32;
                bigStyle.fontStyle = FontStyle.Bold;
                bigStyle.alignment = TextAnchor.MiddleCenter;
                infoStyle = new GUIStyle(GUI.skin.label);
                infoStyle.fontSize = 16;
                infoStyle.alignment = TextAnchor.MiddleCenter;
                ready = true;
            }

            float x = Screen.width / 2f;
            float a = Mathf.Min(1f, msgTimer);

            bigStyle.normal.textColor = new Color(1, 1, 0, a);
            GUI.Label(new Rect(x - 250, Screen.height * 0.30f, 500, 70), msgText, bigStyle);

            if (active)
            {
                Vessel v = FlightGlobals.ActiveVessel;
                if (v != null)
                {
                    double spd = GroundSpeed(v);
                    infoStyle.normal.textColor = new Color(1, 1, 1, a);
                    GUI.Label(new Rect(x - 300, Screen.height * 0.30f + 75, 600, 25),
                        "SPD " + spd.ToString("F0") + "  |  V1 " + v1.ToString("F0") + "  VR " + vr.ToString("F0") + "  V2 " + v2.ToString("F0"), infoStyle);
                }
            }

            msgTimer -= Time.deltaTime;
        }
    }
}
