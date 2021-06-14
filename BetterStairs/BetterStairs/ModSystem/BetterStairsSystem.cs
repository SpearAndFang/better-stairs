namespace BetterStairs.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Server;

    public class BetterStairsSystem : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide forSide)
               => true;


        public void RegisterClasses(ICoreAPI api) => api.RegisterBlockClass("blockbstairs", typeof(BlockBStairs));
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            this.RegisterClasses(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {

        }

        public override void StartClientSide(ICoreClientAPI api)
        {

        }
    }
}
