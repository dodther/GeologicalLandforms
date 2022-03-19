using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using GeologicalLandforms.GraphEditor;
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GeologicalLandforms.Patches;

[HarmonyPatch(typeof (WITab_Terrain), "FillTab")]
internal static class RimWorld_WITab_Terrain
{
    private static readonly MethodInfo Method_AppendWithComma = AccessTools.Method(typeof(GenText), "AppendWithComma");
    private static readonly MethodInfo Method_LabelDouble = AccessTools.Method(typeof(Listing_Standard), "LabelDouble");
    private static readonly MethodInfo Method_GetSpecialFeatures = AccessTools.Method(typeof(RimWorld_WITab_Terrain), "GetSpecialFeatures");
    
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var foundAWC = false;
        var patched = false;

        foreach (CodeInstruction instruction in instructions)
        {
            if (foundAWC && !patched)
            {
                if (instruction.opcode == OpCodes.Ldc_I4_0)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_M1);
                    continue;
                }
                
                if (instruction.Calls(Method_LabelDouble))
                {
                    yield return new CodeInstruction(OpCodes.Call, Method_GetSpecialFeatures);
                    patched = true;
                    continue;
                }
            }
            
            if (instruction.Calls(Method_AppendWithComma))
            {
                foundAWC = true;
            }
            
            yield return instruction;
        }
        
        if (patched == false)
            Log.Error("Failed to patch RimWorld_WITab_Terrain");
    }

    private static void GetSpecialFeatures(Listing_Standard listingStandard, string str0, string str1, string str2 = null)
    {
        StringBuilder sb = new();
        
        int tileId = Find.WorldSelector.selectedTile;
        IWorldTileInfo worldTileInfo = WorldTileInfo.GetWorldTileInfo(tileId);

        if (worldTileInfo.Landform != null)
        {
            string append = worldTileInfo.Landform.TranslatedName;
            
            if (worldTileInfo.Landform.DisplayNameHasDirection)
            {
                if (worldTileInfo.Landform.IsCornerVariant)
                {
                    append = TranslateDoubleRot4(worldTileInfo.LandformDirection) + " " + append;
                }
                else
                {
                    append = TranslateRot4(worldTileInfo.LandformDirection) + " " + append;
                }
            }
            
            sb.AppendWithComma(append);
        }
        
        if (Find.World.HasCaves(tileId))
        {
            sb.AppendWithComma("HasCaves".Translate());
        }

        if (sb.Length > 0)
            listingStandard.LabelDouble("SpecialFeatures".Translate(), sb.ToString().CapitalizeFirst());

        listingStandard.Gap();
        Rect rect = listingStandard.GetRect(28f);
        if (!listingStandard.BoundingRectCached.HasValue || rect.Overlaps(listingStandard.BoundingRectCached.Value))
        {
            if (Widgets.ButtonText(rect, "GeologicalLandforms.WorldMap.FindLandform".Translate()))
            {
                List<FloatMenuOption> options = LandformManager.Landforms.Values.Select(e => 
                    new FloatMenuOption(e.TranslatedNameForSelection.CapitalizeFirst(), () => FindLandform(e))).ToList();
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
        
        listingStandard.Gap();
        if (Prefs.DevMode)
        {
            listingStandard.LabelDouble("GeologicalLandforms.WorldMap.Topology".Translate(), worldTileInfo.Topology.ToString());
            listingStandard.LabelDouble("GeologicalLandforms.WorldMap.TopologyDirection".Translate(), worldTileInfo.LandformDirection.ToStringHuman());
            listingStandard.LabelDouble("GeologicalLandforms.WorldMap.Swampiness".Translate(), worldTileInfo.Swampiness.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void FindLandform(Landform landform)
    {
        int tileId = Find.WorldSelector.selectedTile;
        WorldGrid grid = Find.WorldGrid;

        HashSet<int> tested = new();
        HashSet<int> pending = new() {tileId};
        List<int> nb = new();

        for (int i = 0; i < ModInstance.Settings.MaxLandformSearchRadius; i++)
        {
            List<int> copy = pending.ToList();
            pending.Clear();
            foreach (var p in copy)
            {
                IWorldTileInfo tileInfo = WorldTileInfo.GetWorldTileInfo(p);
                if (tileInfo.Landform == landform)
                {
                    CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(p));
                    Find.WorldSelector.selectedTile = p;
                    float dist = grid.ApproxDistanceInTiles(tileId, p);
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        "GeologicalLandforms.WorldMap.FindLandformSuccess".Translate() + Math.Round(dist, 2)));
                    return;
                }

                tested.Add(p);
                
                nb.Clear();
                grid.GetTileNeighbors(p, nb);
                foreach (var nTile in nb)
                {
                    if (tested.Contains(nTile)) continue;
                    pending.Add(nTile);
                }
            }
        }
        
        Find.WindowStack.Add(new Dialog_MessageBox(
            "GeologicalLandforms.WorldMap.FindLandformFail".Translate() + ModInstance.Settings.MaxLandformSearchRadius));
    }

    private static string TranslateRot4(Rot4 rot4)
    {
        return ("GeologicalLandforms.Rot4." + rot4.AsInt).Translate();
    }
    
    private static string TranslateDoubleRot4(Rot4 rot4)
    {
        return ("GeologicalLandforms.Rot4.Double." + rot4.AsInt).Translate();
    }
}