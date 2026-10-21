"use client";

import { Terminal } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ToneBadge } from "@/components/common/status-badge";
import { useConsoleLogStorage } from "../hooks";

/** Dashboard "status" widget. Hidden when the server has no storage endpoint (404) or on error. */
export function ConsoleLogStorageWidget() {
  const query = useConsoleLogStorage();
  if (!query.data) return null;
  const { droppedWriteCount, hasStorageDiagnosticsProvider } = query.data;
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-sm">
          <Terminal className="size-4" /> Console log storage
        </CardTitle>
      </CardHeader>
      <CardContent className="flex items-center justify-between gap-2 text-sm">
        {hasStorageDiagnosticsProvider ? (
          <>
            <span className="text-muted-foreground">Dropped writes</span>
            <ToneBadge tone={droppedWriteCount > 0 ? "warning" : "success"} className="tabular-nums">
              {droppedWriteCount}
            </ToneBadge>
          </>
        ) : (
          <span className="text-muted-foreground">No persistent storage configured (in-memory buffer).</span>
        )}
      </CardContent>
    </Card>
  );
}
