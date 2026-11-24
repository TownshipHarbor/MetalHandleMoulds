using Alta;
using Alta.Inventory;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MetalHandleMoulds
{
    internal static class Plat
    {
        internal static T Find<T>(uint hash) where T : HashedGeneralValue<T>
        {
            HashedGeneralValue<T>.CheckItems();
            Il2CppSystem.Collections.Generic.Dictionary<uint, T> items = HashedGeneralValue<T>.items;
            return items != null && items.ContainsKey(hash) ? items[hash] : null;
        }

        internal static AssetBundle BundleFromFile(string path) => AssetBundle.LoadFromFile(path);

        internal static MouldDefinition LoadMould(AssetBundle bundle, string name)
        {
            Object o = bundle.LoadAsset(name, Il2CppType.Of<MouldDefinition>());
            return o == null ? null : o.TryCast<MouldDefinition>();
        }

        internal static void Keep(Object o)
        {
            if (o != null)
                o.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }

        internal static bool HasMouldComponent(Item item, MouldDefinition definition)
        {
            if (item == null)
                return false;
            Il2CppSystem.Collections.Generic.List<ItemComponent> components = item.components;
            if (components == null)
                return false;
            for (int i = 0; i < components.Count; i++)
            {
                MouldItemComponent m = components[i] == null ? null : components[i].TryCast<MouldItemComponent>();
                if (m != null && m.Definition != null && m.Definition.Pointer == definition.Pointer)
                    return true;
            }
            return false;
        }

        internal static bool DockHas(string dock, Item item)
        {
            foreach (Object o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<PickupDockSettings>()))
            {
                PickupDockSettings settings = o == null ? null : o.TryCast<PickupDockSettings>();
                if (settings == null || settings.name != dock)
                    continue;
                for (int i = 0; i < settings.IncludedItems.Count; i++)
                {
                    Item included = settings.IncludedItems[i];
                    if (included != null && included.Hash == item.Hash)
                        return true;
                }
                return false;
            }
            return false;
        }
    }
}
