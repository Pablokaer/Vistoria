# 0005 — AI abstraction

**Context.** AI speeds up descriptions but must not become the source of truth, must not leak the API key, and the product must work without a key in development.

**Decision.**
- Domain/application depend only on `IImageAnalysisService` (room description, defect description, room comparison). `OpenAiImageAnalysisService` (Chat Completions + vision + **strict JSON-schema structured output**) is one implementation; `MockImageAnalysisService` is used when no key is configured in Development and labels every output "[Development mock AI]". Outside Development, missing configuration yields clear, recoverable failures instead of mock text.
- Prompts (`AiPrompts`, versioned) require: describe only what is visible, never assume hidden areas, never assign blame/responsibility or legal conclusions, cautious wording ("No visible damage is apparent in the provided images").
- Every request is an `AiAnalysis` row (provider, model, prompt version, photo ids, structured JSON, status). Processing runs in the background (in-process queue; pending jobs are re-queued at start-up) and the UI polls.
- The AI text is stored separately (`AiDescription`, `AIDescription`, `AIAnalysis`); the agent's text (`FinalDescription`, `AgentDecision`) is separate and AI never overwrites it (pre-fill happens only while empty, via a conditional update). AI never sets `AgentConfirmed` or a comparison decision.

**Consequences.** Provider can be swapped (Azure OpenAI, another vendor) by adding an implementation. The key lives only in server configuration and is sent only in the `Authorization` header; provider error bodies are never logged or returned. The in-process queue is not durable across multiple API instances (see README, technical debt).
