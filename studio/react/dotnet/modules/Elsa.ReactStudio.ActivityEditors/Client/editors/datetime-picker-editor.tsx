
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { useState } from "react";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import type { UiHintEditorProps } from "../types";
import { dateTimeParts, dateTimeValue } from "./editor-values";
import { LiteralControl } from "./literal-control";

export function DateTimePickerEditor(props: UiHintEditorProps) {
  useTranslation();
  return <LiteralControl props={props}>{({ value, onChange }) =>
    <DateTimeFields key={`${props.context.activity.id}:${props.descriptor.name}`} value={value ?? props.descriptor.defaultValue}
      onChange={onChange} label={props.descriptor.displayName ?? props.descriptor.name} readOnly={props.readOnly} />
  }</LiteralControl>;
}

function DateTimeFields({ value, onChange, label, readOnly }: {
  value: unknown; onChange: (value: unknown) => void; label: string; readOnly?: boolean;
}) {
  const _t = useTranslation();
  const { date, time } = dateTimeParts(value);
  const [showEmptyTime, setShowEmptyTime] = useState(false);
  const showTime = !!date && (showEmptyTime || (time !== "" && time !== "00:00:00"));
  return <div className="flex flex-col gap-2">
    <Input type="date" aria-label={_t("{{value0}} date", { value0: label })} value={date} readOnly={readOnly}
      onChange={event => {
        if (!event.target.value) setShowEmptyTime(false);
        onChange(dateTimeValue(event.target.value, showTime ? time : ""));
      }} />
    {showTime && <Input type="time" step={1} aria-label={_t("{{value0}} time", { value0: label })} value={time} readOnly={readOnly}
      onChange={event => onChange(dateTimeValue(date, event.target.value))} />}
    {date && <Button type="button" size="sm" variant="outline" disabled={readOnly} onClick={() => {
      if (showTime) onChange(dateTimeValue(date, ""));
      setShowEmptyTime(!showTime);
    }}>{showTime ? _t("Clear time") : _t("Add time")}</Button>}
  </div>;
}
