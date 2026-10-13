import { describe, expect, it } from "vitest";
import { storeFields, storeRequiresValue, type SecretStoreDescriptor } from "../api";

const store = (overrides: Partial<SecretStoreDescriptor> & Pick<SecretStoreDescriptor, "name">): SecretStoreDescriptor => ({
  displayName: overrides.name,
  description: "",
  capabilities: "Read, Write, Test",
  isReadOnly: false,
  ...overrides,
});

describe("storeFields", () => {
  it("returns what a store declares, including a store this studio has never heard of", () => {
    const declared = store({
      name: "azure-key-vault",
      fields: [{ name: "vaultName", displayName: "Vault secret name", description: "", kind: "Text", required: false, options: [] }],
    });

    expect(storeFields(declared).map((f) => f.name)).toEqual(["vaultName"]);
  });

  it("declares nothing for a store with an empty field list", () => {
    expect(storeFields(store({ name: "encrypted", fields: [] }))).toEqual([]);
  });

  it("synthesizes the configuration key for servers predating store-declared fields", () => {
    const field = storeFields(store({ name: "configuration", isReadOnly: true }))[0];

    expect(field.name).toBe("configurationKey");
    expect(field.required).toBe(true);
  });

  it("declares nothing for other stores on those servers", () => {
    expect(storeFields(store({ name: "encrypted" }))).toEqual([]);
    expect(storeFields(undefined)).toEqual([]);
  });
});

describe("storeRequiresValue", () => {
  it("uses the store's own answer when it has one", () => {
    expect(storeRequiresValue(store({ name: "azure-key-vault", isReadOnly: true, requiresValue: false }))).toBe(false);
    expect(storeRequiresValue(store({ name: "azure-key-vault", requiresValue: true }))).toBe(true);
  });

  it("falls back to the pre-descriptor rule on older servers", () => {
    expect(storeRequiresValue(store({ name: "encrypted" }))).toBe(true);
    expect(storeRequiresValue(store({ name: "configuration", isReadOnly: true }))).toBe(false);
    expect(storeRequiresValue(store({ name: "read-only", capabilities: "Read, Test" }))).toBe(false);
  });

  it("requires nothing when there is no store yet", () => {
    expect(storeRequiresValue(undefined)).toBe(false);
  });
});
