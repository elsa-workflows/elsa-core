import unittest

from verify_expected_break import is_expected_break


EXPECTED = "LegacySecretsConsumer.cs(8,19): error CS0246: 'SecretsDbContext' could not be found"


class ExpectedBreakTests(unittest.TestCase):
    def test_expected_missing_type_is_accepted(self):
        self.assertTrue(is_expected_break(EXPECTED + "\nBuild FAILED."))

    def test_unrelated_codeless_msbuild_error_is_rejected(self):
        self.assertFalse(is_expected_break(EXPECTED + "\nerror : restore source unavailable"))

    def test_unrelated_coded_error_is_rejected(self):
        self.assertFalse(is_expected_break(EXPECTED + "\nerror MSB4018: task failed"))

    def test_failure_without_expected_diagnostic_is_rejected(self):
        self.assertFalse(is_expected_break("Build FAILED.\n0 Error(s)"))


if __name__ == "__main__":
    unittest.main()
