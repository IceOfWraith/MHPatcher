# MHPatcher

A patcher for Marvel Heroes client executables.

This is the maintained fork of [Crypto137/MHPatcher](https://github.com/Crypto137/MHPatcher). Get releases from [this fork's Releases page](https://github.com/IceOfWraith/MHPatcher/releases).

MHPatcher does not include, download, or distribute any game files. You need your own copy of the game.

## Requirements

- Windows (64-bit).
- `MarvelHeroesOmega.exe` from client version **1.52.0.1700 (Steam)**, unmodified. The patcher checks the file's hash and refuses any other version.
- You don't need to install .NET. The release build includes everything it needs.

## How to use

1. Download the latest `MHPatcher-<version>-win-x64.zip` from [Releases](https://github.com/IceOfWraith/MHPatcher/releases).
2. Extract the whole zip to a folder. Keep `Patches32.json` and `Patches64.json` next to `MHPatcher.exe`.
3. Drag and drop `MarvelHeroesOmega.exe` onto `MHPatcher.exe`.
   - The Win64 client is in `UnrealEngine3\Binaries\Win64`. The Win32 client is in `UnrealEngine3\Binaries\Win32`.
4. Type the numbers of the patches you want, separated by commas (for example `4` or `1,3`), and press Enter.
5. The patcher writes `MarvelHeroesOmegaPatched.exe` next to the original. Your original `MarvelHeroesOmega.exe` is not changed.
6. Start the game with `MarvelHeroesOmegaPatched.exe` instead of the original.

Windows SmartScreen may warn that the app is from an unknown publisher, because the exe is not code-signed. Select **More info**, then **Run anyway**. Compare the zip's hash with `SHA256SUMS.txt` on the release page if you want to verify your download.

## Patches

### Win64 (`Patches64.json`)

| Patch | What it does |
| --- | --- |
| Fix Session Token Encryption by TobiX | Fixes the session token encryption issue. |
| Bypass Session Token Encryption Error by FF_Lowthor | Skips the session token encryption error. |
| Disable Screenshots When Sending Bug Report | Stops the client from attaching screenshots to bug reports. |
| Custom ports (6 parts) | Lets you choose the SiteConfig and auth server ports and use HTTP for auth. Always applied as one patch. |

#### Custom ports options

After you apply the custom ports patch, add these options to the patched client's command line:

| Option | Effect |
| --- | --- |
| `-siteconfigport=<port>` | Port used to request SiteConfig. |
| `-authport=<port>` | Auth server port. Without it, the client uses the port from SiteConfig instead of always using 443. |
| `-authhttp` | Use HTTP for auth requests. Without it, auth requests use HTTPS. |

### Win32 (`Patches32.json`)

| Patch |
| --- |
| Enable Region Generation |
| Force Console UI |
| Disable Force Load from Pak |
| Set Game Database Approval Threshold to NotInGame |
| Enable Verbose Web Helper Logging |

## Pre-selecting patches

To skip the prompt, set `"IsEnabled": true` on the patches you want in the JSON file. If any patch is enabled, the patcher applies the enabled patches without asking.

Before it writes anything, the patcher checks for these problems and stops if it finds one:

- A patch that runs past the end of the file.
- Two enabled patches that write to the same bytes.
- A group that is only partly enabled.
- An offset or data value that is not valid hex.

## Adding patches

Each entry in `Patches32.json` or `Patches64.json` looks like this:

```json
{
	"Name": "Describe the patch",
	"IsEnabled": false,
	"Offset": "0x018E8F95",
	"Data": "00",
	"Group": "Optional group name"
}
```

- `Offset` is a hex file offset (not a virtual address).
- `Data` is the replacement bytes as a hex string.
- `Group` is optional. Patches with the same group depend on each other. The prompt shows the group as one choice and applies all of its parts together.

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet test MHPatcher.Tests
dotnet publish MHPatcher -c Release -o out
```

`out/` then contains `MHPatcher.exe` and both patch files.

## Releasing

Push a tag that starts with `v`, for example `v1.0.0`. GitHub Actions runs the tests, builds the exe, and attaches `MHPatcher-<version>-win-x64.zip` and `SHA256SUMS.txt` to a new GitHub release.

## License

MIT. See [LICENSE](LICENSE). Marvel Heroes and all related names are trademarks of their respective owners. This project is not affiliated with or endorsed by them.
