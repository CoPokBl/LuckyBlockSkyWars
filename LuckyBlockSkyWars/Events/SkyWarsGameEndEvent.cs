using ManagedServer.Entities.Types;
using ManagedServer.Worlds;

namespace LuckyBlockSkyWars.Events;

public class SkyWarsGameEndEvent : ISkyWarsGameEvent {
    public required SkyWarsGame Game { get; init; }
    public required PlayerEntity? Winner { get; init; }
    
    public World World {
        get => Game.World;
        init { }
    }
}
