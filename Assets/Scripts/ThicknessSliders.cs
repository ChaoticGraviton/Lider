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
using UI.Xml;
using UnityEngine;
using UnityEngine.UI;

public static class WallThicknessSliders
{
    public static void OnThicknessSliderChanged(int thicknessType, float Thickness, bool isManual)
    {
        Thickness = isManual ? Thickness : Mathf.Round(Thickness * 100f) / 100f;
        FuselageScript fuselageScript = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedFuselage;
        FuselageData fuselageData = fuselageScript.Data;
        Vector2 wallThickness = fuselageData.BottomScale;
        if (thicknessType == 0) wallThickness.x = Thickness;
        else wallThickness.y = Thickness;
        Traverse.Create(fuselageData).Field("_bottomScale").SetValue(wallThickness);
        fuselageData.Script.QueueDesignerMeshUpdate();
        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", fuselageData.Script).GetValue();
        Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();

        //////

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
                        wallThickness.x = fuselageData.BottomScale.x;
                        wallThickness.y = fuselageData.BottomScale.y;
                        // // Gets relavent part data and figures out the relative orietation
                        // var OriginUp = fuselageScript.PartScript.Transform.up;
                        // var SecondaryUp = connectedFuselageData.Script.PartScript.Transform.up;
                        // var RelUp = Vector3.SignedAngle(OriginUp, SecondaryUp, fuselageScript.PartScript.Transform.right);
                        // var isFlipped = RelUp <= 270 && RelUp >= 90 || RelUp >= -270 && RelUp <= -90;
                        // var pDeformations = connectedFuselageData.Deformations;
                        //
                        // // Sets the updated deformations value based on the relative orientations 
                        // var markerTest = attachPoint.Position == fuselageScript.AttachPointTop.Position;
                        // if (isFlipped)
                        // {
                        //     if (markerTest)
                        //     {
                        //         pDeformations.x = fuselageData.Deformations.x;
                        //     }
                        //     else
                        //     {
                        //         pDeformations.z = fuselageData.Deformations.z;
                        //     }
                        // }
                        // else
                        // {
                        //     if (markerTest)
                        //     {
                        //         pDeformations.z = fuselageData.Deformations.x;
                        //     }
                        //     else
                        //     {
                        //         pDeformations.x = fuselageData.Deformations.z;
                        //     }
                        // }
                        Traverse.Create(connectedFuselageData).Field("_bottomScale").SetValue(wallThickness);
                        connectedFuselageData.Script.QueueDesignerMeshUpdate();
                        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", connectedFuselageData.Script).GetValue();
                    }
                }
            }
        }
    }
    public static void OnSliderValueClicked(FuselageShapePanelScript __instance, string thicknessType, string display)
    {
        Slider slider = __instance.xmlLayout.GetElementById<Slider>("thickness-" + thicknessType);
        ModApi.Ui.InputDialogScript dialog = Game.Instance.UserInterface.CreateInputDialog();
        dialog.MessageText = "Enter new value for Thickness " + display;
        dialog.InputText = slider.value.ToString();
        dialog.OkayClicked += delegate (ModApi.Ui.InputDialogScript d)
        {
            d.Close();
            if (float.TryParse(d.InputText, out var result))
            {
                result = Mathf.Clamp01(result);
                if (thicknessType == "total")
                {
                    WallThicknessSliders.OnThicknessSliderChanged(0, result, true);
                    WallThicknessSliders.OnThicknessSliderChanged(1, result, true);
                }
                else if (thicknessType == "top")
                {
                    WallThicknessSliders.OnThicknessSliderChanged(0, result, true);
                }
                else if (thicknessType == "bottom")
                {
                    WallThicknessSliders.OnThicknessSliderChanged(1, result, true);
                }
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            }
        };
    }
    [HarmonyPatch(typeof(FuselageShapePanelScript), "LayoutRebuilt")]
    class LayoutRebuiltPatch
    {
        static bool Prefix(FuselageShapePanelScript __instance)
        {
            // The brains for detecting a slider change
            var thicknessTotal = __instance.xmlLayout.GetElementById<Slider>("thickness-total");
            var thicknessTop = __instance.xmlLayout.GetElementById<Slider>("thickness-top");
            var thicknessBottom = __instance.xmlLayout.GetElementById<Slider>("thickness-bottom");

            __instance.xmlLayout.GetElementById<XmlElement>("thicktotal-label").AddOnClickEvent(delegate { OnSliderValueClicked(__instance, "total", "Total"); });
            thicknessTotal.onValueChanged.AddListener((thicknessValue) =>
            {
                WallThicknessSliders.OnThicknessSliderChanged(0, thicknessValue, false);
                WallThicknessSliders.OnThicknessSliderChanged(1, thicknessValue, false);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            __instance.xmlLayout.GetElementById<XmlElement>("thickx-label").AddOnClickEvent(delegate { OnSliderValueClicked(__instance, "top", "Top"); });
            thicknessTop.onValueChanged.AddListener((thicknessValue) =>
            {
                WallThicknessSliders.OnThicknessSliderChanged(0, thicknessValue, false);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            __instance.xmlLayout.GetElementById<XmlElement>("thicky-label").AddOnClickEvent(delegate { OnSliderValueClicked(__instance, "bottom", "Bottom"); });
            thicknessBottom.onValueChanged.AddListener((thicknessValue) =>
            {
                WallThicknessSliders.OnThicknessSliderChanged(1, thicknessValue, false);
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
                __instance.xmlLayout.GetElementById<TextMeshProUGUI>("thickx-value").SetText(Units.GetPercentageString(fuselageScript.Data.BottomScale.x));
                var thicknessX = __instance.xmlLayout.GetElementById<Slider>("thickness-top");
                thicknessX.SetValueWithoutNotify(fuselageScript.Data.BottomScale.x);

                __instance.xmlLayout.GetElementById<TextMeshProUGUI>("thicky-value").SetText(Units.GetPercentageString(fuselageScript.Data.BottomScale.y));
                var thicknessY = __instance.xmlLayout.GetElementById<Slider>("thickness-bottom");
                thicknessY.SetValueWithoutNotify(fuselageScript.Data.BottomScale.y);

                var thicknessAverage = (thicknessX.value + thicknessY.value) * .5f;
                __instance.xmlLayout.GetElementById<TextMeshProUGUI>("thicktotal-value").SetText(Units.GetPercentageString(thicknessAverage));
                var thicknessSlider = __instance.xmlLayout.GetElementById<Slider>("thickness-total");
                thicknessSlider.SetValueWithoutNotify(thicknessAverage);
            }
            return true;
        }
    }
}