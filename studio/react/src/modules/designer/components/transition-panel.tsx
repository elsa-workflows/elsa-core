"use client";

import { useMemo } from "react";
import { Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/common/field";
import { SimpleSelect } from "@/components/common/simple-select";
import { isStateMachine, type ActivityInput } from "@/lib/api/types/activities";
import type { InputDescriptor } from "@/lib/api/types/descriptors";
import { useExpressionDescriptors } from "@/lib/api/hooks/descriptors";
import { ExpressionEditor } from "@/modules/activity-editors/expression-editor";
import { selectableSyntaxes } from "@/modules/activity-editors/expression";
import { useDesigner, useDesignerState } from "../context";
import { resolveContainer } from "../model/flowchart-ops";
import { selectionId } from "../store";
import { SlotChip } from "./slot-chip";

const CONDITION_DESCRIPTOR: InputDescriptor = {
  name: "Condition",
  typeName: "Boolean",
  displayName: "Condition",
  uiHint: "singleline",
  isWrapped: true,
  defaultSyntax: "JavaScript",
};

/** Properties of the selected transition: endpoints, names, trigger, condition and action. */
export function TransitionPanel({ index }: { index: number }) {
  const { store } = useDesigner();
  const root = useDesignerState((s) => s.root);
  const path = useDesignerState((s) => s.path);
  const readOnly = useDesignerState((s) => s.readOnly);
  const { items: expressionDescriptors } = useExpressionDescriptors();
  const syntaxes = useMemo(() => selectableSyntaxes(expressionDescriptors), [expressionDescriptors]);
  const sm = resolveContainer(root, path);
  if (!isStateMachine(sm)) return null;
  const t = sm.transitions[index];
  if (!t) return null;
  const s = store.getState;
  const update = (patch: Partial<typeof t>) => s().updateTransition(index, (x) => ({ ...x, ...patch }));
  const stateOptions = sm.states.map((st) => ({ value: st.name, label: st.name }));

  return (
    <div className="flex flex-col gap-5 p-3">
      <div>
        <div className="eyebrow">Transition</div>
        <div className="text-lg font-semibold">{t.displayName || t.name || `${t.from} → ${t.to}`}</div>
        <p className="text-xs text-muted-foreground">
          {t.trigger ? "Fires when the trigger completes while the source state is current." : "Automatic: taken as soon as the source state is entered (in declaration order), if the condition holds."}
        </p>
      </div>
      <div className="grid grid-cols-2 gap-3">
        <Field label="From">
          <SimpleSelect value={t.from} options={stateOptions} onChange={(v) => v && update({ from: v })} disabled={readOnly} />
        </Field>
        <Field label="To">
          <SimpleSelect value={t.to} options={stateOptions} onChange={(v) => v && update({ to: v })} disabled={readOnly} />
        </Field>
      </div>
      <div className="grid grid-cols-2 gap-3">
        <Field label="Name" hint="Technical name">
          <Input value={t.name ?? ""} readOnly={readOnly} onChange={(e) => update({ name: e.target.value || null })} />
        </Field>
        <Field label="Display name">
          <Input value={t.displayName ?? ""} readOnly={readOnly} onChange={(e) => update({ displayName: e.target.value || null })} />
        </Field>
      </div>
      <SlotChip ownerId={sm.id} slot={`transitions.${index}.trigger`} child={t.trigger} label="Trigger" hint="A trigger activity (Timer, Event, HTTP Endpoint…) armed while the source state is current. Leave empty for an automatic transition. Each transition needs its own trigger." readOnly={readOnly} triggersOnly />
      <Field label="Condition" hint="Optional. When false, the trigger is re-armed until it holds or another transition wins.">
        <ExpressionEditor
          descriptor={CONDITION_DESCRIPTOR}
          value={t.condition ?? undefined}
          syntaxes={syntaxes.filter((x) => x !== "Literal")}
          readOnly={readOnly}
          onChange={(v) => update({ condition: (v as ActivityInput | undefined) ?? null })}
          literalSyntax="Literal"
        >
          {({ value, onChange }) => <Input value={typeof value === "string" ? value : ""} readOnly={readOnly} placeholder="true" onChange={(e) => onChange(e.target.value)} />}
        </ExpressionEditor>
      </Field>
      <SlotChip ownerId={sm.id} slot={`transitions.${index}.action`} child={t.action} label="Action" hint="Runs after the source state's exit activity and before the target state is entered." readOnly={readOnly} />
      {!readOnly && (
        <Button variant="ghost" size="sm" className="self-start text-status-danger hover:bg-status-danger/10" onClick={() => s().removeTransitions([index])}>
          <Trash2 /> Delete transition
        </Button>
      )}
      <Button variant="link" size="sm" className="self-start px-0" onClick={() => s().select([selectionId.state(t.from)])}>
        Go to state {t.from}
      </Button>
    </div>
  );
}
