namespace Assets.Scripts
{
    using ModApi.Settings.Core;

    public class ModSettings : SettingsCategory<ModSettings>
    {
        private static ModSettings _instance;

        public ModSettings() : base("Lider") { }

        public static ModSettings Instance => _instance ?? (_instance = Game.Instance.Settings.ModSettings.GetCategory<ModSettings>());
        public BoolSetting DeformationsEnabled { get; private set; }
        public BoolSetting ClampDistancesEnabled { get; private set; }
        public BoolSetting ClampDuplicateFacesEnabled { get; private set; }
        //public BoolSetting ClampLipCompensateEnabled { get; private set; }
        public BoolSetting WallThicknessEnabled { get; private set; }
        public BoolSetting WallThicknessAdaption { get; private set; }
        protected override void InitializeSettings()
        {
            DeformationsEnabled = CreateBool("Advanced Deformation Sliders")
                .SetDescription("Enables overhauled sliders for Pinch and Slant")
                .SetDefault(true);
            ClampDistancesEnabled = CreateBool("Fuselage Clamp Sliders")
                .SetDescription("Enables sliders for Fuselage Clamping")
                .SetDefault(true);/*
            ClampLipCompensateEnabled = this.CreateBool("Lip Compensation")
                .SetDescription("Automatically compensates 'lips' that can appear when clamping")
                .SetDefault(false);*/
            ClampDuplicateFacesEnabled = CreateBool("Mirror Faces")
                .SetDescription("Mirrors Fuselage Clamp values to the opposing face")
                .SetDefault(false);
            WallThicknessEnabled = CreateBool("Fuselage Wall Thickness Sliders")
                .SetDescription("Enable sliders for fuselage Wall Thickness")
                .SetDefault(true);
            WallThicknessAdaption = CreateBool("Wall Thickness Adaption")
                .SetDescription("Enables Fuselage Adaption for Wall Thickness")
                .SetDefault(false)
                .AddWarningOnEnabled("There are known issues with Wall Thickness Adaption (If you leave this option disabled, you can manually update the thickness values and avoid the issue)");
        }
    }
}