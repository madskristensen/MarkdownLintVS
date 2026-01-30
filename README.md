# Markdown Lint for Visual Studio

[![Build](https://github.com/madskristensen/MarkdownLintVS/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/MarkdownLintVS/actions/workflows/build.yaml)
![GitHub Sponsors](https://img.shields.io/github/sponsors/madskristensen)

A powerful Visual Studio extension that brings real-time Markdown linting to your editor, based on the popular [markdownlint](https://github.com/DavidAnson/markdownlint) rules (MD001-MD062).

## Features

✅ **Real-time linting** - See issues as you type with squiggly underlines  
✅ **Error List integration** - All warnings appear in Visual Studio's Error List  
✅ **Quick fixes** - Press `Ctrl+.` for automatic fixes (light bulb suggestions)  
✅ **Fix All support** - Fix all violations of a rule or all auto-fixable issues at once  
✅ **Inline suppression** - Suppress rules with `<!-- markdownlint-disable-line -->` comments  
✅ **Format Document** - Auto-fix all issues via Format Document (`Ctrl+K, Ctrl+D`)  
✅ **Lint Folder/Solution** - Lint all Markdown files in a folder, project, or solution  
✅ **EditorConfig support** - Configure rules per-project using `.editorconfig` files  
✅ **Options page** - Toggle rules on/off via Tools → Options → Markdown Lint → Rules  
✅ **Context menu** - Quick access to enable/disable linting and settings  
✅ **50+ rules** - Comprehensive coverage based on markdownlint standards (MD001-MD062)  
✅ **Works with any Markdown editor** - Including the built-in Markdown editor and most Markdown extensions

## Installation

1. Open Visual Studio 2022
2. Go to **Extensions** → **Manage Extensions**
3. Search for "**Markdown Lint**"
4. Click **Download** and restart Visual Studio

Or download directly from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.MarkdownLint).

## Getting Started

Once installed, the extension works automatically on any `.md` file. It is compatible with both the built-in Markdown editor in Visual Studio and the [Markdown Editor v2](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.MarkdownEditor2) extension.

1. Open a Markdown file
2. Linting issues appear as squiggly underlines
3. Hover over issues to see the rule description
4. Press `Ctrl+.` on an issue to see available quick fixes
5. Check the **Error List** (`Ctrl+\, E`) for all issues in the document

### Context Menu

Right-click in any Markdown file to access the **Markdown Lint** submenu:

- **Enabled** - Toggle linting on/off (checkmark indicates current state)
- **Settings...** - Open the Rules options page directly

### Lint Folder / Solution

Right-click on any folder, project, or solution in **Solution Explorer** to lint all Markdown files:

- **Lint Markdown Files** - Lint all `.md` files in the selected folder or project
- **Lint All Markdown Files** - Lint all `.md` files in the entire solution

**Open Folder mode:** When using Visual Studio's **Open Folder** feature (without a solution), right-click on any folder or file in the **Folder View** to lint all Markdown files in that location.

Results appear in the **Error List** window, with clickable entries to navigate directly to issues. Files are processed in parallel for fast performance. The error list is automatically cleared when you close the solution or folder.

**Ignored folders:** By default, common folders like `node_modules`, `vendor`, `.git`, `bin`, and `obj` are automatically ignored. You can customize this list in **Tools → Options → Markdown Lint → General → Ignored Folders**.

**`.markdownlintignore` support:** Create a `.markdownlintignore` file in your project root to exclude additional files or patterns (uses `.gitignore` syntax).

### Format Document

Use **Format Document** (`Ctrl+K, Ctrl+D`) or **Format Selection** (`Ctrl+K, Ctrl+F`) to automatically fix all auto-fixable violations. The first time you use this feature, you'll be prompted to enable or disable automatic fixing on format commands. You can change this setting later in **Tools → Options → Markdown Lint → General**.

## Configuration

Rules can be configured in two ways:

1. **Options Page** - Configure general settings and toggle rules via **Tools → Options → Markdown Lint**
2. **EditorConfig** - Configure rules per-project using `.editorconfig` files

EditorConfig settings take precedence over the Options page. If no EditorConfig rule exists for a specific rule, the Options page setting is used.

### General Options

Go to **Tools → Options → Markdown Lint → General** to configure:

| Setting                  | Description                                                                                                                     |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------- |
| Linting Enabled          | Enable or disable all markdown linting                                                                                          |
| Format Document Behavior | Control auto-fixing on Format Document/Selection (Ask, On, Off)                                                                 |
| Ignored Folders          | Comma-separated list of folders to ignore when linting (default: `node_modules, vendor, .git, bin, obj, packages, TestResults`) |

### Rules Options

Go to **Tools → Options → Markdown Lint → Rules** to access all rule settings organized by category:

| Category         | Rules                                          |
| ---------------- | ---------------------------------------------- |
| 1. Headings      | MD001, MD003, MD018-MD026, MD041               |
| 2. Lists         | MD004, MD005, MD007, MD029, MD030, MD032       |
| 3. Whitespace    | MD009, MD010, MD012, MD013, MD047              |
| 4. Code Blocks   | MD014, MD031, MD040, MD046, MD048              |
| 5. Links         | MD011, MD034, MD039, MD042, MD045, MD051-MD054, MD061, MD062 |
| 6. Inline        | MD033, MD035-MD038, MD049, MD050               |
| 7. Blockquotes   | MD027, MD028                                   |
| 8. Tables        | MD055, MD056, MD058, MD060                     |
| 9. Accessibility | MD059                                          |

Changes take effect immediately when you click OK or Apply - all open markdown files are automatically revalidated.

### EditorConfig

Rules can also be configured in your `.editorconfig` file. All rules use the `md_` prefix.

> 💡 **Tip:** Install the [EditorConfig Language Service](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.EditorConfig) extension to get IntelliSense and validation for markdown lint rules in your `.editorconfig` files.

### Example .editorconfig

```ini
[*.md]
# Disable trailing spaces rule
md_no_trailing_spaces = false

# Set line length to 120 characters
md_line_length = 120

# Change severity to error
md_no_hard_tabs = true:error

# Change severity to suggestion
md_no_multiple_blanks = true:suggestion

# Use specific heading style with warning severity
md_heading_style = atx:warning
```

### Severity Levels

Severity is specified after the value using a colon: `rule = value:severity`

```ini
md_no_trailing_spaces = true:error
md_line_length = 120:suggestion
md_heading_style = atx:warning
```

Available severity levels:

- `error` - Shown as error (red squiggle)
- `warning` - Shown as warning (green squiggle) [default]
- `suggestion` - Shown as suggestion/hint
- `silent` - Rule runs but doesn't report
- `none` - Rule is disabled

To disable a rule entirely, set the value to `false`:

```ini
md_no_trailing_spaces = false
```

### Available Rules

<details>
<summary>Click to expand the full list of supported rules</summary>
<br>

| EditorConfig Property                 | Rule  | Description                                                              |
| ------------------------------------- | ----- | ------------------------------------------------------------------------ |
| `md_heading_increment`                | MD001 | Heading levels should only increment by one level at a time              |
| `md_heading_style`                    | MD003 | Heading style (atx, atx_closed, setext, consistent)                      |
| `md_ul_style`                         | MD004 | Unordered list style (asterisk, plus, dash, consistent)                  |
| `md_list_indent`                      | MD005 | Inconsistent indentation for list items at the same level                |
| `md_ul_indent`                        | MD007 | Unordered list indentation (default: 2 spaces)                           |
| `md_no_trailing_spaces`               | MD009 | Trailing spaces not allowed                                              |
| `md_no_hard_tabs`                     | MD010 | Hard tabs not allowed                                                    |
| `md_no_reversed_links`                | MD011 | Reversed link syntax                                                     |
| `md_no_multiple_blanks`               | MD012 | Multiple consecutive blank lines                                         |
| `md_line_length`                      | MD013 | Line length (default: 80 characters, disabled by default)                |
| `md_commands_show_output`             | MD014 | Dollar signs used before commands without showing output                 |
| `md_no_missing_space_atx`             | MD018 | No space after hash on atx style heading                                 |
| `md_no_multiple_space_atx`            | MD019 | Multiple spaces after hash on atx style heading                          |
| `md_no_missing_space_closed_atx`      | MD020 | No space inside hashes on closed atx style heading                       |
| `md_no_multiple_space_closed_atx`     | MD021 | Multiple spaces inside hashes on closed atx style heading                |
| `md_blanks_around_headings`           | MD022 | Headings should be surrounded by blank lines                             |
| `md_heading_start_left`               | MD023 | Headings must start at the beginning of the line                         |
| `md_no_duplicate_heading`             | MD024 | Multiple headings with the same content                                  |
| `md_single_title`                     | MD025 | Multiple top-level headings in the same document                         |
| `md_no_trailing_punctuation`          | MD026 | Trailing punctuation in heading                                          |
| `md_no_multiple_space_blockquote`     | MD027 | Multiple spaces after blockquote symbol                                  |
| `md_no_blanks_blockquote`             | MD028 | Blank line inside blockquote                                             |
| `md_ol_prefix`                        | MD029 | Ordered list item prefix (one, ordered, one_or_ordered, zero)            |
| `md_list_marker_space`                | MD030 | Spaces after list markers                                                |
| `md_blanks_around_fences`             | MD031 | Fenced code blocks should be surrounded by blank lines                   |
| `md_blanks_around_lists`              | MD032 | Lists should be surrounded by blank lines                                |
| `md_no_inline_html`                   | MD033 | Inline HTML not allowed (disabled by default)                            |
| `md_no_bare_urls`                     | MD034 | Bare URL used                                                            |
| `md_hr_style`                         | MD035 | Horizontal rule style (consistent, ---, ***, ___)                        |
| `md_no_emphasis_as_heading`           | MD036 | Emphasis used instead of a heading (disabled by default)                 |
| `md_no_space_in_emphasis`             | MD037 | Spaces inside emphasis markers                                           |
| `md_no_space_in_code`                 | MD038 | Spaces inside code span elements                                         |
| `md_no_space_in_links`                | MD039 | Spaces inside link text                                                  |
| `md_fenced_code_language`             | MD040 | Fenced code blocks should have a language specified                      |
| `md_first_line_heading`               | MD041 | First line in a file should be a top-level heading (disabled by default) |
| `md_no_empty_links`                   | MD042 | No empty links                                                           |
| `md_required_headings`                | MD043 | Required heading structure (disabled by default)                         |
| `md_proper_names`                     | MD044 | Proper names should have correct capitalization (disabled by default)    |
| `md_no_alt_text`                      | MD045 | Images should have alternate text (alt text)                             |
| `md_code_block_style`                 | MD046 | Code block style (fenced, indented, consistent)                          |
| `md_single_trailing_newline`          | MD047 | Files should end with a single newline character                         |
| `md_code_fence_style`                 | MD048 | Code fence style (backtick, tilde, consistent)                           |
| `md_emphasis_style`                   | MD049 | Emphasis style (asterisk, underscore, consistent)                        |
| `md_strong_style`                     | MD050 | Strong style (asterisk, underscore, consistent)                          |
| `md_link_fragments`                   | MD051 | Link fragments should be valid                                           |
| `md_reference_links_images`           | MD052 | Reference links and images should use a label that is defined            |
| `md_link_image_reference_definitions` | MD053 | Link and image reference definitions should be needed                    |
| `md_link_image_style`                 | MD054 | Link and image style                                                     |
| `md_table_pipe_style`                 | MD055 | Table pipe style (leading_and_trailing, consistent)                      |
| `md_table_column_count`               | MD056 | Table column count should be consistent                                  |
| `md_blanks_around_tables`             | MD058 | Tables should be surrounded by blank lines                               |
| `md_descriptive_link_text`            | MD059 | Link text should be descriptive                                          |
| `md_table_column_style`               | MD060 | Table column style should be consistent                                  |
| `md_file_links_exist`                 | MD061 | Local file links should exist (disabled by default)                      |
| `md_image_links_exist`                | MD062 | Local image links should exist (disabled by default)                     |
| `md_root_path`                        | -     | Root path for resolving root-relative links (e.g., `/images/logo.png`)   |

</details>

### Rule Documentation

For detailed documentation on each rule, see the [markdownlint Rules Documentation](https://github.com/DavidAnson/markdownlint/blob/main/doc/Rules.md).

## Quick Fixes

The extension provides intelligent quick fixes (`Ctrl+.`) for many common issues:

| Rule                    | Quick Fix                               |
| ----------------------- | --------------------------------------- |
| MD009                   | Remove trailing whitespace              |
| MD010                   | Replace tabs with spaces                |
| MD011                   | Fix reversed link syntax                |
| MD012                   | Remove extra blank lines                |
| MD018                   | Add space after heading hash            |
| MD019/MD021/MD027       | Normalize multiple spaces               |
| MD022/MD031/MD032/MD058 | Add blank lines around elements         |
| MD023                   | Remove leading whitespace from headings |
| MD026                   | Remove trailing punctuation             |
| MD034                   | Wrap bare URLs in angle brackets        |
| MD040                   | Add language identifier to code blocks  |
| MD045                   | Add alt text placeholder to images      |
| MD047                   | Add/remove trailing newline             |

### Fix All Actions

Right-click on any fixable issue to access bulk fix options:

- **Fix all [rule] violations in document** - Fix all instances of a specific rule
- **Fix all auto-fixable violations in document** - Fix all auto-fixable issues at once

## Inline Suppression

Suppress specific rules directly in your Markdown files using HTML comments, compatible with the standard [markdownlint](https://github.com/DavidAnson/markdownlint) suppression syntax.

### Suppressing Errors

There are multiple ways to suppress an error:

1. **From QuickInfo** - Hover over an error and click the "Suppress in code" link
2. **From Error List** - Right-click an error in the Error List and select "Suppress in code"
3. **Manually** - Type `<!-- markdownlint-disable-line -->` and use IntelliSense to add rule codes

### Suppression Comment Syntax

```markdown
<!-- markdownlint-disable-line MD013 -->
This line is excluded from the MD013 (line length) rule.

<!-- markdownlint-disable-line MD013 MD033 -->
This line is excluded from multiple rules.

<!-- markdownlint-disable MD013 -->
All lines below are excluded from MD013...
<!-- markdownlint-enable MD013 -->

<!-- markdownlint-disable-next-line MD041 -->
The next line is excluded from MD041.

<!-- markdownlint-disable-file MD013 -->
The entire file is excluded from MD013.
```

### IntelliSense Support

When typing suppression comments, IntelliSense provides:

- **Rule code completion** - Type `MD` to see all available rule codes (MD001, MD003, etc.)
- **Rule name completion** - Type the rule name like `heading-increment` or `line-length`
- **QuickInfo tooltips** - Hover over rule codes in suppression comments to see the rule description

### Supported Directives

| Directive | Description |
| --------- | ----------- |
| `disable-line` | Suppress rules on the current line |
| `disable-next-line` | Suppress rules on the next line |
| `disable` / `enable` | Suppress rules for a block of lines |
| `disable-file` | Suppress rules for the entire file |
| `capture` / `restore` | Save and restore suppression state |

## Contributing

Found a bug or have a feature request? Please open an issue on [GitHub](https://github.com/madskristensen/MarkdownLintVS/issues).

## License

This extension is open source. See the [LICENSE](LICENSE.txt) file for details.
