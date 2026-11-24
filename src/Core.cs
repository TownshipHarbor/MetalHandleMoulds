using Alta;
using Alta.Inventory;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(MetalHandleMoulds.Core), "MetalHandleMoulds", MetalHandleMoulds.Port.Version, "CGNik, ported by Shadow", null)]
[assembly: MelonGame("Alta", "Att_Quest")]
[assembly: MelonAdditionalDependencies("CustomRecipesAPI")]

namespace MetalHandleMoulds
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            if (!Port.Init(LoggerInstance))
                return;
            LoggerInstance.Msg("Initialized.");
            CustomRecipesAPI.Core.SetUpRecipes += SetUpRecipes;
        }

        private void SetUpRecipes()
        {
            float start = Time.realtimeSinceStartup;
            AssetBundle assetBundle = Port.LoadBundle();

            MouldDefinition mouldDefinition_handleFist = Port.Mould(assetBundle, "Handle Fist Mould.asset");
            MouldDefinition mouldDefinition_handleLargeCool = Port.Mould(assetBundle, "Handle Large Cool Mould.asset");
            MouldDefinition mouldDefinition_handleMediumCool = Port.Mould(assetBundle, "Handle Medium Cool Mould.asset");
            MouldDefinition mouldDefinition_handleMediumCurved = Port.Mould(assetBundle, "Handle Medium Curved Mould.asset");
            MouldDefinition mouldDefinition_handleShortCCurve = Port.Mould(assetBundle, "Handle Short C Curve Mould.asset");
            MouldDefinition mouldDefinition_handleShortCool = Port.Mould(assetBundle, "Handle Short Cool Mould.asset");
            MouldDefinition mouldDefinition_handleShortPointyEnd = Port.Mould(assetBundle, "Handle Short Pointy End Mould.asset");

            CustomRecipesAPI.Core.SetUpMould(Port.Item(46034u), mouldDefinition_handleFist, true, false, true, null, null);
            CustomRecipesAPI.Core.SetUpMould(Port.Item(40154u), mouldDefinition_handleLargeCool, true, true, false, new Vector3(0f, -0.4f, -1.1f), null);
            CustomRecipesAPI.Core.SetUpMould(Port.Item(25450u), mouldDefinition_handleMediumCool, true, true, false, new Vector3(0f, -0.2f, -0.7f), null);
            CustomRecipesAPI.Core.SetUpMould(Port.Item(22576u), mouldDefinition_handleMediumCurved, true, true, false, new Vector3(0f, 0f, -0.1f), null);
            CustomRecipesAPI.Core.SetUpMould(Port.Item(36240u), mouldDefinition_handleShortCCurve, true, true, false, null, null);
            CustomRecipesAPI.Core.SetUpMould(Port.Item(61562u), mouldDefinition_handleShortCool, true, true, false, null, null);
            CustomRecipesAPI.Core.SetUpMould(Port.Item(19208u), mouldDefinition_handleShortPointyEnd, true, true, false, null, null);

            Port.Ready(start);
        }
    }
}
