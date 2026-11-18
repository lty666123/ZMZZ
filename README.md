# ZMZZ

ZMZZ is a 2D action game built with Unity URP. The repository also includes an ASP.NET Core server prototype for accounts, friends, and cloud saves.

## Project structure

- `ZMZZ-Unity/`: Unity client project
- `Server/ZmzzServer/`: ASP.NET Core 8 server
- `Docs/`: design documents and development notes
- `Tools/`: build, test, and asset-processing scripts

## Unity client

- Unity version: `2022.3.62f3c1`
- Render pipeline: Universal Render Pipeline 14

Add the `ZMZZ-Unity` directory in Unity Hub and open the project. On the first launch, Unity restores packages from `Packages/manifest.json` and rebuilds the ignored `Library` directory.

## Server

Install the .NET 8 SDK, then run:

```powershell
dotnet run --project Server/ZmzzServer/ZmzzServer.csproj
```

Local SQLite data is stored in `Server/ZmzzServer/Data/` and is excluded from version control.
