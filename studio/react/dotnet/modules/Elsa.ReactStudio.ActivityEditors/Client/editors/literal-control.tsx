import type { ReactNode } from "react";
import { ExpressionEditor } from "../expression-editor";
import type { UiHintEditorProps } from "../types";

/** Unwrapped properties write their native value; wrapped inputs retain their expression envelope. */
export function LiteralControl({ props, syntax = "Literal", children }: {
  props: UiHintEditorProps;
  syntax?: "Literal" | "Object";
  children: (literal: { value: unknown; onChange: (value: unknown) => void }) => ReactNode;
}) {
  if (props.descriptor.isWrapped === false) {
    return children({ value: props.value, onChange: value => props.onChange(value as UiHintEditorProps["value"]) });
  }
  return <ExpressionEditor {...props} literalSyntax={syntax}>{children}</ExpressionEditor>;
}
