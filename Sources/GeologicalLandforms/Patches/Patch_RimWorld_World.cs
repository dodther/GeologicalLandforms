using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(World))]
internal static class Patch_RimWorld_World
{
    public static int LastKnownInitialWorldSeed { get; private set; }

    public static int StableSeedForTile(int tileId) => Gen.HashCombineInt(LastKnownInitialWorldSeed, tileId);

    [HarmonyPostfix]
    [HarmonyPatch("ConstructComponents")]
    private static void ConstructComponents(WorldInfo ___info, World __instance)
    {
        LastKnownInitialWorldSeed = ___info.Seed;

        WorldTileInfo.RemoveCache();

        var landformData = __instance.GetComponent<LandformData>();
        var lastWorld = Patch_RimWorld_WorldGenStep_Terrain.LastWorld;
        var biomeTransitions = Patch_RimWorld_WorldGenStep_Terrain.BiomeTransitions;

        if (biomeTransitions != null && landformData != null && lastWorld == __instance)
        {
            landformData.SetBiomeTransitions(biomeTransitions);
            Patch_RimWorld_WorldGenStep_Terrain.BiomeTransitions = null;
            Patch_RimWorld_WorldGenStep_Terrain.LastWorld = null;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("FinalizeInit")]
    private static void FinalizeInit(World __instance)
    {
        WorldTileInfo.CreateNewCache();
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(World.HasCaves))]
    [HarmonyAfter("zylle.MapDesigner")]
    private static bool HasCaves(ref bool __result, int tile)
    {
        var worldTileInfo = WorldTileInfo.Get(tile);
        var landform = worldTileInfo.Landforms?.LastOrDefault(l => !l.IsLayer);
        if (landform == null) return true;

        int seed = worldTileInfo.MakeSeed(8266);
        __result = Rand.ChanceSeeded(landform.WorldTileReq.CaveChance, seed);
        return false;
    }
}
