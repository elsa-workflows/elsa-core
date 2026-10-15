"""Deterministic synthetic fixture time; production expiry guards stay unchanged."""
from contextlib import ExitStack, contextmanager
from datetime import datetime, timezone
import io
import unittest
from unittest.mock import patch


SYNTHETIC_NOW = datetime(2026, 10, 7, 12, tzinfo=timezone.utc)
AFTER_EXPIRY = datetime(2030, 1, 1, tzinfo=timezone.utc)


@contextmanager
def candidate_clock(*modules, at=SYNTHETIC_NOW):
    class ControlledDateTime(datetime):
        @classmethod
        def now(cls, tz=None):
            return at.astimezone(tz) if tz is not None else at.replace(tzinfo=None)

    with ExitStack() as stack:
        for module in modules:
            stack.enter_context(patch.object(module, "datetime", ControlledDateTime))
        yield


def install_candidate_clock(test, *modules, at=SYNTHETIC_NOW):
    stack = ExitStack()
    test.addCleanup(stack.close)
    stack.enter_context(candidate_clock(*modules, at=at))


class CandidateClockContracts(unittest.TestCase):
    def test_success_fixtures_survive_future_wall_clock_and_restore_it(self):
        import test_consolidated_package_executor as execution
        import test_consolidated_executor_approval_summary as approvals
        import test_stable_persisted_workflow_upgrade_fixture as stable

        modules = (execution.executor, execution.recovery, execution.recovery_fixtures,
                   approvals.summary, approvals, stable.candidate_input, stable.candidate, stable)
        with candidate_clock(*modules, at=AFTER_EXPIRY), \
                patch.object(unittest.TestCase, "enterContext", side_effect=AssertionError("Requires Python 3.11"), create=True), \
                patch.object(execution.executor, "bounded_child", side_effect=AssertionError("Unexpected external action")):
            future_clock = execution.executor.datetime
            suite = unittest.TestSuite([
                execution.FullInventoryTests("test_verify_missing_is_complete_observation_not_release_acceptance"),
                execution.AdmissionTests("test_native_approval_and_exact_preexisting_policy"),
                execution.AdmissionTests("test_preschedule_requires_policy_but_not_future_approval"),
                approvals.SummaryTests("test_complete_missing_observation_reports_actual_668_and_immutable_bindings"),
                stable.StableAdapterContracts("test_two_package_scope_and_distinct_execution_identity"),
            ])
            output = io.StringIO()
            result = unittest.TextTestRunner(stream=output).run(suite)
            self.assertEqual(result.testsRun, 5)
            self.assertTrue(result.wasSuccessful(), output.getvalue())
            self.assertFalse(result.skipped)
            for module in modules:
                with self.subTest(module=module.__name__):
                    self.assertIs(module.datetime, future_clock)
