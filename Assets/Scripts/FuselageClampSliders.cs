using Assets.Scripts;
using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
using Assets.Scripts.Design;
using Assets.Scripts.Design.Tools.Fuselage;
using HarmonyLib;
using ModApi.Math;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class FuselageClampSliders 
{
    public static void OnSliderChanged(int sliderIndex, float clamp)
    {
        clamp = Mathf.Round(clamp * 100f) / 100f;
        FuselageJoint fuselageJoint = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedJoint;
        FuselageData fuselageData = fuselageJoint.Fuselages[0].Fuselage.Data;
        int autoRezise = 1;
        if (fuselageData.AutoResize && Game.Instance.Settings.Game.Designer.EnableAutoResize)
        {
            autoRezise = fuselageJoint.Fuselages.Count;
        }
        for (int i = 0; i < autoRezise; i++)
        {
            fuselageData = fuselageJoint.Fuselages[i].Fuselage.Data;
            float[] clampDistances = fuselageData.ClampDistances;
            int index1 = sliderIndex;
            if (fuselageData.Script.MarkerBottom == fuselageJoint.Fuselages[i].TargetPoint)
            {
                index1 += 4;
            }
            clampDistances[index1] = clamp;
            Traverse.Create(fuselageData).Field("_clampDistances").SetValue(clampDistances);
            fuselageData.Script.QueueDesignerMeshUpdate();
            Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", fuselageData.Script).GetValue();
            Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        }
    }

    [HarmonyPatch(typeof(FuselageJoint), "AdaptSecondFuselage")]
    class AdaptSecondFuselagePatch
    {
        static void Postfix(FuselageJoint __instance, bool updateOppositeSide)
        {
            float[] clampDistanceSwap = __instance.Fuselages[0].Fuselage.Data.ClampDistances;
            if (__instance.Fuselages[0].Fuselage.MarkerBottom == __instance.Fuselages[0].TargetPoint)
            {
                clampDistanceSwap = new float[8] { clampDistanceSwap[4], clampDistanceSwap[5], clampDistanceSwap[6], clampDistanceSwap[7], clampDistanceSwap[4], clampDistanceSwap[5], clampDistanceSwap[6], clampDistanceSwap[7] };
            }
            else
            {
                clampDistanceSwap = new float[8] { clampDistanceSwap[0], clampDistanceSwap[1], clampDistanceSwap[2], clampDistanceSwap[3], clampDistanceSwap[0], clampDistanceSwap[1], clampDistanceSwap[2], clampDistanceSwap[3] };
            }
            clampDistanceSwap = new float[8] { clampDistanceSwap[4], clampDistanceSwap[5], clampDistanceSwap[6], clampDistanceSwap[7], clampDistanceSwap[0], clampDistanceSwap[1], clampDistanceSwap[2], clampDistanceSwap[3] };
            Traverse.Create(__instance.Fuselages[1].Fuselage.Data).Field("_clampDistances").SetValue(clampDistanceSwap);
        }
    }

    [HarmonyPatch(typeof(FuselageShapePanelScript), "LayoutRebuilt")]
    class LayoutRebuiltPatch
    {
        static bool Prefix(FuselageShapePanelScript __instance)
        {
            var slider = __instance.xmlLayout.GetElementById<Slider>("clamp-1"); 
            slider.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnSliderChanged(0, x);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });
            slider = __instance.xmlLayout.GetElementById<Slider>("clamp-2");
            slider.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnSliderChanged(1, x);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });
            slider = __instance.xmlLayout.GetElementById<Slider>("clamp-3"); 
            slider.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnSliderChanged(2, x);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });
            slider = __instance.xmlLayout.GetElementById<Slider>("clamp-4");
            slider.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnSliderChanged(3, x);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });
            var resetButton = __instance.xmlLayout.GetElementById<Button>("default-clamp"); 
            resetButton.onClick.AddListener(() =>
            {
                FuselageClampSliders.OnSliderChanged(0, -1);
                FuselageClampSliders.OnSliderChanged(1, 1);
                FuselageClampSliders.OnSliderChanged(2, -1);
                FuselageClampSliders.OnSliderChanged(3, 1);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });
            return true;
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
                slider.value = clampDistances[index];
            }
            return true;
        }
        static void Postfix(FuselageShapePanelScript __instance)
        {

            if (Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedFuselage != null)
            {
                Traverse.Create(__instance).Field("_sliders").GetValue<List<Slider>>()[5].transform.parent.gameObject.SetActive(value: false);
            }
        }
    }
}