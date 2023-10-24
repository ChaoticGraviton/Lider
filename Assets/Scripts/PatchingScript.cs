using Assets.Scripts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
using Assets.Scripts.Craft.Parts.Modifiers.Wing;
using Assets.Scripts.Design;
using Assets.Scripts.Design.PartProperties;
using Assets.Scripts.Design.Tools.Fuselage;
using Assets.Scripts.Design.Tools.Wing;
using HarmonyLib;
using ModApi;
using ModApi.Common.Events;
using ModApi.Craft;
using ModApi.Craft.Parts.Attributes;
using ModApi.Design;
using ModApi.Design.PartProperties;
using ModApi.Math;
using ModApi.Ui.Inspector;
using System.Collections.Generic;
using TMPro;
using UI.Xml;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// TODO

// Look into game code for how it deals with adjusing faces, and replicate (possible solution to wall thickness adaption bug)

// Improve clamp updating to not be nested if/ifelse ; addaption changes now

// Figure out how to add a button to the part properties menu
// >> Create inspector for editing the wing airfoils

// Create functionality for auto deburring 

// TOOD

public static class PatchScript
{
    // The brains behind detecting updates to UI elements
    [HarmonyPatch(typeof(FuselageShapePanelScript), "LayoutRebuilt")]
    class LayoutRebuiltPatch
    {
        static bool Prefix(FuselageShapePanelScript __instance)
        {
            // ClampDistances Listeners
            var slider = __instance.xmlLayout.GetElementById<Slider>("clamp-1");
            __instance.xmlLayout.GetElementById<XmlElement>("clamp1-label").AddOnClickEvent(delegate { ManualDialongInput.OnSliderValueClicked(__instance, "clamp-1", "Clamp 1", "clamp", 0); });
            slider.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnSliderChanged(0, x, true);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            slider = __instance.xmlLayout.GetElementById<Slider>("clamp-2");
            __instance.xmlLayout.GetElementById<XmlElement>("clamp2-label").AddOnClickEvent(delegate { ManualDialongInput.OnSliderValueClicked(__instance, "clamp-2", "Clamp 2", "clamp", 1); });
            slider.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnSliderChanged(1, x, true);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            slider = __instance.xmlLayout.GetElementById<Slider>("clamp-3");
            __instance.xmlLayout.GetElementById<XmlElement>("clamp3-label").AddOnClickEvent(delegate { ManualDialongInput.OnSliderValueClicked(__instance, "clamp-3", "Clamp 3", "clamp", 2); });
            slider.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnSliderChanged(2, x, true);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            slider = __instance.xmlLayout.GetElementById<Slider>("clamp-4");
            __instance.xmlLayout.GetElementById<XmlElement>("clamp4-label").AddOnClickEvent(delegate { ManualDialongInput.OnSliderValueClicked(__instance, "clamp-4", "Clamp 4", "clamp", 3); });
            slider.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnSliderChanged(3, x, true);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            /*
            var toggle = __instance.xmlLayout.GetElementById<Toggle>("lip-compensate-toggle");
            toggle.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnFeatureToggleChanged(ModSettings.Instance.ClampLipCompensateEnabled, x); 
            });
            */

            var toggle = __instance.xmlLayout.GetElementById<Toggle>("duplicate-faces-toggle");
            toggle.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnFeatureToggleChanged(ModSettings.Instance.ClampDuplicateFacesEnabled, x);
            });

            var resetButton = __instance.xmlLayout.GetElementById<Button>("default-clamp");
            resetButton.onClick.AddListener(() =>
            {
                FuselageClampSliders.OnSliderChanged(0, -1, true);
                FuselageClampSliders.OnSliderChanged(1, 1, true);
                FuselageClampSliders.OnSliderChanged(2, -1, true);
                FuselageClampSliders.OnSliderChanged(3, 1, true);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            // Deformation Listeners
            var pinchSlider = __instance.xmlLayout.GetElementById<Slider>("pinch-total");
            var pinchX = __instance.xmlLayout.GetElementById<Slider>("pinch-x");
            var pinchZ = __instance.xmlLayout.GetElementById<Slider>("pinch-z");

            __instance.xmlLayout.GetElementById<XmlElement>("pinchtotal-label").AddOnClickEvent(delegate { ManualDialongInput.OnSliderValueClicked(__instance, "pinch-total", "Pinch total", "deformations", 3); });
            pinchSlider.onValueChanged.AddListener((pinchvalue) =>
            {
                SeparatePinchSliders.OnPinchSliderChanged(0, pinchvalue, false);
                SeparatePinchSliders.OnPinchSliderChanged(2, pinchvalue, false);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            __instance.xmlLayout.GetElementById<XmlElement>("pinchx-label").AddOnClickEvent(delegate { ManualDialongInput.OnSliderValueClicked(__instance, "pinch-x", "Pitch Top", "deformations", 0); });
            pinchX.onValueChanged.AddListener((pinchvalue) =>
            {
                SeparatePinchSliders.OnPinchSliderChanged(0, pinchvalue, false);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            __instance.xmlLayout.GetElementById<XmlElement>("pinchz-label").AddOnClickEvent(delegate { ManualDialongInput.OnSliderValueClicked(__instance, "pinch-z", "Pinch Bottom", "deformations", 2); });
            pinchZ.onValueChanged.AddListener((pinchvalue) =>
            {
                SeparatePinchSliders.OnPinchSliderChanged(2, pinchvalue, false);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            var improvedSlant = __instance.xmlLayout.GetElementById<Slider>("iSlant");
            __instance.xmlLayout.GetElementById<XmlElement>("iSlant-label").AddOnClickEvent(delegate { ManualDialongInput.OnSliderValueClicked(__instance, "iSlant", "Slant", "deformations", 1); });
            improvedSlant.onValueChanged.AddListener((slantvalue) =>
            {
                ISlantSlider.OnSlantChanged(slantvalue, false);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            // WallThickness Sliders
            var totalThickness = __instance.xmlLayout.GetElementById<Slider>("thickness-total");
            var thicknessX = __instance.xmlLayout.GetElementById<Slider>("thickness-top");
            var thicknessY = __instance.xmlLayout.GetElementById<Slider>("thickness-bottom");

            __instance.xmlLayout.GetElementById<XmlElement>("thicktotal-label").AddOnClickEvent(delegate { ManualDialongInput.OnSliderValueClicked(__instance, "thickness-total", "Thickness Total", "wallthickness", 2); });
            totalThickness.onValueChanged.AddListener((thicknessValue) =>
            {
                WallThicknessSliders.OnThicknessSliderChanged(0, thicknessValue, false);
                WallThicknessSliders.OnThicknessSliderChanged(1, thicknessValue, false);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            __instance.xmlLayout.GetElementById<XmlElement>("thickx-label").AddOnClickEvent(delegate { ManualDialongInput.OnSliderValueClicked(__instance, "thickness-top", "Thickness Top", "wallthickness", 0); });
            thicknessX.onValueChanged.AddListener((thicknessValue) =>
            {
                WallThicknessSliders.OnThicknessSliderChanged(0, thicknessValue, false);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            __instance.xmlLayout.GetElementById<XmlElement>("thicky-label").AddOnClickEvent(delegate { ManualDialongInput.OnSliderValueClicked(__instance, "thickness-bottom", "Thickness Bottom", "wallthickness", 1); });
            thicknessY.onValueChanged.AddListener((thicknessValue) =>
            {
                WallThicknessSliders.OnThicknessSliderChanged(1, thicknessValue, false);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            return true;
        }
    }

    // Refreshes the UI with updated values/activation states
    [HarmonyPatch(typeof(FuselageShapePanelScript), "RefreshUi")]
    class RefreshUiPatchPrefix
    {
        static bool Prefix(FuselageShapePanelScript __instance)
        {
            FuselageShapeTool ShapeTool = Game.Instance.Designer.GetTool<FuselageShapeTool>();
            FuselageJoint fuselageJoint = ShapeTool.SelectedJoint;
            FuselageScript fuselageScript = ShapeTool.SelectedFuselage;
            if (fuselageJoint != null)
            {
                // ClampDistances
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
            }
            else if (fuselageScript != null)
            {
                // Deformations
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

                __instance.xmlLayout.GetElementById<TextMeshProUGUI>("iSlant-value").SetText(Units.GetPercentageString(fuselageScript.Data.Deformations.y));
                var improvedSlant = __instance.xmlLayout.GetElementById<Slider>("iSlant");
                improvedSlant.SetValueWithoutNotify(fuselageScript.Data.Deformations.y);

                // Wall Thickness
                __instance.xmlLayout.GetElementById<TextMeshProUGUI>("thickx-value").SetText(Units.GetPercentageString(fuselageScript.Data.WallThickness[0]));
                var thicknessX = __instance.xmlLayout.GetElementById<Slider>("thickness-top");
                thicknessX.SetValueWithoutNotify(fuselageScript.Data.WallThickness[0]);

                __instance.xmlLayout.GetElementById<TextMeshProUGUI>("thicky-value").SetText(Units.GetPercentageString(fuselageScript.Data.WallThickness[1]));
                var thicknessY = __instance.xmlLayout.GetElementById<Slider>("thickness-bottom");
                thicknessY.SetValueWithoutNotify(fuselageScript.Data.WallThickness[1]);

                var thickAvg = (fuselageScript.Data.WallThickness[0] + fuselageScript.Data.WallThickness[1]) * .5f;
                __instance.xmlLayout.GetElementById<TextMeshProUGUI>("thicktotal-value").SetText(Units.GetPercentageString(thickAvg));
                var thicknessSlider = __instance.xmlLayout.GetElementById<Slider>("thickness-total");
                thicknessSlider.SetValueWithoutNotify(thickAvg);
            }
            return true;
        }

        static void Postfix(FuselageShapePanelScript __instance)
        {
            FuselageShapeTool ShapeTool = Game.Instance.Designer.GetTool<FuselageShapeTool>();

            // Element activation management
            if (ShapeTool.SelectedFuselage != null)
            {
                if (ShapeTool.SelectedJoint == null)
                {
                    // For updating sliders sets when selecting the "middle" of a fuselage
                    Traverse.Create(__instance).Field("_sliders").GetValue<List<Slider>>()[5].transform.parent.gameObject.SetActive(!ModSettings.Instance.DeformationsEnabled.Value);
                    Traverse.Create(__instance).Field("_sliders").GetValue<List<Slider>>()[6].transform.parent.gameObject.SetActive(!ModSettings.Instance.DeformationsEnabled.Value);
                    __instance.xmlLayout.GetElementById<Slider>("pinch-total").transform.parent.gameObject.SetActive(ModSettings.Instance.DeformationsEnabled.Value);
                    __instance.xmlLayout.GetElementById<Slider>("pinch-x").transform.parent.gameObject.SetActive(ModSettings.Instance.DeformationsEnabled.Value);
                    __instance.xmlLayout.GetElementById<Slider>("pinch-z").transform.parent.gameObject.SetActive(ModSettings.Instance.DeformationsEnabled.Value);
                    __instance.xmlLayout.GetElementById<Slider>("iSlant").transform.parent.gameObject.SetActive(ModSettings.Instance.DeformationsEnabled.Value);
                    __instance.xmlLayout.GetElementById("thickness-total").transform.parent.gameObject.SetActive(ModSettings.Instance.WallThicknessEnabled.Value);
                    __instance.xmlLayout.GetElementById("thickness-top").transform.parent.gameObject.SetActive(ModSettings.Instance.WallThicknessEnabled.Value);
                    __instance.xmlLayout.GetElementById("thickness-bottom").transform.parent.gameObject.SetActive(ModSettings.Instance.WallThicknessEnabled.Value);
                    __instance.xmlLayout.GetElementById("thickness-disabled").transform.parent.gameObject.SetActive(!ModSettings.Instance.WallThicknessEnabled.Value);
                }
            }
            else if (ShapeTool.SelectedJoint != null)
            {
                // For updating sliders sets when selecting the "top/bottom" of a fuselage
                __instance.xmlLayout.GetElementById<Slider>("clamp-1").transform.parent.gameObject.SetActive(ModSettings.Instance.ClampDistancesEnabled.Value);
                __instance.xmlLayout.GetElementById<Slider>("clamp-2").transform.parent.gameObject.SetActive(ModSettings.Instance.ClampDistancesEnabled.Value);
                __instance.xmlLayout.GetElementById<Slider>("clamp-3").transform.parent.gameObject.SetActive(ModSettings.Instance.ClampDistancesEnabled.Value);
                __instance.xmlLayout.GetElementById<Slider>("clamp-4").transform.parent.gameObject.SetActive(ModSettings.Instance.ClampDistancesEnabled.Value);
                __instance.xmlLayout.GetElementById("default-clamp").transform.parent.gameObject.SetActive(ModSettings.Instance.ClampDistancesEnabled.Value);
                __instance.xmlLayout.GetElementById("clamp-disabled").transform.parent.gameObject.SetActive(!ModSettings.Instance.ClampDistancesEnabled.Value);
                //__instance.xmlLayout.GetElementById("lip-compensate-toggle").transform.parent.gameObject.SetActive(ModSettings.Instance.ClampDistancesEnabled.Value);
                //__instance.xmlLayout.GetElementById("lip-compensate-toggle").SetValue(ModSettings.Instance.ClampLipCompensateEnabled.Value.ToString());
                __instance.xmlLayout.GetElementById("lip-compensate-toggle").SetValue(false.ToString());
                __instance.xmlLayout.GetElementById("lip-compensate-toggle").transform.parent.gameObject.SetActive(false);
                __instance.xmlLayout.GetElementById("duplicate-faces-toggle").SetValue(ModSettings.Instance.ClampDuplicateFacesEnabled.Value.ToString());
                __instance.xmlLayout.GetElementById("duplicate-faces-toggle").transform.parent.gameObject.SetActive(ModSettings.Instance.ClampDistancesEnabled.Value);
            }
        }
    }

    // Called when attaching a new fuselage to an existing fuselage
    [HarmonyPatch(typeof(FuselageJoint), "AdaptSecondFuselage")]
    class AdaptSecondFuselagePatch
    {
        static void Postfix(FuselageJoint __instance)
        {
            // ClampDistances Adaption
            float[] cDS = __instance.Fuselages[0].Fuselage.Data.ClampDistances;
            var adaptIsFlipped = __instance.Fuselages[1].InvertCornerIndexOrder;
            var adaptRelAngle = FuselageClampSliders.RelAngle(__instance.Fuselages[0].Fuselage.PartScript.Transform, __instance.Fuselages[1].Fuselage.PartScript.Transform);
            var aMBO = __instance.Fuselages[0].Fuselage.MarkerBottom == __instance.Fuselages[0].TargetPoint ? 0 : 4;
            if (adaptIsFlipped)
            {
                if (adaptRelAngle < 45 && adaptRelAngle > -45) // Checks if the Fuselages are aligned in the first 'quadrant'
                {
                    cDS = new float[8] { -cDS[5 - aMBO], -cDS[4 - aMBO], cDS[6 - aMBO], cDS[7 - aMBO], -cDS[5 - aMBO], -cDS[4 - aMBO], cDS[6 - aMBO], cDS[7 - aMBO] };
                }
                else if (adaptRelAngle >= 45 && adaptRelAngle < 135) // Checks if the Fuselages are aligned in the second 'quadrant'
                {
                    cDS = new float[8] { cDS[6 - aMBO], cDS[7 - aMBO], cDS[4 - aMBO], cDS[5 - aMBO], cDS[6 - aMBO], cDS[7 - aMBO], cDS[4 - aMBO], cDS[5 - aMBO] };
                }
                else if (adaptRelAngle >= 135 && adaptRelAngle < 225 || adaptRelAngle <= -135 && adaptRelAngle > -225) // Checks if the Fuselages are aligned in the third 'quadrant'
                {
                    cDS = new float[8] { cDS[4 - aMBO], cDS[5 - aMBO], -cDS[7 - aMBO], -cDS[6 - aMBO], cDS[4 - aMBO], cDS[5 - aMBO], -cDS[7 - aMBO], -cDS[6 - aMBO] };
                }
                else if (adaptRelAngle >= -135 && adaptRelAngle < -45) // Checks if the Fuselages are aligned in the fourth 'quadrant'
                {
                    cDS = new float[8] { -cDS[7 - aMBO], -cDS[6 - aMBO], -cDS[5 - aMBO], -cDS[4 - aMBO], -cDS[7 - aMBO], -cDS[6 - aMBO], -cDS[5 - aMBO], -cDS[4 - aMBO] };
                }
            }
            else
            {
                if (adaptRelAngle < 45 && adaptRelAngle > -45) // Checks if the Fuselages are aligned in the first 'quadrant'
                {
                    cDS = new float[8] { cDS[4 - aMBO], cDS[5 - aMBO], cDS[6 - aMBO], cDS[7 - aMBO], cDS[4 - aMBO], cDS[5 - aMBO], cDS[6 - aMBO], cDS[7 - aMBO] };
                }
                else if (adaptRelAngle >= 45 && adaptRelAngle < 135) // Checks if the Fuselages are aligned in the second 'quadrant'
                {
                    cDS = new float[8] { -cDS[7 - aMBO], -cDS[6 - aMBO], cDS[4 - aMBO], cDS[5 - aMBO], -cDS[7 - aMBO], -cDS[6 - aMBO], cDS[4 - aMBO], cDS[5 - aMBO], };
                }
                else if (adaptRelAngle >= 135 && adaptRelAngle < 225 || adaptRelAngle <= -135 && adaptRelAngle > -225) // Checks if the Fuselages are aligned in the third 'quadrant'
                {
                    cDS = new float[8] { -cDS[5 - aMBO], -cDS[4 - aMBO], -cDS[7 - aMBO], -cDS[6 - aMBO], -cDS[5 - aMBO], -cDS[4 - aMBO], -cDS[7 - aMBO], -cDS[6 - aMBO], };
                }
                else if (adaptRelAngle >= -135 && adaptRelAngle < -45) // Checks if the Fuselages are aligned in the fourth 'quadrant'
                {
                    cDS = new float[8] { cDS[6 - aMBO], cDS[7 - aMBO], -cDS[5 - aMBO], -cDS[4 - aMBO], cDS[6 - aMBO], cDS[7 - aMBO], -cDS[5 - aMBO], -cDS[4 - aMBO], };
                }
            }
            // Deformations Adaption

            var markerBottom = __instance.Fuselages[0].Fuselage.MarkerBottom == __instance.Fuselages[0].TargetPoint;
            var adaptDeformation = __instance.Fuselages[0].Fuselage.Data.Deformations;
            adaptDeformation[0] = adaptDeformation[2] = markerBottom ? adaptDeformation[2] : adaptDeformation[0];
            adaptDeformation[1] = __instance.Fuselages[1].Fuselage.Data.Deformations[1];

            // Wall Thickness  Adaption -- commented out, inexplainable behavior
            var adaptThickness = __instance.Fuselages[0].Fuselage.Data.WallThickness;
            adaptThickness[0] = adaptThickness[1] = markerBottom ? adaptThickness[1] : adaptThickness[0];

            // Updating Part Data
            Traverse.Create(__instance.Fuselages[1].Fuselage.Data).Field("_clampDistances").SetValue(cDS);
            Traverse.Create(__instance.Fuselages[1].Fuselage.Data).Field("_deformations").SetValue(adaptDeformation);
            Traverse.Create(__instance.Fuselages[1].Fuselage.Data).Field("_wallThickness").SetValue(adaptThickness);
            Game.Instance.Designer.CraftScript.SetStructureChanged();
            Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        }
    }

    // Flips clamps on mirrored fuselages
    [HarmonyPatch(typeof(FuselageScript), "OnSymmetry")]
    class SymmetryPatch
    {
        static bool Prefix(FuselageScript __instance, SymmetryMode mode)
        {
            if (mode == SymmetryMode.Mirror)
            {
                Utilities.Swap(ref __instance.Data.ClampDistances[0], ref __instance.Data.ClampDistances[1]);
                __instance.Data.ClampDistances[0] *= -1;
                __instance.Data.ClampDistances[1] *= -1;
                Utilities.Swap(ref __instance.Data.ClampDistances[4], ref __instance.Data.ClampDistances[5]);
                __instance.Data.ClampDistances[4] *= -1;
                __instance.Data.ClampDistances[5] *= -1;
            }
            return true;
        }
    }

    // Disables the existing pinch slider
    [HarmonyPatch(typeof(FuselageShapeTool), "UpdateFuselagePinch")]
    class UpdateFuselagePinchPatch
    {
        static bool Prefix(FuselageShapeTool __instance, float pinch)
        {
            return !ModSettings.Instance.DeformationsEnabled;
        }
    }

    // Disables the existing slant slider
    [HarmonyPatch(typeof(FuselageShapeTool), "UpdateFuselageSlant")]
    class UpdateFuselageSlantPatch
    {
        static bool Prefix(FuselageShapeTool __instance, float slant)
        {
            return !ModSettings.Instance.DeformationsEnabled;
        }
    }

    [HarmonyPatch(typeof(WingPartProperties))]
    public class AddInspectorButton
    {
        private static Button _airfoildEditorButton;

        private static UnityAction CreateInspectorAction;

        [DesignerPropertyCenterButton(Label = "TEXT", Order = 100, Tooltip = "TEXT")]
        private bool _varName = false;

        [HarmonyPatch("OnInitialized")]
        static bool Prefix(WingPartProperties __instance)
        {
            CreateInspectorAction += CreateAirfioldEditorInspector;
            XmlElement airfoilEditorXmlElement = (__instance.Flyout as PartPropertiesFlyoutScript).CloneTemplateElement("template-button", __instance.transform);
            _airfoildEditorButton = airfoilEditorXmlElement.GetElementByInternalId<Button>("button");
            airfoilEditorXmlElement.GetElementByInternalId<TextMeshProUGUI>("label").text = "Airfoil Editor";
            _airfoildEditorButton.onClick.AddListener(new UnityAction(CreateInspectorAction));
            return true;
        }

        private static void CreateAirfioldEditorInspector()
        {
            AirfoilEditor.ToggleInspectorPanel();
        }
    }
}