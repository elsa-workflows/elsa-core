import { defineConfig, globalIgnores } from "eslint/config";
import tseslint from "typescript-eslint";
import react from "eslint-plugin-react";
import hooks from "eslint-plugin-react-hooks";

export default defineConfig([
  globalIgnores(["dist/**", "packages/*/dist/**", "docs/api/**", "node_modules/**", "dotnet/**/bin/**", "dotnet/**/obj/**", "dotnet/**/wwwroot/**", "module-assets/**", "test-results/**", "playwright-report/**"]),
  ...tseslint.configs.recommended,
  {
    files: ["src/**/*.{ts,tsx}", "packages/*/src/**/*.{ts,tsx}", "samples/**/*.{ts,tsx}", "dotnet/modules/*/Client/**/*.{ts,tsx}"],
    plugins: { react, "react-hooks": hooks },
    settings: { react: { version: "detect" } },
    rules: {
      ...react.configs.recommended.rules,
      ...react.configs["jsx-runtime"].rules,
      "react/prop-types": "off",
      "react-hooks/rules-of-hooks": "error",
      "react-hooks/exhaustive-deps": "warn",
    },
  },
]);
