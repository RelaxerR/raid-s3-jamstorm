#!/usr/bin/env python3
"""Build the PDF handout for the Yandex Wiki CLI."""

from pathlib import Path
import sys

sys.path.insert(0, "/tmp/top-reportlab")

from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import KeepTogether, PageBreak, Paragraph, Preformatted, SimpleDocTemplate, Spacer


ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "docs" / "yandex-wiki-guide.pdf"
FONT = "/System/Library/Fonts/Supplemental/Arial Unicode.ttf"


def build():
    pdfmetrics.registerFont(TTFont("ArialUnicode", FONT))
    styles = getSampleStyleSheet()
    for style in styles.byName.values():
        style.fontName = "ArialUnicode"
    title = ParagraphStyle("TitleRu", parent=styles["Title"], alignment=TA_CENTER, fontSize=22, leading=27, spaceAfter=14)
    subtitle = ParagraphStyle("SubtitleRu", parent=styles["Normal"], alignment=TA_CENTER, fontSize=11, leading=15, textColor="#555555", spaceAfter=20)
    heading = ParagraphStyle("HeadingRu", parent=styles["Heading1"], fontSize=15, leading=19, spaceBefore=12, spaceAfter=7, textColor="#17365d")
    body = ParagraphStyle("BodyRu", parent=styles["BodyText"], fontSize=10, leading=14, spaceAfter=7)
    code = ParagraphStyle("CodeRu", parent=styles["Code"], fontName="Courier", fontSize=8.5, leading=11, leftIndent=8, rightIndent=8, backColor="#f1f3f5", borderPadding=6, spaceBefore=4, spaceAfter=9)

    story = [
        Paragraph("Яндекс Вики через tools/wiki.py", title),
        Paragraph("Настройка и использование для команды TOP", subtitle),
        Paragraph("Назначение", heading),
        Paragraph("Скрипт читает страницы Яндекс Вики, получает комментарии и публикует новые комментарии или ответы в обсуждениях.", body),
        Paragraph("Требования", heading),
        Paragraph("Python 3.8 или новее, доступ к организации в Яндекс Вики, включённый Public API и OAuth-токен с правом wiki:read. Для публикации комментариев нужно право wiki:write.", body),
        Paragraph("Настройка", heading),
        Paragraph("В корне репозитория создайте локальный файл .env на основе .env.example:", body),
        Preformatted("WIKI_TOKEN=ваш-токен\nWIKI_ORG_ID=идентификатор-организации\nWIKI_ORG_HEADER=X-Cloud-Org-Id", code),
        Paragraph("Для OAuth оставьте WIKI_AUTH_SCHEME пустым. Для IAM-токена добавьте WIKI_AUTH_SCHEME=Bearer. Файл .env исключён из Git и не должен попадать в PR или чат.", body),
        Paragraph("Чтение страницы", heading),
        Preformatted("python3 tools/wiki.py read \\\n  'https://wiki.yandex.ru/raid3/gdd-vtorojj-ulov-second-catch/'", code),
        Paragraph("Текст страницы находится в поле content JSON-ответа.", body),
        Paragraph("Чтение комментариев", heading),
        Preformatted("python3 tools/wiki.py comments \\\n  raid3/gdd-vtorojj-ulov-second-catch", code),
        Paragraph("Для следующей порции передайте cursor из предыдущего ответа через --cursor.", body),
        PageBreak(),
        Paragraph("Публикация комментариев", title),
        Preformatted("python3 tools/wiki.py comment \\\n  raid3/gdd-vtorojj-ulov-second-catch \\\n  --body 'Текст комментария'", code),
        Paragraph("Длинный комментарий можно передать из файла или стандартного ввода:", body),
        Preformatted("python3 tools/wiki.py comment \\\n  raid3/gdd-vtorojj-ulov-second-catch \\\n  --body-file comment.txt\n\ncat comment.txt | python3 tools/wiki.py comment \\\n  raid3/gdd-vtorojj-ulov-second-catch \\\n  --body-file -", code),
        Paragraph("Ответ в существующей ветке обсуждения публикуется с --parent-id и --thread-id:", body),
        Preformatted("python3 tools/wiki.py comment \\\n  raid3/gdd-vtorojj-ulov-second-catch \\\n  --body 'Ответ на замечание' \\\n  --parent-id 123 \\\n  --thread-id 456", code),
        Paragraph("Рабочий порядок для ревью", heading),
        Paragraph("Сначала выполните read, затем comments. После анализа сформулируйте замечание и опубликуйте его командой comment. После сетевой ошибки сначала проверьте comments, чтобы не создать дубликат.", body),
        Paragraph("Частые ошибки", heading),
        Paragraph("Set WIKI_TOKEN and WIKI_ORG_ID означает, что .env отсутствует или не заполнен. 401 Unauthorized означает ошибку токена или способа авторизации. 403 Forbidden означает отсутствие прав в организации или на странице. Ошибка compdef относится к автодополнению Yandex Cloud CLI и не влияет на работу скрипта.", body),
        Paragraph("Быстрая проверка", heading),
        Preformatted("python3 tools/wiki.py --help\npython3 tools/wiki.py read raid3/gdd-vtorojj-ulov-second-catch\npython3 tools/wiki.py comments raid3/gdd-vtorojj-ulov-second-catch", code),
        Paragraph("Полная текстовая версия инструкции находится в docs/yandex-wiki-guide.md.", body),
    ]
    doc = SimpleDocTemplate(str(OUTPUT), pagesize=A4, rightMargin=18 * mm, leftMargin=18 * mm, topMargin=16 * mm, bottomMargin=16 * mm, title="Инструкция по tools/wiki.py")
    doc.build(story)
    print(OUTPUT)


if __name__ == "__main__":
    build()
