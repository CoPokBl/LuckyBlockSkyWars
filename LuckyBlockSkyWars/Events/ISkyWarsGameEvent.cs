using ManagedServer.Events.Types;

namespace LuckyBlockSkyWars.Events;

public interface ISkyWarsGameEvent : IWorldEvent {
    public SkyWarsGame Game { get; init; }
}
