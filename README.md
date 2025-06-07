# SHIMI: Semantic Hierarchical Index for Memory and Inference

SHIMI is an AI-native memory and retrieval system designed for **semantically structured** and **hierarchically abstracted** knowledge access. It offers a scalable alternative to flat vector-based and graph-based retrieval, enabling agents or systems to reason over structured information with deeper contextual grounding.

## 📦 Solution Overview

This repository contains multiple projects under a unified `.NET` solution:

### 🔹 Main Projects

| Project                | Description                                                                 |
|------------------------|-----------------------------------------------------------------------------|
| `Shimi`                | Core implementation of the SHIMI memory model and semantic tree logic.     |
| `Shimi.Console`        | CLI utility to run queries, insert entities, and inspect tree structure.   |
| `Shimi.Shared`         | Shared data structures and utility functions used across SHIMI projects.   |
| `Shimi.Samples.Data`   | Sample datasets used for evaluation and experimentation.                   |

### 🔸 Comparison Project

| Project   | Description                                                                 |
|-----------|-----------------------------------------------------------------------------|
| `Rag`     | A baseline vector-based Retrieval-Augmented Generation (RAG) implementation for comparison. |

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022+ or JetBrains Rider (recommended)  
- Optional: [LINQPad](https://www.linqpad.net/) or Postman if integrating with any API layer later.

### Clone and Build

```bash
git clone <this-repo>
cd shimi
dotnet build Shimi.sln
```

### Run Console App

```bash
cd Shimi.Console
dotnet run
```

This will launch the CLI interface where you can test SHIMI's behavior, load sample data, or issue queries.

---

## 📊 Using Sample Data (`Shimi.Samples.Data`)

The `Shimi.Samples.Data` project includes representative data for evaluating SHIMI's semantic reasoning:

- **CSV/JSON Files** contain labeled entities for insertion.
- **Agent/task records** simulate real-world structured concepts.
- These are loaded via helper methods in `Shimi.Console` to populate SHIMI's semantic tree.

### To Load Sample Data:

In `Shimi.Console`:

```csharp
SampleDataLoader.LoadAll(); // Pseudocode - replace with actual call in Main
```

Or via CLI:

```bash
dotnet run -- load-sample-data
```

---

## ⚖️ Comparing with RAG

The `Rag` project provides a baseline vector search implementation using standard embedding-based retrieval. It serves as a point of comparison for:

- Accuracy
- Retrieval time
- Memory usage
- Interpretability

To run RAG:

```bash
cd Rag
dotnet run
```

The output will mimic retrieval behavior for the same sample data, helping you benchmark SHIMI's hierarchical advantage.

---

## 📁 Folder Structure

```
shimi/
├── Shimi/                # Core logic and memory system
├── Shimi.Console/        # CLI interface
├── Shimi.Samples.Data/   # Sample input data for agents, tasks, etc.
├── Shimi.Shared/         # Shared models and helpers
├── Rag/                  # Vector-based comparison implementation
└── Shimi.sln             # .NET Solution file
```

---

## 📌 Notes

- SHIMI supports recursive abstraction and multi-level query resolution.
- The semantic tree can be inspected live using CLI.
- Sample data is easily extensible with new entities.
