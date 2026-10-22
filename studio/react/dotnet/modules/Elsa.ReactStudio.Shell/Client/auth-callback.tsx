"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { studioRoute } from "@elsa-workflows/react-studio-sdk/navigation/base-path";
import { useEffect, useState } from "react";
import { useRouter } from "@elsa-workflows/react-studio-sdk/navigation/index";
import { useAuth } from "@elsa-workflows/react-studio-auth";
import { FullPageSpinner } from "@elsa-workflows/react-studio-ui/full-page-spinner";
import { AccessDenied } from "./status-pages";

/** Landing route for redirect-based sign-in (OIDC, external authentication broker). */
export function AuthCallback() {
  const _t = useTranslation();
  const { strategy } = useAuth();
  const router = useRouter();
  const [error, setError] = useState<string>();

  useEffect(() => {
    if (!strategy.completeSignIn) {
      router.replace("/");
      return;
    }
    strategy
      .completeSignIn()
      .then(() => router.replace(studioRoute(window.location.pathname) + window.location.search))
      .catch((e: Error) => setError(e.message));
  }, [strategy, router]);

  if (error) return <AccessDenied message={_t("Sign-in could not be completed: {{value0}}", { value0: error })} />;
  return <FullPageSpinner label={_t("Completing sign-in…")} />;
}
