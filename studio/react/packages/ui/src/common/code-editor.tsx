"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


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
import { autocompletion, insertCompletionText, type CompletionContext } from "@codemirror/autocomplete";
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
    const name = item.label.match(/^([\w$]+(?:\.[\w$]+)*(?:<[^<>\n]+>)?)\(\s*["']/)?.[1];
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
        const call = ctx.matchBefore(/[\w$]+(?:\.[\w$]+)*(?:<[^<>\n]+>)?\(\s*(?:(?:"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')\s*,\s*)*(?:"(?:\\.|[^"\\])*"?|'(?:\\.|[^'\\])*'?)\s*/);
        const head = call?.text.slice(0, call.text.indexOf("("));
        const accessor = head && calls.has(head) ? call : null;
        const word = accessor ?? ctx.matchBefore(wordPattern);
        if (!word || (word.from === word.to && !ctx.explicit)) return null;
        let to = ctx.pos;
        if (accessor) {
          let quote = "", escaped = false, depth = 0, quoteFrom = -1;
          let quotedEnd = ctx.pos, closedCall = false;
          let pos = accessor.from + accessor.text.indexOf("(");
          for (; pos < ctx.state.doc.length; pos++) {
            const char = ctx.state.doc.sliceString(pos, pos + 1);
            if (escaped) { escaped = false; continue; }
            if (quote) {
              if (char === "\n") {
                if (quoteFrom < ctx.pos) quotedEnd = Math.max(quotedEnd, pos);
                break;
              }
              if (char === "\\") escaped = true;
              else if (char === quote) {
                if (quoteFrom < ctx.pos && pos >= ctx.pos) quotedEnd = pos + 1;
                quote = "";
              }
            } else if (char === '"' || char === "'") { quote = char; quoteFrom = pos; }
            else if (char === "(") depth++;
            else if (char === ")" && --depth === 0) { to = Math.max(to, pos + 1); closedCall = true; break; }
          }
          if (!closedCall) {
            if (quote && quoteFrom < ctx.pos && pos === ctx.state.doc.length) quotedEnd = pos;
            to = Math.max(to, quotedEnd);
          }
        }
        // Filter only the prefix before the cursor. The suffix can include
        // whitespace/newlines absent from the completion label.
        const suffixLength = to - ctx.pos;
        return { from: word.from, options: suffixLength ? options.map(option => ({ ...option,
          apply: (view, completion, from, end) => view.dispatch(insertCompletionText(view.state, completion.label, from, end + suffixLength)),
        })) : options };
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
  useTranslation();
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
