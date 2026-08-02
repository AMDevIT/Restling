# Agent Instructions

These instructions apply to the entire repository.

## Project Context

- The main project is a cross-platform .NET library that supports .NET for iOS, .NET for Android, and .NET MAUI for HTTP rest operations.
- It is located in the `Sources\AMDevIT.Restling` subfolder.

## Workflow

1. Before starting, read the `.md` files in the `.agents` subfolder.
2. Preserve the user's changes and do not modify files unrelated to the task.
3. Always analyze the requested work first and ask for confirmation.
4. Prefer small, targeted changes that are consistent with the existing architecture. When the requested change is extensive, warn the user and assess the risks.
5. After each change, run the relevant checks whenever the environment allows it.
6. At the end of a significant task, update the `.agents` subfolder with additional `.md` files containing the context for the current step, then update `.agents/context.md` with:
   - objective and status;
   - decisions made;
   - affected files;
   - checks performed and their results;
   - open issues and the recommended next step.

# Project Coding Conventions

Apply these conventions to all code added to or modified in this repository.

## Comments

- Write all code comments in English.
- If new methods are created, add dotnet XML comments to allow intellinsense help for the new methods.

## Class Member Organization

Order class members as follows:

1. Constants
2. Events
3. Private fields
4. Properties
5. Constructors
6. Public methods
7. Protected methods
8. Private methods
9. Event handlers

Group each applicable category in a `#region`/`#endregion` block, using exactly the following region names:

- `Const` for constants
- `Events` for events
- `Fields` for private fields
- `Properties` for properties
- `.ctor` for constructors
- `Methods` for all methods, with public methods first, followed by protected methods and then private methods
- `Event handlers` for all event handlers

Do not create empty regions when a class does not contain members belonging to the corresponding category.

## Indentation

Always indent code correctly, following the style already used in the source files.

## Build and Verification

If a build and verification is needed always ask first before use tokens in an unwanted manner.
If confirmation to build and verify is given, from the repository root, restore the solution packages using `dotnet restore` and build each solution using `dotnet`:

```powershell
dotnet restore Sources\AMDevIT.Restling
dotnet build Sources\AMDevIT.Restling
```

If a check cannot be performed, briefly record the reason in the progressive
context and in the final summary.
