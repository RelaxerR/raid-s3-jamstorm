# Проверки Pull Request

GitHub Actions проверяет версию проекта и согласованность зависимости Unity Test Framework между `Packages/manifest.json` и `Packages/packages-lock.json` в каждом PR в `main`. Затем GameCI запускает тесты EditMode и PlayMode в версии редактора из `ProjectSettings/ProjectVersion.txt` (сейчас Unity 6000.5.1f1). Отчёты Unity доступны в артефакте `unity-test-results`, когда редактор их создаёт.

В `Assets/_Project/Scripts/Tests` находятся тесты модели и пайплайна v2. Проверка структуры выполняется также через `python3 tools/check_project_structure.py`. [Команды и сценарии v2](v2/validation.md).

## Активация Unity

В настройках [GitHub Actions secrets](https://github.com/RelaxerR/raid-s3-jamstorm/settings/secrets/actions) администратор добавляет `UNITY_EMAIL` и `UNITY_PASSWORD` для учётной записи CI и один из секретов по типу лицензии. Для Personal нужен `UNITY_LICENSE`, для Pro — `UNITY_SERIAL`. См. [инструкцию GameCI](https://game.ci/docs/github/activation/). Если активация не настроена или секреты недоступны событию PR, проверка Unity завершится ошибкой. Секреты доступны коду, выполняемому Unity. Запускайте CI с учётными данными только для доверенных веток.

## Windows-релиз

Workflow `.github/workflows/windows-release.yml` запускается после каждого push в `main` (в том числе после слияния PR) и вручную через `workflow_dispatch`. Он собирает `raid-s3-jamstorm-Windows.zip` и macOS Universal-сборку `raid-s3-jamstorm-macOS-Universal.zip`, после чего создаёт GitHub Release. Версия `0.0.<номер запуска>` записывается и в Unity Player Settings (`Application.version`/bundle version), и в GitHub Release tag (`v0.0.<номер запуска>`). Для публикации релиза workflow использует `contents: write`; Unity-активация использует те же `UNITY_EMAIL`, `UNITY_PASSWORD` и `UNITY_LICENSE` или `UNITY_SERIAL`.

## Защита main

В приватном репозитории требуется тариф GitHub с поддержкой защиты веток и доступ администратора. После успешного прогона Unity администратор настраивает правило для `main` в [Settings → Branches](https://github.com/RelaxerR/raid-s3-jamstorm/settings/branches).

1. Включить **Require a pull request before merging**.
2. Включить **Require status checks to pass before merging**, выбрать проверку **Unity EditMode and PlayMode** от GitHub Actions и потребовать актуальности ветки PR относительно `main`.
3. Включить **Do not allow bypassing the above settings**. Оставить запрет на force push и удаление ветки.

После появления workflow в `main` открытый PR #1 запустит проверки при следующем обновлении ветки или переоткрытии.
