using System.Collections.Generic;
using GeologicalLandforms.GraphEditor;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeologicalLandforms;

public interface IWorldTileInfo
{
    public Landform Landform { get; }
    public Topology Topology { get; }
    public Rot4 LandformDirection  { get; }
    
    public MapParent WorldObject { get; }
    public BiomeDef Biome { get; }
    
    public bool HasOcean { get; }
    
    public List<BorderingBiome> BorderingBiomes { get; }

    public Hilliness Hilliness { get; }
    public float Elevation { get; }
    public float Temperature { get; }
    public float Rainfall { get; }
    public float Swampiness { get; }
    
    public RiverDef MainRiver { get; }
    public float MainRiverAngle { get; }
    
    public RoadDef MainRoad { get; }
    public float MainRoadAngle { get; }
    
    public readonly struct BorderingBiome
    {
        public readonly BiomeDef Biome;
        public readonly float Angle;

        public BorderingBiome(BiomeDef biome, float angle)
        {
            Biome = biome;
            Angle = angle;
        }
    }
}