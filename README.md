# Job Generation Compatibility Layer (JGCL)

TODO: Write mod description here.

## Improving the Compatibility Layer

Before opening pull requests, developers should build and test their changes locally to make sure everything is working as expected.

### Environment Setup

After cloning the repository, some setup is required in order to successfully build the mod DLL. You will need to create a new [`Directory.Build.targets`](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build?view=vs-2022) file to specify your reference paths. This file will be located in the main directory, next to `DVJobGenCompatLayer.sln`.

Below is an example of the necessary structure. When creating your targets file, you will need to replace the two reference pahts with the corresponding folders on your system. They can be found in your Derail Valley install directory. Make sure to include the semicolons **between** each othe the paths (and no semicolon after the last path). Also note that shortcuts that you might use in flie explorer (such as %ProgramFiles%) won't be expanded in these paths. You need to use the full, absolute path.

```xml
<Project>
    <PropertyGroup>
        <ReferencePath>
            X:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed\;
            X:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed\UnityModManager\
        </ReferencePath>
        <AssemblySearchPaths>$(AssemblySearchPaths);$(ReferencePath);</AssemblySearchPaths>
    </PropertyGroup>
</Project>
```

### Build Output

The output DLL will need to be copied into `Derail Valley install directory > Mods > JobGenCompatLayer` each time the solution is built. Copy it from `bin\Debug\netframework4.8` or `bin\Release\netframework4.8` depending on the selected build configuration.
