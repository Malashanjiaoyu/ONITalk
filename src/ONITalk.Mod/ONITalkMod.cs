using HarmonyLib;
using ONITalk.Infrastructure;
using ONITalk.LocalizationSupport;
using ONITalk.Runtime;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace ONITalk {
    public sealed class ONITalkMod : KMod.UserMod2 {
        public override void OnLoad(Harmony harmony) {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            ONITalkLocalization.Register(mod?.ContentPath ?? path);
            new POptions().RegisterOptions(this, typeof(ONITalkConfig));
            ONITalkConfig config = ONITalkConfig.LoadOrCreate();
            ONITalkLocalization.Configure(config.InterfaceLanguage);
            ONITalkService.Initialize(config);
            Log.Info("Loaded. Provider=" + config.Provider + ", enabled=" + config.Enabled);
        }
    }
}
