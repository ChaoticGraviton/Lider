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
        public BoolSetting ClampLipCompensateEnabled { get; private set; }
        public BoolSetting WallThicknessEnabled { get; private set; }
        protected override void InitializeSettings()
        {
            DeformationsEnabled = this.CreateBool("Advanced Deformation Sliders")
                .SetDescription("Enables remade sliders for pinch and slant")
                .SetDefault(true);
            ClampDistancesEnabled = this.CreateBool("Fuselage Clamp Sliders")
                .SetDescription("Enables sliders for fuselage clamping")
                .SetDefault(true);/*
            ClampLipCompensateEnabled = this.CreateBool("Lip Compensation")
                .SetDescription("Automatically compensates 'lips' that can appear when clamping")
                .SetDefault(false);*/
            ClampDuplicateFacesEnabled = this.CreateBool("Mirror Faces")
                .SetDescription("Mirrors clamp values to the opposing face")
                .SetDefault(false);
            WallThicknessEnabled = this.CreateBool("Fuselage Wall Thickness Sliders")
                .SetDescription("Enable sliders for fuselage wall thickness")
                .SetDefault(true);
        }
    }
}