using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
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
        public DataManager DataManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("Manatrigger");

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] SigScanner sigScanner,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            [RequiredVersion("1.0")] DataManager dataManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            SigScanner = sigScanner;
            ObjectTable = objectTable;
            DataManager = dataManager;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            Game = new Game(this);

            ConfigWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(ConfigWindow);
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens config."
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
