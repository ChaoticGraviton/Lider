using Assets.Scripts;
using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
using Assets.Scripts.Design;
using Assets.Scripts.Design.Tools.Fuselage;
using HarmonyLib;
using ModApi;
using ModApi.Design;
using ModApi.Math;
using System;
using System.Collections.Generic;
using TMPro;
using UI.Xml;
using UnityEngine;
using UnityEngine.UI;
using static Assets.Scripts.Design.Tools.Fuselage.FuselageJoint;

public static class FuselageClampSliders
{
    
    static bool isPanelInput = false;

    public static void OnSliderChanged(int sliderIndex, float clamp)
    {
        clamp = isPanelInput ? clamp : Mathf.Round(clamp * 100f) / 100f;
        isPanelInput = false;
        FuselageJoint fuselageJoint = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedJoint;
        FuselageData fuselageData = fuselageJoint.Fuselages[0].Fuselage.Data;
        int autoRezise = 1;
        if (fuselageData.AutoResize && Game.Instance.Settings.Game.Designer.EnableAutoResize)
        {
            autoRezise = fuselageJoint.Fuselages.Count;
        }
        for (int i = 0; i < autoRezise; i++)
        {
            // Gets the base information that is needed
            var OriginFuselage = fuselageJoint.Fuselages[0].Fuselage.PartScript.Transform;
            var SecondaryFuselage = fuselageJoint.Fuselages[i].Fuselage.PartScript.Transform;
            var isFlipped = fuselageJoint.Fuselages[i].InvertCornerIndexOrder;
            var fuselageDataB = fuselageJoint.Fuselages[i].Fuselage.Data;
            float[] FuselageClamp = fuselageDataB.ClampDistances;

            // Get the relative angle between the two fuselages on the joint
            var RelAngleClean = RelAngle(OriginFuselage, SecondaryFuselage);

            // Gets the corner index, which is replicated from the game's code for what's nessecary  
            int index1 = GetCornerIndex(fuselageJoint.Fuselages[i], sliderIndex, fuselageJoint.Fuselages[i].Flipped);
            FuselageClamp[index1] = clamp;
            float[] ClampDistance = new float[8];
            Array.Copy(FuselageClamp, ClampDistance, 8);
            var MarkerBottom = (fuselageData.Script.MarkerBottom.position == fuselageJoint.Transform.position);
            var FDCD = fuselageData.ClampDistances;

            if (OriginFuselage != SecondaryFuselage)
            {
                if (isFlipped)
                {
                    if (RelAngleClean < 45 && RelAngleClean > -45) // Checks if the Fuselages are aligned in the first 'quadrant'
                    {
                        ClampDistance = MarkerBottom ? new float[8] { ClampDistance[0], ClampDistance[1], ClampDistance[2], ClampDistance[3], -FDCD[5], -FDCD[4], FDCD[6], FDCD[7] } : new float[8] { -FDCD[1], -FDCD[0], FDCD[2], FDCD[3], ClampDistance[4], ClampDistance[5], ClampDistance[6], ClampDistance[7] };
                    }
                    else if (RelAngleClean >= 45 && RelAngleClean < 135) // Checks if the Fuselages are aligned in the second 'quadrant'
                    {
                        ClampDistance = MarkerBottom ? new float[8] { ClampDistance[0], ClampDistance[1], ClampDistance[2], ClampDistance[3], FDCD[6], FDCD[7], FDCD[4], FDCD[5] } : new float[8] { FDCD[2], FDCD[3], FDCD[0], FDCD[1], ClampDistance[4], ClampDistance[5], ClampDistance[6], ClampDistance[7] };
                    }
                    else if (RelAngleClean >= 135 && RelAngleClean < 225 || RelAngleClean <= -135 && RelAngleClean > -225) // Checks if the Fuselages are aligned in the third 'quadrant'
                    {
                        ClampDistance = MarkerBottom ? new float[8] { ClampDistance[0], ClampDistance[1], ClampDistance[2], ClampDistance[3], FDCD[4], FDCD[5], -FDCD[7], -FDCD[6] } : new float[8] { FDCD[0], FDCD[1], -FDCD[3], -FDCD[2], ClampDistance[4], ClampDistance[5], ClampDistance[6], ClampDistance[7] };
                    }
                    else if (RelAngleClean >= -135 && RelAngleClean < -45) // Checks if the Fuselages are aligned in the fourth 'quadrant'
                    {
                        ClampDistance = MarkerBottom ? new float[8] { ClampDistance[0], ClampDistance[1], ClampDistance[2], ClampDistance[3], -FDCD[7], -FDCD[6], -FDCD[5], -FDCD[4] } : new float[8] { -FDCD[3], -FDCD[2], -FDCD[1], -FDCD[0], ClampDistance[4], ClampDistance[5], ClampDistance[6], ClampDistance[7] };
                    }
                }
                else if (!isFlipped)
                {
                    if (RelAngleClean < 45 && RelAngleClean > -45) // Checks if the Fuselages are aligned in the first 'quadrant'
                    {
                        ClampDistance = MarkerBottom ? new float[8] { FDCD[4], FDCD[5], FDCD[6], FDCD[7], ClampDistance[4], ClampDistance[5], ClampDistance[6], ClampDistance[7] } : new float[8] { ClampDistance[0], ClampDistance[1], ClampDistance[2], ClampDistance[3], FDCD[0], FDCD[1], FDCD[2], FDCD[3] };
                    }
                    else if (RelAngleClean >= 45 && RelAngleClean < 135) // Checks if the Fuselages are aligned in the second 'quadrant'
                    {
                        ClampDistance = MarkerBottom ? new float[8] { -FDCD[7], -FDCD[6], FDCD[4], FDCD[5], ClampDistance[4], ClampDistance[5], ClampDistance[6], ClampDistance[7] } : new float[8] { ClampDistance[0], ClampDistance[1], ClampDistance[2], ClampDistance[3], -FDCD[3], -FDCD[2], FDCD[0], FDCD[1] };
                    }
                    else if (RelAngleClean >= 135 && RelAngleClean < 225 || RelAngleClean <= -135 && RelAngleClean > -225) // Checks if the Fuselages are aligned in the third 'quadrant'
                    {
                        ClampDistance = MarkerBottom ? new float[8] { -FDCD[5], -FDCD[4], -FDCD[7], -FDCD[6], ClampDistance[4], ClampDistance[5], ClampDistance[6], ClampDistance[7] } : new float[8] { ClampDistance[0], ClampDistance[1], ClampDistance[2], ClampDistance[3], -FDCD[1], -FDCD[0], -FDCD[3], -FDCD[2] };
                    }
                    else if (RelAngleClean >= -135 && RelAngleClean < -45) // Checks if the Fuselages are aligned in the fourth 'quadrant'
                    {
                        ClampDistance = MarkerBottom ? new float[8] { FDCD[6], FDCD[7], -FDCD[5], -FDCD[4], ClampDistance[4], ClampDistance[5], ClampDistance[6], ClampDistance[7] } : new float[8] { ClampDistance[0], ClampDistance[1], ClampDistance[2], ClampDistance[3], FDCD[2], FDCD[3], -FDCD[1], -FDCD[0] };
                    }
                }
            }

            Traverse.Create(fuselageDataB).Field("_clampDistances").SetValue(ClampDistance);
            fuselageDataB.Script.QueueDesignerMeshUpdate();
            Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", fuselageDataB.Script).GetValue();
            Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        }
    }

    public static int GetCornerIndex(FuselageInfo fuselageInfo, int globalCornerIndex, bool flipped)
    {
        int num1 = globalCornerIndex;
        int num2 = ((!fuselageInfo.InvertCornerIndexOrder) ? (num1 - fuselageInfo.CornerIndexOffset) : (3 - (num1 - fuselageInfo.CornerIndexOffset)));
        if (num2 >= 4)
        {
            num2 -= 4;
        }
        else if (num2 < 0)
        {
            num2 += 4;
        }
        if (flipped)
        {
            num2 += 4;
        }
        return num2;
    }

    public static int RelAngle(Transform Origin, Transform Secondary)
    {
        var OriginFlat = Vector3.ProjectOnPlane(Origin.forward, Origin.up);
        var SecondaryFlat = Vector3.ProjectOnPlane(Secondary.forward, Origin.up);
        var RelAngle = Vector3.SignedAngle(OriginFlat, SecondaryFlat, Origin.up);
        return Mathf.RoundToInt(RelAngle);
    }

    [HarmonyPatch(typeof(FuselageJoint), "AdaptSecondFuselage")]
    class AdaptSecondFuselagePatch
    {
        static void Postfix(FuselageJoint __instance)
        {
            // used when attaching a new fuselage to an existing fuselage
            float[] cDS = __instance.Fuselages[0].Fuselage.Data.ClampDistances;
            var adaptIsFlipped = __instance.Fuselages[1].InvertCornerIndexOrder;
            var adaptRelAngle = RelAngle(__instance.Fuselages[0].Fuselage.PartScript.Transform, __instance.Fuselages[1].Fuselage.PartScript.Transform);
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
            Traverse.Create(__instance.Fuselages[1].Fuselage.Data).Field("_clampDistances").SetValue(cDS);
        }
    }

    [HarmonyPatch(typeof(FuselageShapePanelScript), "LayoutRebuilt")]
    class LayoutRebuiltPatch
    {
        static bool Prefix(FuselageShapePanelScript __instance)
        {
            var slider = __instance.xmlLayout.GetElementById<Slider>("clamp-1");
            __instance.xmlLayout.GetElementById<XmlElement>("clamp1-label").AddOnClickEvent(delegate { OnSliderValueClicked(__instance, 1); });
            slider.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnSliderChanged(0, x);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            slider = __instance.xmlLayout.GetElementById<Slider>("clamp-2");
            __instance.xmlLayout.GetElementById<XmlElement>("clamp2-label").AddOnClickEvent(delegate { OnSliderValueClicked(__instance, 2); });
            slider.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnSliderChanged(1, x);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            slider = __instance.xmlLayout.GetElementById<Slider>("clamp-3");
            __instance.xmlLayout.GetElementById<XmlElement>("clamp3-label").AddOnClickEvent(delegate { OnSliderValueClicked(__instance, 3); });
            slider.onValueChanged.AddListener((x) =>
            {
                FuselageClampSliders.OnSliderChanged(2, x);
                Traverse.Create(__instance).Method("RefreshUi").GetValue();
            });

            slider = __instance.xmlLayout.GetElementById<Slider>("clamp-4");
            __instance.xmlLayout.GetElementById<XmlElement>("clamp4-label").AddOnClickEvent(delegate { OnSliderValueClicked(__instance, 4); });
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

    public static void OnSliderValueClicked(FuselageShapePanelScript __instance, int sliderID)
    {
        isPanelInput = true;
        Slider slider = __instance.xmlLayout.GetElementById<Slider>("clamp-" + sliderID);
        ModApi.Ui.InputDialogScript dialog = Game.Instance.UserInterface.CreateInputDialog();
        dialog.MessageText = "Enter new value for Clamp " + sliderID;
        dialog.InputText = slider.value.ToString();
        dialog.OkayClicked += delegate (ModApi.Ui.InputDialogScript d)
        {
            d.Close();
            if (float.TryParse(d.InputText, out var result))
            {
                slider.value = result;
            }
        };
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