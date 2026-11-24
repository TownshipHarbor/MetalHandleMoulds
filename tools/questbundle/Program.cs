using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

static class Program
{
    const int Windows64 = 19;
    const int Android = 13;
    const string Engine = "2020.3.23f1";

    static readonly HashSet<AssetClassID> Portable = new HashSet<AssetClassID>
    {
        AssetClassID.AssetBundle, AssetClassID.MonoBehaviour, AssetClassID.MonoScript, AssetClassID.Mesh
    };

    static int Main(string[] args)
    {
        if (args.Length == 4 && args[0] == "--embedded")
            return Embedded(args[1], args[2], args[3]);
        if (args.Length != 2)
        {
            Console.WriteLine("usage: QuestBundle <PC bundle> <Android bundle out>");
            Console.WriteLine("Marks a copy of a PC bundle for Android and proves every object in it is byte for byte the same.");
            Console.WriteLine("Refuses bundles holding anything built per platform (textures, shaders, materials, audio).");
            Console.WriteLine("       QuestBundle --embedded <dll> <resource name> <bundle>");
            Console.WriteLine("Checks that the DLL embeds exactly that bundle.");
            return 2;
        }
        try
        {
            Convert(args[0], args[1]);
            Verify(args[0], args[1]);
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL " + e.Message);
            return 1;
        }
    }

    static void Convert(string input, string output)
    {
        AssetsManager am = new AssetsManager();
        BundleFileInstance bun = am.LoadBundleFile(input, true);
        int files = bun.file.BlockAndDirInfo.DirectoryInfos.Count;
        for (int i = 0; i < files; i++)
        {
            if (!bun.file.IsAssetsFile(i))
                throw new InvalidDataException(bun.file.GetFileName(i) + " is a resource stream; streamed data is built per platform");
            AssetsFileInstance inst = am.LoadAssetsFileFromBundle(bun, i, false);
            AssetsFile f = inst.file;
            if (f.Metadata.TargetPlatform != Windows64)
                throw new InvalidDataException(bun.file.GetFileName(i) + " is for platform " + f.Metadata.TargetPlatform + ", not StandaloneWindows64 (19)");
            if (f.Metadata.UnityVersion != Engine)
                throw new InvalidDataException(bun.file.GetFileName(i) + " was built with Unity " + f.Metadata.UnityVersion + "; the Quest game is " + Engine);
            if (!f.Metadata.TypeTreeEnabled)
                throw new InvalidDataException(bun.file.GetFileName(i) + " has no type trees");
            if (f.Metadata.Externals.Count != 0)
                throw new InvalidDataException(bun.file.GetFileName(i) + " points into " + f.Metadata.Externals.Count + " other file(s): " + string.Join(", ", f.Metadata.Externals.Select(e => e.PathName)));
            Dictionary<AssetClassID, int> types = new Dictionary<AssetClassID, int>();
            foreach (AssetFileInfo info in f.AssetInfos)
            {
                AssetClassID id = (AssetClassID)info.TypeId;
                if (!Portable.Contains(id))
                    throw new InvalidDataException(bun.file.GetFileName(i) + " holds a " + id + " (path id " + info.PathId + "), which is built per platform; rebuild the bundle for Android in Unity instead");
                types[id] = types.TryGetValue(id, out int c) ? c + 1 : 1;
                if (id == AssetClassID.Mesh)
                    Mesh(am, inst, info);
            }
            Console.WriteLine(bun.file.GetFileName(i) + ": Unity " + f.Metadata.UnityVersion + ", " + f.AssetInfos.Count + " objects (" + string.Join(", ", types.OrderBy(k => k.Key.ToString()).Select(k => k.Value + " " + k.Key)) + ")");
            f.Metadata.TargetPlatform = Android;
            bun.file.BlockAndDirInfo.DirectoryInfos[i].SetNewData(f);
        }
        string raw = output + ".raw";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        using (AssetsFileWriter w = new AssetsFileWriter(raw))
            bun.file.Write(w);
        am.UnloadAll(true);
        AssetBundleFile plain = new AssetBundleFile();
        using (FileStream rs = File.OpenRead(raw))
        {
            plain.Read(new AssetsFileReader(rs));
            using (AssetsFileWriter w = new AssetsFileWriter(output))
                plain.Pack(w, AssetBundleCompressionType.LZ4);
            plain.Close();
        }
        File.Delete(raw);
    }

    static void Mesh(AssetsManager am, AssetsFileInstance inst, AssetFileInfo info)
    {
        AssetTypeValueField m = am.GetBaseField(inst, info);
        string name = m["m_Name"].AsString;
        AssetTypeValueField sd = m["m_StreamData"];
        if (!sd.IsDummy && sd["size"].AsULong != 0)
            throw new InvalidDataException("mesh " + name + " streams its vertices from a resource file");
        if (m["m_MeshCompression"].AsByte != 0)
            throw new InvalidDataException("mesh " + name + " is compressed");
        if (!m["m_IsReadable"].AsBool)
            throw new InvalidDataException("mesh " + name + " is not readable");
    }

    static void Verify(string input, string output)
    {
        AssetsManager am = new AssetsManager(), bm = new AssetsManager();
        BundleFileInstance a = am.LoadBundleFile(input, true);
        BundleFileInstance b = bm.LoadBundleFile(output, true);
        int files = a.file.BlockAndDirInfo.DirectoryInfos.Count;
        if (b.file.BlockAndDirInfo.DirectoryInfos.Count != files)
            throw new InvalidDataException("file count changed");
        int objects = 0;
        long bytes = 0;
        for (int i = 0; i < files; i++)
        {
            if (a.file.GetFileName(i) != b.file.GetFileName(i))
                throw new InvalidDataException("file " + i + " renamed");
            AssetsFile fa = am.LoadAssetsFileFromBundle(a, i, false).file;
            AssetsFile fb = bm.LoadAssetsFileFromBundle(b, i, false).file;
            if (fb.Metadata.TargetPlatform != Android)
                throw new InvalidDataException("output is for platform " + fb.Metadata.TargetPlatform);
            if (fa.Metadata.UnityVersion != fb.Metadata.UnityVersion || fa.Metadata.TypeTreeTypes.Count != fb.Metadata.TypeTreeTypes.Count)
                throw new InvalidDataException("metadata changed beyond the platform");
            for (int t = 0; t < fa.Metadata.TypeTreeTypes.Count; t++)
            {
                TypeTreeType ta = fa.Metadata.TypeTreeTypes[t], tb = fb.Metadata.TypeTreeTypes[t];
                if (ta.TypeId != tb.TypeId || ta.ScriptTypeIndex != tb.ScriptTypeIndex || ta.Nodes.Count != tb.Nodes.Count || !ta.TypeHash.data.SequenceEqual(tb.TypeHash.data))
                    throw new InvalidDataException("type tree " + t + " changed");
            }
            if (fa.AssetInfos.Count != fb.AssetInfos.Count)
                throw new InvalidDataException("object count changed");
            for (int k = 0; k < fa.AssetInfos.Count; k++)
            {
                AssetFileInfo ia = fa.AssetInfos[k], ib = fb.AssetInfos[k];
                if (ia.PathId != ib.PathId || ia.TypeId != ib.TypeId || ia.ByteSize != ib.ByteSize)
                    throw new InvalidDataException("object " + ia.PathId + " changed shape");
                if (!Bytes(fa, ia).SequenceEqual(Bytes(fb, ib)))
                    throw new InvalidDataException("object " + ia.PathId + " bytes changed");
                objects++;
                bytes += ia.ByteSize;
            }
        }
        am.UnloadAll(true);
        bm.UnloadAll(true);
        Console.WriteLine("verified: " + objects + " objects, " + bytes + " bytes, identical to the PC bundle; only the platform changed (19 to 13)");
        Console.WriteLine("wrote " + output + " (" + new FileInfo(output).Length + " bytes, LZ4)");
    }

    static int Embedded(string dll, string name, string bundle)
    {
        byte[] want = File.ReadAllBytes(bundle);
        using (FileStream fs = File.OpenRead(dll))
        using (PEReader pe = new PEReader(fs))
        {
            MetadataReader md = pe.GetMetadataReader();
            foreach (ManifestResourceHandle h in md.ManifestResources)
            {
                ManifestResource r = md.GetManifestResource(h);
                if (md.GetString(r.Name) != name)
                    continue;
                PEMemoryBlock block = pe.GetSectionData(pe.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);
                BlobReader reader = block.GetReader((int)r.Offset, block.Length - (int)r.Offset);
                byte[] have = reader.ReadBytes(reader.ReadInt32());
                bool same = have.SequenceEqual(want);
                Console.WriteLine((same ? "embedded: " : "FAIL embedded: ") + Path.GetFileName(dll) + " holds " + name + " (" + have.Length + " bytes)" + (same ? ", byte for byte " : ", not ") + Path.GetFileName(bundle));
                return same ? 0 : 1;
            }
        }
        Console.WriteLine("FAIL embedded: " + Path.GetFileName(dll) + " has no resource " + name);
        return 1;
    }

    static byte[] Bytes(AssetsFile f, AssetFileInfo info)
    {
        AssetsFileReader r = f.Reader;
        r.Position = info.GetAbsoluteByteOffset(f);
        return r.ReadBytes((int)info.ByteSize);
    }
}
