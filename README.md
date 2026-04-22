# UAMIS221-321-mis-321-pa3-bkthomas7
Blake Thomas
MIS 321 - Spring 2026

Project Summary
This project is a full-stack personal assistant built with:

* Frontend: HTML, CSS, JavaScript
* Backend: C# (.NET Web API)
* Database: MySQL

The assistant includes the 3 required features:

LLM-style response flow for user questions
RAG using a MySQL knowledge_base table to retrieve relevant context
Function calling to run backend tasks (time lookup, message count, simple math)

Features Implemented
1) LLM / Chat Endpoint
User enters a question in the web UI
Frontend sends the message to POST /api/chat
Backend returns assistant response to the UI
2) RAG (Retrieval-Augmented Generation)
Backend pulls relevant rows from knowledge_base based on user prompt keywords
Retrieved context is used to generate a response
Example prompt: what is rag
3) Function Calling
Backend functions implemented:
get_current_time
count_saved_messages
simple_math (adds two values)

Example prompt:
5 + 9 → returns 14


Fallback Mode Note:
Because OpenAI quota was requiring a payment during testing, I implemented a local fallback mode so the app still demonstrates PA3 requirements:
* RAG still works from MySQL knowledge base
* Function calling still works (time/math/message count)
* Responses are labeled with [Local fallback mode]
This allowed the full PA3 workflow to run successfully without external API quota.

How to Run
Run MySQL script: pa3_schema.sql
Start backend: go to Pa3Api folder, run dotnet run
Open frontend: run index.html
Test prompts: what is rag, what time is it, what's 5 + 9
