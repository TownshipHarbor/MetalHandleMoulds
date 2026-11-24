# MetalHandleMoulds for Quest

CGNik's MetalHandleMoulds v6, built for the Quest client. Moulds for the seven metal handles: six from the Standard Mould Press or as loot, the Knuckle Handle only as a temporary Hebios mould.

Original: https://github.com/CG-Nik/MetalHandleMoulds

## Install

Headset, in the headset's Mods folder:

```
/sdcard/Android/data/com.Alta.ATownshipTale/files/MelonLoader/com.Alta.ATownshipTale/Mods/
```

- `MetalHandleMoulds.dll` (this build)
- `CustomRecipesAPI.dll`, the Quest build of CGNik's CustomRecipesAPI

Server: CGNik's own PC `MetalHandleMoulds.dll` v6 and `CustomRecipesAPI.dll` v10, as always. A Quest is never a server, so this build is the player side only.

CGNik recommends Circl-NoE's EnoughIron too, so the handles aren't drawn as iron. That one is not ported.

## What the player's side does

- Puts the seven mould definitions (3401 to 3407) in the game's mould list. A mould made on the server reaches the headset as a number; without the definition the game can't draw it.
- Lets the handles into the mould presses by hand (the press's filter is checked on the player's side).

## Differences from the PC DLL

| PC v6 | Quest build |
|---|---|
| Loads its asset bundle with `AssetBundle.LoadFromStream` | Quest has no `LoadFromStream`: the bundle is written to `UserData/MetalHandleMoulds/` and loaded with `AssetBundle.LoadFromFile` |
| The bundle is built for Windows | The same bundle marked for Android. It holds only the seven definitions, their meshes and a script reference, so every object in it is byte for byte CGNik's |
| `Item.All.Where(...)` | Items looked up by hash in the game's item list |
| | The definitions and meshes are kept from `Resources.UnloadUnusedAssets` |
| | Logs one line per mould and the press filters once set up |

## Building

`build.ps1` turns CGNik's bundle in `res\` into the Android one with `tools\questbundle` (it refuses a bundle with anything Unity builds per platform), then builds the Quest DLL into `build\`.

Put LemonLoader's `net8` folder and its generated `Il2CppAssemblies` in `lemon\`, `AssetsTools.NET.dll` from the PC game's `MelonLoader\net35` folder in `lemon\net35\`, and the Quest build of CustomRecipesAPI in `lib\`.

## Credits

MetalHandleMoulds and its asset bundle are by CGNik, under the MIT licence. AssetsTools.NET is by nesrak1, under the MIT licence. Quest port by Shadow.

This is an unofficial, fan made mod for A Township Tale. It is not affiliated with or endorsed by the owners or developers of A Township Tale.
