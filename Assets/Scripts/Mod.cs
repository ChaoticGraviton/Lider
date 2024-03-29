namespace Assets.Scripts
{
    using HarmonyLib; // Including the Harmony Library
    using ModApi.Scenes.Events;
    using System;
    using UnityEngine;

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