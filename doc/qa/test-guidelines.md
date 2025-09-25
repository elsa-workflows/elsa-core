# Test Guidelines

## Unit tests
All logic should be covered by unit tests. Unit tests should be isolated, fast, and deterministic. Use mocking frameworks to isolate dependencies.

The desired code coverage is 100%.

## dump

Consider the following constraints to have the best possible way of testing of the elsa engine

- Not be affected by execution times
- not having to depend on delays unless there is no other way
- how do we get the workflow definitions in the engine
  - is the test responsible for publish
  - can it be in bulk by an import
  - can it work with docker, env, k8s cluster deployments
  - how can we manage the tests version with the workflow definition version?
- what are steps that we need, the templates to not have repetitive implementations
- is the journal and activity execution endpoints the best to do the asserts upon.
- are there alternatives like querying the db
..... 
- is execute the best to test the activities or a workflow with and http endpoint
- how to test failures , like the fail activity, no workflow instance returned no way to find the instance
  - or by using a correlation id
  - how to avoid get latest instance of a definition etc.

the goal is to have a testing environment that is consistent in the execution. 