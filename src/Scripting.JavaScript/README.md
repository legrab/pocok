# Pocok.Scripting.JavaScript

Jint-backed JavaScript adapter. It disables CLR interop and string compilation, uses fresh engine state, enforces Jint
timeout/statement/recursion/memory limits, and applies parser-backed dynamic-code guardrails before execution.

Validation is a guardrail, not an OS sandbox. Public anonymous deployments should not enable the trusted-local C# or
Python adapters.
