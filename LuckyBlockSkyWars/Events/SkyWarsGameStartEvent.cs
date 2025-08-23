using ManagedServer.Worlds;

namespace LuckyBlockSkyWars.Events;

public class SkyWarsGameStartEvent : ISkyWarsGameEvent {
    public required SkyWarsGame Game { get; init; }
    
    public World World {
        get => Game.World;
        init { }
    }
}
