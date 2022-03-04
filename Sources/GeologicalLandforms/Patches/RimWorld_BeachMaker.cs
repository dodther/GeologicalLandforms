using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Noise;

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof (BeachMaker))]
internal static class RimWorld_BeachMaker
{
    private static WorldTileInfo _worldTileInfo;
    private static GenNoiseConfig _noiseConfig;

    private static TerrainDef _terrainDeep;
    private static TerrainDef _terrainShallow;
    private static TerrainDef _terrainBeach;
    
    [HarmonyPatch(nameof(BeachMaker.Init))]
    private static bool Prefix(ref ModuleBase ___beachNoise, Map map)
    {
        _worldTileInfo = WorldTileInfo.GetWorldTileInfo(map.Tile);
        _noiseConfig = _worldTileInfo.Landform?.GenConfig;
        if (_noiseConfig == null) return true;
        
        Log.Message("Initiating BeachMaker config for landform " + _worldTileInfo.Topology);
        
        _terrainDeep = _worldTileInfo.HasOcean ? TerrainDefOf.WaterOceanDeep : TerrainDefOf.WaterDeep;
        _terrainShallow = _worldTileInfo.HasOcean ? TerrainDefOf.WaterOceanShallow : TerrainDefOf.WaterShallow;
        _terrainBeach = _worldTileInfo.Biome != BiomeDefOf.SeaIce ? TerrainDefOf.Sand : TerrainDefOf.Ice;
        
        _terrainDeep = ParseTerrain(_noiseConfig.TerrainDeep, _terrainDeep);
        _terrainShallow = ParseTerrain(_noiseConfig.TerrainShallow, _terrainShallow);
        _terrainBeach = ParseTerrain(_noiseConfig.TerrainBeach, _terrainBeach);
        
        GenNoiseStack noiseStack = _noiseConfig.NoiseStacks.TryGetValue(GenNoiseConfig.NoiseType.Coast);
        ___beachNoise = noiseStack != null && (!(noiseStack.BaseBias >= 1f) || !(noiseStack.BaseScale <= 0f))
            ? noiseStack.BuildModule(_worldTileInfo, map, "BeachMaker", QualityMode.Medium)
            : null;

        return false;
    }
    
    [HarmonyPatch(nameof(BeachMaker.BeachTerrainAt))]
    private static bool Prefix(ref TerrainDef __result, ref ModuleBase ___beachNoise, IntVec3 loc, BiomeDef biome)
    {
        if (_noiseConfig == null || ___beachNoise == null) return true;
        
        float noiseValue = ___beachNoise.GetValue(loc);
        if (noiseValue < _noiseConfig.ThresholdShallow)
            __result = _terrainDeep ?? (_worldTileInfo.HasOcean ? TerrainDefOf.WaterOceanDeep : TerrainDefOf.WaterDeep);
        else if (noiseValue < _noiseConfig.ThresholdBeach)
            __result = _terrainShallow ?? (_worldTileInfo.HasOcean ? TerrainDefOf.WaterOceanShallow : TerrainDefOf.WaterShallow);
        else if (noiseValue < 1f)
            __result = _terrainBeach ?? (biome != BiomeDefOf.SeaIce ? TerrainDefOf.Sand : TerrainDefOf.Ice);
        else
            __result = null;

        return false;
    }

    private static TerrainDef ParseTerrain(string str, TerrainDef fallback)
    {
        if (string.IsNullOrEmpty(str)) return fallback;
        
        if (str.Equals("$water=deep"))
        {
            return _worldTileInfo.HasOcean ? TerrainDefOf.WaterOceanDeep : TerrainDefOf.WaterDeep;
        }
        
        if (str.Equals("$water=shallow"))
        {
            return _worldTileInfo.HasOcean ? TerrainDefOf.WaterOceanShallow : TerrainDefOf.WaterShallow;
        }

        if (str.StartsWith("$fert=") && float.TryParse(str.Substring(6), out float fert))
        {
            TerrainDef fertTerrain = TerrainThreshold.TerrainAtValue(_worldTileInfo.Biome.terrainsByFertility, fert);
            if (fertTerrain == null) return fallback;
            return fertTerrain;
        }

        TerrainDef terrainDef = DefDatabase<TerrainDef>.GetNamed(_noiseConfig.TerrainDeep, false);
        if (terrainDef == null) return fallback;
        return terrainDef;
    }
}