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

## Предыдущая проверка каркаса PR5

18.11.2026: Unity **6000.5.1f1** импортировала отдельную копию `/tmp/raid-v2-check` и выполнила **6/6 тестов успешно**, 0 failed, 0 skipped. Полный сценарий со входом в Play Mode прошёл. XML: `/tmp/raid-v2-editmode.xml`, лог: `/tmp/raid-v2-unity.log` (локальные артефакты, в Git не добавляются).

`python3 tools/check_project_structure.py --baseline origin/main` прошёл: 129 уникальных GUID, сохранены все исходные GUID ассетов, согласованы сцены, ссылки документов и lock-файл. `git diff --cached --check` прошёл после удаления пробелов в новых YAML-полях.

Исходный проект открыт другим экземпляром Unity, поэтому batch-проверки проводились в копии Assets/Packages/ProjectSettings, без вмешательства в рабочий редактор. Первый запуск в sandbox остановлен из-за недоступности ILPP-сокета; штатный запуск компиляции и тестов вне sandbox завершился успешно.

Windows x64 development-сборка через `SecondCatch.EditorTools.ProjectBuild.BuildWindows` **успешна**, Unity завершилась с кодом 0, лог содержит `Build Finished, Result: Success`. Артефакт: `/tmp/raid-v2-check/Build/Windows/SecondCatch.exe` (вся папка Windows нужна для запуска), лог `/tmp/raid-v2-windows.log`. ValidateScenes выполнена внутри команды сборки. Сборка содержит четыре сцены каркаса; рабочий прототип SampleScene исключён.

Импорт/сборка содержат предупреждения пакетов URP о недоступной ray tracing-компиляции на хосте; они не помешали сборке. Запуск `.exe`, визуальная проверка SampleScene и измерение производительности на Windows не выполнялись. Серый показ ещё требует игрового наполнения из [границ этапа](scope.md).

## Автоматические тесты ветки waves-system

Существующая Editor-сборка `Tests` расширена ссылками на OceanPro и UnityEngine.UI. Создана общая оснастка BoatTestFixture с очисткой объектов после каждого теста. Расчёты используют фиксированный шаг времени через те же методы, которые вызываются Update и LateUpdate. Случайный выбор ветра проверяется с seed 18011 и восстановлением Random.state. Порядок тестов произвольный, каждый создаёт собственные объекты или открывает нужную сцену.

| Класс | Случаев | Автоматическое покрытие |
| --- | --- | --- |
| WaveControllerTests | 6 | Направление к острову, рост силы и наружного сопротивления, общий центр и радиусы четырёх волн, конечность радиальной высоты, перенос центра, независимость дрейфа от гребли, seed ветра, согласованность карты/мини-карты и сценовых ссылок плавучести |
| BoatBuoyancyTests | 1 | Четыре дочерние точки, положительная вертикальная поддержка, ограничение крена/тангажа и вертикальной скорости |
| BoatHealthTests | 4 | Риф 15, скала 25, мусор 5, повторный контакт в cooldown и после его истечения, нижний предел HP, однократное уничтожение |
| BoatRespawnControllerTests | 1 | Запуск при нуле HP, возврат 100 HP, сброс линейной/угловой скорости и дрейфа, освобождение станции, возврат игрока на палубу, сохранение положения/угла паруса |
| WindFlagControllerTests | 6 | Local position и высота, родитель, компенсация курса и крена, глобальный ветер, оси +Z/+X/+Y, дочерний visual, авторская ориентация и yaw offset, последнее направление при некорректном/отключённом ветре |
| BoatMovementTests | 11 | W/S, блокировка обоих знаков гребли при Partial/Full, равенство ускорения парусному ходу, epsilon 0.001, штиль, нулевая эффективность, возвращение доступности после складывания, время разгона, масса, торможение, сопротивление и инерция курса |
| SailStationTests | 4 | Folded/Partial/Full, рост и затухание угловой скорости, сохранение угла после выбега и выхода |
| HelmStationTests | 3 | Возврат руля в центр, плавный отпуск A/D, сохранение управления рулём при блокировке гребли, строки статуса и подсказки через GameplayHud |
| BoatCameraControllerTests | 3 | Азимут, ограничение вертикального угла и дистанции, zoom, команда возврата камеры, плавные переходы, устойчивость горизонта при крене |
| ScenePipelineTests | 3 | Build Settings, ссылки четырёх сцен, Boot → MainMenu → HomePort → HomeShallows → HomePort, MainMenuController/HomePortController/HomeShallowsController, Menu/Continue, новая сессия, единственность GameApplication, защита повторной загрузки, отсутствие Photon/кооперативного хоста в одиночном pipeline |
| SoloGameSessionTests | 5 | Начальные BoatState/FishCargo, HP/груз между рейсами, Revision, лимит и вид рыбы, продажа, апгрейд, однократная потеря груза при гибели |
| StationAssignmentTests | 2 | Эксклюзивность назначения, сохранение старого назначения при отказе, освобождение, невалидные аргументы и повторные команды |

Три класса PR5 сохранились после merge и расширены. Правила HP модели PR5 составляют 60 HP с возвратом к 18 HP при гибели. Graybox использует BoatHealth с 100 HP и локальным респавном. Оба существующих правила покрыты соответствующими тестами. Подробности состояния в памяти приведены в [pipeline](loading-pipeline.md).

## Результат запуска 18.11.2026 после расширения graybox

Unity **6000.5.1f1**, EditMode, **49/49 Passed**, 0 failed, 0 skipped. Полный сценарий PR5 с EnterPlayMode/ExitPlayMode выполнен успешно. XML сохранён в `/tmp/waves-tests.xml`, лог в `/tmp/waves-validation.log`.

Рабочий проект открыт в редакторе, поэтому тесты запускались в отдельной копии `/tmp/waves-validation`. Первые два сбоя тестовой оснастки исправлены до итогового запуска. Для работы ILPP компиляция и тесты выполнены вне sandbox. Проверка `python3 tools/check_project_structure.py --baseline origin/main` подтвердила 178 уникальных GUID, сохранность исходных GUID, порядок сцен, ссылки и package lock. Сравнение graybox с состоянием перед расширением подтвердило сохранность local position, rotation, scale и parent всех 51 существовавших Transform.

Ручная визуальная проверка флага при развороте лодки, реальные нажатия V/ПКМ/колеса, читаемость HUD, вид поверхности и игровых дистанций остаются ручными сценариями. Они описаны в [управлении graybox](../graybox-controls.md#проверка-глобального-флага-и-гребли). В текущем изменении Windows-сборка и интерактивный playtest не выполнялись.
