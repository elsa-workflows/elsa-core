"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState, type FormEvent } from "react";
import { ExternalLink, Loader2 } from "lucide-react";
import { BrandLogo, BrandMark } from "./brand-mark";
import { LoginIllustration } from "./login-illustration";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/common/field";
import { useStudioConfig } from "@/config/provider";
import { useAuth } from "@/lib/auth/provider";
import type { ElsaExternalAuthStrategy, LoginMethod } from "@/lib/auth/strategies/elsa-external";
import { useActiveServer } from "@/lib/environments/active-server";
import { FullPageSpinner } from "./full-page-spinner";

/** Safe, site-local return path. Anything absolute or protocol-relative collapses to `/`. */
function safeReturnTo(value: string | null) {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.includes("\\")) return "/";
  return value;
}

export function LoginPage() {
  const config = useStudioConfig();
  const server = useActiveServer();
  const auth = useAuth();
  const router = useRouter();
  const params = useSearchParams();
  const returnTo = safeReturnTo(params.get("returnTo"));

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [methods, setMethods] = useState<LoginMethod[]>();

  const isExternalBroker = auth.strategy.kind === "elsa-external";

  useEffect(() => {
    if (auth.status === "authenticated") router.replace(returnTo);
  }, [auth.status, router, returnTo]);

  useEffect(() => {
    if (!auth.strategy.interactive && auth.status === "unauthenticated") router.replace("/");
  }, [auth.strategy.interactive, auth.status, router]);

  useEffect(() => {
    if (isExternalBroker) {
      (auth.strategy as ElsaExternalAuthStrategy).getLoginMethods().then(setMethods).catch(() => setMethods([]));
    }
  }, [auth.strategy, isExternalBroker]);

  if (auth.status !== "unauthenticated") return <FullPageSpinner />;

  const submitCredentials = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await auth.login({ username, password });
      router.replace(returnTo);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const startRedirect = async (method?: LoginMethod) => {
    setBusy(true);
    setError(undefined);
    try {
      await auth.login(undefined, method);
    } catch (err) {
      setError((err as Error).message);
      setBusy(false);
    }
  };

  const showCredentials = auth.strategy.usesCredentials && (!isExternalBroker || !methods || methods.some((m) => m.kind === "local"));
  const externalMethods = isExternalBroker ? (methods ?? []).filter((m) => m.kind === "external") : [];

  return (
    <div className="grid min-h-screen lg:grid-cols-[1.1fr_1fr]">
      <aside className="relative hidden overflow-hidden border-r bg-sidebar lg:flex lg:flex-col lg:justify-between">
        <div className="relative z-10 flex flex-col gap-3 p-8">
          <BrandLogo className="h-14 w-auto self-start text-sidebar-foreground" />
          <div>
            <div className="font-semibold tracking-tight">{config.title}</div>
            <div className="text-xs text-muted-foreground">Workflow designer and operations console</div>
          </div>
        </div>
        <LoginIllustration className="relative z-10 mx-auto w-full max-w-xl px-8" />
        <div className="relative z-10 p-8 text-xs text-muted-foreground">
          <div className="eyebrow mb-1">Connected server</div>
          <div className="font-medium text-foreground">{server.name}</div>
          <div className="truncate font-mono">{server.url}</div>
        </div>
      </aside>
      <div className="flex items-center justify-center bg-background p-6">
      <Card className="w-full max-w-sm node-card">
        <CardHeader className="items-center text-center">
          <BrandMark className="mx-auto mb-2 size-10 lg:hidden" />
          <CardTitle className="text-lg">Sign in</CardTitle>
          <CardDescription className="truncate">{config.title} · {server.name}</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {auth.error && !error && <p className="text-sm text-destructive">{auth.error}</p>}
          {error && <p className="text-sm text-destructive">{error}</p>}

          {showCredentials && (
            <form onSubmit={submitCredentials} className="flex flex-col gap-3">
              <Field label="Username" htmlFor="username">
                <Input id="username" autoComplete="username" value={username} onChange={(e) => setUsername(e.target.value)} required autoFocus />
              </Field>
              <Field label="Password" htmlFor="password">
                <Input id="password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} required />
              </Field>
              <Button type="submit" disabled={busy} className="mt-1">
                {busy && <Loader2 className="animate-spin" />}
                Sign in
              </Button>
            </form>
          )}

          {auth.strategy.kind === "oidc" && (
            <Button onClick={() => startRedirect()} disabled={busy}>
              {busy ? <Loader2 className="animate-spin" /> : <ExternalLink />}
              Sign in with {new URL(config.auth.oidc.authority || "https://identity-provider").hostname}
            </Button>
          )}

          {isExternalBroker && methods === undefined && <FullPageSpinner label="Loading sign-in options…" />}
          {externalMethods.map((m) => (
            <Button key={m.key} variant={m.isPreferred ? "default" : "outline"} onClick={() => startRedirect(m)} disabled={busy}>
              <ExternalLink />
              {m.displayName}
            </Button>
          ))}
          {isExternalBroker && methods?.length === 0 && (
            <p className="text-sm text-muted-foreground text-center">The server offers no sign-in methods for this client.</p>
          )}
        </CardContent>
      </Card>
      </div>
    </div>
  );
}
