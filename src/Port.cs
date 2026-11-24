using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Alta.Inventory;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

namespace MetalHandleMoulds
{
    internal static class Port
    {
        internal const string Version = "1.0.0";

        internal const string Release = "v6";

        internal const string Resource = "MetalHandleMoulds.AssetBundles.!metalhandlemoulds";

        internal const string Build = "Quest";
        internal const int Platform = 13;

        internal static readonly uint[] Moulds = { 3401u, 3402u, 3403u, 3404u, 3405u, 3406u, 3407u };

        static MelonLogger.Instance log;
        static string bundleName;
        static int bundleBytes;

        internal static bool Init(MelonLogger.Instance logger)
        {
            log = logger;
            Info(Build + " build of CGNik's MetalHandleMoulds " + Release + ", player side only");
            CustomRecipesAPI.Core.PostPatches += Check;
            return true;
        }

        internal static AssetBundle LoadBundle()
        {
            byte[] data = Embedded();
            string digest = Fnv(data);
            string dir = Path.Combine(MelonEnvironment.UserDataDirectory, "MetalHandleMoulds");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "metalhandlemoulds-" + digest + ".bundle");
            if (!Same(file, data))
            {
                string tmp = file + ".tmp";
                File.WriteAllBytes(tmp, data);
                if (File.Exists(file))
                    File.Delete(file);
                File.Move(tmp, file);
                Info("wrote " + file);
            }
            foreach (string old in Directory.GetFiles(dir, "metalhandlemoulds-*.bundle"))
            {
                if (!string.Equals(Path.GetFileName(old), Path.GetFileName(file), StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(old);
                    Info("removed the old copy " + Path.GetFileName(old));
                }
            }
            AssetBundle bundle = Plat.BundleFromFile(file);
            if (bundle == null)
                throw new InvalidOperationException("Unity refused the bundle " + file + " (" + data.Length + " bytes, built for platform " + Platform + ")");
            bundleName = Path.GetFileName(file);
            bundleBytes = data.Length;
            Info("loaded " + file + " (" + data.Length + " bytes, platform " + Platform + ")");
            return bundle;
        }

        internal static MouldDefinition Mould(AssetBundle bundle, string name)
        {
            float start = Time.realtimeSinceStartup;
            MouldDefinition definition = Plat.LoadMould(bundle, name);
            if (definition == null)
                throw new InvalidOperationException("the bundle has no mould definition named " + name);
            Plat.Keep(definition);
            Plat.Keep(definition.Graphic);
            Info("took " + definition.name + " (" + definition.Hash + ") from the bundle in " + (Time.realtimeSinceStartup - start).ToString("0.000", CultureInfo.InvariantCulture) + " s");
            return definition;
        }

        internal static Item Item(uint hash)
        {
            float start = Time.realtimeSinceStartup;
            Item item = Plat.Find<Item>(hash);
            if (item == null)
                throw new InvalidOperationException("the game has no item " + hash);
            Info("found item " + hash + " " + item.name + " in " + (Time.realtimeSinceStartup - start).ToString("0.000", CultureInfo.InvariantCulture) + " s");
            return item;
        }

        internal static void Ready(float start)
        {
            int found = 0;
            foreach (uint hash in Moulds)
            {
                MouldDefinition definition = Plat.Find<MouldDefinition>(hash);
                if (definition == null)
                {
                    Error("mould " + hash + " is not in the game's mould list");
                    continue;
                }
                found++;
                Item product = definition.Product;
                uint prefab = product == null || product.Prefab == null ? 0u : product.Prefab.Hash;
                Info("mould " + hash + " " + definition.name + ": product " + (product == null ? "none" : product.Hash + " " + product.name) + ", graphic " + Name(definition.Graphic)
                    + ", cost " + definition.Cost + ", findable " + YesNo(definition.IsFindable) + ", standard press " + YesNo(Listed(CustomRecipesAPI.Core.itemsToAddToStandardMouldPress, product))
                    + ", hebios press " + YesNo(Listed(CustomRecipesAPI.Core.itemsToAddToHebiosMouldPress, product)) + ", smelter offset " + Offset(prefab)
                    + ", mould component " + YesNo(Plat.HasMouldComponent(product, definition)));
            }
            string took = (Time.realtimeSinceStartup - start).ToString("0.00", CultureInfo.InvariantCulture);
            if (found == Moulds.Length)
                Info("ready: " + found + " moulds from " + bundleName + " (" + bundleBytes + " bytes), setup took " + took + " s");
            else
                Error("only " + found + " of " + Moulds.Length + " moulds are in the game's mould list");
        }

        static void Check()
        {
            Info("press filters: " + Filter("Mould Press Input Dock Standard", CustomRecipesAPI.Core.itemsToAddToStandardMouldPress) + ", " + Filter("Mould Press Input Dock Hebios", CustomRecipesAPI.Core.itemsToAddToHebiosMouldPress));
        }

        static string Filter(string dock, List<Item> wanted)
        {
            int ours = 0, there = 0;
            foreach (uint hash in Moulds)
            {
                MouldDefinition definition = Plat.Find<MouldDefinition>(hash);
                Item product = definition == null ? null : definition.Product;
                if (product == null || !Listed(wanted, product))
                    continue;
                ours++;
                if (Plat.DockHas(dock, product))
                    there++;
            }
            return dock + " holds " + there + " of our " + ours;
        }

        static bool Listed(List<Item> list, Item item)
        {
            if (item == null)
                return false;
            foreach (Item i in list)
            {
                if (i != null && i.Hash == item.Hash)
                    return true;
            }
            return false;
        }

        static string Offset(uint prefab)
        {
            Vector3 p;
            if (!CustomRecipesAPI.Core.smelterSpawnPositionOffsets.TryGetValue(prefab, out p))
                return "none";
            return "(" + F(p.x) + "," + F(p.y) + "," + F(p.z) + ")";
        }

        static byte[] Embedded()
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource))
            {
                if (s == null)
                    throw new FileNotFoundException("this build has no embedded bundle " + Resource);
                byte[] data = new byte[s.Length];
                int read = 0;
                while (read < data.Length)
                {
                    int n = s.Read(data, read, data.Length - read);
                    if (n <= 0)
                        throw new EndOfStreamException("the embedded bundle ended at " + read + " of " + data.Length + " bytes");
                    read += n;
                }
                return data;
            }
        }

        static bool Same(string file, byte[] data)
        {
            if (!File.Exists(file) || new FileInfo(file).Length != data.Length)
                return false;
            byte[] have = File.ReadAllBytes(file);
            for (int i = 0; i < data.Length; i++)
            {
                if (have[i] != data[i])
                    return false;
            }
            return true;
        }

        static string Fnv(byte[] data)
        {
            ulong h = 14695981039346656037UL;
            foreach (byte b in data)
            {
                h ^= b;
                h *= 1099511628211UL;
            }
            return h.ToString("x16", CultureInfo.InvariantCulture);
        }

        static string Name(UnityEngine.Object o) => o == null ? "none" : o.name;

        static string YesNo(bool b) => b ? "yes" : "no";

        static string F(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);

        internal static void Info(string message) => log?.Msg(message);

        internal static void Error(string message)
        {
            if (log != null)
                log.Error(message);
            else
                MelonLogger.Error(message);
        }
    }
}
