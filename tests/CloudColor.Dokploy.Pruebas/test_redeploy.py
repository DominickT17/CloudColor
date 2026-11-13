import importlib.util
import io
import json
import os
from pathlib import Path
from types import SimpleNamespace
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[2] / "scripts" / "dokploy" / "redeploy.py"
spec = importlib.util.spec_from_file_location("cloudcolor_redeploy", SCRIPT)
redeploy = importlib.util.module_from_spec(spec)
spec.loader.exec_module(redeploy)

ENV = {
    "DOKPLOY_URL": "https://dokploy.invalid",
    "DOKPLOY_COMPOSE_ID": "test-id",
    "DOKPLOY_API_KEY": "test-placeholder",
    "GITHUB_RUN_ID": "123",
    "GITHUB_RUN_ATTEMPT": "1",
    "GITHUB_SHA": "a" * 40,
}


class RedeployTests(unittest.TestCase):
    def test_request_sends_json_without_fresh_volumes(self):
        requests = []
        def open_request(request, timeout):
            requests.append(request)
            return io.BytesIO(b'{"success":true}')
        opener = SimpleNamespace(open=open_request)
        with patch.object(redeploy, "build_opener", return_value=opener) as build:
            payload = {"composeId": "test-id", "freshVolumes": False}
            self.assertTrue(redeploy.api_request("https://dokploy.invalid", "test-placeholder", "/api/compose.redeploy", payload)["success"])
            self.assertTrue(build.called)
            self.assertEqual(requests[0].get_method(), "POST")
            self.assertEqual(json.loads(requests[0].data), payload)
            self.assertEqual(requests[0].get_header("X-api-key"), "test-placeholder")

    def test_missing_configuration_skips_without_api(self):
        with patch.dict(os.environ, {}, clear=True), patch.object(redeploy, "api_request") as api:
            self.assertEqual(redeploy.main(), 0)
            api.assert_not_called()

    def test_waits_for_matching_done_before_smoke(self):
        calls = []

        def api(base, key, path, body=None):
            calls.append((path, body))
            if body is not None:
                self.assertFalse(body["freshVolumes"])
                self.assertEqual(body["title"], "CloudColor Actions 123-1")
                return {"success": True}
            if len(calls) == 1:
                return []
            return [{"title": "CloudColor Actions 123-1", "status": "done"}]

        with patch.dict(os.environ, {**ENV, "CLOUDCOLOR_PUBLIC_URL": "https://site.invalid"}, clear=True), \
             patch.object(redeploy, "api_request", side_effect=api), \
             patch.object(redeploy.subprocess, "run", return_value=SimpleNamespace(returncode=0)) as smoke:
            self.assertEqual(redeploy.main(), 0)
            self.assertEqual(len(calls), 3)
            self.assertTrue(calls[1][0].endswith("/api/compose.redeploy"))
            smoke.assert_called_once()
            self.assertNotIn("DOKPLOY_API_KEY", smoke.call_args.kwargs["env"])

    def test_running_deployment_blocks_redeploy(self):
        with patch.dict(os.environ, ENV, clear=True), \
             patch.object(redeploy, "api_request", return_value=[{"status": "running"}]) as api:
            with self.assertRaises(RuntimeError):
                redeploy.main()
            api.assert_called_once()

    def test_failed_deployment_does_not_smoke(self):
        with patch.dict(os.environ, {**ENV, "CLOUDCOLOR_PUBLIC_URL": "https://site.invalid"}, clear=True), \
             patch.object(redeploy, "api_request", side_effect=[[], True, [{"title": "CloudColor Actions 123-1", "status": "error"}]]), \
             patch.object(redeploy.subprocess, "run") as smoke:
            with self.assertRaises(RuntimeError):
                redeploy.main()
            smoke.assert_not_called()

    def test_unmatched_old_success_cannot_pass(self):
        with patch.dict(os.environ, ENV, clear=True), \
             patch.object(redeploy, "api_request", side_effect=[[], True, [{"title": "older", "status": "done"}]]), \
             patch.object(redeploy.time, "monotonic", side_effect=[0, 1, 901]), \
             patch.object(redeploy.time, "sleep"):
            with self.assertRaises(RuntimeError):
                redeploy.main()


if __name__ == "__main__":
    unittest.main()
