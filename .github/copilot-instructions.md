# AI Agent Guidelines for agent-research Repository

This repository contains independent research projects in subdirectories, where each project explores a specific technical question using AI-generated code. All content is written by LLMs like GitHub Copilot.

## Project Structure & Workflow

- **Create new research projects** in dedicated subdirectories (e.g., `sqlite-query-linter/`, `python-markdown-comparison/`)
- **Track progress** in `notes.md` within each project folder - document what you tried, learned, and any dead ends
- **Build a comprehensive README.md** for each project with sections for findings, methodology, and conclusions
- **Include runnable demos** (e.g., `demo.py`) and tests to validate your work
- **Add requirements.txt** only if external dependencies are needed (many projects use Python stdlib only)

## Key Patterns from Existing Projects

### SQLite Query Linter (`sqlite-query-linter/`)
- Wraps `sqlite3` with regex-based linting rules for common SQL mistakes
- Uses dataclasses for `LintIssue`, enums for `LintLevel` (ERROR/WARNING/INFO)
- Configurable rules via `LintRule` base class - extend for custom checks
- Zero runtime dependencies; pytest for testing
- Example: `InvalidCastTypeRule` catches invalid types in CAST expressions

### Python Markdown Comparison (`python-markdown-comparison/`)
- Benchmarks libraries using `time` and `statistics` modules for performance measurement
- Generates charts with matplotlib/seaborn for visualization
- Handles import errors gracefully for optional dependencies
- Stores results in JSON for reproducibility
- Example: Compares cmarkgfm (C-based, 10-50x faster) vs pure Python parsers

## Repository Maintenance

- **Root README auto-updates** via GitHub Actions using `cog -r -P README.md`
  - **Local development**: Run `cog -r -P README.md` to manually regenerate project summaries
  - **GitHub Actions**: Automatically runs on every push to main branch via `.github/workflows/update-readme.yml`, committing updated README and new `_summary.md` files
  - **Workflow details**: Uses Python 3.11, installs from `requirements.txt`, runs `cog -r -P README.md` with full git history, commits changes with "[skip ci]" to avoid loops
  - **LLM integration**: Uses `llm -m github/gpt-4.1` to generate concise project descriptions (1-2 paragraphs with key findings)
  - **Caching**: Summaries are cached in `_summary.md` files to avoid re-generation; delete to force refresh
- **Commit only new work** - no full copies of external code, only diffs if modifying existing repos

## Development Best Practices

- **Focus on actionable findings** - emphasize practical recommendations over exhaustive analysis
- **LLM setup**: Install `llm` and `llm-github-models` from requirements.txt; configure GitHub API access for model usage (requires GitHub token with models:read permission)
- **LLM usage for summaries**: When generating project descriptions, use `llm -m github/gpt-4.1 -s "Summarize this research project concisely. Write just 1 paragraph (3-5 sentences) followed by an optional short bullet list if there are key findings. Vary your opening - don't start with 'This report' or 'This research'. Include 1-2 links to key tools/projects. Be specific but brief. No emoji."` with the README.md as input

## Final Deliverables

Your final commit should include:
- `notes.md` and `README.md` in the project folder
- Source code you created
- `git diff` output if modifying external repos (not full repo copies)
- Small binary outputs (<2MB) if relevant

Avoid creating `_summary.md` - it's generated automatically.