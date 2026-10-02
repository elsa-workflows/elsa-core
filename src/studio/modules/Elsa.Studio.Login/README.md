# Legacy Studio login

This module is retained for compatibility. New Studio hosts should use the current authentication modules instead of `UseOpenIdConnect` from this module.

For hosts that still use this OIDC flow, sign-in now stores a random, short-lived authorization state and the local return path in the current browser tab's session storage before redirecting to the identity provider. The `/signin-oidc` callback must return to that tab within ten minutes. The state is consumed before the code exchange; missing, expired, mismatched, or repeated callbacks cannot write tokens. The return path is accepted only as a local path. A user whose browser session storage is cleared during the redirect must restart sign-in.
