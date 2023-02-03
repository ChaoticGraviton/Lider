namespace Assets.Scripts
{
    using UnityEngine;
    using HarmonyLib; // Including the Harmony Library
    using Assets.Scripts.Design;
    using UnityEngine.UI;
    using TMPro;
    using ModApi.Math;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
    using Assets.Scripts.Design.Tools.Fuselage;
    using System.Collections.Generic;
    using ModApi.Craft.Parts;
    using System.Linq;

    public class Mod : ModApi.Mods.GameMod
    {
        private Mod() : base()
        {
        }
        public static Mod Instance { get; } = GetModInstance<Mod>();

        protected override void OnModInitialized()
        {
            Harmony harmony = new Harmony("CG.CR.Lider");
            harmony.PatchAll();
        }
    }
}
