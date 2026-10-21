"use client";

import dynamic from "next/dynamic";
import { useMemo } from "react";
import { useTheme } from "next-themes";
import { javascript } from "@codemirror/lang-javascript";
import { json } from "@codemirror/lang-json";
import { python } from "@codemirror/lang-python";
import { liquid } from "@codemirror/lang-liquid";
import { sql } from "@codemirror/lang-sql";
import { autocompletion, type CompletionContext } from "@codemirror/autocomplete";
import { cn } from "@/lib/utils";
import type { Extension } from "@codemirror/state";

const CodeMirror = dynamic(() => import("@uiw/react-codemirror"), { ssr: false });

export type CodeLanguage = "javascript" | "typescript" | "json" | "python" | "liquid" | "csharp" | "sql" | "plaintext";

export interface CodeCompletion {
  label: string;
  detail?: string;
  type?: string;
}

function completionExtension(items: CodeCompletion[]): Extension {
  return autocompletion({
    override: [
      (ctx: CompletionContext) => {
        const word = ctx.matchBefore(/[\w.$'"]+/);
        if (!word || (word.from === word.to && !ctx.explicit)) return null;
        return { from: word.from, options: items.map((i) => ({ label: i.label, detail: i.detail, type: i.type ?? "variable" })), validFor: /^[\w.$'"]*$/ };
      },
    ],
  });
}

function languageExtension(language: CodeLanguage): Extension[] {
  switch (language) {
    case "javascript":
      return [javascript()];
    case "typescript":
      return [javascript({ typescript: true })];
    case "json":
      return [json()];
    case "python":
      return [python()];
    case "liquid":
      return [liquid()];
    case "sql":
      return [sql()];
    case "csharp":
    case "plaintext":
    default:
      return [];
  }
}

/** CodeMirror-based editor used for expressions, JSON and the definition code view. */
export function CodeEditor({
  value,
  onChange,
  language = "plaintext",
  height = "12rem",
  readOnly,
  className,
  placeholder,
  minimal,
  completions,
}: {
  value: string;
  onChange?: (value: string) => void;
  language?: CodeLanguage;
  height?: string;
  readOnly?: boolean;
  className?: string;
  placeholder?: string;
  /** Hides gutters for single-expression use. */
  minimal?: boolean;
  /** Names offered by autocomplete (variables, inputs, activity outputs). */
  completions?: CodeCompletion[];
}) {
  const { resolvedTheme } = useTheme();
  const extensions = useMemo(
    () => [...languageExtension(language), ...(completions?.length ? [completionExtension(completions)] : [])],
    [language, completions],
  );
  return (
    <div className={cn("rounded-md border overflow-hidden text-sm", className)}>
      <CodeMirror
        value={value}
        height={height}
        theme={resolvedTheme === "dark" ? "dark" : "light"}
        extensions={extensions}
        onChange={onChange}
        readOnly={readOnly}
        editable={!readOnly}
        placeholder={placeholder}
        basicSetup={{
          lineNumbers: !minimal,
          foldGutter: !minimal,
          highlightActiveLine: !minimal,
          highlightActiveLineGutter: !minimal,
        }}
      />
    </div>
  );
}
