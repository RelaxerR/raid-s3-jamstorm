# Проверки Pull Request

GitHub Actions проверяет версию проекта и согласованность зависимости Unity Test Framework между `Packages/manifest.json` и `Packages/packages-lock.json` в каждом PR в `main`. Затем GameCI запускает тесты EditMode и PlayMode в версии редактора из `ProjectSettings/ProjectVersion.txt` (сейчас Unity 6000.5.1f1). Отчёты Unity доступны в артефакте `unity-test-results`, когда редактор их создаёт.

В `Assets/_Project/Scripts/Tests` находятся тесты модели и пайплайна v2. Проверка структуры выполняется также через `python3 tools/check_project_structure.py`. [Команды и сценарии v2](v2/validation.md).

## Активация Unity

В настройках [GitHub Actions secrets](https://github.com/RelaxerR/raid-s3-jamstorm/settings/secrets/actions) администратор добавляет `UNITY_EMAIL` и `UNITY_PASSWORD` для учётной записи CI и один из секретов по типу лицензии. Для Personal нужен `UNITY_LICENSE`, для Pro — `UNITY_SERIAL`. См. [инструкцию GameCI](https://game.ci/docs/github/activation/). Если активация не настроена или секреты недоступны событию PR, проверка Unity завершится ошибкой. Секреты доступны коду, выполняемому Unity. Запускайте CI с учётными данными только для доверенных веток.

## Релизные сборки

[Release builds](https://github.com/RelaxerR/raid-s3-jamstorm/actions/workflows/windows-release.yml) запускается после push в `main` и вручную через `workflow_dispatch`. Для проверки PR выберите его ветку при ручном запуске. Windows и macOS собираются параллельно и публикуют ZIP независимо в разделе **Artifacts** прогона.

- **Windows x64**. GameCI кладёт Player в `build/StandaloneWindows64`. `raid-s3-jamstorm-Windows.zip` содержит в корне `raid-s3-jamstorm.exe`, `raid-s3-jamstorm_Data` и остальные файлы Player. Перед загрузкой CI проверяет входные файлы, целостность и содержимое ZIP. Задание **Launch Windows player** распаковывает его и запускает Player в режиме без графики на Windows runner. Чтобы проверить главное меню с графикой, на Windows распакуйте весь ZIP и откройте `raid-s3-jamstorm.exe` из распакованного каталога.
- **macOS Universal**. `CiBuild.BuildMacOSUniversal` собирает `build/StandaloneOSX/raid-s3-jamstorm.app`. В корне `raid-s3-jamstorm-macOS-Universal.zip` лежит `.app` с исполняемым файлом. CI проверяет путь из `Info.plist` и целостность ZIP.

GitHub Release создаётся для `main` после успешной сборки обеих платформ и запуска Windows Player. Обе сборки и тег берут номер из поля `bundleVersion` в `ProjectSettings/ProjectSettings.asset`. Текущая версия — `0.1.0`, тег — `v0.1.0`. Если тег уже указывает на другой коммит, ZIP остаются в артефактах прогона. Для следующего релиза увеличьте `bundleVersion`. Сбой macOS оставляет Windows ZIP доступным в артефактах прогона.

## Защита main

В приватном репозитории требуется тариф GitHub с поддержкой защиты веток и доступ администратора. После успешного прогона Unity администратор настраивает правило для `main` в [Settings → Branches](https://github.com/RelaxerR/raid-s3-jamstorm/settings/branches).

1. Включить **Require a pull request before merging**.
2. Включить **Require status checks to pass before merging**, выбрать проверку **Unity EditMode and PlayMode** от GitHub Actions и потребовать актуальности ветки PR относительно `main`.
3. Включить **Do not allow bypassing the above settings**. Оставить запрет на force push и удаление ветки.

После появления workflow в `main` открытый PR #1 запустит проверки при следующем обновлении ветки или переоткрытии.
