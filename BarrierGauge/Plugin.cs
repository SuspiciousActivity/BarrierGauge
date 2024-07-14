using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/bg";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("SamplePlugin");
    private ConfigWindow ConfigWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private unsafe void OnCommand(string command, string args)
    {
        AddonPartyList* partyList = (AddonPartyList*)GameGui.GetAddonByName("_PartyList");
        if (partyList == null)
        {
            return;
        }
        foreach (var member in partyList->PartyMembers)
        {
            var hpGaugeBar = member.HPGaugeBar;
            Log.Information($"{member.ToString()}");
            if (hpGaugeBar == null || hpGaugeBar->UldManager.NodeListCount < 8)
            {
                continue;
            }
            SetGaugeOverlay(hpGaugeBar, true);
        }
        foreach (var member in partyList->TrustMembers)
        {
            var hpGaugeBar = member.HPGaugeBar;
            Log.Information($"{member.ToString()}");
            if (hpGaugeBar == null || hpGaugeBar->UldManager.NodeListCount < 8)
            {
                continue;
            }
            SetGaugeOverlay(hpGaugeBar, true);
        }
    }

    private unsafe void SetGaugeOverlay(AtkComponentGaugeBar* hpGaugeBar, bool enabled)
    {
        int add = enabled ? 8 : 0;
        var hp = (AtkNineGridNode*)hpGaugeBar->UldManager.NodeList[1];
        var barrierEnd = (AtkNineGridNode*)hpGaugeBar->UldManager.NodeList[4];
        var barrierTop = (AtkNineGridNode*)hpGaugeBar->UldManager.NodeList[7];
        var barrierFadeOut = (AtkNineGridNode*)hpGaugeBar->UldManager.NodeList[8];
        var barrierFadeIn = (AtkNineGridNode*)hpGaugeBar->UldManager.NodeList[9];
        var barrierOverflow = (AtkImageNode*)hpGaugeBar->UldManager.NodeList[10];
        barrierTop->SetYShort((short)(8 + add));
        barrierFadeOut->SetYShort((short)(-8 + add));
        barrierFadeIn->SetYShort((short)(-8 + add));
        barrierOverflow->SetYShort((short)(9 + add));
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
