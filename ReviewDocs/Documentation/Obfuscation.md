# Obfuscation (M8 — ship hardening)

Two independent layers protect a shipped build. Layer 1 (save encryption) is **done and on by
default**; layer 2 (assembly obfuscation) is a **manual, opt-in pre-ship step** because it can break a
Godot C# game if applied naively.

## Layer 1 — Encrypted save files (done, automatic)

`Systems/SaveLoad/SaveCrypto.cs` AES-256 encrypts the save **body**; `Serialization` writes the file as:

```
SEED <n>
<base64 AES ciphertext>
```

- The **seed stays plain** in the first line, so a save is still *shareable* (the whole world regenerates
  from the seed) — exactly the M8 requirement: "unreadable in a text editor **except the seed**."
- Everything else (treasury, stockpiles, buildings, reserves, railroads) is encrypted, so casual save
  editing is deterred.
- Key: PBKDF2 from a constant passphrase baked into the assembly. For a single-player game this is the
  standard trade-off — it stops Notepad edits, not a determined reverse-engineer (that's layer 2's job).
- File moved `user://savegame.json` → `user://savegame.dat` (the `.json` name was misleading once encrypted).
  Old plain saves won't load; not a concern pre-ship.
- Tests: `Tests/SaveCryptoTests.cs` (round-trip + ciphertext is not readable plaintext).
- **Windowed check:** F5 to save, open `user://savegame.dat` in a text editor → only `SEED <n>` is
  readable; F9 reloads the same world + economy.

## Layer 2 — Assembly obfuscation (manual, opt-in)

Config: `Obfuscar.xml` (repo root). **Deliberately not part of the normal build.**

**Why opt-in / careful:** Godot binds C# **by name** through its source generators + reflection —
`_Ready`/`_Process`/`_Draw`/`_UnhandledInput`, `[Export]` fields, `[Signal]` delegates, and `[Tool]`
scripts are all resolved by their original names at runtime. Renaming them makes the engine silently fail
to find them. So the config keeps the entire public + Godot-facing surface (`KeepPublicApi`, and
`RenameProperties/Events/Fields = false`) and only obfuscates **private internals**. Realistically, name
obfuscation of a Godot C# game is limited; the encrypted saves + shipping IL (not source) are the bigger
wins.

**How to run (release machine):**
```sh
dotnet build -c Release            # or the Godot export, which produces the release assembly
dotnet tool install -g Obfuscar.GlobalTool
obfuscar.console Obfuscar.xml      # check/adjust InPath to the actual export assembly location
```
Then **launch the obfuscated build in-engine** and verify the bound-by-name paths still work: worldgen +
ticks, build/sell/extractor/workshop, save/load round-trip, the province highlight, and the diplomacy
hotkey (B). If any break, widen the skips in `Obfuscar.xml`.

> Stronger alternative if deeper protection is wanted later: factor the pure-Core economy logic
> (`Systems/Core/**`, no Godot) into its own class library and obfuscate **only that** assembly — it has
> no Godot-reflected surface, so it can be renamed freely while the Godot-facing assembly stays intact.
