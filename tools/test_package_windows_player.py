"""Regression tests for the GameCI Windows archive layout."""

from pathlib import Path
import subprocess
from tempfile import TemporaryDirectory
import unittest
from zipfile import ZipFile

SCRIPT = Path(__file__).with_name("package_windows_player.sh").resolve()
BUILD_NAME = "raid-s3-jamstorm"


class WindowsPackagingTests(unittest.TestCase):
    def setUp(self):
        workspace = TemporaryDirectory()
        self.addCleanup(workspace.cleanup)
        self.root = Path(workspace.name)
        self.player = self.root / "build" / "StandaloneWindows64"
        self.player.mkdir(parents=True)
        self.exe = self.player / f"{BUILD_NAME}.exe"
        self.data = self.player / f"{BUILD_NAME}_Data"
        self.archive = self.root / f"{BUILD_NAME}-Windows.zip"

    def package(self):
        return subprocess.run(
            ["bash", str(SCRIPT), BUILD_NAME],
            cwd=self.root,
            capture_output=True,
            text=True,
        )

    def test_packages_the_entire_player_with_executable_and_data_at_root(self):
        self.exe.write_bytes(b"player")
        self.data.mkdir()
        (self.data / "globalgamemanagers").write_bytes(b"scenes")
        (self.player / "UnityPlayer.dll").write_bytes(b"runtime")

        result = self.package()

        self.assertEqual(result.returncode, 0, result.stderr)
        with ZipFile(self.archive) as archive:
            self.assertEqual(archive.read(self.exe.name), b"player")
            self.assertEqual(archive.read(f"{self.data.name}/globalgamemanagers"), b"scenes")
            self.assertIn("UnityPlayer.dll", archive.namelist())

    def test_rejects_a_missing_game_executable_even_when_crash_handler_exists(self):
        self.data.mkdir()
        (self.player / "UnityCrashHandler64.exe").touch()

        result = self.package()

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Missing Windows player", result.stderr)
        self.assertFalse(self.archive.exists())

    def test_rejects_missing_unity_data_directory(self):
        self.exe.touch()

        result = self.package()

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Missing Unity data directory", result.stderr)
        self.assertFalse(self.archive.exists())


if __name__ == "__main__":
    unittest.main()
