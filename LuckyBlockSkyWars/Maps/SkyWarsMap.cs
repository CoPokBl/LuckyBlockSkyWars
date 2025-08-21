using Minecraft.Implementations.Server.Terrain;
using Minecraft.Schemas.Vec;

namespace LuckyBlockSkyWars.Maps;

public record SkyWarsMap(ITerrainProvider World, Vec3<double>[] Spawns, Vec3<double> SpecSpawn);
