# Changelog

## 1.0.0

First release from this fork.

### Added

- Custom ports patch for the Win64 client (`-siteconfigport`, `-authport`, `-authhttp`).
- Interactive patch selection when no patch is enabled in the JSON file.
- Patch groups. Patches that depend on each other are applied together.
- Checks before writing: out-of-range patches, overlapping patches, partly enabled groups, and invalid hex.
- Self-contained single-file Windows build. No .NET install needed.
- GitHub Actions workflow that runs the tests and publishes releases from `v*` tags.
- Unit tests.

### Changed

- Target .NET 10 (was .NET 6, which is out of support).
- Clear error messages for a missing or malformed patch file.
- The window now waits for Enter on every exit, including errors, so you can read the output.

### Fixed

- A patch that started inside the file but ran past its end crashed the patcher.
- Patch files were not found when the patcher ran as a single-file exe.
