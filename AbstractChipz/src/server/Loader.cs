using LogicAPI.Server;

namespace Chipz.server
{
    public class ServerLoader : ServerMod
    {
        protected override void Initialize()
        {
            Logger.Info("Chipz mod initialized");
        }
    }
}