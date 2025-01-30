# Restling Contributing Guidelines  

This document explains how to contribute to the Restling project. Restling aims to be accessible and well-supported for everyone. To achieve this, some rules must be followed to ensure a smooth and structured workflow.

## New Contributors  

For new contributors, at least a basic understanding of the project’s mechanics is required. Before contributing, please:  
- Read the **README** and relevant **wiki** pages.  
- Build the project locally and familiarize yourself with its structure.  

Understanding the project is the first step in making meaningful contributions.

## Naming Conventions and Code Syntax  

Restling follows the **Microsoft .NET naming conventions** and aims to use the latest **C# syntax** whenever possible. However, there are some exceptions to these rules:

### Private Fields  

Restling does **not** enforce the use of an underscore (`_`) prefix for private fields. Instead, it prefers standard **camelCase** naming, similar to local variables. To distinguish class-level variables, the `this` keyword should be used.  

#### Why?  
Using `_` does not explicitly indicate whether a variable belongs to the class or is local. Conversely, using `this` ensures clarity. For example:  

❌ **Ambiguous**: `_a` (Could be a class member or a local variable)  
✅ **Clear**: `this.a` (Must be a class member)  

### Syntax Sugar  

Using **syntax sugar** can make code more elegant and readable. However, if it becomes overly complex and difficult to understand—especially at **2 AM after 20 hours of coding**—then it is not serving its purpose.  

🔹 **Keep it simple and easy to read whenever possible.**  

## Working on Your Own Copy of the Repository  

If you are **not** part of the main contributors' team, please follow these guidelines when contributing:  
1. **Fork the repository** and work on your own copy.  
2. **Create a new branch** for each issue or feature you are working on.  
3. **Submit a pull request (PR)** to the main repository once your changes are complete.  
4. **Tag all related issues** in your PR to improve tracking and discussion.  
5. **Avoid working directly on the `main` branch** unless you are a core maintainer.  
