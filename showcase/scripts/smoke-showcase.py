#!/usr/bin/env python3
from __future__ import annotations

import argparse
import html
import http.cookiejar
import os
from pathlib import Path
import re
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


def control_value(document: str, element_id: str) -> str:
    escaped_id = re.escape(element_id)
    textarea = re.search(
        rf'<textarea(?=[^>]*\bid="{escaped_id}")[^>]*>(.*?)</textarea>',
        document,
        re.DOTALL,
    )
    if textarea:
        return textarea.group(1)

    input_tag = re.search(rf'<input(?=[^>]*\bid="{escaped_id}")[^>]*>', document)
    if input_tag:
        value = re.search(r'\bvalue="([^"]*)"', input_tag.group(0))
        if value:
            return value.group(1)

    raise RuntimeError(f"Rendered control '{element_id}' has no value.")


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
                "/packages/licensing",
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

            _, conversion = read_text(opener, f"{base_url}/packages/conversion")
            _, licensing = read_text(opener, f"{base_url}/packages/licensing")
            _, scripting = read_text(opener, f"{base_url}/packages/scripting")
            sample_pages = [conversion, licensing, scripting]
            if any("@bind:get=" in page or "@bind:set=" in page for page in sample_pages):
                raise RuntimeError("A native sample binding was emitted as an inert HTML attribute.")
            if control_value(conversion, "source-value") != "300":
                raise RuntimeError("Conversion default sample did not populate the source editor.")
            if control_value(licensing, "license-id") != "demo-license":
                raise RuntimeError("Licensing default sample did not populate the license ID.")
            if control_value(licensing, "licensed-modules") != "Reporting, Export":
                raise RuntimeError("Licensing default sample did not populate the module editor.")
            if control_value(scripting, "timeout") != "1000":
                raise RuntimeError("Scripting default sample did not populate execution options.")

            culture_url = (
                f"{base_url}/culture/set?"
                + urllib.parse.urlencode({"culture": "hu", "returnUrl": "/"})
            )
            status, hungarian_home = read_text(opener, culture_url)
            if status != 200 or 'lang="hu"' not in hungarian_home or "Főoldal" not in hungarian_home:
                raise RuntimeError("Hungarian culture cookie did not update shell rendering.")

            _, hungarian_conversion = read_text(opener, f"{base_url}/packages/conversion")
            if "Konverzió futtatása" not in hungarian_conversion:
                raise RuntimeError("Hungarian culture cookie did not update Conversion rendering.")

            _, hungarian_scripting = read_text(opener, f"{base_url}/packages/scripting")
            if "Szkript futtatása" not in hungarian_scripting:
                raise RuntimeError("Hungarian culture cookie did not update Scripting rendering.")

            _, hungarian_licensing = read_text(opener, f"{base_url}/packages/licensing")
            if "Licenc ellenőrzése" not in hungarian_licensing:
                raise RuntimeError("Hungarian culture cookie did not update Licensing rendering.")

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
