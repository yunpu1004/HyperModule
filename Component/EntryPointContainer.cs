using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HyperModule
{
    public class EntryPointContainer : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            DontDestroyOnLoad(gameObject);

            builder.RegisterEntryPoint<InitEntryPoint>();
        }
    }
}
