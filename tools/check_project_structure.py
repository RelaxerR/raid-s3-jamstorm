#!/usr/bin/env python3
"""Check v2 scene order, GUIDs, moved asset identities and local documentation links."""
import argparse
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parent.parent
PACKAGE_SCENE_SCRIPTS = {
    'com.unity.render-pipelines.universal': {
        'a79441f348de89743a2939f4d699eac1',  # UniversalAdditionalCameraData
        '474bcb49853aa07438625e644c072ee6',  # UniversalAdditionalLightData
    },
    'com.unity.ugui': {'76c392e42b5098c458856cdf6ecaaaa1'},  # EventSystem
    'com.unity.inputsystem': {'01614664b831546d2ae94a42149d80ac'},  # InputSystemUIInputModule
}


def check(baseline=None):
    errors = []
    manifest = json.loads((ROOT / 'Packages/manifest.json').read_text())['dependencies']
    # CI runs this check before Unity has downloaded registry packages.
    package_scripts = {guid for package, guids in PACKAGE_SCENE_SCRIPTS.items()
                       if package in manifest for guid in guids}
    guids = {}
    for meta in (ROOT / 'Assets').rglob('*.meta'):
        match = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(), re.M)
        if not match:
            errors.append(f'Invalid GUID: {meta.relative_to(ROOT)}')
            continue
        guid = match[1]
        if guid in guids:
            errors.append(f'Duplicate GUID: {meta.relative_to(ROOT)} / {guids[guid]}')
        guids[guid] = meta.relative_to(ROOT).as_posix()
        if not Path(str(meta)[:-5]).exists():
            errors.append(f'Orphan meta: {meta.relative_to(ROOT)}')
    for asset in (ROOT / 'Assets/_Project').rglob('*'):
        if asset.name.startswith('.') or asset.suffix == '.meta':
            continue
        if not Path(str(asset) + '.meta').exists():
            errors.append(f'Missing meta: {asset.relative_to(ROOT)}')
    expected = [f'Assets/_Project/Scenes/{scene}.unity' for scene in ('Boot', 'MainMenu', 'HomePort', 'HomeShallows')]
    build = (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text()
    entries = re.findall(r'- enabled: (\d)\s+path: ([^\n]+)\s+guid: ([0-9a-f]+)', build)
    if [path for enabled, path, guid in entries if enabled == '1'] != expected:
        errors.append('Build scene order differs from Boot -> MainMenu -> HomePort -> HomeShallows')
    for enabled, path, guid in entries:
        if guids.get(guid) != path + '.meta':
            errors.append(f'Build scene GUID/path mismatch: {path}')
        scene_path = ROOT / path
        if scene_path.exists():
            for script_guid in re.findall(r'm_Script:.*guid: ([0-9a-f]{32})', scene_path.read_text()):
                if script_guid not in guids and script_guid not in package_scripts:
                    errors.append(f'Missing script GUID in {path}: {script_guid}')
    for doc in [ROOT / 'README.md', * (ROOT / 'docs').rglob('*.md')]:
        for link in re.findall(r'\]\(([^)]+)\)', doc.read_text()):
            if re.match(r'^[a-zA-Z]+:', link) or link.startswith('#'):
                continue
            target = link.split('#')[0]
            if not (doc.parent / target).exists():
                errors.append(f'Broken link in {doc.relative_to(ROOT)}: {link}')
    lock = json.loads((ROOT / 'Packages/packages-lock.json').read_text())['dependencies']
    for package, version in manifest.items():
        if lock.get(package, {}).get('version') != version:
            errors.append(f'Package lock mismatch: {package}')
    if baseline:
        files = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', baseline, 'Assets'], cwd=ROOT, text=True).splitlines()
        for path in files:
            if not path.endswith('.meta'):
                continue
            old = subprocess.check_output(['git', 'show', f'{baseline}:{path}'], cwd=ROOT, text=True)
            match = re.search(r'^guid: ([0-9a-f]{32})$', old, re.M)
            if match and match[1] not in guids:
                errors.append(f'Lost existing asset identity: {path}')
    for error in errors:
        print('ERROR:', error)
    if errors:
        raise SystemExit(1)
    print(f'OK: {len(guids)} unique asset GUIDs; scene order, references, docs and package lock valid' + ('; baseline GUIDs preserved' if baseline else ''))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline', help='Optional Git ref whose asset GUIDs must be preserved')
    check(parser.parse_args().baseline)
