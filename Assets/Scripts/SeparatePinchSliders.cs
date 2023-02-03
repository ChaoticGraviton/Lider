using Assets.Scripts;
using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
using Assets.Scripts.Design;
using Assets.Scripts.Design.Tools.Fuselage;
using HarmonyLib;
using ModApi.Craft.Parts;
using ModApi.Math;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class SeparatePinchSliders
{
    public static void OnPinchSliderChanged(int pinchType, float pinch)
    {
        pinch = Mathf.Round(pinch * 100f) / 100f;
        FuselageScript fuselageScript = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedFuselage;
        FuselageData fuselageData = fuselageScript.Data;
        Vector3 deformations = fuselageData.Deformations;
        if (pinchType == 0) deformations.x = pinch;
        else deformations.z = pinch;
        Traverse.Create(fuselageData).Field("_deformations").SetValue(deformations);
        fuselageData.Script.QueueDesignerMeshUpdate();
        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", fuselageData.Script).GetValue();
        Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        if (fuselageData.AutoResize && Game.Instance.Settings.Game.Designer.EnableAutoResize)
        {
            List<AttachPoint> attachPoints = fuselageScript.PartScript.Data.AttachPoints.Where(p => p.ConnectionType == AttachPointConnectionType.Shell).ToList();
            foreach (var attachPoint in attachPoints)
            {
                if (attachPoint.PartConnections.Count > 0)
                {
                    var connectedFuselageData = attachPoint.PartConnections[0].GetOtherPart(fuselageScript.PartScript.Data).GetModifier<FuselageData>();
                    if (connectedFuselageData != null)
                    {
                        // Gets relavent part data and figures out the relative orietation
                        var OriginUp = fuselageScript.PartScript.Transform.up;
                        var SecondaryUp = connectedFuselageData.Script.PartScript.Transform.up;
                        var RelUp = Vector3.SignedAngle(OriginUp, SecondaryUp, fuselageScript.PartScript.Transform.right);
                        var isFlipped = RelUp <= 270 && RelUp >= 90 || RelUp >= -270 && RelUp <= -90;
                        var pDeformations = connectedFuselageData.Deformations;

                        // Sets the updated deformations value based on the relative orientations 
                        var markerTest = attachPoint.Position == fuselageScript.AttachPointTop.Position;
                        if (isFlipped)
                        {
                            if (markerTest)
                            {
                                pDeformations.x = fuselageData.Deformations.x;
                            }
                            else
                            {
                                pDeformations.z = fuselageData.Deformations.z;
                            }
                        }
                        else
                        {
                            if (markerTest)
                            {
                                pDeformations.z = fuselageData.Deformations.x;
                            }
                            else
                            {
                                pDeformations.x = fuselageData.Deformations.z;
                            }
                        }
                        Traverse.Create(connectedFuselageData).Field("_deformations").SetValue(pDeformations);
                        connectedFuselageData.Script.QueueDesignerMeshUpdate();
                        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", connectedFuselageData.Script).GetValue();
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(FuselageJoint), "AdaptSecondFuselage")]
    class AdaptSecondFuselagePatch
    {
        static void Postfix(FuselageJoint __instance)
        {
            var adaptDeformation = __instance.Fuselages[0].Fuselage.Data.Deformations;
            var markerBottom = __instance.Fuselages[0].Fuselage.MarkerBottom == __instance.Fuselages[0].TargetPoint;
            adaptDeformation.x = markerBottom ? adaptDeformation.z : adaptDeformation.x;
            adaptDeformation.z = markerBottom ? adaptDeformation.z : adaptDeformation.x;
            Traverse.Create(__instance.Fuselages[1].Fuselage.Data).Field("_deformations").SetValue(adaptDeformation);
        }
    }
}

[HarmonyPatch(typeof(FuselageShapePanelScript), "LayoutRebuilt")]
class LayoutRebuiltPatch
{
    static bool Prefix(FuselageShapePanelScript __instance)
    {
        // The brains for detecting a slider change
        var pinchSlider = __instance.xmlLayout.GetElementById<Slider>("pinch-total");
        var pinchX = __instance.xmlLayout.GetElementById<Slider>("pinch-x");
        var pinchZ = __instance.xmlLayout.GetElementById<Slider>("pinch-z");
        pinchSlider.onValueChanged.AddListener((pinchvalue) =>
        {
            SeparatePinchSliders.OnPinchSliderChanged(0, pinchvalue);
            SeparatePinchSliders.OnPinchSliderChanged(2, pinchvalue);
            Traverse.Create(__instance).Method("RefreshUi").GetValue();
        });
        pinchX.onValueChanged.AddListener((pinchvalue) =>
        {
            SeparatePinchSliders.OnPinchSliderChanged(0, pinchvalue);
            Traverse.Create(__instance).Method("RefreshUi").GetValue();
        });
        pinchZ.onValueChanged.AddListener((pinchvalue) =>
        {
            SeparatePinchSliders.OnPinchSliderChanged(2, pinchvalue);
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
        // Updates the displayed values for the pinch sliders

        FuselageJoint fuselageJoint = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedJoint;

        if (fuselageJoint == null)
        {
            FuselageScript fuselageScript = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedFuselage;
            if (fuselageScript == null)
            {
                return true;
            }
            __instance.xmlLayout.GetElementById<TextMeshProUGUI>("pinchx-value").SetText(Units.GetPercentageString(fuselageScript.Data.Deformations.x));
            var pinchX = __instance.xmlLayout.GetElementById<Slider>("pinch-x");
            pinchX.SetValueWithoutNotify(fuselageScript.Data.Deformations.x);

            __instance.xmlLayout.GetElementById<TextMeshProUGUI>("pinchz-value").SetText(Units.GetPercentageString(fuselageScript.Data.Deformations.z));
            var pinchZ = __instance.xmlLayout.GetElementById<Slider>("pinch-z");
            pinchZ.SetValueWithoutNotify(fuselageScript.Data.Deformations.z);

            var pinchAvg = (pinchX.value + pinchZ.value) * .5f;
            __instance.xmlLayout.GetElementById<TextMeshProUGUI>("pinchtotal-value").SetText(Units.GetPercentageString(pinchAvg));
            var pinchSlider = __instance.xmlLayout.GetElementById<Slider>("pinch-total");
            pinchSlider.SetValueWithoutNotify(pinchAvg);
        }
        return true;
    }

    [HarmonyPatch(typeof(FuselageShapeTool), "UpdateFuselagePinch")]
    class UpdateFuselagePinchPatch
    {
        // Disables the existing pinch slider

        static bool Prefix(FuselageShapeTool __instance, float pinch)
        {
            return false;
        }
    }
}