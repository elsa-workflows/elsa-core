"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { useEffect } from "react";
import { useRouter } from "@elsa-workflows/react-studio-sdk/navigation/index";
import { useAuth } from "@elsa-workflows/react-studio-auth";
import { useModules } from "@elsa-workflows/react-studio-ui/runtime/module-registry";
import { resolveRoute } from "@elsa-workflows/react-studio-sdk/routing";
import { usePermissions } from "@elsa-workflows/react-studio-auth/permissions";
import { AppShell } from "./app-shell";
import { AccessDenied, NotFound } from "./status-pages";
import { FullPageSpinner } from "@elsa-workflows/react-studio-ui/full-page-spinner";

export function StudioRouter({ path }: { path: string }) {
  const _t = useTranslation();
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
    return <AppShell><AccessDenied message={auth.error ?? _t("Not authenticated.")} /></AppShell>;
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
