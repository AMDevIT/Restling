# Restling Contributing Guidelines  

This document explains how to contribute to the Restling project. Restling aims to be accessible and well-supported for everyone. To achieve this, some rules must be followed to ensure a smooth and structured workflow.

## New Contributors  

If you are new to Restling, welcome! Before contributing, please:  
- 📖 **Read the [README](./README.md) and relevant Wiki pages**.  
- 🛠️ **Build the project locally** and explore its structure.   

Understanding the project is the first step in making meaningful contributions.

## Naming Conventions and Code Syntax  

Restling follows the **Microsoft .NET naming conventions** and aims to use the latest **C# syntax** whenever possible. However, there are some exceptions:

### Private Fields  

Restling does **not** enforce the use of an underscore (`_`) prefix for private fields. Instead, it follows **camelCase** naming, similar to local variables.  
To distinguish **class-level variables**, always use the `this` keyword.  

#### ❓ Why?  
Using `_` does not explicitly indicate whether a variable belongs to the class or is local.  
Conversely, using `this` ensures clarity.  

❌ **Ambiguous**: `_a` (Could be a class member or a local variable)  
✅ **Clear**: `this.a` (Must be a class member)  

### Syntax Sugar  

Using **syntax sugar** can make code more elegant and readable. However, if it becomes overly complex and difficult to understand—especially at **2 AM after 20 hours of coding**—then it is not serving its purpose.  

⚡ **Keep it simple and easy to read whenever possible.**  

## 🔄 Working on Your Own Copy of the Repository  

### 🛠️ **For External Contributors**  
If you are **not** part of the main contributors' team, please follow these guidelines:  
1. **Fork the repository** and work on your own copy.  
2. **Create a new branch** for each issue or feature.  
3. **Submit a pull request (PR)** to the main repository once your changes are complete.  
4. **Tag all related issues** in your PR for better tracking and discussion.  
5. **Avoid working directly on the `main` branch** unless you are a core maintainer.  

### 🔧 **For Core Maintainers**  
If you **are** part of the main contributors' team, please follow these additional guidelines:  
1. **Always create a new branch, preferably related to the task ID** for each issue or feature.  
   - Example: `Task-10-DoSomething` for issue **#10**.  
   - If the branch is unrelated to a specific task, use a descriptive name that clearly explains its purpose.  
2. **Submit a pull request (PR)** once your changes are complete.  
3. **Tag all related issues** in your PR for better tracking and discussion.  
4. **Avoid working directly on the `main` branch** unless strictly necessary.  

## ❓ How to Get Help  

If you need help, feel free to:  
- Open an **issue** on GitHub.  
- Ask questions in the project's **discussion section** (if available).  
- Reach out via any **official communication channels**.  

Currently, the only available support channel is **issue discussions**, but we are working on enabling more communication channels in the near future.  