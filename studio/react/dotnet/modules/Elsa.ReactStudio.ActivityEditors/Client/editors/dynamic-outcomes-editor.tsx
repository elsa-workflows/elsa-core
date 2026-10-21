
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { useState } from "react";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Badge } from "@elsa-workflows/react-studio-ui/badge";
import { stringListOf } from "@elsa-workflows/react-studio-sdk/activity-expressions";
import type { UiHintEditorProps } from "../types";
import { fixedOutcomes, serializeOutcomes } from "./editor-values";
import { LiteralControl } from "./literal-control";

export function DynamicOutcomesEditor(props: UiHintEditorProps) {
  const _t = useTranslation();
  const [draft, setDraft] = useState("");
  const fixed = fixedOutcomes(props.descriptor);
  return <LiteralControl props={props} syntax="Object">{({ value, onChange }) => {
    const items = stringListOf(value, props.descriptor).filter(item => !fixed.includes(item));
    const write = (values: string[]) => {
      const serialized = serializeOutcomes(values, props.descriptor);
      onChange(props.descriptor.isWrapped === false ? JSON.parse(serialized) : serialized);
    };
    const commit = () => {
      const name = draft.trim();
      if (!name) return;
      write([...items, name]);
      setDraft("");
    };
    return <div className="flex flex-col gap-2">
      <div className="flex flex-wrap gap-1">
        {fixed.map(item => <Badge key={`fixed:${item}`} variant="outline">{item} {_t("(fixed)")}</Badge>)}
        {items.map((item, index) => <Badge key={`${item}:${index}`} variant="secondary">
          {item}{!props.readOnly && <button type="button" aria-label={_t("Remove outcome {{value0}}", { value0: item })}
            onClick={() => write(items.filter((_, i) => i !== index))}>×</button>}
        </Badge>)}
      </div>
      {!props.readOnly && <Input value={draft} aria-label={_t("New outcome")} placeholder={_t("Type an outcome and press Enter")}
        onChange={event => setDraft(event.target.value)} onBlur={commit} onKeyDown={event => {
          if (event.key === "Enter" || event.key === "Tab" || event.key === ",") {
            if (event.key !== "Tab") event.preventDefault();
            commit();
          }
        }} />}
    </div>;
  }}</LiteralControl>;
}
