# Работа с Яндекс Вики

`tools/wiki.py` работает с публичным API Яндекс Вики из корня репозитория.
Нужен Python 3.8 или новее. Сторонние пакеты не требуются.

Каждый коллаборатор получает свой токен и передаёт его через окружение процесса,
который запускает скрипт. Храните токен локально и не добавляйте его в Git или чат.

| Переменная | Значение |
| --- | --- |
| `WIKI_TOKEN` | OAuth- или IAM-токен пользователя |
| `WIKI_ORG_ID` | ID организации |
| `WIKI_ORG_HEADER` | По умолчанию `X-Org-Id` для Яндекс 360. Для Yandex Identity Hub укажите `X-Cloud-Org-Id`. |
| `WIKI_AUTH_SCHEME` | По умолчанию `OAuth`. Для IAM-токена укажите `Bearer`. |

Для OAuth получите доступ `wiki:read` для чтения и `wiki:write` для публикации.
API применяет права пользователя в Вики. [Как получить доступ к API](https://yandex.ru/support/wiki/ru/api-ref/access)

```bash
python3 tools/wiki.py read 'https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/'
python3 tools/wiki.py comments raid3/gdd-vtorojj-ulov-second-catch
python3 tools/wiki.py comment raid3/gdd-vtorojj-ulov-second-catch --body-file ./comment.txt
```

Каждая команда принимает URL или слаг и выводит ответ API в JSON. У `read`
текст страницы находится в поле `content`. Для следующей порции комментариев
передайте курсор из ответа в `comments --cursor`. Команда `comment` публикует
комментарий от имени владельца токена. Она принимает `--body` для текста,
`--body-file -` для stdin и `--parent-id` с `--thread-id` для ответа в обсуждении.

При ревью начните с `read` и `comments`, чтобы учесть уже открытые обсуждения.
Вызывайте `comment`, когда коллаборатор попросил опубликовать замечание.
Если публикация завершилась таймаутом, перед повтором проверьте список комментариев.

[Чтение страницы](https://yandex.ru/support/wiki/ru/api-ref/pages/pages__get_page_details) ·
[Примеры публикации комментариев и ответов](https://yandex.ru/support/wiki/ru/api-ref/examples)
