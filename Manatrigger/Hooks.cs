using Dalamud.Game.ClientState.Objects;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.Interop;
using System;
using System.Runtime.InteropServices;

namespace Manatrigger
{
    public unsafe class Hooks: IDisposable
    {
        private delegate bool UseActionLocationDelegate(ActionManager* actionManager, ActionType actionType, uint actionId, long targetedActorId, Vector3* vectorLocation, uint param);
        public delegate void UseActionLocationEventDelegate(ActionType actionType, uint actionId, long targetedActorId, Vector3 vectorLocation, uint param);
        private Hook<UseActionLocationDelegate>? useActionLocationHook;
        public event UseActionLocationEventDelegate? OnUseActionLocation;

        private const string GetTextCommandParamIDSignature = "48 89 5C 24 10 48 89 6C 24 18 56 48 83 EC 20 48 83 79 18 00";
        private delegate uint GetTextCommandParamIDDelegate(PronounModule* pronounModule, nint* bytePtrPtr, int length);
        private Hook<GetTextCommandParamIDDelegate>? getTextCommandParamIDHook;

        private const string GetGameObjectFromPronounIDSignature = "E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 0F 85 ?? ?? ?? ?? 8D 4F DD";
        private delegate GameObject* GetGameObjectFromPronounIDDelegate(PronounModule* pronounModule, uint pronounId);
        private Hook<GetGameObjectFromPronounIDDelegate>? getGameObjectFromPronounIDHook;

        private const string TriggerTargetPlaceholder = "<trigger>";
        private const uint TriggerTargetPronounId = 10_500;

        private long lastTargetObjectId;

        private ObjectTable objectTable;

        public Hooks(Plugin plugin)
        {
            objectTable = plugin.ObjectTable;

            var getTextCommandParamIDHookAddr = plugin.SigScanner.ScanText(GetTextCommandParamIDSignature);
            var getGameObjectFromPronounIDAddr = plugin.SigScanner.ScanText(GetGameObjectFromPronounIDSignature);

            useActionLocationHook = Hook<UseActionLocationDelegate>.FromAddress((nint)ActionManager.Addresses.UseActionLocation.Value, UseActionLocationDetour);
            getTextCommandParamIDHook = Hook<GetTextCommandParamIDDelegate>.FromAddress(getTextCommandParamIDHookAddr, GetTextCommandParamIDDetour);
            getGameObjectFromPronounIDHook = Hook<GetGameObjectFromPronounIDDelegate>.FromAddress(getGameObjectFromPronounIDAddr, GetGameObjectFromPronounIDDetour);

            useActionLocationHook.Enable();
            getTextCommandParamIDHook.Enable();
            getGameObjectFromPronounIDHook.Enable();
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
            OnUseActionLocation?.Invoke(actionType, actionId, targetObjectId, *vectorLocation, param);
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
    }
}
