import Link from "next/link";
import { ShieldAlert, SearchX } from "lucide-react";
import { Button } from "@/components/ui/button";

export function AccessDenied({ message }: { message?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
      <span className="flex size-14 items-center justify-center rounded-2xl bg-status-danger/10 text-status-danger"><ShieldAlert className="size-7" /></span>
      <h1 className="text-xl font-semibold">Access denied</h1>
      <p className="text-sm text-muted-foreground max-w-md">{message ?? "You do not have permission to open this page."}</p>
      <Button variant="outline" nativeButton={false} render={<Link href="/" />}>Go to dashboard</Button>
    </div>
  );
}

export function NotFound({ path }: { path: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
      <span className="flex size-14 items-center justify-center rounded-2xl bg-accent text-primary"><SearchX className="size-7" /></span>
      <h1 className="text-xl font-semibold">Page not found</h1>
      <p className="text-sm text-muted-foreground max-w-md">
        Nothing is registered for <code className="font-mono">{path}</code>. The module that owns it may be disabled in the studio
        configuration or not installed on the server.
      </p>
      <Button variant="outline" nativeButton={false} render={<Link href="/" />}>Go to dashboard</Button>
    </div>
  );
}
