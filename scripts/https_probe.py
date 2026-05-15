import json
from datetime import datetime, timezone

from mitmproxy import http


class HttpsProbe:
    def __init__(self):
        self.done = False

    def request(self, flow: http.HTTPFlow):
        if self.done:
            return

        request = flow.request
        if request.method.upper() == "CONNECT":
            return

        if request.scheme.lower() != "https":
            return

        self.done = True
        payload = {
            "url": request.pretty_url,
            "host": request.host,
            "path": request.path,
            "matchedRule": "https-probe",
            "ts": datetime.now(timezone.utc).isoformat(),
        }
        print("CAPTURE_JSON:" + json.dumps(payload, ensure_ascii=False), flush=True)


addons = [HttpsProbe()]
