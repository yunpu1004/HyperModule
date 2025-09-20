using Cysharp.Threading.Tasks;

namespace HyperModule
{
    public class ModuleManagersInitService : IExecute
    {
        public void Execute()
        {
            AddressablesManager.Init().Forget();
        }
    }
}
