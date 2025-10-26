using LuckyBlockSkyWars.Events;
using LuckyBlockSkyWars.Features;
using LuckyBlockSkyWars.Maps;
using ManagedServer;
using ManagedServer.Entities.Types;
using ManagedServer.Events;
using ManagedServer.Features;
using ManagedServer.Features.Basic;
using ManagedServer.Features.Bundles;
using ManagedServer.Features.Impl;
using ManagedServer.Viewables;
using ManagedServer.Worlds;
using Minecraft.Data.Generated;
using Minecraft.Packets.Config.ClientBound;
using Minecraft.Schemas;
using Minecraft.Schemas.Chunks;
using Minecraft.Schemas.Items;
using Minecraft.Schemas.Vec;
using Minecraft.Text;

namespace LuckyBlockSkyWars;

public class SkyWarsGame(ManagedMinecraftServer server, SkyWarsGame.Config config, PlayerEntity[] players, Action gameEndCallback) {
    public const string GameDimension = "skywars:game";
    
    public readonly List<PlayerEntity> RemainingPlayers = [];
    public World World { get; private set; } = null!;
    public bool HasEnded { get; private set; }
    public PlayerEntity[] Players { get; } = players;
    
    private static FeatureBundle SkyWarsFeatures => new(
        new SkyWarsChestsFeature(),
        new DropItemsOnGroundFeature(),
        new ItemPickupFeature(),
        new LuckyBlocksFeature(),
        new SkyWarsItemsFeature(),
        new RespawnFeature(),
        new ArmourSlotEnforcementFeature()
    );

    internal void LoadWorld() {
        ChunkData data = new(384) {
            ChunkX = 0,
            ChunkZ = 0
        };
        config.Map.World.GetChunk(ref data);
    }
    
    public void Start() {
        if (!server.Dimensions.ContainsKey(GameDimension)) {
            server.Dimensions.Add(GameDimension, new Dimension());
        }
        
        World = server.CreateWorld(config.Map.World, GameDimension);
        World.AddFeatures(SkyWarsFeatures);
        
        Queue<Vec3<double>> spawns = CreateRandomSpawns();
        
        foreach (PlayerEntity player in Players) {
            RemainingPlayers.Add(player);
            player.SetWorld(World);
            player.Teleport(spawns.Dequeue());
            player.SendMessage(TextComponent.FromLegacyString("&a&lGame Started! Good luck!"));
        }

        foreach (PlayerEntity player in Players) {
            player.GameMode = GameMode.Survival;
            
            player.SendPacket(new ClientBoundUpdateTagsPacket {
                Tags = [
                    new ClientBoundUpdateTagsPacket.TagSet("block", [
                        new ClientBoundUpdateTagsPacket.Tag("climbable", [
                            Block.Ladder.ProtocolId,
                            Block.Potatoes.ProtocolId
                        ])
                    ])
                ]
            });
        }
        
        World.AddFeature(new SkyWarsCombatFeature(Die));
        
        World.Events.AddListener<EntityMoveEvent>(e => {
            if (e.NewPos.Y > -10) {
                return;
            }

            // death
            if (e.Entity is not PlayerEntity player) {
                return;
            }

            Die(player);
        });

        World.Events.AddListener<PlayerDisconnectEvent>(e => {
            Die(e.Player);
        });

        SkyWarsGameStartEvent startEvent = new() {
            Game = this
        };
        server.Events.CallEvent(startEvent);
    }
    
    private Queue<Vec3<double>> CreateRandomSpawns() {
        Queue<Vec3<double>> spawns = new();
        List<Vec3<double>> spawnList = config.Map.Spawns.ToList();
        
        while (spawnList.Count > 0) {
            int index = Random.Shared.Next(spawnList.Count);
            spawns.Enqueue(spawnList[index]);
            spawnList.RemoveAt(index);
        }
        
        return spawns;
    }

    private void EndGame(PlayerEntity? winner) {
        HasEnded = true;
        winner?.SendMessage(TextComponent.FromLegacyString("&a&lYou won the game!"));
        
        World.SendTitle(
            TextComponent.FromLegacyString("&a&lGame Over!"),
            TextComponent.FromLegacyString("&7Winner: " + (winner?.Name ?? "No One")), 10, 70, 20);

        World.Server.Scheduler.ScheduleTask(10 * server.TargetTicksPerSecond, gameEndCallback.Invoke);
        
        SkyWarsGameEndEvent endEvent = new() {
            Game = this,
            Winner = winner
        };
        server.Events.CallEvent(endEvent);
    }
    
    private void Die(PlayerEntity player) {
        if (HasEnded) {
            return;
        }
        
        World.SendMessage(TextComponent.FromLegacyString($"&6{player.Name} &chas been killed!"));
        player.GameMode = GameMode.Spectator;

        foreach (ItemStack item in player.Inventory.Items) {
            World.DropItem(player.Position, item);
        }
        
        player.Teleport(config.Map.SpecSpawn);
        lock (RemainingPlayers) {
            RemainingPlayers.Remove(player);
            if (RemainingPlayers.Count != 1) return;
            
            // Winner
            EndGame(RemainingPlayers[0]);
        }
    }

    public record Config(SkyWarsMap Map);
}
