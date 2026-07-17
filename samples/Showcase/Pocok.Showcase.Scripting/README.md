# Pocok.Scripting showcase slice

This trusted showcase plugin executes complete JavaScript source through the real `Pocok.Scripting` `ScriptRunner` and displays the returned value or structured failure.

The slice deliberately exposes no CLR objects, filesystem, network, service provider, browser APIs, or user-defined host bindings. Each run creates a fresh Jint engine and applies editable but narrowly bounded timeout, statement, recursion, source-size, and memory limits.

The guide documents imports and explicit capabilities, but the public playground keeps the surface smaller: one complete script in, one isolated result or failure out.
