using Dalamud.Game.ClientState.Objects;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Manatrigger
{
    public unsafe class Game: IDisposable
    {
        private Configuration configuration;
        private ObjectTable objectTable;

        private delegate bool UseActionLocationDelegate(ActionManager* actionManager, ActionType actionType, uint actionId, long targetedActorId, Vector3* vectorLocation, uint param);
        private delegate uint GetTextCommandParamIDDelegate(PronounModule* pronounModule, nint* bytePtrPtr, int length);
        private delegate GameObject* GetGameObjectFromPronounIDDelegate(PronounModule* pronounModule, uint pronounId);
        private delegate void ProcessChatBoxDelegate(UIModule* uiModule, nint message, nint unused, byte a4);

        private Hook<UseActionLocationDelegate>? useActionLocationHook;
#pragma warning disable IDE0044 // Field never assigned
        [Signature("48 89 5C 24 10 48 89 6C 24 18 56 48 83 EC 20 48 83 79 18 00")]
        private Hook<GetTextCommandParamIDDelegate>? getTextCommandParamIDHook;
        [Signature("E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 0F 85 ?? ?? ?? ?? 8D 4F DD")]
        private Hook<GetGameObjectFromPronounIDDelegate>? getGameObjectFromPronounIDHook;
        [Signature("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9")]
        private ProcessChatBoxDelegate? processChatBox;
#pragma warning restore IDE0044

        private const string TriggerTargetPlaceholder = "<trigger>";
        private const uint TriggerTargetPronounId = 10_500;

        private long lastTargetObjectId;

        public Game(Plugin plugin)
        {
            configuration = plugin.Configuration;
            objectTable = plugin.ObjectTable;

            SignatureHelper.Initialise(this);
            useActionLocationHook = Hook<UseActionLocationDelegate>.FromAddress((nint)ActionManager.Addresses.UseActionLocation.Value, UseActionLocationDetour);

            useActionLocationHook.Enable();
            getTextCommandParamIDHook!.Enable();
            getGameObjectFromPronounIDHook!.Enable();
        }

        public void Dispose()
        {
            useActionLocationHook?.Dispose();
            getTextCommandParamIDHook?.Dispose();
            getGameObjectFromPronounIDHook?.Dispose();
        }

        private bool UseActionLocationDetour(ActionManager* actionManager, ActionType actionType, uint actionId, long targetObjectId, Vector3* vectorLocation, uint param)
        {
            lastTargetObjectId = targetObjectId;
            //PluginLog.Debug($"UseActionLocation type={actionType} id={actionId} targetedActorId={targetObjectId:x} vectorLocation={*vectorLocation} param={param}");
            OnUseActionLocation(actionType, actionId, targetObjectId, *vectorLocation, param);
            return useActionLocationHook!.Original(actionManager, actionType, actionId, targetObjectId, vectorLocation, param);
        }

        private uint GetTextCommandParamIDDetour(PronounModule* pronounModule, nint* bytePtrPtr, int len)
        {
            var text = Marshal.PtrToStringAnsi(*bytePtrPtr, len);
            var result = getTextCommandParamIDHook!.Original(pronounModule, bytePtrPtr, len);
            if (result == 0 && text == TriggerTargetPlaceholder)
            {
                result = TriggerTargetPronounId;
            }
            //PluginLog.Debug($"GetTextCommandParamID text={text} result={result}");
            return result;
        }

        private GameObject* GetGameObjectFromPronounIDDetour(PronounModule* pronounModule, uint pronounId)
        {
            var result = getGameObjectFromPronounIDHook!.Original(pronounModule, pronounId);
            if (result == null && pronounId == TriggerTargetPronounId)
            {
                var obj = objectTable.SearchById((ulong)lastTargetObjectId);
                if (obj != null)
                {
                    result = (GameObject*)obj.Address;
                }
                PluginLog.Debug($"{TriggerTargetPlaceholder} id={lastTargetObjectId:x} ptr={(uint)result:x}");
            }
            //PluginLog.Debug($"GetGameObjectFromPronounIDDetour pronounId={pronounId} result={(uint)result:x}");
            return result;
        }

        private void OnUseActionLocation(ActionType actionType, uint actionId, long targetedActorId, Vector3 vectorLocation, uint param)
        {
            foreach (var trigger in configuration.Triggers)
            {
                if (!trigger.Enabled) continue;
                if (trigger.Macro.Trim() == string.Empty) continue;
                if (trigger.Actions.Any(action => action.Id == actionId))
                {
                    PluginLog.Debug($"Firing trigger {trigger.Name}");
                    var uiModule = Framework.Instance()->GetUiModule();
                    var stringPtr = Utf8String.FromString(trigger.Macro);
                    try
                    {
                        processChatBox!(uiModule, (nint)stringPtr, nint.Zero, 0);
                    }
                    finally
                    {
                        IMemorySpace.Free(stringPtr);
                    }
                }
            }
        }
    }
}
