# Проверка каркаса v2

[Оглавление](../index.md) · [Компоненты](components.md) · [Pipeline](loading-pipeline.md)

## Повторяемые команды

Из корня репозитория:

```bash
python3 tools/check_project_structure.py
python3 tools/check_project_structure.py --baseline origin/main
git diff --check
```

Первая проверка проверяет уникальность GUID, наличие `.meta`, порядок сцен, пути/GUID Build Settings, ссылки на скрипты в сценах, локальные Markdown-ссылки, согласованность manifest/lock. Опциональный baseline дополнительно проверяет сохранность всех идентичностей ассетов исходной ветки. Проверка не заменяет импорт Unity и не пытается валидировать ссылки на встроенные/package-ассеты через текстовый поиск.

На macOS с Unity Hub (закрыть именно проверяемый экземпляр проекта либо использовать отдельную копию):

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.5.1f1/Unity.app/Contents/MacOS/Unity
"$UNITY" -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform EditMode \
  -testResults /tmp/raid-v2-editmode.xml -logFile /tmp/raid-v2-unity.log
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" \
  -executeMethod SecondCatch.EditorTools.ProjectBuild.ValidateScenes \
  -logFile /tmp/raid-v2-validate.log
```

Для Windows development-билда при установленном Windows Build Support:

```bash
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" \
  -executeMethod SecondCatch.EditorTools.ProjectBuild.BuildWindows \
  -logFile /tmp/raid-v2-windows.log
```

Результат последней команды — `Build/Windows/SecondCatch.exe`. Билд сам по себе не подтверждает 60 fps, Windows playtest и готовность серого показа.

## Ручной сценарий

Открыть Boot, дождаться MainMenu, New game. В порту Depart, в море собрать 10 рыб debug-кнопкой, вернуться, продать, купить усиление, выйти снова. Проверить HP, 100 вместимости, сохранение усиления, невозможность второй покупки, сохранение текущего рейса через Menu/Continue. Нанести урон до гибели: порт, 18 HP, половина груза. Начать New game из меню: 60 HP, нулевые монеты и груз, усиление отсутствует.

Проверка SampleScene отдельно: океан, спасательный круг и спавнер левиафана используют прежние GUID и API. Этот визуальный сценарий и окончательный Windows playtest требуют проверки в интерактивном редакторе/на Windows.

## Результат текущей ветки

18.11.2026: Unity **6000.5.1f1** импортировала отдельную копию `/tmp/raid-v2-check` и выполнила **6/6 тестов успешно**, 0 failed, 0 skipped. Полный сценарий со входом в Play Mode прошёл. XML: `/tmp/raid-v2-editmode.xml`, лог: `/tmp/raid-v2-unity.log` (локальные артефакты, в Git не добавляются).

`python3 tools/check_project_structure.py --baseline origin/main` прошёл: 129 уникальных GUID, сохранены все исходные GUID ассетов, согласованы сцены, ссылки документов и lock-файл. `git diff --cached --check` прошёл после удаления пробелов в новых YAML-полях.

Исходный проект открыт другим экземпляром Unity, поэтому batch-проверки проводились в копии Assets/Packages/ProjectSettings, без вмешательства в рабочий редактор. Первый запуск в sandbox остановлен из-за недоступности ILPP-сокета; штатный запуск компиляции и тестов вне sandbox завершился успешно.

Windows x64 development-сборка через `SecondCatch.EditorTools.ProjectBuild.BuildWindows` **успешна**, Unity завершилась с кодом 0, лог содержит `Build Finished, Result: Success`. Артефакт: `/tmp/raid-v2-check/Build/Windows/SecondCatch.exe` (вся папка Windows нужна для запуска), лог `/tmp/raid-v2-windows.log`. ValidateScenes выполнена внутри команды сборки. Сборка содержит четыре сцены каркаса; рабочий прототип SampleScene исключён.

Импорт/сборка содержат предупреждения пакетов URP о недоступной ray tracing-компиляции на хосте; они не помешали сборке. Запуск `.exe`, визуальная проверка SampleScene и измерение производительности на Windows не выполнялись. Серый показ ещё требует игрового наполнения из [границ этапа](scope.md).
