#!/usr/bin/env python3
"""Quick standalone test to verify agent setup without pytest."""

import sys
sys.path.insert(0, '..')

from ChronoViewTestAgent import ChronoViewCLI


def main():
    print("ChronoView Test Agent - Quick Test")
    print("=" * 50)

    try:
        cli = ChronoViewCLI()
        print(f"CLI executable: {cli.exe_path}")

        print("\n1. Testing connectivity...")
        result = cli.run(["test", "connectivity"])
        print(f"   Connected: {result.get('data', {}).get('connected')}")

        if result.get('success'):
            print("\n2. Getting statistics...")
            result = cli.run(["stats", "get"])
            print(f"   Result: {result}")

            print("\n3. Listing DataGrid rows...")
            result = cli.run(["datagrid", "rows"])
            row_count = len(result.get('data', {}).get('rows', []))
            print(f"   Rows: {row_count}")

        print("\n" + "=" * 50)
        print("Quick test complete!")
        return 0

    except Exception as e:
        print(f"Error: {e}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
