# `md_root_path` - Root path for resolving root-relative links

Tags: `configuration`, `links`

This setting specifies the root directory used when resolving root-relative links
(links that start with `/`).

## Usage

Root-relative links are commonly used in static site generators like Jekyll and
GitHub Pages, where `/` refers to the site root rather than the file system root.

```markdown
[Home page](/index.md)
![Logo](/assets/images/logo.png)
```

By default, root-relative links are resolved from the repository or solution root.
Use this setting to specify a different base path.

## Configuration

Set the `md_root_path` in your `.editorconfig` file:

```ini
[*.md]
md_root_path = docs
```

This would resolve `/index.md` to `docs/index.md` relative to the repository root.

## YAML Front Matter Override

The root path can also be overridden on a per-file basis using YAML front matter:

```markdown
---
root_path: /docs/v2
---

[API Reference](/api/index.md)
```

This would resolve `/api/index.md` to `/docs/v2/api/index.md`.

## Rationale

Different projects use different conventions for root-relative links. This setting
provides flexibility to match the link resolution behavior of your documentation
platform or static site generator.
