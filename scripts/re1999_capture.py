import json
import os
from datetime import datetime, timezone
from urllib.parse import parse_qs, urlsplit

from mitmproxy import ctx, http


DEFAULT_RULES = [
    {
        "name": "global-default",
        "environment": "official-global",
        "hosts": ["game-re-en-service.sl916.com", "game-re-service.sl916.com"],
        "pathContains": ["/query/summon"],
        "method": "GET",
        "requireHttps": True,
        "requireStatusCode": 200,
        "queryKeys": [],
        "validateResponseJson": False,
        "responseCodeAllow": [200],
        "responseMsgAllow": ["成功"],
    }
]


class Reverse1999Capture:
    def __init__(self):
        self.seen = set()
        self.done = False
        self.rules = DEFAULT_RULES

    def load(self, loader):
        loader.add_option(
            name="re1999_rules",
            typespec=str,
            default="",
            help="Path to Reverse1999UrlCatcher URL rules JSON.",
        )

    def configure(self, updates):
        if "re1999_rules" not in updates:
            return

        path = ctx.options.re1999_rules
        if not path:
            self.rules = DEFAULT_RULES
            return

        try:
            with open(path, "r", encoding="utf-8") as handle:
                document = json.load(handle)
            rules = document.get("rules", [])
            self.rules = rules if rules else DEFAULT_RULES
        except Exception as exc:
            ctx.log.warn(f"Failed to read re1999 rules: {exc}")
            self.rules = DEFAULT_RULES

    def _match(self, flow: http.HTTPFlow):
        request = flow.request
        query = parse_qs(urlsplit(request.pretty_url).query)

        for rule in self.rules:
            method = rule.get("method")
            if method and request.method.upper() != method.upper():
                continue

            if rule.get("requireHttps", True) and request.scheme.lower() != "https":
                continue

            hosts = rule.get("hosts") or []
            if hosts and request.host not in hosts:
                continue

            path_contains = rule.get("pathContains") or []
            if path_contains and not any(part in request.path for part in path_contains):
                continue

            query_keys = rule.get("queryKeys") or []
            if query_keys and not all(key in query for key in query_keys):
                continue

            return rule

        return None

    def response(self, flow: http.HTTPFlow):
        if self.done:
            return

        rule = self._match(flow)
        if not rule:
            return

        required_status = rule.get("requireStatusCode")
        if required_status is not None:
            if flow.response is None or flow.response.status_code != int(required_status):
                return

        if not self._validate_response_json(flow, rule):
            return

        url = flow.request.pretty_url
        if url in self.seen:
            return

        self.seen.add(url)
        self.done = True
        payload = {
            "url": url,
            "host": flow.request.host,
            "path": flow.request.path,
            "matchedRule": rule.get("name", ""),
            "ts": datetime.now(timezone.utc).isoformat(),
        }
        print("CAPTURE_JSON:" + json.dumps(payload, ensure_ascii=False), flush=True)

    def _validate_response_json(self, flow: http.HTTPFlow, rule):
        if not rule.get("validateResponseJson", False):
            return True

        if flow.response is None:
            return False

        content_type = (flow.response.headers.get("content-type") or "").lower()
        if "json" not in content_type:
            return False

        try:
            data = json.loads(flow.response.get_text(strict=False) or "{}")
        except Exception:
            return False

        if not isinstance(data, dict):
            return False

        code_allow = rule.get("responseCodeAllow") or []
        msg_allow = rule.get("responseMsgAllow") or []
        code_ok = not code_allow or data.get("code") in code_allow
        msg_ok = not msg_allow or data.get("msg") in msg_allow
        return code_ok or msg_ok


addons = [Reverse1999Capture()]
