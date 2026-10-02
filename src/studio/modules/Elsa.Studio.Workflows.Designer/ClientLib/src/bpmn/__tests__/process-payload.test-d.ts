/**
 * Type-checks a real serialized `BpmnProcess.Process` payload against the generated types, with no
 * cast anywhere in the assignment below. If `BpmnProcessDefinition` in ../types.generated stops
 * describing the JSON Elsa's activity serializer actually produces, `npm test` fails to compile this
 * file -- that is the whole test.
 *
 * ../__fixtures__/camunda-order-process.process.json is not a hand-written example: it is the exact
 * value of the `"process"` property from the `Elsa.BpmnProcess` activity that
 * `Bpmn.Interchange.BpmnInterchangeDocumentService.ImportAsync` produces for
 * `test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Assets/camunda-order-process.bpmn` in
 * elsa-core, captured by asking `Elsa.Workflows.Serialization.Serializers.JsonActivitySerializer` --
 * the same serializer a GET of a workflow definition goes through -- to serialize the resulting
 * workflow's root activity, then lifting out the `"process"` property. It was NOT captured through
 * the HTTP import endpoint mentioned in studio issue #997, because doing that needs a running
 * elsa-core host, which is outside this repository's test suite; this is the same wire format the
 * endpoint produces, reached through the library code the endpoint itself calls.
 *
 * To reproduce: with elsa-core checked out alongside this repo, run a small program that
 *   1. builds an `IServiceCollection` with `.AddElsa(elsa => elsa.UseWorkflowManagement().UseBpmnInterchange())`,
 *   2. calls `provider.PopulateRegistriesAsync()` (Elsa.Testing.Shared.Integration),
 *   3. resolves `BpmnInterchangeDocumentService` and calls
 *      `ImportAsync(xml, definitionId: null, name: null, processId: null, cancellationToken)` with the
 *      asset's XML,
 *   4. parses `result.ImportResult.WorkflowDefinition.StringData` as JSON and writes out its
 *      `"process"` property.
 */
import type { BpmnProcessDefinition } from '../types.generated';
import processFixture from '../__fixtures__/camunda-order-process.process.json';

const typedProcess: BpmnProcessDefinition = processFixture;
void typedProcess;

/**
 * `BpmnProcessDefinition` (like every structural TypeScript type) happily accepts a value with
 * *extra* properties, so the plain assignment above cannot catch a field disappearing from the
 * generated type as long as the fixture still has a property of that name elsewhere -- structural
 * typing only checks that required properties are present and compatible, not that no others exist.
 *
 * `ExcessKeys<Fixture, Shape>` walks both in lockstep -- recursing into nested objects and arrays, so
 * the same check applies to elements, sequence flows, extensions, etc. -- and collects the name of
 * every key it finds in `Fixture` that `Shape` does not also declare at that position. If nothing is
 * excess anywhere, the result is `never`; asserting that below fails to compile the moment the fixture
 * has a property, at any depth, that the generated type does not.
 *
 * This does not attempt a full bidirectional type-equality check (e.g. `expectTypeOf().toEqualTypeOf`)
 * because the generated types are deliberately looser than any single fixture in places the schema
 * allows to vary (e.g. `bindingRef?: string | null` is only present on some element kinds): a
 * `toEqualTypeOf` assertion would fail on that legitimate looseness, not on a real regression.
 * Excess-key detection is the narrower, correct check for what this test guards against: a field
 * silently disappearing from the generated type while still being produced by the serializer.
 *
 * Two TypeScript quirks the implementation works around, both specific to comparing a type inferred
 * from imported JSON against a hand-authored one:
 *  - `IsAny<T>` short-circuits `unknown`-ish leftovers (e.g. an empty array literal infers as `any[]`)
 *    before they reach `keyof`, which would otherwise widen to `string | number | symbol` and make
 *    every object look like it has excess keys.
 *  - `[F] extends [undefined]` short-circuits an absent optional property (e.g. `bindingRef` on a
 *    fixture element that has none) before the array/object checks: with this project's non-strict
 *    `tsconfig.json` (see ADR 0005), `undefined` satisfies `extends` against every type, including
 *    `extends readonly unknown[]`, which would otherwise be misread as an array/shape mismatch.
 */
type IsAny<T> = 0 extends 1 & T ? true : false;

type ExcessKeys<Fixture, Shape> = IsAny<Fixture> extends true
    ? never
    : [Fixture] extends [undefined]
        ? never
        : Fixture extends readonly (infer FixtureElement)[]
            ? Shape extends readonly (infer ShapeElement)[]
                ? ExcessKeys<FixtureElement, ShapeElement>
                : 'ARRAY_SHAPE_MISMATCH'
            : Fixture extends object
                ? Shape extends object
                    ? { [K in keyof Fixture]: K extends keyof Shape ? ExcessKeys<Fixture[K], Exclude<Shape[K], null | undefined>> : K }[keyof Fixture]
                    : 'OBJECT_SHAPE_MISMATCH'
                : never;

type IsNever<T> = [T] extends [never] ? true : false;

const noExcessKeys: IsNever<ExcessKeys<typeof processFixture, BpmnProcessDefinition>> = true;
void noExcessKeys;
