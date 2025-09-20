using VContainer.Unity;

namespace HyperModule
{
    public class InitEntryPoint : IInitializable
    {
        public void Initialize()
        {
            new ModuleManagersInitService().Execute();
        }
    }
}
