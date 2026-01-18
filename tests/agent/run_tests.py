#!/usr/bin/env python3
"""Convenience script to run ChronoView test agent."""

import sys
import subprocess
from pathlib import Path


def main():
    args = sys.argv[1:]
    cmd = ["pytest", "-v", "--html=reports/report.html", "--self-contained-html"]

    # Default behavior
    if not args:
        # Run only non-destructive tests by default
        cmd.extend(["-m", "not destructive"])
        print("Running non-destructive tests only...")
        print("Use --all to run all tests including destructive ones.")
    elif "--all" in args:
        cmd = ["pytest", "-v", "--html=reports/report.html", "--self-contained-html"]
        args.remove("--all")
        print("Running ALL tests including destructive operations...")
    else:
        cmd.extend(args)

    # Ensure reports directory exists
    Path("reports").mkdir(exist_ok=True)

    # Run pytest
    result = subprocess.run(cmd)
    return result.returncode


if __name__ == "__main__":
    sys.exit(main())
