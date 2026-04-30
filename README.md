# MIS 321 PA3 - Personal Assistant

This project includes:
- Frontend: HTML, CSS, JavaScript
- Backend: C# (.NET 8 minimal API)
- Database: MySQL
- Features required by PA3:
  - LLM chat responses
  - RAG (retrieves context from `knowledge_base`)
  - Function Calling (`get_current_time`, `count_saved_messages`, `simple_math`)

## 1) Database Setup (MySQL)

Run:

```sql
SOURCE path/to/pa3_schema.sql;
```

Or open and run `pa3_schema.sql` in MySQL Workbench.

## 2) Backend Setup (C# API)

Go to:

`exam/Pa3Api`

Edit `appsettings.json`:
- Set your MySQL password in `ConnectionStrings:DefaultConnection`
- Set your LLM API key in `Llm:ApiKey`

Run:

```bash
dotnet restore
dotnet run
```

API runs on:

`http://localhost:5099`

## 3) Frontend Setup

Open `exam/index.html` using Live Server (or any static server).

The frontend calls:

`http://localhost:5099/api/chat`

## 4) Test Prompts for Assignment Features

- LLM: `Explain what an LLM is in simple terms.`
- RAG: `What is RAG?`
- Function calling (time): `What time is it right now?`
- Function calling (DB count): `How many saved messages do we have?`
- Function calling (math): `Add 12 and 30.`

## 5) Suggested Submission Notes

In your README / demo notes, mention:
- RAG source table: `knowledge_base`
- Function calls implemented in `FunctionService.cs`
- Main orchestration in `LlmService.cs` and `Program.cs`
