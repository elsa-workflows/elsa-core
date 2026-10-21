"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth/provider";
import { useModules } from "@/lib/modules/registry";
import { resolveRoute } from "@/lib/modules/router";
import { usePermissions } from "@/lib/permissions";
import { AppShell } from "./app-shell";
import { AccessDenied, NotFound } from "./status-pages";
import { FullPageSpinner } from "./full-page-spinner";

export function StudioRouter({ path }: { path: string }) {
  const auth = useAuth();
  const router = useRouter();
  const { enabled, featuresResolved } = useModules();
  const permissions = usePermissions();

  const needsLogin = auth.status === "unauthenticated" && auth.strategy.interactive;

  useEffect(() => {
    if (needsLogin) router.replace(`/login?returnTo=${encodeURIComponent(path)}`);
  }, [needsLogin, router, path]);

  if (auth.status === "initializing" || needsLogin) return <FullPageSpinner />;

  if (auth.status === "unauthenticated") {
    // Non-interactive strategies (api-key without key) cannot recover: show the reason.
    return <AppShell><AccessDenied message={auth.error ?? "Not authenticated."} /></AppShell>;
  }

  const match = resolveRoute(enabled, path);

  if (!match) {
    // Routes of modules gated on server features may appear once the catalog loads.
    if (!featuresResolved) return <FullPageSpinner />;
    return <AppShell><NotFound path={path} /></AppShell>;
  }

  if (match.route.permissions && !permissions.hasAll(match.route.permissions)) {
    return <AppShell><AccessDenied /></AppShell>;
  }

  const Page = match.route.component;
  return (
    <AppShell title={match.route.title}>
      <Page params={match.params} />
    </AppShell>
  );
}
