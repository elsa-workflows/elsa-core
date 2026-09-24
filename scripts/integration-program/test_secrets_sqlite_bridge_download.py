"""Keep the published-artifact fixture resilient to brief feed failures."""

import io
from datetime import datetime, timedelta, timezone
from email.utils import format_datetime
from unittest import TestCase
from unittest.mock import patch
from urllib.error import HTTPError

import run_secrets_sqlite_bridge_contract as bridge


class PackageDownloadTests(TestCase):
    def test_retries_transient_response_then_reads_artifact(self):
        url = 'https://example.invalid/package.nupkg'
        transient = HTTPError(url, 503, 'Service Unavailable', {}, None)
        with patch.object(bridge.urllib.request, 'urlopen', side_effect=[transient, io.BytesIO(b'package')]) as request, \
                patch.object(bridge.time, 'sleep') as sleep:
            self.assertEqual(bridge.download_package(url), b'package')
        self.assertEqual(request.call_count, 2)
        sleep.assert_called_once_with(1)

    def test_does_not_retry_permanent_response(self):
        url = 'https://example.invalid/missing.nupkg'
        permanent = HTTPError(url, 404, 'Not Found', {}, None)
        with patch.object(bridge.urllib.request, 'urlopen', side_effect=permanent) as request, \
                patch.object(bridge.time, 'sleep') as sleep:
            with self.assertRaises(HTTPError):
                bridge.download_package(url)
        request.assert_called_once()
        sleep.assert_not_called()

    def test_rate_limit_waits_for_advertised_delay_with_a_cap(self):
        url = 'https://example.invalid/package.nupkg'
        future_date = format_datetime(datetime.now(timezone.utc) + timedelta(seconds=60))
        for header, expected_delay in [('12', 12), ('1000', 30), (future_date, 30)]:
            with self.subTest(retry_after=header):
                limited = HTTPError(url, 429, 'Too Many Requests', {'Retry-After': header}, None)
                with patch.object(bridge.urllib.request, 'urlopen', side_effect=[limited, io.BytesIO(b'package')]), \
                        patch.object(bridge.time, 'sleep') as sleep:
                    self.assertEqual(bridge.download_package(url), b'package')
                sleep.assert_called_once_with(expected_delay)

    def test_transient_retries_are_bounded(self):
        url = 'https://example.invalid/package.nupkg'
        transient = HTTPError(url, 503, 'Service Unavailable', {}, None)
        with patch.object(bridge.urllib.request, 'urlopen', side_effect=transient) as request, \
                patch.object(bridge.time, 'sleep') as sleep:
            with self.assertRaises(HTTPError):
                bridge.download_package(url)
        self.assertEqual(request.call_count, 4)
        self.assertEqual([call.args for call in sleep.call_args_list], [(1,), (2,), (4,)])
