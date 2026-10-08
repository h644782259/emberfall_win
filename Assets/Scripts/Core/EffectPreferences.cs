using UnityEngine;

namespace Emberfall
{
    /// <summary>Local accessibility preferences, independent of character saves.</summary>
    public static class EffectPreferences
    {
        public static float InterfaceTextScale
        {
            get { float value=PlayerPrefs.GetFloat("Emberfall.InterfaceTextScale",1f);return float.IsNaN(value)||float.IsInfinity(value)?1f:Mathf.Clamp(value,1f,1.2f); }
            set { PlayerPrefs.SetFloat("Emberfall.InterfaceTextScale",float.IsNaN(value)||float.IsInfinity(value)?1f:Mathf.Clamp(value,1f,1.2f));PlayerPrefs.Save(); }
        }
        public static void CycleInterfaceTextScale()
        {InterfaceTextScale=InterfaceTextScale>=1.19f?1f:InterfaceTextScale>=1.09f?1.2f:1.1f;}
        public static float CombatTextScale { get { return Mathf.Clamp(PlayerPrefs.GetFloat("Emberfall.CombatTextScale", 1.25f), 1f, 1.8f); } set { PlayerPrefs.SetFloat("Emberfall.CombatTextScale", Mathf.Clamp(value, 1f, 1.8f)); PlayerPrefs.Save(); } }
        public static float EffectsScale { get { return Mathf.Clamp(PlayerPrefs.GetFloat("Emberfall.EffectsScale", 1f), .25f, 1f); } set { PlayerPrefs.SetFloat("Emberfall.EffectsScale", Mathf.Clamp(value, .25f, 1f)); PlayerPrefs.Save(); } }
        public static bool CameraShake { get { return PlayerPrefs.GetInt("Emberfall.CameraShake", 1) != 0; } set { PlayerPrefs.SetInt("Emberfall.CameraShake", value ? 1 : 0); PlayerPrefs.Save(); } }
        public static int TouchPosition {get{return Mathf.Clamp(PlayerPrefs.GetInt("Emberfall.TouchPosition",0),-1,1);}set{PlayerPrefs.SetInt("Emberfall.TouchPosition",Mathf.Clamp(value,-1,1));PlayerPrefs.Save();}}
        public static float TouchOpacity {get{return FiniteControlSetting(PlayerPrefs.GetFloat("Emberfall.TouchOpacity",1),.5f);}set{PlayerPrefs.SetFloat("Emberfall.TouchOpacity",FiniteControlSetting(value,.5f));PlayerPrefs.Save();}}
        public static float TouchVisualScale {get{return FiniteControlSetting(PlayerPrefs.GetFloat("Emberfall.TouchVisualScale",1),.86f);}set{PlayerPrefs.SetFloat("Emberfall.TouchVisualScale",FiniteControlSetting(value,.86f));PlayerPrefs.Save();}}
        private static float FiniteControlSetting(float value,float minimum){return float.IsNaN(value)||float.IsInfinity(value)?1:Mathf.Clamp(value,minimum,1);}
        public static bool ReducedEffects { get { return EffectsScale <= .5f; } }
    }
}
