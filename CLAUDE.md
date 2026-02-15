# CLAUDE.md - LikeAllStrava Project Guide

## Project Overview

**LikeAllStrava** is a .NET 10.0 console application that automates interactions with the Strava fitness platform using Selenium WebDriver with Microsoft Edge. It provides three automation features:

1. **Auto-Like (default)** - Gives kudos to all workouts in the Strava feed
2. **Follow Athletes** - Automates following athletes from follower lists
3. **Congratulation Comments** - Posts personalized comments on specific workout types

## Tech Stack

- **.NET 10.0** (Console Application)
- **Selenium WebDriver 4.35.0** with **Microsoft Edge** browser
- **Microsoft.Extensions.Configuration** for settings management
- **TextCopy** for clipboard operations (emoji support in comments)
- **WebDriverManager** for automatic EdgeDriver binary management
- C# with implicit usings and nullable reference types enabled

## Project Structure

```
LikeAllStrava/
├── Program.cs                    # Entry point, CLI argument parsing, orchestration
├── LikeAllStrava.csproj          # Project file with NuGet dependencies
├── LikeAllStrava.sln             # Visual Studio solution
├── appsettings.json              # Runtime config (Strava credentials, encrypted name)
├── app.ico                       # Application icon
├── .editorconfig                 # Code style (disables CS8618, CS8601 warnings)
├── Classes/
│   ├── Settings.cs               # Configuration model (StravaSettings)
│   ├── ShraredObjects.cs         # Global static shared state (WebDriver, config, flags)
│   ├── StavaLike.cs              # Kudos/like automation (117 max per session, 3s delay)
│   ├── StravaFollow.cs           # Follow automation (2s delay between follows)
│   ├── StravaCongrats.cs         # Comment automation (10s delay, duplicate detection)
│   └── StravaLoad.cs             # Strava initialization (decrypt name, launch browser)
└── Utilities/
    ├── EdgeDriverControl.cs      # Edge browser management (port 59492, retry logic)
    ├── Encryption.cs             # AES encryption for stored full name
    ├── Utilities.cs              # Config loading, JS click, scroll helpers
    └── WebDriverExtensions.cs    # Selenium wait extensions (WaitUntilElement)
```

## Build & Run

```bash
# Build
dotnet build

# Run (default: like all workouts)
dotnet run

# Follow people
dotnet run -- followpeople "https://www.strava.com/athletes/{ID}/follows?type=following"

# Congratulation comments
dotnet run -- congratscomment "Run" "10-50" "Great run [name]!"
```

**Prerequisites:** Microsoft Edge must be open and logged into Strava before running. The app closes all Edge instances and reconnects via remote debugging port 59492.

## Version Management

**IMPORTANT: Increment assembly versions when making code changes.**

Versions are managed in `LikeAllStrava.csproj` under `<PropertyGroup>`:
- `<Version>` - NuGet/package version (e.g., `1.0.0`)
- `<AssemblyVersion>` - Assembly version (e.g., `1.0.0.0`)
- `<FileVersion>` - File version (e.g., `1.0.0.0`)

**Versioning rules for each session:**
- Bump the **patch** version for bug fixes (e.g., `1.0.0` -> `1.0.1`)
- Bump the **minor** version for new features or enhancements (e.g., `1.0.0` -> `1.1.0`)
- Bump the **major** version for breaking changes (e.g., `1.0.0` -> `2.0.0`)
- Always keep `AssemblyVersion` and `FileVersion` in sync with `Version` (append `.0` for the 4th segment)

## Code Conventions

### Naming
- **PascalCase** for classes, methods, and public members
- **camelCase** for local variables and parameters
- **_prefixed** for static/private fields (e.g., `_key`)
- Global shared state accessed via alias: `using _s = LikeAllStrava.ShraredObjects;`

### Patterns
- **One class per feature** in `Classes/` directory
- **Utility/helper classes** in `Utilities/` directory
- **Static methods** throughout (no dependency injection)
- **Global state** via static `ShraredObjects` class (note: historical typo in name, do not rename)
- **Console.WriteLine** for all user-facing output (no logging framework)
- **Selenium CSS selectors** and **XPath** for element location
- **JavaScript execution** for clicking elements that bypass visibility checks
- **Regex** for parsing HTML content (athlete names, distances)

### Rate Limiting Constants
- **Likes:** 3-second delay between kudos, 117 max per session (~100/hour Strava limit)
- **Follows:** 2-second delay between follows
- **Comments:** 10-second delay after posting a comment

### Language Support
- Application supports both **Portuguese** and **English** Strava interfaces
- Distance regex patterns exist for both languages in `StravaCongrats.cs`

## Key Implementation Details

### Browser Automation Flow
1. Close all existing Edge processes
2. Launch Edge with remote debugging on port 59492
3. Connect Selenium WebDriver to running Edge instance
4. Navigate to `https://www.strava.com/dashboard`
5. Execute automation (like/follow/comment)
6. Kill all EdgeDriver processes on exit

### Configuration
- `appsettings.json` stores encrypted user full name (AES with static key)
- Config loaded via: JSON file -> Environment variables -> CLI arguments
- First run prompts for Strava full name, encrypts and saves it

### Error Handling
- Top-level try/catch in `Program.cs` catches all exceptions
- `finally` block ensures EdgeDriver cleanup
- EdgeDriver initialization retries up to 3 times
- Bare catch blocks in some automation loops (intentional: skip failures, continue)

## Common Strava Selectors

These CSS selectors change when Strava updates their UI - this is the most common reason for fixes:
- Kudos buttons: `[data-testid='kudos_button']`, `button[class*='Kudos--unpressed']`
- Owner name: `a[data-testid='owners-name']`
- Feed entries: `div[class*='FeedEntry']`
- Comment input: `textarea[data-testid='comment-input']`, `div[class*='mentionable-input-field']`

## Git Workflow

- Single branch: **master**
- Single author project
- Commit messages: short, descriptive (e.g., "Fix issues when Strava takes too long to load")
- No automated tests - manual testing only
- No CI/CD pipeline

## Known Technical Debt

None currently - all previous technical debt has been resolved:
- ~~`ShraredObjects` class name typo~~ - Fixed: renamed to `SharedObjects`
- ~~`StavaLike.cs` filename typo~~ - Fixed: renamed to `StravaLike.cs`
- ~~Uses `goto` statements~~ - Fixed: replaced with while loops
- ~~README outdated~~ - Fixed: updated to .NET 10.0
