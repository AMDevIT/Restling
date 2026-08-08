# Documentation context

## Objective and status

- Objective: update the main Restling README with the current public APIs and quick starts, then create a complete GitHub Wiki guide for using Restling.
- Status: completed.

## Decisions made

- Kept the public documentation in English to match the existing repository language and public API terminology.
- Corrected the installation package ID from `AMDevIT.Restling.Core` to the published `Restling` package.
- Documented the target frameworks currently declared by the library: .NET 8, .NET 9, and .NET 10.
- Kept the README focused on installation, core capabilities, and copyable quick starts.
- Split detailed guidance into topic-specific GitHub Wiki pages and added `_Sidebar.md` for navigation.
- Derived examples and behavioral notes from the current source and tests rather than from the previous README.
- Created only this context file, without a separate progress file, as explicitly requested by the user.

## Affected files

Main repository:

- `README.md`
- `.agents/context.md`

Wiki repository:

- `Home.md`
- `_Sidebar.md`
- `Installation.md`
- `Quick-Start.md`
- `Requests.md`
- `Client-Configuration.md`
- `Headers-and-Authentication.md`
- `Serialization.md`
- `Cookies.md`
- `Responses-and-Errors.md`
- `Security.md`

## Checks performed

- Compared documented types, overloads, properties, serializer choices, handler behavior, cookie APIs, success codes, and security defaults against the current source files and tests.
- Ran `git diff --check` in both repositories: the edited Markdown files passed; only line-ending conversion warnings were reported.
- Checked all edited Markdown files for balanced fenced code blocks: passed.
- Checked relative links between GitHub Wiki pages and their target files: passed.
- Searched for the obsolete package ID and known example/version mistakes: no remaining matches.
- Did not run `dotnet restore`, `dotnet build`, or tests because the user explicitly limited verification to Markdown files.

## Open issues and recommended next step

- No documentation blocker remains.
- The wiki repository also contains a staged `.gitignore` outside this task. It was left untouched because the user limited changes to Markdown files; `git diff --cached --check` reports existing trailing whitespace on its line 9.
- Recommended next step: review the rendered README and GitHub Wiki after publishing, then commit and push the two repositories independently.
