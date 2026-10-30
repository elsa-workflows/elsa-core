"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { MultiTextEditor } from "./multitext-editor";
import type { UiHintEditorProps } from "../types";

/** Outcomes list; stored as an `Object` expression holding a string array. */
export function OutcomePickerEditor(props: UiHintEditorProps) {
  useTranslation();
  return <MultiTextEditor {...props} />;
}
