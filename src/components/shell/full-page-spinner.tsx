import { Loader2 } from "lucide-react";

export function FullPageSpinner({ label }: { label?: string }) {
  return (
    <div className="flex flex-1 min-h-screen items-center justify-center text-muted-foreground">
      <Loader2 className="size-5 animate-spin mr-2" />
      {label ?? "Loading…"}
    </div>
  );
}
