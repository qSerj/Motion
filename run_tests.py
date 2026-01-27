import subprocess
import sys
import os


def run_command(command, cwd=None):
    """Запускает команду и возвращает True, если успешно."""
    print(f"🔄 Running: {command}...")
    try:
        # shell=True нужно для Windows, чтобы подхватить пути
        result = subprocess.run(command, cwd=cwd, shell=True)
        if result.returncode == 0:
            print("✅ Success\n")
            return True
        else:
            print("❌ Failed\n")
            return False
    except Exception as e:
        print(f"❌ Error executing {command}: {e}\n")
        return False


def main():
    print("=" * 40)
    print("🛡️  MOTION TRAINER: GLOBAL TEST RUNNER")
    print("=" * 40)

    all_passed = True

    # 1. Python Backend Tests
    # Используем python -m pytest, как указано в docs/testing.md
    print("--- 🐍 BACKEND TESTS ---")
    if not run_command("python -m pytest", cwd="backend"):
        all_passed = False

    # 2. C# Frontend Tests
    # Используем dotnet test, как указано в docs/frontend/testing.md
    print("--- 🔷 FRONTEND TESTS ---")
    # Путь к проекту тестов
    test_proj = os.path.join("frontend", "Motion.Desktop.Tests")
    if not run_command(f"dotnet test", cwd=test_proj):
        all_passed = False

    print("=" * 40)
    if all_passed:
        print("🎉 ALL SYSTEMS GREEN. READY TO COMMIT.")
        sys.exit(0)
    else:
        print("🔥 SOME TESTS FAILED. DO NOT COMMIT.")
        sys.exit(1)


if __name__ == "__main__":
    main()