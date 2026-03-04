# Project Brief

## Overview

Freddy is a healthcare chat application designed for care institutions (zorginstellingen) in the Netherlands. It helps care workers find the right care protocol (package) through natural language conversation.

## Core Requirements

- AI-powered chat interface that routes questions to relevant care packages
- Package management system for administrators (backoffice)
- Document management attached to packages (PDFs, step-by-step guides, links)
- Publish/unpublish lifecycle for packages — only published packages visible in chat
- Admin API key authentication for management endpoints
- Dutch language support as primary language

## Technology Stack

- **Backend**: ASP.NET Core 9, C# 13, Clean Architecture (Api → Application → Infrastructure)
- **Frontend**: React 19 + TypeScript + Vite 6 + Tailwind CSS
- **Database**: PostgreSQL 16 (port 5433) with EF Core 9 + Npgsql
- **AI**: Semantic Kernel 1.72.0 + Ollama (mistral:7b, port 11434)
- **Patterns**: CQRS with MediatR 14, FluentValidation 12, Result<T> pattern
- **Infrastructure**: Docker containers (freddy-db, freddy-seq, freddy-ollama)

## Source of Truth

- Documentation in `docs/` folder (00-10 numbered files + subfolders)
- API project instructions in `instructions.md`
