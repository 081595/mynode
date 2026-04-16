# mynode

## Codespaces setup

This repository configures GitHub Codespaces with the following development tools:

- ASP.NET Core 8 runtime
- Node.js 22
- OpenSpec installed globally in the dev container
- SQLite 3

After opening the repository in GitHub Codespaces, rebuild the dev container so the setup in [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json) is applied.

### Verify installed tools

- `dotnet --list-runtimes`
- `node --version`
- `openspec --version`
- `sqlite3 --version`

### Useful commands

- `npm start`
- `npm run openspec -- --help`
- `openspec --version`
- `dotnet --info`
- `sqlite3 --version`

## Common development workflow

1. Rebuild the Codespace container after changes to [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json).
2. Run `npm start` to verify the Node.js app starts correctly.
3. Run `openspec --version` to confirm the global OpenSpec install is available.
4. Run `openspec config list` to verify expanded workflows are active (`new`, `continue`, `ff`, `sync`, `verify`, `bulk-archive`, `onboard`).
5. Run `dotnet --info` when you need to inspect the installed .NET SDK and runtimes.

## SQLite quick start

Create a local database file:

```bash
sqlite3 app.db ".databases"
```

Create a sample table:

```bash
sqlite3 app.db "CREATE TABLE IF NOT EXISTS messages (id INTEGER PRIMARY KEY, text TEXT NOT NULL);"
```

Insert and query sample data:

```bash
sqlite3 app.db "INSERT INTO messages (text) VALUES ('hello');"
sqlite3 app.db "SELECT * FROM messages;"
```