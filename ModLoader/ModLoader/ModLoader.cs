using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using UnityEngine;

// TODO: Research how to hot-load DLLs and/or how to hot-compile C# files instead
// TODO: Better error reporting, currently just says there was an error basically.

public class Mod
{
    public string name = "";
    public Assembly assembly = null;
    public bool loaded = false;
    public MethodInfo unload;
}

public class ModLoadedArg : EventArgs
{
    public ModLoadedArg(Mod m) { mod = m; }
    public Mod mod { get; }
}

public static class ModLoader
{
    private static Dictionary<string, Mod> mods = new Dictionary<string, Mod>();
    public delegate void ModLoadedHandler(object sender, ModLoadedArg arg);

    public static event ModLoadedHandler ModLoadedEvent;

    public static Mod[] GetMods()
    {
        return mods.Values.ToArray();
    }

    public static Mod[] GetLoadedMods()
    {
        return mods.Values.Where((Mod m) =>
        {
            return m.loaded;
        }).ToArray();
    }

    public static void LoadAll()
    {
        if (ModResources.get == null)
            ModResources.get = new ModResources();
        string dir = Path.Combine(Application.dataPath, "Modding/Assemblies");

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string[] files = Directory.GetFiles(dir, "*.dll");

        foreach (string file in files)
        {
            try
            {
                LoadFromPath(file);
                DevConsole.Console.Log("Loaded mod " + Path.GetFileNameWithoutExtension(file));
            }
            catch (Exception e)
            {
                DevConsole.Console.LogError("Error while loading mod " + Path.GetFileNameWithoutExtension(file) + ": " + e.ToString());
            }
        }

    }

    public static void LoadFromPath(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("", path);
        }
        string name = Path.GetFileNameWithoutExtension(path);
        if (mods.ContainsKey(name))
        {
            return;
        }


        Mod mod = new Mod();
        mod.name = name;
        mods.Add(name, mod);

        Assembly assembly = Assembly.LoadFrom(path);
        mod.assembly = assembly ?? throw new Exception("assembly is null");

        Type modType = null;
        foreach (Type t in assembly.GetTypes())
        {
            if (t.IsClass && t.Name == "Mod")
            {
                modType = t;
                break;
            }
        }

        if (modType == null)
            throw new Exception("Assembly does not have a valid Mod class");

        MethodInfo info = modType.GetMethod("Load");
        if (info == null || !info.IsStatic)
            throw new Exception("class 'Mod' is missing static Load method");

        MethodInfo unload = modType.GetMethod("UnLoad");
        if (unload == null || !unload.IsStatic)
            throw new Exception("class 'Mod' is missing static UnLoad method");
        mod.unload = unload;

        try
        {
            Modding.CommandLoader cl = new Modding.CommandLoader(assembly);
            DevConsole.Console.AddCommands(cl.GetCommands());
        }catch(Exception e)
        {
            DevConsole.Console.LogError("Error while loading commands: " + e.ToString());
        }

        try
        {
            mod.loaded = true;
            info.Invoke(null, new object[] { });
            ModLoadedEvent?.Invoke(null, new ModLoadedArg(mod));
        }
        catch(Exception e) {
            DevConsole.Console.LogError("Error while invoking load: " + e.ToString());
        }
        
    }

    public static void UnLoadMod(Mod m)
    {
        if (!m.loaded)
            return;
        if (m.unload == null)
            throw new Exception(m.name + " has a null UnLoad method!");

        m.unload.Invoke(null, new object[] { });
    }

    public static void UnLoadAll()
    {
        foreach (Mod mod in mods.Values)
        {
            UnLoadMod(mod);
        }
    }

}

