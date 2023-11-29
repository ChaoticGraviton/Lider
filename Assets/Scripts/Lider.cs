using Assets.Scripts;
using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
using Assets.Scripts.Design;
using Assets.Scripts.Design.Tools.Fuselage;
using HarmonyLib;
using ModApi.Craft.Parts;
using ModApi.Settings.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Assets.Scripts.Design.Tools.Fuselage.FuselageJoint;

public static class FuselageClampSliders
{
    public static void OnSliderChanged(int sliderIndex, float clamp, bool isSlider)
    {
        clamp = isSlider ? Mathf.Round(clamp * 100f) / 100f : clamp;
        FuselageShapeTool shapeTool = Game.Instance.Designer.GetTool<FuselageShapeTool>();
        FuselageJoint fuselageJoint = shapeTool.SelectedJoint;
        FuselageData fuselageData = fuselageJoint.Fuselages[0].Fuselage.Data;
        FuselageScript selectedFuselageScript = shapeTool.SelectedJoint.Fuselages[0].Fuselage;
        Transform originFuselage = fuselageJoint.Fuselages[0].Fuselage.PartScript.Transform;
        float[] clampDistance;
        bool markerBottom = (fuselageData.Script.MarkerBottom.position == fuselageJoint.Transform.position);

        int autoRezise = 1;
        if (LiderFuselageMethods.isAutoResize(true))        
            autoRezise = fuselageJoint.Fuselages.Count;        
        for (int i = 0; i < autoRezise; i++)
        {
            // Gets the base information that is needed
            Transform secondaryFuselage = fuselageJoint.Fuselages[i].Fuselage.PartScript.Transform;
            bool isFlipped = fuselageJoint.Fuselages[i].InvertCornerIndexOrder;
            FuselageData fuselageDataB = fuselageJoint.Fuselages[i].Fuselage.Data;
            float[] fuselageClamp = fuselageDataB.ClampDistances;

            // Get the relative angle between the two fuselages on the joint
            int relAngleClean = RelAngle(originFuselage, secondaryFuselage);

            // Gets the corner index, which is replicated from the game's code for what's nessecary  
            int index1 = GetCornerIndex(fuselageJoint.Fuselages[i], sliderIndex, fuselageJoint.Fuselages[i].Flipped);
            fuselageClamp[index1] = clamp;
            clampDistance = new float[8];
            Array.Copy(fuselageClamp, clampDistance, 8);

            // Duplicates the edited face onto the opposing face if mirror faces is enabled
            if (ModSettings.Instance.ClampDuplicateFacesEnabled.Value)
            {
                float[] clampTop = clampDistance[0..4];
                float[] clampBottom = clampDistance[4..8];
                float[] clampUpdate = markerBottom ? (isFlipped ? clampTop : clampBottom) : (isFlipped ? clampBottom : clampTop);
                clampUpdate.Concat(clampUpdate).ToArray().CopyTo(clampDistance, 0);
            }

            // Gets the clamp values for fuselages while accounting for rotation
            AlterClampPerRotation(fuselageData, relAngleClean, isFlipped, markerBottom, originFuselage, secondaryFuselage, clampDistance);
            Traverse.Create(fuselageDataB).Field("_clampDistances").SetValue(clampDistance);
            LiderFuselageMethods.FinalUpdates(fuselageDataB.Script);
        }

        // Updates the fuselage that may be on the other face that isn't apart of the joint
        if (ModSettings.Instance.ClampDuplicateFacesEnabled.Value && fuselageData.AutoResize && Game.Instance.Settings.Game.Designer.EnableAutoResize)
        {
            List<AttachPoint> attachPoints = selectedFuselageScript.PartScript.Data.AttachPoints.Where(p => p.ConnectionType == AttachPointConnectionType.Shell | p.ConnectionType == AttachPointConnectionType.Fairing).ToList();
            foreach (var attachPoint in attachPoints)
            {
                if (attachPoint.PartConnections.Count > 0)
                {
                    var oppositeFaceFuselage = attachPoint.PartConnections[0].GetOtherPart(selectedFuselageScript.PartScript.Data).GetModifier<FuselageData>();
                    if (oppositeFaceFuselage != null && VerifyNotBaseFuselage(oppositeFaceFuselage, fuselageJoint) && oppositeFaceFuselage.AutoResize)
                    {
                        // Gets the pseudo data, emulating having the other joint selected
                        var pseudoSecondaryFuselage = oppositeFaceFuselage.Script.PartScript.Transform;
                        var psuedoClampDistance = oppositeFaceFuselage.ClampDistances;
                        FuselageData psuedoFuselageDataB = oppositeFaceFuselage.Script.Data;
                        float relUp = Vector3.SignedAngle(originFuselage.up, pseudoSecondaryFuselage.up, originFuselage.right);
                        bool psuedoIsFlipped = relUp <= 270 && relUp >= 90 || relUp >= -270 && relUp <= -90;
                        int psuedorelAngleClean = RelAngle(originFuselage, pseudoSecondaryFuselage);

                        // Gets the clamp values for fuselages while accounting for rotation
                        float[] updateMirrorClamp = AlterClampPerRotation(fuselageData, psuedorelAngleClean, psuedoIsFlipped, !markerBottom, originFuselage, pseudoSecondaryFuselage, psuedoClampDistance);
                        Traverse.Create(psuedoFuselageDataB).Field("_clampDistances").SetValue(updateMirrorClamp);
                        LiderFuselageMethods.FinalUpdates(psuedoFuselageDataB.Script);

                    }
                }
            }
        }
    }

    private static bool VerifyNotBaseFuselage(FuselageData oppositeFuselage, FuselageJoint fuselageJoint)
    {
        if (!(fuselageJoint.Fuselages.Count > 1)) 
            return true;
        else if (oppositeFuselage != fuselageJoint.Fuselages[1].Fuselage.Data)
            return true;
        else
            return false;
    }

    internal static float[] AlterClampPerRotation(FuselageData fuselageData, int relAngleClean, bool isFlipped, bool markerBottom, Transform originFuselage, Transform secondaryFuselage, float[] clampDistance)
    {
        var FDCD = fuselageData.ClampDistances;
        // Following code made by 1nsanity github.com/1nsanity247
        int[,] indices =
        {
                { 5, 4, 6, 7 }, { 6, 7, 4, 5 }, { 4, 5, 7, 6 }, { 7, 6, 5, 4 },
                { 4, 5, 6, 7 }, { 7, 6, 4, 5 }, { 5, 4, 7, 6 }, { 6, 7, 5, 4 },
                { 1, 0, 2, 3 }, { 2, 3, 0, 1 }, { 0, 1, 3, 2 }, { 3, 2, 1, 0 },
                { 0, 1, 2, 3 }, { 3, 2, 0, 1 }, { 1, 0, 3, 2 }, { 2, 3, 1, 0 }
            };

        int[,] signs =
        {
                { -1, -1,  1,  1 }, {  1,  1,  1,  1 }, {  1,  1, -1, -1 }, { -1, -1, -1, -1 },
                {  1,  1,  1,  1 }, { -1, -1,  1,  1 }, { -1, -1, -1, -1 }, {  1,  1, -1, -1 }
            };

        int quadrant = (relAngleClean + 405) / 90 % 4;
        int indexOffset = isFlipped ^ markerBottom ? 0 : 4;
        int basePermutation = (isFlipped ? 0 : 4) + quadrant;
        int indexPermutation = (markerBottom ? 0 : 8) + basePermutation;

        if (originFuselage != secondaryFuselage)       
            for (int j = 0; j < 4; j++)
                clampDistance[j + indexOffset] = signs[basePermutation, j] * FDCD[indices[indexPermutation, j]];        
        return clampDistance;
    }

    internal static void OnFeatureToggleChanged(BoolSetting settingType, bool value)
    {
        settingType.UpdateAndCommit(value);
        settingType.CommitChanges();
    }

    public static int GetCornerIndex(FuselageInfo fuselageInfo, int globalCornerIndex, bool flipped)
    {
        int num1 = globalCornerIndex;
        int num2 = ((!fuselageInfo.InvertCornerIndexOrder) ? (num1 - fuselageInfo.CornerIndexOffset) : (3 - (num1 - fuselageInfo.CornerIndexOffset)));
        if (num2 >= 4)        
            num2 -= 4;        
        else if (num2 < 0)        
            num2 += 4;        
        if (flipped)        
            num2 += 4;        
        return num2;
    }

    public static int RelAngle(Transform Origin, Transform Secondary)
    {
        var OriginFlat = Vector3.ProjectOnPlane(Origin.forward, Origin.up);
        var SecondaryFlat = Vector3.ProjectOnPlane(Secondary.forward, Origin.up);
        var RelAngle = Vector3.SignedAngle(OriginFlat, SecondaryFlat, Origin.up);
        return Mathf.RoundToInt(RelAngle);
    }
}

public static class SeparatePinchSliders
{
    public static void OnPinchSliderChanged(int pinchType, float pinch, bool isManual)
    {
        pinch = isManual ? pinch : Mathf.Round(pinch * 100f) / 100f;
        FuselageScript fuselageScript = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedFuselage;
        FuselageData fuselageData = fuselageScript.Data;
        Vector3 deformations = fuselageData.Deformations;
        if (pinchType == 0) deformations.x = pinch;
        else deformations.z = pinch;
        Traverse.Create(fuselageData).Field("_deformations").SetValue(deformations);
        fuselageData.Script.QueueDesignerMeshUpdate();
        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", fuselageData.Script).GetValue();
        Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        if (LiderFuselageMethods.isAutoResize())
        {
            List<AttachPoint> attachPoints = fuselageScript.PartScript.Data.AttachPoints.Where(p => p.ConnectionType == AttachPointConnectionType.Shell | p.ConnectionType == AttachPointConnectionType.Fairing).ToList();
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
                        int updatePosition = isFlipped ? markerTest ? 0 : 2 : markerTest ? 2 : 0;
                        pDeformations[updatePosition] = fuselageData.Deformations[markerTest ? 0 : 2];
                        Traverse.Create(connectedFuselageData).Field("_deformations").SetValue(pDeformations);
                        LiderFuselageMethods.FinalUpdates(connectedFuselageData.Script);
                    }
                }
            }
        }
    }
}

public static class ISlantSlider
{
    public static void OnSlantChanged(float slant, bool isManual)
    {
        slant = isManual ? slant : Mathf.Round(slant * 100f) / 100f;
        FuselageScript fuselageScript = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedFuselage;
        FuselageData fuselageData = fuselageScript.Data;
        Vector3 deformations = fuselageData.Deformations;
        deformations.y = slant;
        Traverse.Create(fuselageData).Field("_deformations").SetValue(deformations);
        fuselageData.Script.QueueDesignerMeshUpdate();
        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", fuselageData.Script).GetValue();
        Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
    }
}

public static class WallThicknessSliders
{
    public static void OnThicknessSliderChanged(int thicknessType, float thickness, bool isManual)
    {
        thickness = isManual ? thickness : Mathf.Round(thickness * 100f) / 100f;
        FuselageScript fuselageScript = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedFuselage;
        FuselageData fuselageData = fuselageScript.Data;

        float[] wallthickness = fuselageData.WallThickness;
        if (thicknessType == 0) wallthickness[0] = thickness;
        else wallthickness[1] = thickness;

        Traverse.Create(fuselageData).Field("_wallThickness").SetValue(wallthickness);
        fuselageData.Script.QueueDesignerMeshUpdate();

        fuselageData.Script.UpdateMeshes(true);
        Game.Instance.Designer.CraftScript.SetStructureChanged();
        Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", fuselageData.Script).GetValue();

        if (LiderFuselageMethods.isAutoResize()) 
            DoAutoResizeUpdate(fuselageScript);
    }

    public static void DoAutoResizeUpdate(FuselageScript fuselageScript)
    {
        List<AttachPoint> attachPoints = fuselageScript.PartScript.Data.AttachPoints.Where(p => p.ConnectionType == AttachPointConnectionType.Shell | p.ConnectionType == AttachPointConnectionType.Fairing).ToList();

        foreach (var attachPoint in attachPoints)
        {
            if (attachPoint.PartConnections.Count > 0)
            {
                var connectedFuselageData = attachPoint.PartConnections[0].GetOtherPart(fuselageScript.PartScript.Data).GetModifier<FuselageData>();
                if (connectedFuselageData != null)
                {
                    // Checks if the parts are flipped
                    bool isFlipped = LiderFuselageMethods.isFlipped(fuselageScript, connectedFuselageData.Script);
                    var wThickness = connectedFuselageData.WallThickness;

                    // Sets the updated deformations value based on the relative orientations 
                    var markerTest = attachPoint.Position == fuselageScript.AttachPointTop.Position;
                    int updatePosition = isFlipped ? markerTest ? 0 : 1 : markerTest ? 1 : 0;
                    wThickness[updatePosition] = fuselageScript.Data.WallThickness[markerTest ? 0 : 1];

                    // Update values on the autoresize-connected fuselages
                    Traverse.Create(connectedFuselageData).Field("_wallThickness").SetValue(wThickness);
                    LiderFuselageMethods.FinalUpdates(connectedFuselageData.Script);
                }
            }
        }
    }
}

public static class LiderFuselageMethods
{
    public static void OnSliderValueClicked(FuselageShapePanelScript fuselagePanel, string elementID, string displayName, string sliderType, int sliderID)
    {
        Slider slider = fuselagePanel.xmlLayout.GetElementById<Slider>(elementID);
        ModApi.Ui.InputDialogScript dialog = Game.Instance.UserInterface.CreateInputDialog();
        dialog.MessageText = "Enter new value for " + displayName;
        dialog.InputText = DisplayValueAdjust(slider.value.ToString(), sliderType, sliderID);
        dialog.OkayClicked += delegate (ModApi.Ui.InputDialogScript d)
        {
            d.Close();
            if (float.TryParse(d.InputText, out var result))
            {
                if (sliderType == "clamp")
                {
                    result = Mathf.Clamp(result, -1, 1);
                    FuselageClampSliders.OnSliderChanged(sliderID, result, false);
                }
                else if (sliderType == "deformations")
                {
                    result = Mathf.Clamp01(result);
                    // Checks against being Pinch
                    if (sliderID != 1)
                    {
                        if (sliderID == 3)
                        {
                            SeparatePinchSliders.OnPinchSliderChanged(0, result, true);
                            SeparatePinchSliders.OnPinchSliderChanged(2, result, true);
                        }
                        else                        
                            SeparatePinchSliders.OnPinchSliderChanged(sliderID, result, true);                        
                    }
                    // Must be slant
                    else                    
                        ISlantSlider.OnSlantChanged(result, true);                    
                }
                else if (sliderType == "wallthickness")
                {
                    result = Mathf.Clamp(result, 0, result);
                    if (sliderID == 2)
                    {
                        WallThicknessSliders.OnThicknessSliderChanged(0, result, true);
                        WallThicknessSliders.OnThicknessSliderChanged(1, result, true);
                    }
                    else                    
                        WallThicknessSliders.OnThicknessSliderChanged(sliderID, result, true);                    
                }
                Traverse.Create(fuselagePanel).Method("RefreshUi").GetValue();
            }
        };
    }

    public static string DisplayValueAdjust(string inputPreview, string sliderType, int sliderID)
    {
        if (sliderType == "wallthickness")
        {
            FuselageScript fuselageScript = Game.Instance.Designer.GetTool<FuselageShapeTool>().SelectedFuselage;
            if (sliderID == 2)
                inputPreview = ((fuselageScript.Data.WallThickness[0] + fuselageScript.Data.WallThickness[1]) * .5f).ToString();
            else
                inputPreview = fuselageScript.Data.WallThickness[sliderID].ToString();
        }
        return inputPreview;
    }

    public static bool isFlipped(FuselageScript originFuselage, FuselageScript secondaryFuselage)
    {
        var OriginUp = originFuselage.PartScript.Transform.up;
        var SecondaryUp = secondaryFuselage.PartScript.Transform.up;
        var RelUp = Math.Abs(Vector3.SignedAngle(OriginUp, SecondaryUp, originFuselage.PartScript.Transform.right));
        return RelUp <= 270 && RelUp >= 90;
    }

    public static void FinalUpdates(FuselageScript updateFuselage)
    {
        updateFuselage.UpdateMeshes(true);
        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", updateFuselage).GetValue();
        Game.Instance.Designer.CraftScript.SetStructureChanged();
        Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
    }

    public static bool isAutoResize(bool isJoint = false)
    {
        FuselageData fuselageData;
        FuselageShapeTool shapeTool = Game.Instance.Designer.GetTool<FuselageShapeTool>();
        if (isJoint) 
            fuselageData = shapeTool.SelectedJoint.Fuselages[0].Fuselage.Data;
        else 
            fuselageData = shapeTool.SelectedFuselage.Data;
        return fuselageData.AutoResize && Game.Instance.Settings.Game.Designer.EnableAutoResize;
    }
}