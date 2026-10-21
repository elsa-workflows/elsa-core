"use client";

import CodeMirror from "@uiw/react-codemirror";
import { useMemo } from "react";
import { useTheme } from "next-themes";
import { javascript } from "@codemirror/lang-javascript";
import { json } from "@codemirror/lang-json";
import { python } from "@codemirror/lang-python";
import { liquid } from "@codemirror/lang-liquid";
import { sql } from "@codemirror/lang-sql";
import { StreamLanguage } from "@codemirror/language";
import { csharp } from "@codemirror/legacy-modes/mode/clike";
import { autocompletion, type CompletionContext } from "@codemirror/autocomplete";
import { cn } from "../utils.js";
import type { Extension } from "@codemirror/state";


import type { CodeLanguage } from "@elsa-workflows/react-studio-sdk";
export type { CodeLanguage } from "@elsa-workflows/react-studio-sdk";

export interface CodeCompletion {
  label: string;
  detail?: string;
  type?: string;
}

function completionExtension(items: CodeCompletion[]): Extension {
  const options = items.map((i) => ({ label: i.label, detail: i.detail, type: i.type ?? "variable" }));
  const calls = new Set(items.flatMap(item => {
    const name = item.label.match(/^([\w$]+(?:\.[\w$]+)*)\((?!\))/)?.[1];
    return name ? [name] : [];
  }));
  // A balanced accessor call can be part of a member chain; an unmatched opening
  // parenthesis belongs to the surrounding expression and must never be replaced.
  const wordPattern = /[A-Za-z_$][\w$]*(?:\([^()\n]*\)|\[[^[\]\n]*\])?(?:\.[\w$]+(?:\([^()\n]*\)|\[[^[\]\n]*\])?)*\.?/;
  return autocompletion({
    override: [
      (ctx: CompletionContext) => {
        // When the cursor is inside a known accessor's unfinished arguments,
        // replace that accessor as a whole rather than inserting it in a string.
        // Quoted names may themselves contain parentheses or escaped quotes.
        const call = ctx.matchBefore(/[\w$]+(?:\.[\w$]+)*\((?:"(?:\\.|[^"\\])*"?|'(?:\\.|[^'\\])*'?|[^()'"\n])*/);
        const head = call?.text.slice(0, call.text.indexOf("("));
        const word = head && calls.has(head) ? call : ctx.matchBefore(wordPattern);
        if (!word || (word.from === word.to && !ctx.explicit)) return null;
        return { from: word.from, options };
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
      return [StreamLanguage.define(csharp)];
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
