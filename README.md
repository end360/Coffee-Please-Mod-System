# Coffee Please Mod System
A very simple system to load DLLs at runtime for the Coffee Please beta.
The repo contains three Visual Studio 2017 projects: the mod loader, my janitor mod source, and my more console commands mod source.

# Setup
In both projects you'll have to either go into visual studio and fix the missing references (files can be found in your game's data directory/Managed) OR go into the .csproj files and replace "REPLACETHISWITHYOURUSERPATH" with the path to your user profile (e.g. C:\Users\Johnny)

# Installing the Mod Loader
Copy the Assembly-CSharp and ModLoader dlls I've provided (use the releases tab to get them) for your version into your game's data directory/Managed

# Use
After compiling a mod, copy it to the game's data directory/Modding/Assemblies
If the Assemblies folder does not exist create it.