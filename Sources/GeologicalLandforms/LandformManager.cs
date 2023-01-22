using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using NodeEditorFramework;
using NodeEditorFramework.IO;
using Verse;

namespace GeologicalLandforms.GraphEditor;

public static class LandformManager
{
    public const int CurrentVersion = 1;
    
    public static string LandformsDir(string loadFolder, int version) => Path.Combine(loadFolder, "Landforms-v" + version);
    public static string CustomLandformsDir(int version) => Path.Combine(GenFilePaths.ConfigFolderPath, "CustomLandforms-v" + version);
    
    private static Dictionary<string, ModContentPack> _mcpLandformDirs = new();
    
    private static Dictionary<string, Landform> _landforms = new();
    public static IReadOnlyDictionary<string, Landform> Landforms => _landforms;

    private static ImportExportFormat IOFormat => ImportExportManager.ParseFormat("XML");

    public static void InitialLoad()
    {
        NodeEditor.checkInit(false);
        
        Directory.CreateDirectory(CustomLandformsDir(CurrentVersion));

        var landformSources = new HashSet<string>();
        _mcpLandformDirs = new();
        
        foreach (var mcp in LoadedModManager.RunningMods)
        {
            if (mcp?.RootDir == null) continue;

            foreach (var dir in mcp.foldersToLoadDescendingOrder
                         .Select(loadFolder => LandformsDir(loadFolder, CurrentVersion))
                         .Reverse().Where(Directory.Exists))
            {
                _mcpLandformDirs.Add(dir, mcp);
                landformSources.Add(mcp.Name);
            }
        }
        
        Log.ResetMessageCount();
        GeologicalLandformsAPI.Logger.Log("Found landform data in the following mods: " + landformSources.Join());
        
        _landforms = LoadAll();
    }

    public static Dictionary<string, Landform> LoadAll(string fileFilter = "*", bool includeCustom = true)
    {
        var mcpLandforms = _mcpLandformDirs.Aggregate<KeyValuePair<string, ModContentPack>, Dictionary<string,Landform>>(null, 
            (current, source) => LoadLandformsFromDirectory(source.Key, current, fileFilter, source.Value));
        
        foreach (var landform in mcpLandforms.Values)
        {
            landform.Manifest.IsEdited = false;
            landform.Manifest.IsCustom = false;
        }
        
        if (!includeCustom) return mcpLandforms;
        
        var mergedLandforms = LoadLandformsFromDirectory(CustomLandformsDir(CurrentVersion), mcpLandforms, fileFilter);
        var upgradableLandforms = mcpLandforms.Values
            .Where(lf => mergedLandforms[lf.Id].Manifest.RevisionVersion < lf.Manifest.RevisionVersion)
            .Select(lf => mergedLandforms[lf.Id]).ToList();

        int customCount = mergedLandforms.Values.Count(l => l.IsCustom);
        int editedCount = mergedLandforms.Values.Count(l => l.Manifest.IsEdited);
        
        GeologicalLandformsAPI.Logger.Log("Loaded " + mergedLandforms.Count + " landforms of which " + editedCount + " are edited and " + customCount + " are custom.");

        if (upgradableLandforms.Count > 0) GeologicalLandformsAPI.LunarAPI.LifecycleHooks.DoOnceOnMainMenu(() =>
        {
            string msg = "GeologicalLandforms.LandformManager.LandformUpgrade".Translate() + "\n";
            msg = upgradableLandforms.Aggregate(msg, (current, lf) => current + ("\n" + lf.TranslatedNameForSelection.CapitalizeFirst()));

            void UpgradeAction()
            {
                foreach (var lf in upgradableLandforms) Reset(lf);
                SaveAllEdited();
            }
            
            void KeepAction()
            {
                foreach (var lf in upgradableLandforms) lf.Manifest.RevisionVersion += 1;
                SaveAllEdited();
            }

            Find.WindowStack.Add(new Dialog_MessageBox(msg, 
                "GeologicalLandforms.LandformManager.LandformUpgradeYes".Translate(), UpgradeAction,
                "GeologicalLandforms.LandformManager.LandformUpgradeNo".Translate(), KeepAction));
        });
        
        return mergedLandforms;
    }
    
    public static void SaveAllEdited()
    {
        SaveLandformsToDirectory(CustomLandformsDir(CurrentVersion), _landforms);
    }

    public static Landform FindById(string id)
    {
        return Landforms.TryGetValue(id, out var landform) ? landform : null;
    }

    public static Landform Duplicate(Landform landform)
    {
        SaveAllEdited();
        
        var duplicate = LoadAll("*" + landform.Id).TryGetValue(landform.Manifest.Id);
        duplicate ??= LoadAll().TryGetValue(landform.Manifest.Id);
        if (duplicate == null) return null;
        
        string newId = landform.Manifest.Id + "Copy";
        while (Landforms.ContainsKey(newId))
        {
            newId += "Copy";
        }
        
        duplicate.Manifest.IsCustom = true;
        duplicate.Manifest.IsEdited = true;
        duplicate.Manifest.TimeCreated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        duplicate.Manifest.Id = newId;
        
        _landforms.Add(newId, duplicate);
        return duplicate;
    }
    
    public static void Rename(Landform landform, string newId)
    {
        while (_landforms.ContainsKey(newId))
        {
            newId = "New" + newId;
        }
        
        if (landform.Manifest.Id != null) _landforms.Remove(landform.Manifest.Id);
        landform.Manifest.Id = newId;
        _landforms.Add(landform.Manifest.Id, landform);
    }

    public static void Delete(Landform landform)
    {
        _landforms.Remove(landform.Manifest.Id);
    }
    
    public static Landform Reset(Landform landform)
    {
        var reset = LoadAll("*" + landform.Id, false).TryGetValue(landform.Id);
        reset ??= LoadAll("*", false).TryGetValue(landform.Id);
        if (reset == null) return landform;

        _landforms[landform.Id] = reset;
        return reset;
    }
    
    public static void ResetAll()
    {
        _landforms = LoadAll("*", false);
    }
    
    public static Dictionary<string, Landform> LoadLandformsFromDirectory(string directory, Dictionary<string, Landform> fallback, string fileFilter = "*", ModContentPack source = null)
    {
        Dictionary<string, Landform> landforms = new(fallback ?? new());
        
        foreach (string file in Directory.GetFiles(directory, fileFilter + ".xml"))
        {
            Landform landform = null;
            
            try
            {
                if (File.Exists(file))
                {
                    landform = (Landform) ImportExportManager.ImportCanvas(IOFormat, file);
                }

                if (landform != null && landform.Id != null)
                {
                    if (landforms.ContainsKey(landform.Id)) landforms[landform.Id] = landform;
                    else landforms.Add(landform.Id, landform);
                    landform.ModContentPack = source;
                }
            }
            catch (Exception ex)
            {
                GeologicalLandformsAPI.Logger.Warn($"Caught exception while loading landform from file {file}.", ex);
            }
        }

        if (landforms.Count == 0 && fallback != null)
        {
            var landform = NodeCanvas.CreateCanvas<Landform>();
            landform.Manifest.Id = "NewLandform";
            landform.Manifest.DisplayName = "NewLandform";
            landform.Manifest.IsCustom = true;
            landforms.Add(landform.Id, landform);
        }

        return landforms;
    }
    
    public static void SaveLandformsToDirectory(string directory, Dictionary<string, Landform> landforms)
    {
        var existing = Directory.GetFiles(directory, "*.xml").ToList();

        foreach (var pair in landforms.Where(p => p.Value.Manifest.IsEdited || p.Value.Manifest.IsCustom))
        {
            string file = Path.Combine(directory, "Landform" + pair.Key + ".xml");
            var landform = pair.Value;
            existing.Remove(file);
            
            ImportExportManager.ExportCanvas(landform, IOFormat, file);
        }
        
        foreach (string file in existing)
        {
            File.Delete(file);
        }
    }
}