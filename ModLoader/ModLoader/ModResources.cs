using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

// TODO: Test the system.
// TODO: More loadable types


public class ModResources
{
    public static ModResources modResources;
    public static ModResources @get;

    private Dictionary<string, Object> loaded = new Dictionary<string, UnityEngine.Object>();

    public ModResources()
    {
        modResources = this;
        get = this;
#pragma warning disable CS4014
        LoadResources(Path.Combine(Application.dataPath, "Modding"), true, Path.Combine(Application.dataPath, "Modding\\Assemblies"), Path.Combine(Application.dataPath, "Modding\\Pictures"));
#pragma warning restore CS4014
    }

    public async Task LoadResources(string folder, bool recursive = true, params string[] ignoredirs)
    {
        if (!Directory.Exists(folder))
        {
            DevConsole.Console.LogWarning("ModResources couldn't load from nonexistent folder '" + folder + "'.");
            return;
        }

        string[] files = Directory.GetFiles(folder);
        string[] directories = Directory.GetDirectories(folder);

        foreach(string file in files)
        {
            string identifier = Path.GetFileNameWithoutExtension(file.Remove(0, folder.Length));
            await LoadResource(file, identifier);
        }

        List<string> ignorelist = new List<string>(ignoredirs);

        foreach (string dir in directories)
        {
            if ( ignorelist.Contains(dir) )
                continue;
            await LoadResources(dir, recursive, ignoredirs);
        }
    }

    public async Task LoadResource(string file, string identifier)
    {
        if (!File.Exists(file))
        {
            DevConsole.Console.LogWarning("ModResources couldn't load from nonexistent file'" + file + "'.");
            return;
        }

        string ext = Path.GetExtension(file).Remove(0, 1);

        switch (ext)
        {
            case "png":
            case "jpg":
            case "jpeg":
                Texture2D tex = await LoadTexture(file);
                loaded.Add(identifier, tex);
                if (tex != null)
                {
                    DevConsole.Console.LogInfo("ModResources Loaded Texture2D " + identifier);
                }
                else
                {
                    DevConsole.Console.LogWarning("ModResources couldn't load Texture2D " + identifier + " because it returned a null value.");
                }
                break;
            default:
                DevConsole.Console.LogWarning("ModResources ignoring file '" + file + "' because it doesn't contain a loadable extension '" + ext + "'.");
                break;
        }
    }

    public async Task<Texture2D> LoadTexture(string file)
    {
        if (!File.Exists(file))
        {
            DevConsole.Console.LogWarning("ModResources couldn't load texture from nonexistent file'" + file + "'.");
            return null;
        }
        byte[] contents = null;
        try
        {
            using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read))
            {
                contents = new byte[fs.Length];
                await fs.ReadAsync(contents, 0, (int)fs.Length);
            }
        }catch(System.Exception e)
        {
            DevConsole.Console.LogError("ModResources couldn't load texture " + file + ": " + e.Message + "\n" + e.StackTrace);
            return null;
        }
        if(contents == null)
        {
            DevConsole.Console.LogError("ModResources couldn't load texture " + file + " because the contents were null");
            return null;
        }

        Texture2D tex = new Texture2D(2, 2);
        tex.LoadRawTextureData(contents);
        tex.Apply();

        return tex;
    }
}
