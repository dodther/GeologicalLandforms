using System;
using System.Collections.Generic;
using System.Globalization;
using Verse;

namespace GeologicalLandforms;

public class GenNoiseConfig : IExposable
{
    public Dictionary<NoiseType, GenNoiseStack> NoiseStacks = new();

    public float ThresholdShallow = 0.1f;
    public float ThresholdBeach = 0.45f;

    public string TerrainDeep;
    public string TerrainShallow;
    public string TerrainBeach;
    
    public float HillModifierEffectiveness = 1f;
    public float MaxElevationIfWaterCovered;
    
    public GenNoiseConfig() {}
    
    public void ExposeData()
    {
        Scribe_Collections.Look(ref NoiseStacks, "NoiseStacks", LookMode.Value, LookMode.Deep);
        Scribe_Values.Look(ref ThresholdShallow, "ThresholdShallow", 0.1f);
        Scribe_Values.Look(ref ThresholdBeach, "ThresholdBeach", 0.45f);
        Scribe_Values.Look(ref TerrainDeep, "TerrainDeep");
        Scribe_Values.Look(ref TerrainShallow, "TerrainShallow");
        Scribe_Values.Look(ref TerrainBeach, "TerrainBeach");
        Scribe_Values.Look(ref HillModifierEffectiveness, "HillModifierEffectiveness", 1f);
        Scribe_Values.Look(ref MaxElevationIfWaterCovered, "MaxElevationIfWaterCovered");
    }

    public void DoSettingsWindowContents(Listing_Standard listingStandard)
    {
        Settings.Dropdown(listingStandard, "Edit settings for NoiseType: ", Settings.SelectedNoiseType, e => Settings.SelectedNoiseType = e, 200f);
        listingStandard.Gap(18f);

        if (Settings.SelectedNoiseType == NoiseType.Coast)
        {
            Settings.CenteredLabel(listingStandard, "ThresholdShallow", Math.Round(ThresholdShallow, 2).ToString(CultureInfo.InvariantCulture));
            ThresholdShallow = listingStandard.Slider(ThresholdShallow, -1f, 1f);
            Settings.CenteredLabel(listingStandard, "ThresholdBeach", Math.Round(ThresholdBeach, 2).ToString(CultureInfo.InvariantCulture));
            ThresholdBeach = listingStandard.Slider(ThresholdBeach, -1f, 1f);
            listingStandard.Gap();
            
            TerrainDeep = Settings.TextEntry(listingStandard, "TerrainDeep", TerrainDeep, 200f);
            TerrainShallow = Settings.TextEntry(listingStandard, "TerrainShallow", TerrainShallow, 200f);
            TerrainBeach = Settings.TextEntry(listingStandard, "TerrainBeach", TerrainBeach, 200f);
            listingStandard.Gap(18f);
        }

        if (Settings.SelectedNoiseType == NoiseType.Elevation)
        {
            Settings.CenteredLabel(listingStandard, "HillModifierEffectiveness", Math.Round(HillModifierEffectiveness, 2).ToString(CultureInfo.InvariantCulture));
            HillModifierEffectiveness = listingStandard.Slider(HillModifierEffectiveness, 0f, 2f);
            Settings.CenteredLabel(listingStandard, "MaxElevationIfWaterCovered", Math.Round(MaxElevationIfWaterCovered, 2).ToString(CultureInfo.InvariantCulture));
            MaxElevationIfWaterCovered = listingStandard.Slider(MaxElevationIfWaterCovered, 0f, 2f);
            listingStandard.Gap(18f);
        }
        
        NoiseStacks.TryGetValue(Settings.SelectedNoiseType, out GenNoiseStack noiseStack);
        if (noiseStack == null) NoiseStacks.Add(Settings.SelectedNoiseType, noiseStack = new GenNoiseStack(Settings.SelectedNoiseType));
        noiseStack.DoSettingsWindowContents(listingStandard, Settings.SelectedNoiseType);
    }

    public enum NoiseType
    {
        Coast, Elevation, Fertility
    }
}