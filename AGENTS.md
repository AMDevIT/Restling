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

## Source Files

Try to strictly follow the rule of one class/object per source file. Do not declare multiple objects—whether classes, interfaces, enums, or any other type—in the same source file unless they are nested objects.
For generic types, follow the Microsoft convention where the non-generic parent type is placed in a file with the same name as the class, while the generic version is placed in a file using the `OfT` suffix.

Example:

```text
class List       => List.cs
class List<T>    => ListOfT.cs
```


## Variable Declarations

Whenever possible, and especially when this does not interfere with `using` and `await using` directives, try to declare local variables at the beginning of their enclosing scope.
Avoid using the `var` keyword as much as possible, unless it is necessary or provides a clear improvement in readability, such as when dealing with multiple nested generic types or very long class names.
Always use `this` before class fields and properties whenever possible, even when it is redundant and optional. Do not use underscores (`_`) when declaring class fields.
The rule is simple: inside methods, when a field or property belongs to the class definition, access it through `this`. When a variable is local to the method, reference it directly because `this` does not apply.
This makes it immediately clear whether a variable should be looked for in the current local scope or in the class declaration.

## Indentation

Always indent code correctly. Do not insert a line break immediately after an opening parenthesis. Always align function arguments, and try to keep short lambdas on a single line.
Each indentation level uses 4 spaces.

### Method and Constructor Call Formatting

These rules apply both to source files and to C# examples inside Markdown files.

- Never place the first argument on a new line after the opening parenthesis.
- When arguments span multiple lines, keep the first argument on the same line as the method or constructor call.
- Align every subsequent argument vertically with the first argument.
- Apply the same formatting to target-typed `new(...)` expressions.
- Before completing documentation changes, verify every multiline C# invocation against these rules.

Example:

```csharp
HttpCookieData sessionCookie = new("session-id",
                                   sessionId,
                                   domain: "api.example.com",
                                   path: "/",
                                   isSecure: true);
```

Examples:

```csharp
builder.Services.AddTransient<IMyInterface>(services => services.GetRequiredService<MyInterfaceImplementation>());

builder.Services.AddDbContextFactory<MyDataSQLiteDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
    options.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.RedundantIndexRemoved));
});

public sealed partial class DataSourcesPageViewModel(IMyInterface myInterface,
                                                     ILogger<DataSourcesPageViewModel> logger)
    : ViewModelBase(logger)
```

instead of:

```csharp
builder.Services.AddTransient<IMyInterface>(services => 
     services.GetRequiredService<MyInterfaceImplementation>());

   builder.Services.AddDbContextFactory<MyDataSQLiteDbContext>(options =>
   {
       options.UseSqlite($"Data Source={dbPath}");
       options.ConfigureWarnings(warnings =>
           warnings.Ignore(CoreEventId.RedundantIndexRemoved));
   });

   public sealed partial class DataSourcesPageViewModel(
    IMyInterface myInterface,
    ILogger<DataSourcesPageViewModel> logger)
    : ViewModelBase(logger)
```

## Build and Verification

If a build and verification is needed always ask first before use tokens in an unwanted manner.
If confirmation to build and verify is given, from the repository root, restore the solution packages using `dotnet restore` and build each solution using `dotnet`:

```powershell
dotnet restore Sources\AMDevIT.Restling
dotnet build Sources\AMDevIT.Restling
```

If a check cannot be performed, briefly record the reason in the progressive
context and in the final summary.
