using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace Manatrigger
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public class Action
        {
            public uint Id = 0;
        }

        public class Trigger
        {
            public string Name = string.Empty;
            public bool Enabled = true;
            public List<Action> Actions = new();
            public string Macro = string.Empty;
        }

        public int Version { get; set; } = 0;
        public List<Trigger> Triggers = new();

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface!.SavePluginConfig(this);
        }
    }
}
