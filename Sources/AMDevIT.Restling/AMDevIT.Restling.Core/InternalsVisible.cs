// This file makes the internals of the assembly visible to the Unit Test project.
// This is necessary to allow the unit tests to access internal members of the assembly for testing purposes.
// This is a common practice in .NET development to facilitate testing without exposing
// internal members to the public API.

#if DEBUG

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("AMDevIT.Restling.Tests")]

#endif