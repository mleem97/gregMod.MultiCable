# gregMod.MultiCable

> Carry **one** cable, complete **N parallel** cables.

Copy `gregMod.MultiCable.dll` to `Data Center/Mods/`.

![License](https://img.shields.io/github/license/mleem97/gregMod.MultiCable?style=for-the-badge)

## Links

- **Repository:** [https://github.com/mleem97/gregMod.MultiCable](https://github.com/mleem97/gregMod.MultiCable)
- **Issues:** [https://github.com/mleem97/gregMod.MultiCable/issues](https://github.com/mleem97/gregMod.MultiCable/issues)
- **Releases:** [https://github.com/mleem97/gregMod.MultiCable/releases](https://github.com/mleem97/gregMod.MultiCable/releases)

## Overview

The vanilla game connects exactly **one cable per pull** (1-to-1). For redundant
links (e.g. two uplinks switch-to-switch, LACP-style backup over long
distances) you walk the same route twice.

**gregMod.MultiCable** keeps the vanilla flow intact and adds, on top:

1. **Live mirror ghosts** — while you carry a cable, `N-1` extra preview lines
   follow you on the exact same path (true multi-carry visual).
2. **Automatic sibling creation** — when you complete the cable, the remaining
   `N-1` cables are replayed through vanilla's own methods onto **free ports
   of the same start/end devices**: same route, redundant ports, discrete
   cables you can group with the vanilla LACP UI.

Set the parallel count (`1-4`, default `2`) in the gregCore **F1 Mod Config UI**
(`gregMod.MultiCable` -> `Parallel cables`), or in the **F8 panel** when
gregCore is not installed.

See [docs/INDEX.md](docs/INDEX.md) for the complete documentation, and
[docs/USAGE.md](docs/USAGE.md) for the workflow.

## Compatibility

| Platform    | Status    |
| ----------- | --------- |
| Windows x64 | Supported |
| Linux x64   | Supported |

Game/loader baseline: see [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md).

## Features

- Configurable parallel count `1-4` (`1` = vanilla passthrough).
- Mirror ghosts follow you while carrying (cosmetic, zero netcode impact).
- Siblings land on free same-device ports (same type, same SFP/fibre character).
- Standalone F8 panel + gregCore F1 config entries + HUD registration.
- No vanilla behaviour is suppressed or replaced; failed siblings are discarded
  and reported, the original cable is never touched.

## Installation

See [QUICKSTART.md](QUICKSTART.md).

## Build from Source

```bash
git clone https://github.com/mleem97/gregMod.MultiCable.git
cd gregMod.MultiCable
dotnet build gregMod.MultiCable.csproj -c Release
```

Details: [QUICKSTART.md](QUICKSTART.md), [CONTRIBUTING.md](CONTRIBUTING.md).

## Repository Layout

```
├── README.md            # This file
├── QUICKSTART.md        # Quickstart
├── CHANGELOG.md         # Changelog (Keep a Changelog)
├── CONTRIBUTING.md      # Contributing
├── SECURITY.md          # Security reports
├── CODE_OF_CONDUCT.md   # Code of conduct
├── AGENTS.md            # Notes for AI agents
├── LICENSE              # Apache-2.0
├── VERSION              # Single source of truth for the version
├── manifest.json        # Mod manifest
├── docs/                # Documentation ([Index](docs/INDEX.md))
├── scripts/             # Build/helper scripts
├── tests/               # Tests
├── references/          # Game/loader assemblies (symlinks, never committed)
├── examples/            # Examples
└── src/                 # C# source
```

## API Documentation

See [`docs/INDEX.md`](docs/INDEX.md).

## Credits

| Role       | Contributor                                        |
| ---------- | -------------------------------------------------- |
| **Codebase** | [mleem97](https://github.com/mleem97)            |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

Apache-2.0 — see [`LICENSE`](LICENSE).

## 🚀 Join the gregFramework Team!

Do you enjoy building mods, tools, or docs? Get in touch: **apply@gregframework.eu** or via
[Discord](https://discord.gg/greg) — Code, Assets, Docs, Testing, Infra, Community.

---

**gregFramework — powered by the community.**
