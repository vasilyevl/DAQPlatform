#!/usr/bin/env python3
"""
Script: comment_csproj_lines.py
Location: Scripts\Scripts.py

Recursively finds .csproj files under a root folder and comments out lines that contain
one or more specified keys. By default the script performs a dry run and prints what
would change. Use --apply to modify files. Backups (.bak) are created by default when applying.

Usage examples:
  python Scripts\Scripts.py -r .. -k PackageReference ProjectReference --apply
  python Scripts\Scripts.py -r C:\Projects\MySolution -k "MySecretKey" --pattern "*.csproj" --regex --apply
  python Scripts\Scripts.py -r . -kf keys.txt --apply

Notes:
  - Lines already containing XML comments ('<!--' or '-->') are skipped.
  - Matching is case-sensitive by default; use --ignore-case to ignore case.
  - When creating backups the script will rotate existing backups into a numbered
    pattern: .bak01, .bak02, ... Newest backup will have the highest number.
"""
from pathlib import Path
import argparse
import re
import shutil
import sys

def find_project_files(root: Path, pattern: str):
    return sorted(root.rglob(pattern))

def line_is_commented(line: str) -> bool:
    # quick check: if the line already contains an XML comment token, treat as commented
    return '<!--' in line or '-->' in line

def make_commented_line(line: str) -> str:
    # Preserve leading whitespace, wrap the trimmed content in an XML comment.
    leading_ws = re.match(r'^\s*', line).group(0)
    content = line.rstrip('\r\n')
    return f"{leading_ws}<!-- {content.lstrip()} -->\n"

def matches_any_key(line: str, patterns, ignore_case: bool, use_regex: bool) -> bool:
    hay = line if not ignore_case else line.lower()
    if use_regex:
        for p in patterns:
            if p.search(line if not ignore_case else line.lower()):
                return True
        return False
    else:
        for p in patterns:
            if (p in hay):
                return True
        return False

def load_keys_from_file(path: Path) -> list:
    with path.open('r', encoding='utf-8') as f:
        lines = [ln.strip() for ln in f if ln.strip() and not ln.strip().startswith('#')]
    return lines

def _rotate_and_create_backup(path: Path) -> Path:
    """
    Create a numbered backup for `path`. Rotation rules:
      - Find existing backups named "<name>.bak", "<name>.bak01", "<name>.bak02", ...
      - If a plain .bak exists, rename it into the numbering sequence (makes it an older backup).
      - Create the new backup as .bakNN where NN is max(existing_number) + 1 (formatted with at least 2 digits).
    Returns the Path to the created backup file.
    """
    parent = path.parent
    basename = path.name  # includes extension, e.g. MyProj.csproj
    # regex to match backups: basename + .bak or basename.bakNN (digits)
    pattern = re.compile(r'^' + re.escape(basename) + r'\.bak(?:([0-9]+))?$')
    existing_nums = []
    plain_bak = None

    for p in parent.iterdir():
        if not p.is_file():
            continue
        m = pattern.match(p.name)
        if not m:
            continue
        grp = m.group(1)
        if grp is None:
            plain_bak = p
        else:
            try:
                existing_nums.append(int(grp))
            except ValueError:
                pass

    max_num = max(existing_nums) if existing_nums else 0

    # If a plain .bak exists, move it into the numbering sequence
    if plain_bak is not None:
        if max_num == 0:
            # no numbered backups yet -> rename plain .bak to .bak01
            new_num = 1
        else:
            # numbered backups exist -> append plain .bak as next number
            new_num = max_num + 1
        target = parent / f"{basename}.bak{new_num:02d}"
        # if target exists (unlikely), find next free number
        tn = new_num
        while target.exists():
            tn += 1
            target = parent / f"{basename}.bak{tn:02d}"
        plain_bak.rename(target)
        max_num = max(max_num, tn)

    # Now create new backup with next number (max_num + 1)
    new_backup_num = max_num + 1
    new_backup = parent / f"{basename}.bak{new_backup_num:02d}"
    # If, for some reason, that file exists, advance until free
    nb = new_backup_num
    while (parent / f"{basename}.bak{nb:02d}").exists():
        nb += 1
    new_backup = parent / f"{basename}.bak{nb:02d}"
    shutil.copy2(path, new_backup)
    return new_backup

def process_file(path: Path, keys, use_regex: bool, ignore_case: bool, apply: bool, backup: bool):
    original = path.read_text(encoding='utf-8')
    lines = original.splitlines(keepends=True)
    changed = False
    new_lines = []
    regex_patterns = None

    if use_regex:
        flags = re.IGNORECASE if ignore_case else 0
        regex_patterns = [re.compile(k, flags) for k in keys]
    else:
        if ignore_case:
            keys = [k.lower() for k in keys]

    for ln in lines:
        if line_is_commented(ln):
            new_lines.append(ln)
            continue

        if matches_any_key(ln, regex_patterns if use_regex else keys, ignore_case, use_regex):
            new_lines.append(make_commented_line(ln))
            changed = True
        else:
            new_lines.append(ln)

    if not changed:
        return False, 0

    if not apply:
        # Dry run: don't write anything
        return True, sum(1 for o, n in zip(lines, new_lines) if o != n)

    # Apply changes: backup and write
    if backup:
        # create a rotated numbered backup so newest has highest number
        _rotate_and_create_backup(path)

    path.write_text(''.join(new_lines), encoding='utf-8')
    return True, sum(1 for o, n in zip(lines, new_lines) if o != n)

def main(argv):
    ap = argparse.ArgumentParser(description="Comment out lines in project files that contain specified keys.")
    ap.add_argument('-r', '--root', default='.', help="Root folder to search (default current directory).")
    ap.add_argument('-p', '--pattern', default='*.csproj', help="File glob pattern to search (default '*.csproj').")
    ap.add_argument('-k', '--keys', nargs='+', help="Keys to search for (space-separated).")
    ap.add_argument('-kf', '--keys-file', type=str, help="File with keys (one per line). Lines starting with # are ignored.")
    ap.add_argument('--regex', action='store_true', help="Treat keys as regular expressions.")
    ap.add_argument('--ignore-case', action='store_true', help="Ignore case when matching.")
    ap.add_argument('--apply', action='store_true', help="Write changes to files. If not set, performs a dry run.")
    ap.add_argument('--no-backup', dest='backup', action='store_false', help="Do not create .bak backups when applying.")
    ap.add_argument('--quiet', action='store_true', help="Minimize output.")
    args = ap.parse_args(argv)

    root = Path(args.root).resolve()
    if not root.exists() or not root.is_dir():
        print(f"Root folder does not exist or is not a directory: {root}")
        return 2

    keys = []
    if args.keys_file:
        keys.extend(load_keys_from_file(Path(args.keys_file)))
    if args.keys:
        keys.extend(args.keys)

    if not keys:
        print("No keys provided. Use --keys or --keys-file.")
        return 2

    files = find_project_files(root, args.pattern)
    if not files:
        if not args.quiet:
            print(f"No files found using pattern {args.pattern} under {root}")
        return 0

    total_files = 0
    total_changed_files = 0
    total_changed_lines = 0

    for f in files:
        total_files += 1
        changed, count = process_file(f, keys, args.regex, args.ignore_case, args.apply, args.backup)
        if changed:
            total_changed_files += 1
            total_changed_lines += count
            if args.quiet:
                continue
            if args.apply:
                print(f"Modified: {f}  (lines commented: {count})")
            else:
                print(f"Would modify: {f}  (lines to comment: {count})")

    if not args.quiet:
        mode = "APPLY" if args.apply else "DRY-RUN"
        print(f"\nSummary ({mode}): scanned {total_files} files, changed {total_changed_files} files, total lines commented: {total_changed_lines}")

    return 0

if __name__ == '__main__':
    raise SystemExit(main(sys.argv[1:]))