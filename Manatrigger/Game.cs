using Dalamud.Logging;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Linq;

namespace Manatrigger
{
    public unsafe class Game: IDisposable
    {
        private Configuration configuration;
        private Chat chat;
        private Hooks hooks;

        public Game(Plugin plugin)
        {
            configuration = plugin.Configuration;
            chat = plugin.Chat;
            hooks = plugin.Hooks;

            hooks.OnUseActionLocation += OnUseActionLocation;
        }

        public void Dispose()
        {
            hooks.OnUseActionLocation -= OnUseActionLocation;
        }

        private void OnUseActionLocation(ActionType actionType, uint actionId, long targetedActorId, Vector3 vectorLocation, uint param)
        {
            foreach (var trigger in configuration.Triggers)
            {
                if (trigger.Actions.Any(action => action.Id == actionId))
                {
                    PluginLog.Debug($"Firing trigger {trigger.Name}");
                    chat.SendMessage(trigger.Macro);
                }
            }
        }
    }
}
