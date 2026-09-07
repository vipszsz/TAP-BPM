<div align="center">

<img src="docs/hero.png" alt="Tap BPM" width="720">

# Tap BPM

**Tap along to a track. Read the BPM. That's the whole app.**

[![build](https://github.com/vipszsz/TAP-BPM/actions/workflows/build.yml/badge.svg)](https://github.com/vipszsz/TAP-BPM/actions/workflows/build.yml)
[![download](https://img.shields.io/github/v/release/vipszsz/TAP-BPM?label=download&color=5ebe74)](https://github.com/vipszsz/TAP-BPM/releases/latest)
[![license](https://img.shields.io/github/license/vipszsz/TAP-BPM?color=855be1)](LICENSE)

[Português](README.pt-BR.md)

</div>

---

## Download

**[⬇ Get the latest installer](https://github.com/vipszsz/TAP-BPM/releases/latest)** — download `TapBPM-x.y.z-setup.exe` and run it.

Nothing else to install. No .NET runtime, no dependencies, no admin password. Windows 10
(version 1809) or newer, 64-bit.

<details>
<summary><b>Windows will show a blue warning screen. Here is why, and what to do.</b></summary>

<br>

You will see **"Windows protected your PC"**. Click **More info**, then **Run anyway**.

This happens because the installer is not signed with a code-signing certificate, which
costs a few hundred dollars a year. Windows shows that screen for every unsigned app,
regardless of what it does. If you would rather not take anyone's word for it, you can
[build it yourself from source](#building-it-yourself) — it is the same result.

</details>

## Using it

Press **Space** in time with the music. The number appears from your second tap and gets
more accurate as you keep going.

| What you want | How |
| --- | --- |
| Tap | **Space** or **Enter** — or click the lower half of the window |
| Tap while your DAW is in front | **Ctrl+Alt+Space**, from anywhere |
| Start over | **R** |
| Keep the window above everything | **T**, or the 📌 button |
| Hear the tempo you just tapped | **M**, or the ♪ button |
| Fix a tempo that came out half or double | The **1/2** and **x2** buttons |
| Copy the number | **Ctrl+C** |
| Move the window | Drag the top half |
| Everything else | Right-click anywhere |
| Close | **Esc** |

### Reading the result

The **bar under the number** is how consistent your last few taps were. When it is full,
the reading is solid down to the decimal — trust it. When it is short, keep tapping and it
will settle.

Stop for more than 2.5 seconds and the next tap starts a fresh measurement, so you can
move straight on to the next track without touching anything.

If you fumble a single tap, the app ignores it. If you deliberately switch to a different
speed, it catches up within two taps.

### About that global shortcut

**Ctrl+Alt+Space** works even when Tap BPM is not the window you are looking at, so you can
tap along while your DAW, browser or media player stays in front.

Windows only lets one program own a given shortcut, so if something else on your machine
already claimed that combination, Tap BPM quietly takes the next one free instead. The
shortcut you actually got is written at the bottom of the window and in the right-click
menu — where you can also switch the feature off.

## Questions

**Does it send anything anywhere?**
No. It has no network code at all. Your settings are a small file on your own machine.

**Where are my settings kept?**
`%APPDATA%\Vipz\TapBPM\settings.json`. Uninstalling removes it.

**Why is the window a different colour every time?**
Because it is nicer that way. Right-click → **New colour** if you want a different one now.

**My antivirus flagged it.**
Self-contained .NET apps trip heuristic scanners sometimes. The source is all here and CI
builds every commit in public, so you can see exactly what goes into a release.

**Can I use it on Mac or Linux?**
Not currently — it is a Windows app.

## Building it yourself

You need the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
git clone https://github.com/vipszsz/TAP-BPM.git
cd TAP-BPM
dotnet run --project src/TapBpm
```

```bash
dotnet test        # the tempo engine's test suite
```

To build the installer as well, you need [Inno Setup 6](https://jrsoftware.org/isdl.php):

```bash
dotnet publish src/TapBpm/TapBpm.csproj -c Release -o artifacts/publish
iscc installer/TapBPM.iss
```

Everything lands in `artifacts/`.

<details>
<summary><b>How the code is laid out</b></summary>

<br>

| Path | What lives there |
| --- | --- |
| `src/TapBpm/Core` | Tempo estimation, metronome, global hotkey, settings — no UI |
| `src/TapBpm/Ui` | Custom-drawn controls, palette, embedded font loading |
| `src/TapBpm/MainForm.cs` | The window |
| `tests/TapBpm.Tests` | Tempo engine tests, driven by a fake clock |
| `installer/TapBPM.iss` | Inno Setup script |
| `tools/make-icon.ps1` | Rebuilds the multi-resolution `.ico` |

There are no WinForms designer files. The layout is written in code against logical 96 DPI
units and scaled at runtime, which is what keeps it sharp on high-DPI and mixed-DPI setups.

The tempo is a least-squares fit over a sliding window of the last 16 taps, with outlier
rejection, timed by `Stopwatch`. That is why the reading settles instead of drifting, and
why one bad tap does not throw it off.

</details>

## Credits

Made by **[Vipz](https://open.spotify.com/intl-pt/artist/63F0KeKFXQd5S4b3BKBfAI)**.

Built with [IBM Plex Mono](https://github.com/IBM/plex) (SIL Open Font License 1.1) and
[NAudio](https://github.com/naudio/NAudio) (MIT). Released under the [MIT License](LICENSE).
