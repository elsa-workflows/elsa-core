
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { Loader2 } from "lucide-react";

export function FullPageSpinner({ label }: { label?: string }) {
  const _t = useTranslation();
  return (
    <div className="flex flex-1 min-h-screen items-center justify-center text-muted-foreground">
      <Loader2 className="size-5 animate-spin mr-2" />
      {label ?? _t("Loading…")}
    </div>
  );
}
