using Assets.Scripts;
using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
using Assets.Scripts.Design;
using Assets.Scripts.Design.Tools.Fuselage;
using HarmonyLib;
using ModApi.Math;
using UI.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class iSlantSlider
{
    public static void OnSlantChanged(float slant, bool isManual)
    {
        slant = isManual? slant : Mathf.Round(slant * 100f) / 100f;
        FuselageScript fuselageScript = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedFuselage;
        FuselageData fuselageData = fuselageScript.Data;
        Vector3 deformations = fuselageData.Deformations;
        deformations.y = slant;
        Traverse.Create(fuselageData).Field("_deformations").SetValue(deformations);
        fuselageData.Script.QueueDesignerMeshUpdate();
        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", fuselageData.Script).GetValue();
        Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
    }

    [HarmonyPatch(typeof(FuselageShapePanelScript), "LayoutRebuilt")]
    class LayoutRebuiltPatch
    {
        static bool Prefix(FuselageShapePanelScript __instance)
        {
            // The brains for detecting a slider change
            var improvedSlant = __instance.xmlLayout.GetElementById<Slider>("iSlant");
            __instance.xmlLayout.GetElementById<XmlElement>("iSlant-label").AddOnClickEvent(delegate { OnSliderValueClicked(__instance); });
            improvedSlant.onValueChanged.AddListener((slantvalue) =>
            {
                iSlantSlider.OnSlantChanged(slantvalue, false);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });
            return true;
        }
    }
public static void OnSliderValueClicked(FuselageShapePanelScript __instance)
    {
        Slider slider = __instance.xmlLayout.GetElementById<Slider>("iSlant");
        ModApi.Ui.InputDialogScript dialog = Game.Instance.UserInterface.CreateInputDialog();
        dialog.MessageText = "Enter new value for Slant";
        dialog.InputText = slider.value.ToString();
        dialog.OkayClicked += delegate (ModApi.Ui.InputDialogScript d)
        {
            d.Close();
            if (float.TryParse(d.InputText, out var result))
            {
                result = Mathf.Clamp01(result);
                iSlantSlider.OnSlantChanged(result, true);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            }
        };
    }

    [HarmonyPatch(typeof(FuselageShapePanelScript), "RefreshUi")]
    class RefreshUiPatch
    {
        static bool Prefix(FuselageShapePanelScript __instance)
        {
            // Updates the displayed values for the slant sliders

            FuselageJoint fuselageJoint = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedJoint;

            if (fuselageJoint == null)
            {
                FuselageScript fuselageScript = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedFuselage;
                if (fuselageScript == null)
                {
                    return true;
                }
                __instance.xmlLayout.GetElementById<TextMeshProUGUI>("iSlant-value").SetText(Units.GetPercentageString(fuselageScript.Data.Deformations.y));
                var improvedSlant = __instance.xmlLayout.GetElementById<Slider>("iSlant");
                improvedSlant.SetValueWithoutNotify(fuselageScript.Data.Deformations.y);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FuselageShapeTool), "UpdateFuselageSlant")]
    class UpdateFuselageSlantPatch
    {
        // Disables the existing slant slider

        static bool Prefix(FuselageShapeTool __instance, float slant)
        {
            return !ModSettings.Instance.DeformationsEnabled;
        }
    }
}