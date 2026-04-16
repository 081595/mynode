# mynode

## Workspace purpose

This repository is an OpenSpec workspace for documentation-driven change design and Copilot prompt/skill sync.

Current contents are centered on:

- OpenSpec change artifacts under [myproject/openspec](myproject/openspec)
- Root-level Copilot prompt discovery under [.github](.github)
- Helper scripts under [scripts](scripts) for syncing and verifying `opsx-*` prompts
- Supporting reference docs under [docs](docs)

There is no application runtime in this repository right now; `package.json` exists only to expose helper npm scripts.

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

- `npm run openspec -- --help`
- `npm run sync:opsx`
- `npm run verify:opsx`
- `openspec --version`
- `openspec config list`
- `dotnet --info`
- `sqlite3 --version`

## Common development workflow

1. Rebuild the Codespace container after changes to [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json).
2. Run `openspec --version` to confirm the global OpenSpec install is available.
3. Run `openspec config list` to verify expanded workflows are active (`new`, `continue`, `ff`, `sync`, `verify`, `bulk-archive`, `onboard`).
4. Run `npm run sync:opsx` if you want to manually refresh prompts after OpenSpec changes.
5. Run `npm run verify:opsx` to confirm the root workspace still exposes `opsx-*.prompt.md` files after rebuild or attach.
6. Run `dotnet --info` when you need to inspect the installed .NET SDK and runtimes.

If Copilot slash commands such as `/opsx-propose` do not appear right after a rebuild, wait for the attach step to finish or run `npm run sync:opsx` and `npm run verify:opsx` once manually. The dev container now re-syncs prompt files on attach and prints an `OPSX PROMPT CHECK` banner so you can confirm the prompt files were discovered at workspace root.

## OpenSpec prompt source of truth

- Source of truth: [myproject/.github](myproject/.github)
- Synced target for root workspace chat discovery: [.github](.github)

The sync script `scripts/sync-opsx-prompts.sh` updates OpenSpec prompts/skills under `myproject/.github` and then copies them to the root `.github` folder.

## Repository notes

- [package.json](package.json) is kept only for helper scripts used by the workspace.
- The removed sample app entrypoint and Dockerfile were obsolete and are no longer part of the workflow.

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