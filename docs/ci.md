# Проверки Pull Request

GitHub Actions проверяет версию проекта и согласованность зависимости Unity Test Framework между `Packages/manifest.json` и `Packages/packages-lock.json` в каждом PR в `main`. Затем GameCI запускает тесты EditMode и PlayMode в версии редактора из `ProjectSettings/ProjectVersion.txt` (сейчас Unity 6000.5.1f1). Отчёты Unity доступны в артефакте `unity-test-results`, когда редактор их создаёт.

В исходной версии проекта в `Assets/Scripts/Tests` есть сборка для EditMode. Она пока не содержит тестовых методов.

## Активация Unity

В настройках [GitHub Actions secrets](https://github.com/egorlehanov600-ux/TOP/settings/secrets/actions) администратор добавляет `UNITY_EMAIL` и `UNITY_PASSWORD` для учётной записи CI и один из секретов по типу лицензии. Для Personal нужен `UNITY_LICENSE`, для Pro — `UNITY_SERIAL`. См. [инструкцию GameCI](https://game.ci/docs/github/activation/). Если активация не настроена или секреты недоступны событию PR, проверка Unity завершится ошибкой. Секреты доступны коду, выполняемому Unity. Запускайте CI с учётными данными только для доверенных веток.

## Защита main

В приватном репозитории требуется тариф GitHub с поддержкой защиты веток и доступ администратора. После успешного прогона Unity администратор настраивает правило для `main` в [Settings → Branches](https://github.com/egorlehanov600-ux/TOP/settings/branches).

1. Включить **Require a pull request before merging**.
2. Включить **Require status checks to pass before merging**, выбрать проверку **Unity EditMode and PlayMode** от GitHub Actions и потребовать актуальности ветки PR относительно `main`.
3. Включить **Do not allow bypassing the above settings**. Оставить запрет на force push и удаление ветки.

После появления workflow в `main` открытый PR #1 запустит проверки при следующем обновлении ветки или переоткрытии.
