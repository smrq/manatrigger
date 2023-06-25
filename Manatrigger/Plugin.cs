using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using Manatrigger.Windows;

namespace Manatrigger
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Manatrigger";
        private const string CommandName = "/manatrigger";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private ConfigWindow ConfigWindow { get; init; }
        private Game Game { get; init; }

        public SigScanner SigScanner { get; init; }
        public ObjectTable ObjectTable { get; init; }
        public Chat Chat { get; init; }
        public Configuration Configuration { get; init; }
        public Hooks Hooks { get; init; }
        public WindowSystem WindowSystem = new("Manatrigger");

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] SigScanner sigScanner,
            [RequiredVersion("1.0")] ObjectTable objectTable)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            SigScanner = sigScanner;
            ObjectTable = objectTable;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            ECommonsMain.Init(pluginInterface, this);

            Chat = new Chat();
            Hooks = new Hooks(this);
            Game = new Game(this);

            ConfigWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(ConfigWindow);
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
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
            Hooks.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            ConfigWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void OpenConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }
}
