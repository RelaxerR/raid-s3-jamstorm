#!/usr/bin/env bash
set -euo pipefail

app=${1:?Pass the Universal Unity app}
archive=${2:?Pass the final ZIP path}

fail() {
    echo "::error::$*" >&2
    exit 1
}

for secret in MACOS_CERTIFICATE_P12_BASE64 MACOS_CERTIFICATE_PASSWORD APPLE_ID \
    APPLE_APP_SPECIFIC_PASSWORD APPLE_TEAM_ID; do
    [[ -n ${!secret:-} ]] || fail "Configure the GitHub Actions secret $secret. See docs/ci.md."
done
[[ $APPLE_TEAM_ID =~ ^[A-Z0-9]{10}$ ]] || fail 'APPLE_TEAM_ID must be a ten-character Apple Developer Team ID.'

[[ $app = /* ]] || app="$PWD/$app"
[[ $archive = /* ]] || archive="$PWD/$archive"
[[ ! -e $archive ]] || fail 'Final macOS ZIP already exists.'
entitlements="$(dirname "$0")/macos_player.entitlements"
executable=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$app/Contents/Info.plist")
main_executable="$app/Contents/MacOS/$executable"
[[ -x $main_executable ]] || fail 'Unity app is missing its executable.'
lipo -verify_arch x86_64 arm64 "$main_executable"

# Keep the certificate and its temporary keychain on the signing runner only.
umask 077
work_dir=$(mktemp -d "${RUNNER_TEMP:-${TMPDIR:-/tmp}}/macos-signing.XXXXXX")
keychain="$work_dir/signing.keychain-db"
trap 'security delete-keychain "$keychain" >/dev/null 2>&1 || true; rm -rf -- "$work_dir"' EXIT
keychain_password=$(openssl rand -hex 32)
printf '%s' "$MACOS_CERTIFICATE_P12_BASE64" | base64 -D > "$work_dir/signing.p12"
security create-keychain -p "$keychain_password" "$keychain"
security unlock-keychain -p "$keychain_password" "$keychain"
security import "$work_dir/signing.p12" -k "$keychain" -f pkcs12 \
    -P "$MACOS_CERTIFICATE_PASSWORD" -T /usr/bin/codesign >/dev/null
security set-key-partition-list -S apple-tool:,apple: -s -k "$keychain_password" "$keychain" >/dev/null
identity=$(security find-identity -v -p codesigning "$keychain" | awk '
    /"Developer ID Application:/ { print $2; count++ }
    END { if (count != 1) exit 1 }
') || fail 'The PKCS#12 must contain one valid Developer ID Application identity with its private key.'
sign=(codesign --force --sign "$identity" --keychain "$keychain" --timestamp --options runtime)
requirement="=anchor apple generic and certificate leaf[field.1.2.840.113635.100.6.1.13] exists and certificate leaf[subject.OU] = \"$APPLE_TEAM_ID\""

# Process nested code inside-out. Unity may put native libraries under Resources.
# Only the main player receives the Mono entitlement, via the top-level bundle.
find "$app/Contents" -depth -mindepth 1 \( -type f -o -type d \) -print0 > "$work_dir/entries"
while IFS= read -r -d '' entry; do
    [[ $entry != "$main_executable" ]] || continue
    if [[ -f $entry ]]; then
        file_type=$(file -b "$entry")
        case $file_type in
            Mach-O*executable*|Mach-O*'dynamically linked shared library'*|Mach-O*'bundle'*)
                "${sign[@]}" "$entry" ;;
        esac
    else
        case $entry in
            *.app|*.appex|*.bundle|*.framework|*.plugin|*.xpc) "${sign[@]}" "$entry" ;;
        esac
    fi
done < "$work_dir/entries"
"${sign[@]}" --entitlements "$entitlements" "$app"
codesign --verify --deep --strict -R "$requirement" "$app"

# Submit a ZIP to Apple, then staple the app and create a fresh distribution ZIP.
ditto -c -k --sequesterRsrc --keepParent "$app" "$work_dir/submission.zip"
xcrun notarytool submit "$work_dir/submission.zip" --apple-id "$APPLE_ID" \
    --password "$APPLE_APP_SPECIFIC_PASSWORD" --team-id "$APPLE_TEAM_ID" \
    --wait --timeout 25m --output-format plist > "$work_dir/notary.plist"
status=$(plutil -extract status raw -o - "$work_dir/notary.plist")
submission_id=$(plutil -extract id raw -o - "$work_dir/notary.plist")
[[ $status == Accepted ]] || fail "Apple notarization returned $status for submission $submission_id. See docs/ci.md."
xcrun stapler staple "$app"

final_zip="$work_dir/final.zip"
ditto -c -k --sequesterRsrc --keepParent "$app" "$final_zip"
unzip -tq "$final_zip"
ditto -x -k "$final_zip" "$work_dir/extracted"
extracted_app="$work_dir/extracted/${app##*/}"
extracted_executable="$extracted_app/Contents/MacOS/$executable"
[[ -x $extracted_executable ]] || fail 'The final ZIP does not contain an executable Unity app.'
lipo -verify_arch x86_64 arm64 "$extracted_executable"
codesign --verify --deep --strict -R "$requirement" "$extracted_app"
xcrun stapler validate "$extracted_app"
mv "$final_zip" "$archive"
