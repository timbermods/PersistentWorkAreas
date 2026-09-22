using Bindito.Core;
using Timberborn.EntityPanelSystem;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace PersistentWorkAreas
{
    public sealed class ModStarter : IModStarter
    {
        public void StartMod(IModEnvironment environment)
        {
            var version = typeof(ModStarter).Assembly.GetName().Version.ToString(3);
            Debug.Log("[PersistentWorkAreas] " + version + " loaded. Local working-area pins; no simulation or multiplayer patches.");
        }
    }

    [Context("Game")]
    public sealed class WorkAreasConfigurator : Configurator
    {
        protected override void Configure()
        {
            Bind<WorkAreaService>().AsSingleton();
            Bind<WorkAreaFragment>().AsSingleton();
            MultiBind<EntityPanelModule>().ToProvider<WorkAreaModuleProvider>().AsSingleton();
        }

        public sealed class WorkAreaModuleProvider : IProvider<EntityPanelModule>
        {
            private readonly WorkAreaFragment _fragment;
            public WorkAreaModuleProvider(WorkAreaFragment fragment) { _fragment = fragment; }
            public EntityPanelModule Get()
            {
                var builder = new EntityPanelModule.Builder();
                builder.AddTopFragment(_fragment, 90);
                return builder.Build();
            }
        }
    }
}
