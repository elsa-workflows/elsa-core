"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { JsonViewer } from "@elsa-workflows/react-studio-ui/common/json-viewer";

function Block({ title, value }: { title: string; value: Record<string, unknown> | undefined }) {
  const _t = useTranslation();
  const empty = !value || Object.keys(value).length === 0;
  return (
    <div className="flex flex-col gap-1.5">
      <h4 className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{title}</h4>
      {empty ? <p className="text-sm text-muted-foreground">{_t("None.")}</p> : <JsonViewer value={value} maxHeight="24rem" />}
    </div>
  );
}

export function InputOutputPanel({ input, output }: { input?: Record<string, unknown>; output?: Record<string, unknown> }) {
  const _t = useTranslation();
  return (
    <div className="flex flex-col gap-4 p-3">
      <Block title={_t("Input")} value={input} />
      <Block title={_t("Output")} value={output} />
    </div>
  );
}
