using Assets.Scripts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
using Assets.Scripts.Craft.Parts.Modifiers.Wing;
using Assets.Scripts.Design;
using Assets.Scripts.Design.Tools.Fuselage;
using HarmonyLib;
using ModApi;
using ModApi.Common.Events;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Math;
using ModApi.Settings.Core;
using ModApi.Ui.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Assets.Scripts.Design.Tools.Fuselage.FuselageJoint;


//Debug.Log("Returned Clamp: " + string.Join(", ", clampDistance));
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
        if (fuselageData.AutoResize && Game.Instance.Settings.Game.Designer.EnableAutoResize)
        {
            autoRezise = fuselageJoint.Fuselages.Count;
        }
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
            fuselageDataB.Script.QueueDesignerMeshUpdate();
            Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", fuselageDataB.Script).GetValue();
            Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
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
                        psuedoFuselageDataB.Script.QueueDesignerMeshUpdate();
                        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", psuedoFuselageDataB.Script).GetValue();
                        Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
                    }
                }
            }
        }
    }

    private static bool VerifyNotBaseFuselage(FuselageData oppositeFuselage, FuselageJoint fuselageJoint)
    {
        if (!(fuselageJoint.Fuselages.Count > 1)) return true;
        else if (oppositeFuselage != fuselageJoint.Fuselages[1].Fuselage.Data) return true;
        else return false;
    }

    private static float[] AlterClampPerRotation(FuselageData fuselageData, int relAngleClean, bool isFlipped, bool markerBottom, Transform originFuselage, Transform secondaryFuselage, float[] clampDistance)
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
        {
            for (int j = 0; j < 4; j++)
                clampDistance[j + indexOffset] = signs[basePermutation, j] * FDCD[indices[indexPermutation, j]];
        }
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
        if (fuselageData.AutoResize && Game.Instance.Settings.Game.Designer.EnableAutoResize)
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
                        connectedFuselageData.Script.QueueDesignerMeshUpdate();
                        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", connectedFuselageData.Script).GetValue();
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
        fuselageScript.PartScript.CraftScript.PartHighlighter.AddPartHighlight(fuselageScript.PartScript);
        fuselageScript.PartScript.CraftScript.PartHighlighter.HighlightColor = Color.red;
        FuselageData fuselageData = fuselageScript.Data;

        float[] deformations = fuselageData.WallThickness;
        if (thicknessType == 0) deformations[0] = thickness;
        else deformations[1] = thickness;

        Traverse.Create(fuselageData).Field("_wallThickness").SetValue(deformations);
        fuselageData.Script.QueueDesignerMeshUpdate();

        fuselageData.Script.UpdateMeshes(true);
        Game.Instance.Designer.CraftScript.SetStructureChanged();
        Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", fuselageData.Script).GetValue();

        if (fuselageData.AutoResize && Game.Instance.Settings.Game.Designer.EnableAutoResize)
        {
            DoAutoResizeUpdate(fuselageScript);
        }
    }

    private static void DoAutoResizeUpdate(FuselageScript fuselageScript)
    {
        List<AttachPoint> attachPoints = fuselageScript.PartScript.Data.AttachPoints.Where(p => p.ConnectionType == AttachPointConnectionType.Shell | p.ConnectionType == AttachPointConnectionType.Fairing).ToList();

        foreach (var attachPoint in attachPoints)
        {
            if (attachPoint.PartConnections.Count > 0)
            {
                var connectedFuselageData = attachPoint.PartConnections[0].GetOtherPart(fuselageScript.PartScript.Data).GetModifier<FuselageData>();
                if (connectedFuselageData != null)
                {
                    Debug.Log(connectedFuselageData.Script.PartScript.Data.Name);
                    // Gets relavent part data and figures out the relative orietation
                    var OriginUp = fuselageScript.PartScript.Transform.up;
                    var SecondaryUp = connectedFuselageData.Script.PartScript.Transform.up;
                    var RelUp = Vector3.SignedAngle(OriginUp, SecondaryUp, fuselageScript.PartScript.Transform.right);
                    var isFlipped = RelUp <= 270 && RelUp >= 90 || RelUp >= -270 && RelUp <= -90;
                    var wThickness = connectedFuselageData.WallThickness;

                    // Sets the updated deformations value based on the relative orientations 
                    var markerTest = attachPoint.Position == fuselageScript.AttachPointTop.Position;
                    int updatePosition = isFlipped ? markerTest ? 0 : 1 : markerTest ? 1 : 0;
                    wThickness[updatePosition] = fuselageScript.Data.WallThickness[markerTest ? 0 : 1];

                    Debug.Log("Thickness: " + string.Join(", ", wThickness));
                    // Update values on the autoresize-connected fuselages
                    Traverse.Create(connectedFuselageData).Field("_wallThickness").SetValue(wThickness);
                    connectedFuselageData.Script.QueueDesignerMeshUpdate();
                    connectedFuselageData.Script.UpdateMeshes(true);
                    Traverse.Create(Game.Instance.Designer.GetTool<FuselageShapeTool>()).Method("UpdateSymmetricFuselages", connectedFuselageData.Script).GetValue();
                    Game.Instance.Designer.CraftScript.SetStructureChanged();
                }
            }
        }
    }
}

public static class ManualDialongInput
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
                        {
                            SeparatePinchSliders.OnPinchSliderChanged(sliderID, result, true);
                        }
                    }
                    // Must be slant
                    else
                    {
                        ISlantSlider.OnSlantChanged(result, true);
                    }
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
                    {
                        WallThicknessSliders.OnThicknessSliderChanged(sliderID, result, true);
                    }
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
            {
                inputPreview = ((fuselageScript.Data.WallThickness[0] + fuselageScript.Data.WallThickness[1]) * .5f).ToString();
            }
            else
            {
                inputPreview = fuselageScript.Data.WallThickness[sliderID].ToString();
            }
        }
        return inputPreview;
    }
}

public class AirfoilEditor
{
    /* List of Properties to edit
     * curve Length
     * Visual airfoil -isStylish
     * Visual Curve - isFancy
     * ThicknessDelta - ThicknessDeltaStyle for Reference
     * ThicknessOffset - ThicknessOffsetStyle for Reference
     * ThicknessTip
     * LeadingBulge - LeadingBulgeStyle for Reference
     * 
    */

    private static IInspectorPanel _inspectorPanel;
    private static Vector2? _currentOffset;
    private static bool _refreshPending;
    private static InspectorPanelCreationInfo creationInfo;
    // Visual Airfoil
    private static ToggleModel _isStylish;
    // Visual Curve
    private static ToggleModel _isFancy;
    private static SliderModel _curveLengthSlider;
    private static SliderModel _thicknessOffsetSlider;
    private static SliderModel _thicknessDeltaSlider;
    private static SliderModel _leadingBulgeSlider;
    private static SliderModel _thicknessSlider;
    private static TextModel _thicknessOffsetStyleText;
    private static TextModel _thicknessDeltaStyleText;
    private static TextModel _leadingBulgeStyleText;
    private static float _curveLength;
    private static float _thicknessOffset;
    private static string _thicknessOffsetStyle = "TMDWU";
    private static float _thicknessDelta;
    private static string _thicknessDeltaStyle = "TMDWU";
    private static float _leadingBulge;
    private static string _leadingBulgeStyle = "TMDWU";
    private static float _thickness;
    private static SliderModel _thicknessTipScaleSlider;
    private static float _thicknessTip;
    private static float _thicknessTipScalar = 1f;

    public static bool Visible
    {
        get => _inspectorPanel != null;
        set
        {
            if (value)
            {
                if (_inspectorPanel != null)
                {
                    Debug.Log("Returning in visible?");
                    return;
                }
                Debug.Log("Opened Panel");
                RefreshInspectorPanel(true);
            }
            else ClosePanel();
        }
    }

    public static void ToggleInspectorPanel()
    {
        Debug.Log("Toggle Inpector");
        Visible = !Visible;
    }

    public static void ClosePanel()
    {
        Debug.Log("Closed Panel");
        if (_inspectorPanel == null)
            return;
        _currentOffset = new Vector2?(_inspectorPanel.Position);
        _inspectorPanel.Close();
        _inspectorPanel = null;
        Game.Instance.Designer.SelectedPartChanged -= OnSelectedPartChanged;
    }

    private static void OnSelectedPartChanged(IPartScript oldPart, IPartScript newPart)
    {
        Debug.Log("Selected Part Changed");
        if (Game.Instance.Designer.SelectedPart != null && newPart.GetModifier<WingScript>() != null)
        {
            if (oldPart != null) oldPart.GetModifier<WingScript>().WingUpdated -= SelectedWingUpdated;
            Debug.Log("is Wing");
            RefreshInspectorPanel(true, true);
        }
        else RefreshInspectorPanel(true, false);
    }

    private static void RefreshInspectorPanel(bool immediate, bool isWingSelected = false)
    {
        Debug.Log("Refresh Inspector");
        if (!immediate)
        {
            _refreshPending = true;
        }
        else
        {
            _refreshPending = false;
            ClosePanel();
            Game.Instance.Designer.SelectedPartChanged += OnSelectedPartChanged;
            InspetorCreationInfo();
            InspectorModel inspectorModel = new InspectorModel("AirfoilEditor", "Airfoil Editor");
            if ((Game.Instance.Designer.SelectedPart != null && Game.Instance.Designer.SelectedPart.GetModifier<WingScript>() != null) || isWingSelected)
            {
                Game.Instance.Designer.SelectedPart.GetModifier<WingScript>().WingUpdated += SelectedWingUpdated;
                Debug.Log("Wing Selected");
                UpdatePropertyData();
                /*
                inspectorModel.Add(new TextModel("(might)Work in Progress"));
                _thicknessSlider = inspectorModel.Add(new SliderModel("Root Thickness", () => _thickness, x => _thickness = Mathf.Clamp01(x)));
                _thicknessSlider.ValueChangedByUserInput += InspectorSliderUpdate;
                
                _thicknessSlider.MinValue = 0.02f;
                inspectorModel.Add(new TextModel("Tip Thickness", () => Units.GetDistanceString(_thicknessTip)));
                
                _thicknessTipScaleSlider = inspectorModel.Add(new SliderModel("Tip Thickness Scale", () => _thicknessTipScalar, x => _thicknessTipScalar = Mathf.Clamp(x, 0, 2)));
                _thicknessTipScaleSlider.ValueChangedByUserInput += InspectorSliderUpdate;
                _thicknessTipScaleSlider.MinValue = 0.02f;
                _thicknessTipScaleSlider.MaxValue = 2;*/

                _curveLengthSlider = inspectorModel.Add(new SliderModel("Curve Length", () => _curveLength, x => _curveLength = Mathf.Clamp01(x)));
                _curveLengthSlider.ValueChangedByUserInput += InspectorSliderUpdate;

                _thicknessSlider = inspectorModel.Add(new SliderModel("Root Thickness", () => _thickness, x => _thickness = Mathf.Clamp01(x)));
                _thicknessSlider.ValueChangedByUserInput += InspectorSliderUpdate;

                _thicknessOffsetStyleText = inspectorModel.Add(new TextModel("Thickness Offset Style", () => _thicknessOffsetStyle));
                _thicknessOffsetSlider = inspectorModel.Add(new SliderModel("Thickness Offset", () => _thicknessOffset, x => _thicknessOffset = Mathf.Clamp01(x)));
                _thicknessOffsetSlider.MaxValue = .99f;
                _thicknessOffsetSlider.ValueChangedByUserInput += InspectorSliderUpdate;

                _thicknessDeltaStyleText = inspectorModel.Add(new TextModel("Thickness Delta Style", () => _thicknessDeltaStyle));
                _thicknessDeltaSlider = inspectorModel.Add(new SliderModel("Thickness Delta", () => _thicknessDelta, x => _thicknessDelta = Mathf.Clamp01(x)));
                _thicknessDeltaSlider.MaxValue = .99f;
                _thicknessDeltaSlider.ValueChangedByUserInput += InspectorSliderUpdate;

                _leadingBulgeStyleText = inspectorModel.Add(new TextModel("Leading Bulge Style", () => _leadingBulgeStyle));
                _leadingBulgeSlider = inspectorModel.Add(new SliderModel("Leading Bulge", () => _leadingBulge, x => _leadingBulge = Mathf.Clamp(x, 0.5f, 4)));
                _leadingBulgeSlider.MinValue = 0.5f;
                _leadingBulgeSlider.MaxValue = 4f;
                _leadingBulgeSlider.ValueChangedByUserInput += InspectorSliderUpdate;
            }
            else inspectorModel.Add(new TextModel("No part selected"));

            _inspectorPanel = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel, creationInfo);
            _inspectorPanel.CloseButtonClicked += new InspectorPanelDelegate(OnInspectorPanelCloseButtonClicked);
        }
    }

    private static void InspectorSliderUpdate(ItemModel model, string name, bool finished)
    {
        WingData wing = Game.Instance.Designer.SelectedPart.GetModifier<WingScript>().Data;

        if (name == "Root Thickness") Traverse.Create(wing).Field("_thickness").SetValue(_thicknessSlider.Value);
        else if (name == "Tip Thickness Scale") Traverse.Create(wing).Field("_thicknessTip").SetValue(_thicknessTipScaleSlider.Value * Traverse.Create(wing).Field("_thicknessTipAuto").GetValue<float>());
        else if (name == "Curve Length") Traverse.Create(wing).Field("_curveLength").SetValue(_curveLengthSlider.Value);
        else if (name == "Thickness Offset") Traverse.Create(wing).Field("_thicknessOffset").SetValue(_thicknessOffsetSlider.Value);
        else if (name == "Thickness Delta") Traverse.Create(wing).Field("_thicknessDelta").SetValue(_thicknessDeltaSlider.Value);
        else if (name == "Leading Bulge") Traverse.Create(wing).Field("_leadingBulge").SetValue(_leadingBulgeSlider.Value);

        wing.Script.UpdateWingShape();
    }

    private static void SelectedWingUpdated(WingScript wing)
    {
        UpdatePropertyData();
    }
    internal static void UpdatePropertyData()
    {
        if (Game.Instance.Designer.SelectedPart != null && Game.Instance.Designer.SelectedPart.GetModifier<WingScript>() != null)
        {
            Debug.Log("Should be updating the values");
            WingData selectedPart = Game.Instance.Designer.SelectedPart.GetModifier<WingScript>().Data;
            //_thicknessTipScalar = selectedPart.ThicknessTip / Traverse.Create(selectedPart).Field("_thicknessTipAuto").GetValue<float>();
            //_thicknessTip = selectedPart.ThicknessTip * _thicknessTipScalar;
            _curveLength = selectedPart.CurveLength;
            _thickness = selectedPart.Thickness;
            _thicknessOffset = selectedPart.ThicknessOffset;
            _thicknessOffsetStyle = Traverse.Create(selectedPart).Field("_thicknessOffsetStyle").GetValue<float>().ToString();
            _thicknessDelta = selectedPart.ThicknessDelta;
            _thicknessDeltaStyle = Traverse.Create(selectedPart).Field("_thicknessDeltaStyle").GetValue<float>().ToString();
            _leadingBulge = selectedPart.LeadingBulge;
            _leadingBulgeStyle = Traverse.Create(selectedPart).Field("_leadingBulgeStyle").GetValue<float>().ToString();
        }
    }

    public static void InspetorCreationInfo()
    {
        Debug.Log("Inspector Creation Info");
        creationInfo = new();
        creationInfo.StartPosition = InspectorPanelCreationInfo.InspectorStartPosition.Center;
        creationInfo.Resizable = true;
        creationInfo.StartOffset = !_currentOffset.HasValue ? new Vector2(-60f, 0.0f) : _currentOffset.Value;
        creationInfo.PanelWidth = 300;
        creationInfo.PanelMaxHeight = 0.6f;
    }

    private static void OnInspectorPanelCloseButtonClicked(IInspectorPanel panel) => ClosePanel();

}