// #8855: keep the built-in UI catalog synchronized with explicit translation calls.
import ts from "typescript";
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { join, relative } from "node:path";

const root = process.cwd();
const write = process.argv.includes("--write");
function files(directory) {
  return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    if (["node_modules", "dist", "bin", "obj", "wwwroot", "locales", "__tests__"].includes(entry.name)) return [];
    const path = join(directory, entry.name);
    return entry.isDirectory() ? files(path) : /\.tsx?$/.test(path) && !/\.test\.|\.d\.ts$/.test(path) ? [path] : [];
  });
}
const attributes = new Set(["heading", "title", "placeholder", "aria-label", "aria-description", "alt", "label", "description", "hint", "eyebrow", "searchPlaceholder", "emptyText", "confirmLabel", "cancelLabel", "alternateLabel", "loadingText", "helperText", "tooltip", "emptyMessage", "caption", "message"]);
const keys = new Set();
const errors = [];
for (const path of ["src", "packages/sdk/src", "packages/ui/src", "packages/auth/src", "dotnet/modules"].flatMap(name => files(join(root, name)))) {
  const source = ts.createSourceFile(path, readFileSync(path, "utf8"), ts.ScriptTarget.Latest, true, path.endsWith("tsx") ? ts.ScriptKind.TSX : ts.ScriptKind.TS);
  const report = (node, message) => errors.push(`${relative(root, path)}:${source.getLineAndCharacterOfPosition(node.getStart(source)).line + 1}: ${message}`);
  function visit(node) {
    if (ts.isCallExpression(node) && ["t", "_t", "translate", "message"].includes(node.expression.getText(source))) {
      const key = node.arguments[0];
      if (key && (ts.isStringLiteral(key) || ts.isNoSubstitutionTemplateLiteral(key))) keys.add(key.text);
    }
    if (ts.isJsxText(node) && /[A-Za-z]/.test(node.text)) report(node, "UI text needs a translation call");
    if (ts.isJsxAttribute(node) && attributes.has(node.name.getText(source)) && node.initializer) {
      const value = ts.isJsxExpression(node.initializer) ? node.initializer.expression : node.initializer;
      if (value && (ts.isStringLiteral(value) || ts.isNoSubstitutionTemplateLiteral(value)) && /[A-Za-z]/.test(value.text) && !/^https?:\/\//.test(value.text)) report(node, "UI attribute needs a translation call");
    }
    if (ts.isJsxExpression(node) && !ts.isJsxAttribute(node.parent) && node.expression && (ts.isStringLiteral(node.expression) || ts.isNoSubstitutionTemplateLiteral(node.expression)) && /[A-Za-z]/.test(node.expression.text)) report(node, "UI display string needs a translation call");
    if (ts.isCallExpression(node) && /^toast\.(success|error|warning|info|message)$/.test(node.expression.getText(source))) {
      const value = node.arguments[0];
      if (value && (ts.isStringLiteral(value) || ts.isNoSubstitutionTemplateLiteral(value) || ts.isTemplateExpression(value))) report(node, "Toast text needs a translation call");
    }
    ts.forEachChild(node, visit);
  }
  visit(source);
}
const english = Object.fromEntries([...keys].sort().map(key => [key, key]));
const directory = join(root, "packages/sdk/src/locales");
if (write) {
  writeFileSync(join(directory, "en.json"), JSON.stringify(english, null, 2) + "\n");
  const previous = JSON.parse(readFileSync(join(directory, "nl.json"), "utf8"));
  writeFileSync(join(directory, "nl.json"), JSON.stringify(Object.fromEntries(Object.keys(english).map(key => [key, previous[key] ?? key])), null, 2) + "\n");
}
const placeholders = value => [...value.matchAll(/\{\{\s*([^{}]+)\s*\}\}/g)].map(match => match[1].trim()).sort().join(",");
for (const culture of ["en", "nl"]) {
  const catalog = JSON.parse(readFileSync(join(directory, `${culture}.json`), "utf8"));
  for (const key of keys) {
    if (typeof catalog[key] !== "string" || !catalog[key].trim()) errors.push(`${culture}: missing or empty message ${JSON.stringify(key)}`);
    else if (placeholders(catalog[key]) !== placeholders(key)) errors.push(`${culture}: interpolation differs for ${JSON.stringify(key)}`);
  }
  if (Object.keys(catalog).some(key => !keys.has(key))) errors.push(`${culture}: catalog contains unused messages; run pnpm localization:extract`);
}
if (errors.length) { console.error(errors.join("\n")); process.exitCode = 1; }
else console.log(`PASS: ${keys.size} built-in UI messages have English/Dutch catalog entries and matching interpolation`);
