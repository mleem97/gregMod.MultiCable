# Quickstart — gregMod.MultiCable

> Carry one cable, complete N parallel cables.

Repo: [https://github.com/mleem97/gregMod.MultiCable](https://github.com/mleem97/gregMod.MultiCable) · Version: `0.1.0` · License: Apache-2.0.

## 1. Clone

```bash
git clone https://github.com/mleem97/gregMod.MultiCable.git
cd gregMod.MultiCable
```

## 2. Build

```bash
# Sync game/loader assemblies first (repo root helper)
../ModRepositories/tools/sync-melon-assemblies.sh

dotnet build gregMod.MultiCable.csproj -c Release
```

The DLL lands in `bin/Release/net6.0/gregMod.MultiCable.dll`.

## 3. Install

Copy the DLL to the game Mods folder:

```bash
# Linux example
cp bin/Release/net6.0/gregMod.MultiCable.dll \
  "$HOME/.local/share/Steam/steamapps/common/Data Center/Mods/"
```

(Or from the repo root: `./build.sh MultiCable --deploy`.)

## 4. Use

1. Start the game (with or without gregCore).
2. With gregCore: press **F1** -> Mod Config -> `gregMod.MultiCable` ->
   set **Parallel cables** to `2` (or up to `4`).
   Without gregCore: press **F8** and set the count in the panel.
3. Click a start port, walk & manage the cable as usual — extra ghosts follow you.
4. Click the end port. The remaining cable(s) are created automatically on free
   ports of the same devices.
5. Optionally group the parallel cables with the vanilla LACP UI for redundancy.

Details: [README.md](README.md), [docs/USAGE.md](docs/USAGE.md),
[docs/COMPATIBILITY.md](docs/COMPATIBILITY.md).
If you run into problems: file an issue
([Issues](https://github.com/mleem97/gregMod.MultiCable/issues)) or read
[CONTRIBUTING.md](CONTRIBUTING.md).
