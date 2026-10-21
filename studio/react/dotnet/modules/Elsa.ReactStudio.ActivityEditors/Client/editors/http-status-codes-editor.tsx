
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { useState } from "react";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Badge } from "@elsa-workflows/react-studio-ui/badge";
import type { UiHintEditorProps } from "../types";
import { parseStatusCode, statusCodes, statusCodeValue } from "./editor-values";
import { LiteralControl } from "./literal-control";

export function HttpStatusCodesEditor(props: UiHintEditorProps) {
  const _t = useTranslation();
  const [draft, setDraft] = useState("");
  const [error, setError] = useState<string>();
  return <LiteralControl props={props} syntax="Object">{({ value, onChange }) => {
    const items = statusCodes(value ?? props.descriptor.defaultValue);
    const write = (codes: number[]) => {
      onChange(statusCodeValue(value, codes, props.descriptor));
    };
    const commit = () => {
      if (!draft.trim()) return;
      const code = parseStatusCode(draft);
      if (code === undefined) { setError(_t("Enter an HTTP status code from 100 to 599.")); return; }
      write([...items, code]);
      setDraft(""); setError(undefined);
    };
    return <div className="flex flex-col gap-2">
      <div className="flex flex-wrap gap-1">
        {items.map(code => <Badge key={code} variant="secondary">{code}
          {!props.readOnly && <button type="button" aria-label={_t("Remove status {{value0}}", { value0: code })}
            onClick={() => write(items.filter(other => other !== code))}>×</button>}
        </Badge>)}
      </div>
      {!props.readOnly && <Input value={draft} inputMode="numeric" aria-label={_t("New HTTP status code")} aria-invalid={!!error}
        placeholder={_t("HTTP status code")} onChange={event => { setDraft(event.target.value); setError(undefined); }}
        onBlur={commit} onKeyDown={event => { if (event.key === "Enter" || event.key === ",") { event.preventDefault(); commit(); } }} />}
      {error && <p role="alert">{error}</p>}
    </div>;
  }}</LiteralControl>;
}
