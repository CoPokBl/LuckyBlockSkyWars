using LuckyBlockSkyWars;
using LuckyBlockSkyWars.Maps;
using Minecraft.Data.Generated;
using Minecraft.Schemas.Vec;
using PolarWorlds;

SkyWarsMap ramen = new(
    new PolarLoader(SkyWarsUtils.ReadPolarMap("ramen.polar"), VanillaRegistry.Data), 
    [
        new Vec3<double>(-20.5, 25, -24.5),
        new Vec3<double>(23.5, 25, -24.5),
        new Vec3<double>(29.5, 25, 0.5),
        new Vec3<double>(21.5, 25, 25.5),
        new Vec3<double>(-22.5, 25, 25.5),
        new Vec3<double>(-28.5, 25, 0.5)
    ], 
    new Vec3<double>(0.5, 25, 0.5)
);
SkyWarsGame.Config config = new(ramen);

await SkyWarsLuckyBlock.StartLobby(
    config, 
    new PolarLoader(SkyWarsUtils.ReadPolarMap("lobby.polar"), VanillaRegistry.Data),
    new Vec3<double>(5, 66, 5),
    5);
