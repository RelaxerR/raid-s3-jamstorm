# Проверки Pull Request

GitHub Actions проверяет версию проекта и согласованность зависимости Unity Test Framework между `Packages/manifest.json` и `Packages/packages-lock.json` в каждом PR в `main`. Затем GameCI запускает тесты EditMode и PlayMode в версии редактора из `ProjectSettings/ProjectVersion.txt` (сейчас Unity 6000.5.1f1). Отчёты Unity доступны в артефакте `unity-test-results`, когда редактор их создаёт.

В `Assets/_Project/Scripts/Tests` находятся тесты модели и пайплайна v2. Проверка структуры выполняется также через `python3 tools/check_project_structure.py`. [Команды и сценарии v2](v2/validation.md).

## Активация Unity

В настройках [GitHub Actions secrets](https://github.com/RelaxerR/raid-s3-jamstorm/settings/secrets/actions) администратор добавляет `UNITY_EMAIL` и `UNITY_PASSWORD` для учётной записи CI и один из секретов по типу лицензии. Для Personal нужен `UNITY_LICENSE`, для Pro — `UNITY_SERIAL`. См. [инструкцию GameCI](https://game.ci/docs/github/activation/). Если активация не настроена или секреты недоступны событию PR, проверка Unity завершится ошибкой. Секреты доступны коду, выполняемому Unity. Запускайте CI с учётными данными только для доверенных веток.

## Релизные сборки

[Release builds](https://github.com/RelaxerR/raid-s3-jamstorm/actions/workflows/windows-release.yml) запускается после push в `main` и вручную через `workflow_dispatch`. Для проверки PR выберите его ветку при ручном запуске. Windows и macOS собираются параллельно и публикуют ZIP независимо в разделе **Artifacts** прогона.

- **Windows x64**. GameCI кладёт Player в `build/StandaloneWindows64`. `raid-s3-jamstorm-Windows.zip` содержит в корне `raid-s3-jamstorm.exe`, `raid-s3-jamstorm_Data` и остальные файлы Player. Перед загрузкой CI проверяет входные файлы, целостность и содержимое ZIP. Задание **Launch Windows player** распаковывает его и запускает Player в режиме без графики на Windows runner. Чтобы проверить главное меню с графикой, на Windows распакуйте весь ZIP и откройте `raid-s3-jamstorm.exe` из распакованного каталога.
- **macOS Universal**. `CiBuild.BuildMacOSUniversal` собирает `build/StandaloneOSX/raid-s3-jamstorm.app`. Задание **Sign and notarize macOS Universal player** подписывает вложенный код и приложение с Developer ID Application и Hardened Runtime, отправляет ZIP в Apple через `notarytool` и прикрепляет тикет к `.app`. После повторной упаковки задание распаковывает итоговый ZIP в отдельный каталог и проверяет исполняемый файл для `x86_64` и `arm64`, подпись и тикет. Артефакт `raid-s3-jamstorm-macOS-Universal.zip` загружается после этих проверок.

GitHub Release создаётся для `main` после сборки обеих платформ, запуска Windows Player и проверки подписанного и нотарифицированного macOS ZIP. Обе сборки и тег берут номер из поля `bundleVersion` в `ProjectSettings/ProjectSettings.asset`. Если тег уже указывает на другой коммит, ZIP остаются в артефактах прогона. Для следующего релиза увеличьте `bundleVersion`. Сбой macOS оставляет Windows ZIP доступным в артефактах прогона.

### Настройка подписи macOS

Администратор добавляет следующие имена в [GitHub Actions secrets](https://github.com/RelaxerR/raid-s3-jamstorm/settings/secrets/actions).

| Секрет | Значение |
| --- | --- |
| `MACOS_CERTIFICATE_P12_BASE64` | PKCS#12 с действующим сертификатом **Developer ID Application** и его закрытым ключом, закодированный в Base64. Экспортируйте идентификатор подписи из Keychain Access. |
| `MACOS_CERTIFICATE_PASSWORD` | Пароль экспорта PKCS#12. |
| `APPLE_ID` | Apple Account, которому доступна нотарификация от имени команды. |
| `APPLE_APP_SPECIFIC_PASSWORD` | Пароль приложения, созданный для этого Apple Account. |
| `APPLE_TEAM_ID` | Десятисимвольный Team ID той же команды Apple Developer, которая выпустила сертификат. |

Запускайте **Release builds** с этими секретами только для доверенного кода. Для проверки без публикации GitHub Release запустите workflow вручную на доверенной ветке вне `main`. Отсутствие секрета или сбой проверки останавливает задание macOS. Если Apple отклонит сборку и задание напечатает ID заявки, владелец учётной записи может получить диагностику командой `xcrun notarytool log <submission-id>` с локально настроенным профилем Keychain (`--keychain-profile`). [Инструкция Apple по нотарификации](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow).

Для приёмки #31 скачайте свежий ZIP браузером на Mac со штатными настройками Gatekeeper, где приложение ещё не получало исключение. Распакуйте ZIP, откройте `.app` двойным кликом из Finder и дойдите до главного меню без **Open Anyway**. До публикации релиза тот же сценарий можно пройти с ZIP из артефактов ручного прогона. Повторите его с опубликованным архивом, когда он появится в GitHub Releases.

## Защита main

В приватном репозитории требуется тариф GitHub с поддержкой защиты веток и доступ администратора. После успешного прогона Unity администратор настраивает правило для `main` в [Settings → Branches](https://github.com/RelaxerR/raid-s3-jamstorm/settings/branches).

1. Включить **Require a pull request before merging**.
2. Включить **Require status checks to pass before merging**, выбрать проверку **Unity EditMode and PlayMode** от GitHub Actions и потребовать актуальности ветки PR относительно `main`.
3. Включить **Do not allow bypassing the above settings**. Оставить запрет на force push и удаление ветки.

После появления workflow в `main` открытый PR #1 запустит проверки при следующем обновлении ветки или переоткрытии.
