# Research projects carried out by AI tools

Each directory in this repo is a separate research project carried out by an LLM tool - usually Github Copilot. Every single line of text and code was written by an LLM.

## Research projects

<!--[[[cog
import os
import subprocess
import pathlib
from datetime import datetime

# Model to use for generating summaries
MODEL = "github/gpt-4.1"

# Get all subdirectories with their first commit dates
research_dir = pathlib.Path.cwd()
subdirs_with_dates = []

for d in research_dir.iterdir():
    if d.is_dir() and not d.name.startswith('.'):
        # Get the date of the first commit that touched this directory
        try:
            result = subprocess.run(
                ['git', 'log', '--diff-filter=A', '--follow', '--format=%aI', '--reverse', '--', d.name],
                capture_output=True,
                text=True,
                timeout=5
            )
            if result.returncode == 0 and result.stdout.strip():
                # Parse first line (oldest commit)
                date_str = result.stdout.strip().split('\n')[0]
                commit_date = datetime.fromisoformat(date_str.replace('Z', '+00:00'))
                subdirs_with_dates.append((d.name, commit_date))
            else:
                # No git history, use directory modification time
                subdirs_with_dates.append((d.name, datetime.fromtimestamp(d.stat().st_mtime)))
        except Exception:
            # Fallback to directory modification time
            subdirs_with_dates.append((d.name, datetime.fromtimestamp(d.stat().st_mtime)))

# Sort by date, most recent first
subdirs_with_dates.sort(key=lambda x: x[1], reverse=True)

for dirname, commit_date in subdirs_with_dates:
    folder_path = research_dir / dirname
    readme_path = folder_path / "README.md"
    summary_path = folder_path / "_summary.md"

    date_formatted = commit_date.strftime('%Y-%m-%d')

    # Get GitHub repo URL
    github_url = None
    try:
        result = subprocess.run(
            ['git', 'remote', 'get-url', 'origin'],
            capture_output=True,
            text=True,
            timeout=2
        )
        if result.returncode == 0 and result.stdout.strip():
            origin = result.stdout.strip()
            # Convert SSH URL to HTTPS URL for GitHub
            if origin.startswith('git@github.com:'):
                origin = origin.replace('git@github.com:', 'https://github.com/')
            if origin.endswith('.git'):
                origin = origin[:-4]
            github_url = f"{origin}/tree/main/{dirname}"
    except Exception:
        pass

    if github_url:
        print(f"### [{dirname}]({github_url}) ({date_formatted})\n")
    else:
        print(f"### {dirname} ({date_formatted})\n")

    # Check if summary already exists
    if summary_path.exists():
        # Use cached summary
        with open(summary_path, 'r') as f:
            description = f.read().strip()
            if description:
                print(description)
            else:
                print("*No description available.*")
    elif readme_path.exists():
        # Generate new summary using llm command
        prompt = """Summarize this research project concisely. Write just 1 paragraph (3-5 sentences) followed by an optional short bullet list if there are key findings. Vary your opening - don't start with "This report" or "This research". Include 1-2 links to key tools/projects. Be specific but brief. No emoji."""
        result = subprocess.run(
            ['llm', '-m', MODEL, '-s', prompt],
            stdin=open(readme_path),
            capture_output=True,
            text=True,
            timeout=60
        )
        if result.returncode != 0:
            error_msg = f"LLM command failed for {dirname} with return code {result.returncode}"
            if result.stderr:
                error_msg += f"\nStderr: {result.stderr}"
            raise RuntimeError(error_msg)
        if result.stdout.strip():
            description = result.stdout.strip()
            print(description)
            # Save to cache file
            with open(summary_path, 'w') as f:
                f.write(description + '\n')
        else:
            raise RuntimeError(f"LLM command returned no output for {dirname}")
    else:
        print("*No description available.*")

    print()  # Add blank line between entries

]]]-->

### [sqlite-query-linter](https://github.com/simonw/research/tree/main/sqlite-query-linter) (2025-11-04)

The SQLite Query Linter is a lightweight Python library that wraps the standard `sqlite3` module to provide configurable linting and rule-based analysis of SQL queries before execution. Acting as a drop-in replacement, it helps catch common syntax errors and platform incompatibilities—such as invalid types in `CAST`, use of unsupported functions, `SELECT *`, missing `WHERE` clauses, and string quoting mistakes—helping developers avoid runtime errors and improve code quality. Users can choose built-in rules, set severity levels, and easily define custom rules via an extensible API. Designed for flexibility, it can block execution on critical issues or run in permissive/audit-only modes, with zero dependencies other than Python's standard library. Explore code and integration options at [GitHub](https://github.com/yourusername/sqlite-query-linter) or view usage in the included [`demo.py`](demo.py) script.

Key Features & Findings:
- Detects SQL mistakes commonly encountered when migrating between databases or writing raw SQLite queries
- Flexible configuration: Enable/disable rules, set strictness, and use audit-only monitoring
- Easy to extend for custom organizational or project rules
- Applicable to development, automated testing, database migrations, and production monitoring

### [python-markdown-comparison](https://github.com/simonw/research/tree/main/python-markdown-comparison) (2025-10-22)

Comparing seven prominent Python markdown libraries, cmarkgfm—bindings to GitHub’s C-based CommonMark/GFM parser—proved dramatically faster (10-50x) than pure Python options such as mistune, Python-Markdown, and marko. The benchmark, spanning small to large markdown documents, consistently found cmarkgfm excels in both speed and stability, making it ideal for high-volume or performance-critical applications. However, cmarkgfm trades extensibility and custom output formats for speed, so libraries like mistune (for fast pure Python and custom rendering) or Python-Markdown (for extension-rich configurability) may be preferable for projects prioritizing flexibility or ease of customization. See [cmarkgfm's repository](https://github.com/theacodes/cmarkgfm) and [mistune](https://github.com/lepture/mistune) for details.

**Key findings:**
- cmarkgfm is 10-50x faster than pure Python markdown libraries, especially for large documents.
- Pure Python options offer greater extensibility, custom output formats, and API access, but at the cost of speed.
- Best library choice depends on project needs: cmarkgfm for raw speed/GFM compatibility, mistune for pure Python speed/customization, Python-Markdown for plugins/extensions.


<!--[[[end]]]-->

---

## Updating this README

This README uses [cogapp](https://nedbatchelder.com/code/cog/) to automatically generate project descriptions.

### Automatic updates

A GitHub Action automatically runs `cog -r -P README.md` on every push to main and commits any changes to the README or new `_summary.md` files.

### Manual updates

To update locally:

```bash
# Run cogapp to regenerate the project list
cog -r -P README.md
```

The script automatically:
- Discovers all subdirectories in this folder
- Gets the first commit date for each folder and sorts by most recent first
- For each folder, checks if a `_summary.md` file exists
- If the summary exists, it uses the cached version
- If not, it generates a new summary using `llm -m <!--[[[cog
print(MODEL, end='')
]]]-->
github/gpt-4.1
<!--[[[end]]]-->` with a prompt that creates engaging descriptions with bullets and links
- Creates markdown links to each project folder on GitHub
- New summaries are saved to `_summary.md` to avoid regenerating them on every run

To regenerate a specific project's description, delete its `_summary.md` file and run `cog -r -P README.md` again.
