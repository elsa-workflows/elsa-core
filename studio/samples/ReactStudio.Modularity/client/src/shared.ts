// One ESM runtime owns React, the query client bindings and SDK contexts. Every
// separately built module resolves its bare imports to this same import-map URL.
export { default, Component, createElement, createContext, useContext, useState, useEffect } from "react";
export { createRoot } from "react-dom/client";
export { QueryClient, QueryClientProvider, useQueryClient, useQuery } from "@tanstack/react-query";
export * from "./sdk";
