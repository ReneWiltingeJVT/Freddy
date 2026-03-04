# Product Context

## Why This Project Exists

Care workers in Dutch healthcare institutions need quick access to care protocols and procedures. Freddy provides an AI-powered chat interface that understands natural language questions and routes them to the relevant care package.

## Problems It Solves

- Care workers waste time searching through paper/digital protocol binders
- New employees don't know which protocols exist
- Protocol information is scattered across different systems

## How It Works

1. Care worker opens Freddy and starts a conversation
2. They describe their question in natural language (Dutch)
3. Freddy's AI classifies the question and matches it to a care package
4. The relevant package content and documents are presented

## User Experience Goals

- Simple chat interface — no training needed
- Fast responses — AI classification within seconds
- Accurate routing — correct package matched to question
- Admin self-service — administrators manage packages without developer help

## Key Concepts

- **Package**: A care protocol/procedure (e.g., "Voedselbank", "Medicatie Protocol")
- **Document**: Supporting material attached to a package (PDF, step-by-step guide, or link)
- **Published**: Only published packages are visible in the chat — allows draft management
