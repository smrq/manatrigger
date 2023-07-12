using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Manatrigger.Windows;
using System.Linq;
using System.Text.RegularExpressions;
using static Manatrigger.Configuration;

namespace Manatrigger
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Manatrigger";
        private const string CommandName = "/manatrigger";
        private enum TriggerUpdate { Disable, Enable, Toggle }

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private ConfigWindow ConfigWindow { get; init; }
        private Game Game { get; init; }

        public ChatGui ChatGui { get; init; }
        public DataManager DataManager { get; init; }
        public ObjectTable ObjectTable { get; init; }
        public SigScanner SigScanner { get; init; }

        public Configuration Configuration { get; init; }

        public WindowSystem WindowSystem = new("Manatrigger");

        private const string HelpText = "Usage: /manatrigger <command>\n" +
            "  (no command) - Shows or hides the configuration window.\n" +
            "  config - Shows or hides the configuration window.\n" +
            "  enable <name> - Enables the specified trigger. Without a name, enables all triggers.\n" +
            "  disable <name> - Disables the specified trigger. Without a name, disables all triggers.\n" +
            "  toggle <name> - Toggles the specified trigger. Without a name, enables or disables all triggers.\n" +
            "  help - Shows this help text.";

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] ChatGui chatGui,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            [RequiredVersion("1.0")] SigScanner sigScanner)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            ChatGui = chatGui;
            DataManager = dataManager;
            ObjectTable = objectTable;
            SigScanner = sigScanner;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            Game = new Game(this);

            ConfigWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(ConfigWindow);
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Shows the configuration window. Additional commands: /manatrigger <help | config | enable | disable | toggle>"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUI;
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler(CommandName);
            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            Game.Dispose();
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void OpenConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }

        private void OnCommand(string command, string args)
        {
            PluginLog.Debug($"OnCommand `{command}` `{args}`");

            if (string.IsNullOrEmpty(args))
            {
                ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
                return;
            }

            var regex = Regex.Match(args, "^(\\w+) ?(.*)");
            var subcommand = regex.Success && regex.Groups.Count > 1 ? regex.Groups[1].Value.ToLower() : string.Empty;

            PluginLog.Debug($"Subcommand `{subcommand}`");

            switch (subcommand)
            {
                case "enable":
                case "disable":
                case "toggle":
                    if (regex.Groups.Count < 2 || string.IsNullOrEmpty(regex.Groups[2].Value))
                    {
                        UpdateAllTriggers(subcommand);
                    }
                    else
                    {
                        var name = regex.Groups[2].Value;
                        UpdateTrigger(subcommand, name);
                    }
                    return;

                case "help":
                    ChatMessage(HelpText);
                    return;

                default:
                    ChatMessage($"Invalid command.\n{HelpText}", XivChatType.ErrorMessage);
                    return;
            }
        }

        private void UpdateAllTriggers(string subcommand)
        {
            var value =
                (subcommand == "enable") ||
                (subcommand == "toggle" && Configuration.Triggers.Any(trigger => !trigger.Enabled));
            foreach (var trigger in Configuration.Triggers)
            {
                trigger.Enabled = value;
            }
            Configuration.Save();
            ChatMessage($"{(value ? "Enabled" : "Disabled")} all triggers.");
        }

        private void UpdateTrigger(string subcommand, string name)
        {
            var trigger = Configuration.Triggers.Find(trigger => trigger.Name == name);
            if (trigger == null)
            {
                ChatMessage($"Trigger \"{name}\" not found.", XivChatType.ErrorMessage);
                return;
            }
            var value =
                (subcommand == "enable") ||
                (subcommand == "toggle" && !trigger.Enabled);
            trigger.Enabled = value;
            Configuration.Save();
            ChatMessage($"Trigger \"{name}\" {(value ? "enabled" : "disabled")}.");
        }

        private void ChatMessage(string message, XivChatType type = XivChatType.Echo)
        {
            var entry = new XivChatEntry
            {
                Type = type,
                Message = new SeStringBuilder().AddText(message).Build()
            };
            ChatGui.PrintChat(entry);
        }
    }
}
