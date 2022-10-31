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
            // if (fuselageData.Script.PartScript.Data.PartType.Id == "Strut1")
            // {
            //     var pDeformations = fuselageScript.Data.Deformations;
            //     if (!fuselageScript.AttachPointTop.IsAvailable)
            //     {
            //         var connectedFuselageData = fuselageScript.AttachPointTop.PartConnections[0].GetOtherPart(fuselageScript.PartScript.Data).GetModifier<FuselageData>();
            //         pDeformations.x = fuselageData.Deformations.x;
            //         pDeformations.z = fuselageData.Deformations.x;
            //     }
            //     if ((!fuselageScript.AttachPointBottom.IsAvailable))
            //     {
            //         var connectedFuselageData = fuselageScript.AttachPointBottom.PartConnections[0].GetOtherPart(fuselageScript.PartScript.Data).GetModifier<FuselageData>();
            //         pDeformations.x = fuselageData.Deformations.z;
            //         pDeformations.z = fuselageData.Deformations.z;
            //         Traverse.Create(fuselageScript.AttachPointBottom.PartConnections[0]).Field("_deformations").SetValue(pDeformations);
            //         connectedFuselageData.Script.QueueDesignerMeshUpdate();
            //         Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", connectedFuselageData.Script).GetValue();
            //     }
            // }
            // else
            // {
            List<AttachPoint> attachPoints = fuselageScript.PartScript.Data.AttachPoints.Where(p => p.ConnectionType == AttachPointConnectionType.Shell).ToList();
            foreach (var attachPoint in attachPoints)
            {
                if (attachPoint.PartConnections.Count > 0)
                {
                    var connectedFuselageData = attachPoint.PartConnections[0].GetOtherPart(fuselageScript.PartScript.Data).GetModifier<FuselageData>();
                    if (connectedFuselageData != null)
                    {
                        var pDeformations = connectedFuselageData.Deformations;
                        //Debug.Log("attach pos: " + attachPoint.Position + "marker top pos: " + fuselageData.Script.MarkerTop.position);
                        if (attachPoint.Position == fuselageScript.AttachPointTop.Position)
                        {
                            pDeformations.z = fuselageData.Deformations.x;
                        }
                        else
                        {
                            pDeformations.x = fuselageData.Deformations.z;
                        }
                        Traverse.Create(connectedFuselageData).Field("_deformations").SetValue(pDeformations);
                        connectedFuselageData.Script.QueueDesignerMeshUpdate();
                        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", connectedFuselageData.Script).GetValue();
                    }
                }   
            }
            //  }
        }
    }
}

[HarmonyPatch(typeof(FuselageShapePanelScript), "LayoutRebuilt")]
class LayoutRebuiltPatch
{
    static bool Prefix(FuselageShapePanelScript __instance)
    {


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
        static bool Prefix(FuselageShapeTool __instance, float pinch)
        {
            return false;
        }
    }

}