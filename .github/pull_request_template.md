## Summary
- What does this change do? Why?

## Readability Checklist (must pass)
- [ ] No ambiguous abbreviations in names (classes/methods/variables/events)
- [ ] Public APIs documented with XML docs
- [ ] Single responsibility per method; early returns used where appropriate
- [ ] No magic numbers (use ScriptableObject configs or constants)
- [ ] Unity best practices observed (cached components, correct Update/FixedUpdate usage)
- [ ] Files named after the main public type; one public type per file
- [ ] Private fields use `_camelCase`; methods/properties/events use PascalCase

## Risks / Notes
- Any behavior changes? Performance considerations? Event ordering dependencies?

## Testing
- How was this validated (play mode tests, unit tests, manual scenarios)?