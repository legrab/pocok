#!/usr/bin/env python3
from __future__ import annotations

import argparse
import html
import http.cookiejar
import os
from pathlib import Path
import socket
import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request


def free_port() -> int:
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        return int(sock.getsockname()[1])


def read_text(opener: urllib.request.OpenerDirector, url: str) -> tuple[int, str]:
    with opener.open(url, timeout=3) as response:
        return response.status, html.unescape(response.read().decode("utf-8", errors="replace"))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("publish_root", type=Path)
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET_HOST_PATH", "dotnet"))
    args = parser.parse_args()

    root = args.publish_root.resolve()
    app = root / "Pocok.Showcase.Web.dll"
    plugins = root / "plugins"
    if not app.is_file() or not plugins.is_dir():
        raise SystemExit("Publish output is incomplete.")

    port = free_port()
    base_url = f"http://127.0.0.1:{port}"
    env = os.environ.copy()
    env.pop("PLATFORM", None)
    env.update(
        {
            "PORT": str(port),
            "ASPNETCORE_ENVIRONMENT": "Production",
            "SHOWCASE_PLUGIN_DIR": str(plugins),
        }
    )

    stdout = root / "smoke.stdout.log"
    stderr = root / "smoke.stderr.log"
    cookie_jar = http.cookiejar.CookieJar()
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookie_jar))

    with stdout.open("w", encoding="utf-8") as out, stderr.open("w", encoding="utf-8") as err:
        process = subprocess.Popen(
            [args.dotnet, str(app)],
            cwd=root,
            env=env,
            stdout=out,
            stderr=err,
        )
        try:
            endpoints = [
                "/health/live",
                "/health/ready",
                "/",
                "/packages/conversion",
                "/packages/scripting",
                "/packages/readiness",
                "/system",
            ]
            deadline = time.monotonic() + 45
            pending = set(endpoints)
            while pending and time.monotonic() < deadline:
                for endpoint in list(pending):
                    try:
                        status, _ = read_text(opener, f"{base_url}{endpoint}")
                        if status == 200:
                            pending.remove(endpoint)
                    except (urllib.error.URLError, TimeoutError):
                        pass

                if pending:
                    if process.poll() is not None:
                        break
                    time.sleep(0.25)

            if pending:
                raise RuntimeError(
                    f"Smoke endpoints failed: {sorted(pending)}. "
                    f"stdout={stdout}, stderr={stderr}"
                )

            culture_url = (
                f"{base_url}/culture/set?"
                + urllib.parse.urlencode({"culture": "hu", "returnUrl": "/"})
            )
            status, hungarian_home = read_text(opener, culture_url)
            if status != 200 or "Kis csomagok, valódi működés" not in hungarian_home:
                raise RuntimeError("Hungarian culture cookie did not update shell rendering.")

            _, hungarian_conversion = read_text(opener, f"{base_url}/packages/conversion")
            if "Konverzió futtatása" not in hungarian_conversion:
                raise RuntimeError("Hungarian culture cookie did not update Conversion rendering.")

            _, hungarian_scripting = read_text(opener, f"{base_url}/packages/scripting")
            if "Szkript futtatása" not in hungarian_scripting:
                raise RuntimeError("Hungarian culture cookie did not update Scripting rendering.")

            print("Smoke test passed:", ", ".join(endpoints), "and EN/HU cookie localization")
            return 0
        finally:
            process.terminate()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=5)


if __name__ == "__main__":
    raise SystemExit(main())
