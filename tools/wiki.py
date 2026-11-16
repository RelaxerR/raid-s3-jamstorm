#!/usr/bin/env python3
"""Read and comment on Yandex Wiki pages using the public API."""

import argparse
import json
import os
from pathlib import Path
import sys
from urllib.error import HTTPError, URLError
from urllib.parse import unquote, urlencode, urlsplit
from urllib.request import HTTPRedirectHandler, Request, build_opener


API = "https://api.wiki.yandex.net/v1"


def load_dotenv():
    """Load simple KEY=value entries from the repository's local .env file."""
    env_path = Path(__file__).resolve().parent.parent / ".env"
    if not env_path.is_file():
        return
    for raw_line in env_path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        key = key.strip()
        value = value.strip()
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
            value = value[1:-1]
        os.environ.setdefault(key, value)


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def page_slug(value):
    parsed = urlsplit(value)
    if parsed.scheme or parsed.netloc:
        if parsed.scheme != "https" or parsed.netloc != "wiki.yandex.ru":
            raise argparse.ArgumentTypeError("Expected a wiki.yandex.ru URL or a slug")
    slug = unquote(parsed.path).strip("/")
    if not slug:
        raise argparse.ArgumentTypeError("Page slug is empty")
    return slug


def api_request(path, params=None, body=None):
    token = os.environ.get("WIKI_TOKEN")
    org_id = os.environ.get("WIKI_ORG_ID")
    if not token or not org_id:
        raise ValueError("Set WIKI_TOKEN and WIKI_ORG_ID")

    org_header = os.environ.get("WIKI_ORG_HEADER", "X-Org-Id")
    auth_scheme = os.environ.get("WIKI_AUTH_SCHEME", "OAuth")
    if org_header not in ("X-Org-Id", "X-Cloud-Org-Id"):
        raise ValueError("WIKI_ORG_HEADER must be X-Org-Id or X-Cloud-Org-Id")
    if auth_scheme not in ("OAuth", "Bearer"):
        raise ValueError("WIKI_AUTH_SCHEME must be OAuth or Bearer")

    headers = {
        "Authorization": f"{auth_scheme} {token}",
        org_header: org_id,
        "Accept": "application/json",
    }
    url = API + path
    if params:
        url += "?" + urlencode(params)
    data = None
    if body is not None:
        headers["Content-Type"] = "application/json"
        data = json.dumps(body, ensure_ascii=False).encode("utf-8")
    request = Request(url, data=data, headers=headers)
    with build_opener(NoRedirect()).open(request, timeout=30) as response:
        return json.load(response)


def main():
    load_dotenv()
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    read = commands.add_parser("read", help="Read page content and metadata as JSON")
    comments = commands.add_parser("comments", help="List page comments as JSON")
    comment = commands.add_parser("comment", help="Publish a comment or reply")
    for command in (read, comments, comment):
        command.add_argument("page", type=page_slug, help="Wiki URL or slug")
    comments.add_argument("--cursor", help="Cursor for the next page of comments")
    body = comment.add_mutually_exclusive_group(required=True)
    body.add_argument("--body", help="Comment text")
    body.add_argument("--body-file", help="UTF-8 text file, or - for stdin")
    comment.add_argument("--parent-id", type=int)
    comment.add_argument("--thread-id", type=int)
    args = parser.parse_args()

    try:
        params = {"slug": args.page}
        payload = None
        if args.command == "read":
            params["fields"] = "content,last_revision_id"
        elif args.command == "comment":
            params["raise_on_redirect"] = "true"
            if args.body_file == "-":
                text = sys.stdin.buffer.read().decode("utf-8")
            elif args.body_file:
                text = Path(args.body_file).read_text(encoding="utf-8")
            else:
                text = args.body
            if not text.strip():
                raise ValueError("Comment text is empty")
            payload = {"body": text}
            for key in ("parent_id", "thread_id"):
                value = getattr(args, key)
                if value is not None:
                    payload[key] = value

        result = api_request("/pages", params)
        if args.command != "read":
            page_id = result["id"]
            if type(page_id) is not int or page_id <= 0:
                raise ValueError("The API returned an invalid page ID")
            query = {}
            if args.command == "comments" and args.cursor:
                query["cursor"] = args.cursor
            result = api_request(f"/pages/{page_id}/comments", query, payload)
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    except HTTPError as error:
        details = error.read().decode("utf-8", errors="replace").strip()
        message = f"Wiki API returned HTTP {error.code}"
        if details:
            message += f": {details}"
        print(message, file=sys.stderr)
    except URLError as error:
        print(f"Cannot reach Wiki API: {error.reason}", file=sys.stderr)
    except (OSError, ValueError, KeyError, TypeError) as error:
        print(f"Wiki error: {error}", file=sys.stderr)
    return 1


if __name__ == "__main__":
    sys.exit(main())
