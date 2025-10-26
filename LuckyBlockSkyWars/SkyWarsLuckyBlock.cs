using ManagedServer;
using ManagedServer.Entities.Types;
using ManagedServer.Events;
using ManagedServer.Viewables;
using ManagedServer.Worlds;
using ManagedServer.Worlds.Lighting;
using Minecraft.Implementations.Server.Features;
using Minecraft.Implementations.Server.Terrain;
using Minecraft.Packets.Status.ClientBound;
using Minecraft.Schemas;
using Minecraft.Schemas.Entities.Meta.Types;
using Minecraft.Schemas.Vec;
using Minecraft.Text;

namespace LuckyBlockSkyWars;

public static class SkyWarsLuckyBlock {
    private const string LobbyDimension = "skywars:lobby";

    public static ManagedMinecraftServer CreateLobbyServer(SkyWarsGame.Config config, ITerrainProvider lobbyWorld, 
        Vec3<double> lobbySpawn, int startDelaySeconds, bool requeueOnGameEnd = true) {
        ManagedMinecraftServer server = ManagedMinecraftServer.NewBasic();
        server.AddFeatures(new ServerListPingFeature(connection => new ClientBoundStatusResponsePacket {
            VersionName = "dotnet",
            VersionProtocol = connection.Handshake!.ProtocolVersion,
            OnlinePlayers = 1,
            MaxPlayers = 1,
            SamplePlayers = [new SamplePlayer("Potato", "4566e69f-c907-48ee-8d71-d7ba5aa00d20")],
            Description = TextComponent.FromLegacyString("&a&lSkyWars"),
            PreventsChatReports = true
        }));
        
        server.Dimensions.Add(LobbyDimension, new Dimension());
        
        Console.WriteLine("Loading maps...");
        World lobby = server.CreateWorld(lobbyWorld, LobbyDimension, new FullBrightLightingProvider());
        // SkyWarsGame.LoadWorld();
        Console.WriteLine("Maps loaded successfully.");

        NpcEntity billy = new(new PlayerMeta {
            SkinFlags = SkinParts.All
        }) {
            Position = lobbySpawn,
            Name = ChatUtils.FormatLegacy("&a&lBilly"),
            Skin = PlayerSkin.FromUsername("Technoblade").Result
        };
        billy.SetWorld(lobby);
        
        Timer? startTimer = null;
        DateTime startTime = DateTime.Now;
        List<PlayerEntity> waitingPlayers = [];

        server.Events.AddListener<PlayerPreLoginEvent>(e => {
            e.World = lobby;
            e.GameMode = GameMode.Adventure;
            e.Hardcore = true;
        });

        server.Events.AddListener<PlayerLoginEvent>(e => {
            EnqueuePlayer(e.Player);
        });

        lobby.Events.AddListener<PlayerBreakBlockEvent>(e => {
            e.Cancelled = true;
        });

        lobby.Events.AddListener<EntityMoveEvent>(e => {
            if (e.Entity is not PlayerEntity player) {
                return;
            }

            if (e.NewPos.Y < 50) {
                player.Teleport(lobbySpawn);
            }
        });
        
        return server;
        

        void EnqueuePlayer(PlayerEntity player) {
            player.GameMode = GameMode.Survival;
            player.Inventory.Clear();
            player.Health = 20;
            player.ClearAttributeModifiers();
            
            if (player.World != lobby) {
                player.SetWorld(lobby);
            }
            
            lock (waitingPlayers) {
                waitingPlayers.Add(player);
                player.Connection.Disconnected += () => {
                    lock (waitingPlayers) {
                        waitingPlayers.Remove(player);
                    }
                };

                if (waitingPlayers.Count >= 2 && startTimer == null) {
                    startTime = DateTime.Now.AddSeconds(startDelaySeconds);
                    startTimer = new Timer(_ => {
                        int secondsLeft = (int)(startTime - DateTime.Now).TotalSeconds;
                        if (secondsLeft <= 0) {
                            StartGame();
                            return;
                        }
                        
                        lobby.SendTitle(
                            TextComponent.FromLegacyString("&a&lGame Starting!"), 
                            TextComponent.FromLegacyString("&7Starting in " + secondsLeft + " seconds"), 0);
                    }, null, TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5));
                }
            }
        }

        void StartGame() {
            startTimer?.Dispose();
            startTimer = null;

            lock (waitingPlayers) {
                PlayerEntity[] players = waitingPlayers.ToArray();
                SkyWarsGame game = new(server, config, players, requeueOnGameEnd ? () => {
                    foreach (PlayerEntity player in players) {
                        EnqueuePlayer(player);
                    }
                } : () => { });
                game.Start();
                waitingPlayers.Clear();
            }
        }
    }
    
    public static async Task StartLobby(SkyWarsGame.Config config, ITerrainProvider lobbyWorld, 
        Vec3<double> lobbySpawn, int startDelaySeconds) {
        ManagedMinecraftServer server = CreateLobbyServer(config, lobbyWorld, lobbySpawn, startDelaySeconds);

        Console.WriteLine("Starting SkyWars Lucky Block server...");
        server.Start();
        await server.ListenTcp(25565);
    }
}
