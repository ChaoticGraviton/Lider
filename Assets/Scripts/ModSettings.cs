namespace Assets.Scripts
{
    using System.Collections.Generic;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
    using Assets.Scripts.Design;
    using Assets.Scripts.Design.Tools.Fuselage;
    using HarmonyLib;
    using ModApi.Math;
    using ModApi.Settings.Core;
    using TMPro;
    using UnityEngine.UI;

    /// <summary>
    /// The settings for the mod.
    /// </summary>
    /// <seealso cref="ModApi.Settings.Core.SettingsCategory{Assets.Scripts.ModSettings}" />
    public class ModSettings : SettingsCategory<ModSettings>
    {
        /// <summary>
        /// The mod settings instance.
        /// </summary>
        private static ModSettings _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModSettings"/> class.
        /// </summary>
        public ModSettings() : base("Lider")
        {
        }

        /// <summary>
        /// Gets the mod settings instance.
        /// </summary>
        /// <value>
        /// The mod settings instance.
        /// </value>
        public static ModSettings Instance => _instance ?? (_instance = Game.Instance.Settings.ModSettings.GetCategory<ModSettings>());
        public BoolSetting DeformationsEnabled { get; private set; }
        public BoolSetting ClampDistancesEnabled { get; private set; }
        public BoolSetting WallThicknessEnabled { get; private set; }


        protected override void InitializeSettings()
        {
            DeformationsEnabled = this.CreateBool("Advanced Deformation Sliders")
                .SetDescription("Enables remade sliders for pinch and slant")
                .SetDefault(true);
            ClampDistancesEnabled = this.CreateBool("Fuselage Clamp Sliders")
                .SetDescription("Enables sliders for fuselage clamping")
                .SetDefault(true);
            WallThicknessEnabled = this.CreateBool("Fuselage Wall Thickness Sliders")
                .SetDescription("Enable sliders for fuselage wall thickness")
                .SetDefault(true);
        }
    }
    [HarmonyPatch(typeof(FuselageShapePanelScript), "RefreshUi")]
    class RefreshUiPatch
    {
        static bool Prefix(FuselageShapePanelScript __instance)
        {
            FuselageJoint fuselageJoint = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedJoint;

            if (fuselageJoint == null)
            {
                return true;
            }
            FuselageData fuselageData = fuselageJoint.Fuselages[0].Fuselage.Data;
            float[] clampDistances = fuselageData.ClampDistances;
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                if (fuselageData.Script.MarkerBottom == fuselageJoint.Fuselages[0].TargetPoint)
                {
                    index += 4;
                }
                __instance.xmlLayout.GetElementById<TextMeshProUGUI>("clamp" + (i + 1) + "-value").SetText(Units.GetPercentageString(clampDistances[index]));
                var slider = __instance.xmlLayout.GetElementById<Slider>("clamp-" + (i + 1));
                slider.SetValueWithoutNotify(clampDistances[index]);
            }
            return true;
        }
        static void Postfix(FuselageShapePanelScript __instance)
        {
            // Slider enable management
            var fuselageData = Game.Instance.Designer.GetTool<FuselageShapeTool>();

            if (fuselageData.SelectedFuselage != null)
            {
                // For updating sliders sets when selecting the "middle" of a fuselage
                Traverse.Create(__instance).Field("_sliders").GetValue<List<Slider>>()[5].transform.parent.gameObject.SetActive(value: !ModSettings.Instance.DeformationsEnabled);
                Traverse.Create(__instance).Field("_sliders").GetValue<List<Slider>>()[6].transform.parent.gameObject.SetActive(value: !ModSettings.Instance.DeformationsEnabled);
                __instance.xmlLayout.GetElementById<Slider>("pinch-total").transform.parent.gameObject.SetActive(value: ModSettings.Instance.DeformationsEnabled);
                __instance.xmlLayout.GetElementById<Slider>("pinch-x").transform.parent.gameObject.SetActive(value: ModSettings.Instance.DeformationsEnabled);
                __instance.xmlLayout.GetElementById<Slider>("pinch-z").transform.parent.gameObject.SetActive(value: ModSettings.Instance.DeformationsEnabled);
                __instance.xmlLayout.GetElementById<Slider>("iSlant").transform.parent.gameObject.SetActive(value: ModSettings.Instance.DeformationsEnabled);
                __instance.xmlLayout.GetElementById("thickness-total").transform.parent.gameObject.SetActive(value: ModSettings.Instance.WallThicknessEnabled);
                __instance.xmlLayout.GetElementById("thickness-top").transform.parent.gameObject.SetActive(value: ModSettings.Instance.WallThicknessEnabled);
                __instance.xmlLayout.GetElementById("thickness-bottom").transform.parent.gameObject.SetActive(value: ModSettings.Instance.WallThicknessEnabled);
                __instance.xmlLayout.GetElementById("thickness-disabled").transform.parent.gameObject.SetActive(value: !ModSettings.Instance.WallThicknessEnabled);
            }
            else if (fuselageData.SelectedJoint.Transform.position == fuselageData.SelectedJoint.Fuselages[0].Fuselage.Data.Script.MarkerBottom.position && fuselageData.SelectedJoint.Transform.position != null) ;
            {
                // For updating sliders sets when selecting the "top/bottom" of a fuselage
                __instance.xmlLayout.GetElementById<Slider>("clamp-1").transform.parent.gameObject.SetActive(value: ModSettings.Instance.ClampDistancesEnabled);
                __instance.xmlLayout.GetElementById<Slider>("clamp-2").transform.parent.gameObject.SetActive(value: ModSettings.Instance.ClampDistancesEnabled);
                __instance.xmlLayout.GetElementById<Slider>("clamp-3").transform.parent.gameObject.SetActive(value: ModSettings.Instance.ClampDistancesEnabled);
                __instance.xmlLayout.GetElementById<Slider>("clamp-4").transform.parent.gameObject.SetActive(value: ModSettings.Instance.ClampDistancesEnabled);
                __instance.xmlLayout.GetElementById("default-clamp").transform.parent.gameObject.SetActive(value: ModSettings.Instance.ClampDistancesEnabled);
                __instance.xmlLayout.GetElementById("clamp-disabled").transform.parent.gameObject.SetActive(value: !ModSettings.Instance.ClampDistancesEnabled);
            }
        }
    }
}