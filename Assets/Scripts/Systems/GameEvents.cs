using UnityEngine;

namespace LivingAbyss.Systems
{
    // Typed event structs for EventBus — no coupling between systems
    public struct SymbChangedEvent      { public float OldValue; public float NewValue; }
    public struct SymbZoneEnteredEvent  { public SymbiosisZone Zone; }
    public struct SymbDamageEvent       { public float Amount; }

    public struct PlayerMoveEvent       { public Vector2 Velocity; }
    public struct PlayerSkillUseEvent   { public string SkillId; }
    public struct PlayerFloorEnterEvent { public int Floor; }

    public struct TerrainPulseEvent     { public Vector2Int TilePos; }
    public struct TerrainTileMutateEvent { public Vector2Int TilePos; }
    public struct TerrainPathwayOpenEvent { public string PathwayId; }

    public struct PlayerDeathEvent      { }
    public struct PlayerRespawnEvent    { public Vector2 Position; }
}
