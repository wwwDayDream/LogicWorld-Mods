using LogicAPI.Server;

namespace ScriptableChip.server
{
    public class ServerLoader : ServerMod
    {
        protected override void Initialize()
        {
            Logger.Info("Chipz mod initialized");
        }
    }
}