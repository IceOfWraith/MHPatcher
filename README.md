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
| Fix Session Token Encryption by TobiX | Fixes the session token encryption error at its cause. Use this one. |
| Bypass Session Token Encryption Error by FF_Lowthor | Ignores the session token encryption error instead of fixing it. Only use it if the TobiX fix doesn't work for you. You don't need both. |
| Disable Screenshots When Sending Bug Report | Stops the client from attaching a screenshot to bug reports. |
| Custom ports (6 parts) | Adds launch options to set the SiteConfig port, the auth server port, and HTTP for auth. Always applied as one patch. |

#### Session token patches

When the client logs in, it encrypts the session token with AES-256 through the Windows CryptoAPI. The client builds the key header but leaves 2 bytes of it uninitialized. When those bytes hold garbage, Windows rejects the key and login fails with an encryption error.

- **TobiX's fix** writes the whole 4-byte header (`08 02 00 00`) in one instruction, so the stray bytes are always zero.
- **FF_Lowthor's bypass** turns the error check after encryption into an unconditional jump. The client continues even if encryption failed.

#### Custom ports

The unpatched client always requests SiteConfig on port 80, always connects to the auth server on port 443, and always uses HTTPS for auth. The custom ports patch lets you change all three when you start the client.

Add these options to the patched client's command line:

| Option | Effect | If you leave it out |
| --- | --- | --- |
| `-siteconfigport=<port>` | Port used to request SiteConfig. Must be a number from 1 to 65535. | Port 80. An invalid value also falls back to 80. |
| `-authport=<port>` | Auth server port. The value is used as-is, so check it for typos. | The port from SiteConfig. The unpatched client always used 443. |
| `-authhttp` | Use plain HTTP for auth requests. | HTTPS. |

Example:

```
MarvelHeroesOmegaPatched.exe -robocopy -nosteam -siteconfigport=8080 -authport=8080 -authhttp
```

Ports are read every time the client starts, so you can change them without patching again.

How it works: the client's `.text` section ends with unused zero bytes. The patch writes three small routines there and extends the section's size in the PE header to include them. It then replaces four spots in the original code with calls to those routines:

| Part | Offset | Change |
| --- | --- | --- |
| Header | `0x000002B0` | Extends `.text` VirtualSize from `0x1DB9E0F` to `0x1DB9F22` so the new routines are loaded. |
| SiteConfig request | `0x018ED01B` | Calls a routine that reads `-siteconfigport=` and uses it in place of the hard-coded port 80. |
| AuthServerPort | `0x018F085C` | Calls a routine that reads `-authhttp` and `-authport=`, then skips the code that forced port 443. |
| Auth request 2 | `0x019AD4BD` | Calls a routine that returns "use HTTPS" unless `-authhttp` was given. |
| Auth request 1 | `0x019B0828` | Same as auth request 2, for the other auth request. |
| Routines | `0x01DBA210` | The 273 bytes of new code and the option-name strings. |

The routines read the options through the client's own command-line functions. The parts depend on each other, so the patcher only applies them as a set.

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
