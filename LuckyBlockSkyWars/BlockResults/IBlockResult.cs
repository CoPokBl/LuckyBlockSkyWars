using ManagedServer.Entities.Types;
using ManagedServer.Worlds;
using Minecraft.Schemas.Vec;

namespace LuckyBlockSkyWars.BlockResults;

public interface IBlockResult {
    public void Trigger(World world, PlayerEntity? player, Vec3<int> position);
}
