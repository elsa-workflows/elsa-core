"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth/provider";
import { FullPageSpinner } from "./full-page-spinner";
import { AccessDenied } from "./status-pages";

/** Landing route for redirect-based sign-in (OIDC, external authentication broker). */
export function AuthCallback() {
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
      .then(() => router.replace(window.location.pathname + window.location.search))
      .catch((e: Error) => setError(e.message));
  }, [strategy, router]);

  if (error) return <AccessDenied message={`Sign-in could not be completed: ${error}`} />;
  return <FullPageSpinner label="Completing sign-in…" />;
}
