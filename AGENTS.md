# Multi-Agent Orchestration & Token Optimization Guidelines
# Unity Farming Project

## 1. Orchestration Model, Argon Quota Optimization & Delegation Policy
- **Fast-Exit Plan Mode (`exit_plan_mode`)**: When running under `Orchestrator (Argon)`, Plan Mode consumes Gemini 4 Argon (`gemini-pro-orchestrator-g1-pro`) quota while Execution Mode switches to a faster, cheaper execution model. For routine tasks, direct queries, or straightforward implementations, minimize Plan Mode tool calls and call `exit_plan_mode` immediately so work executes on the cheaper model.
- **Minimize Plan Review Hand-Backs**: Keep execution turns concise and scoped to avoid unnecessary `[plan review due]` hand-backs to Gemini 4 Argon unless an architectural decision or re-plan is genuinely required.
- **Lead Orchestrator Direct Execution**: For simple tasks, direct queries, or minor single-file changes, work directly without spawning subagents.
- **Value Justification Gate**: Before spawning any subagent, verify that delegation provides sufficient value to justify the additional token and context overhead.
- **Selective Activation**: Never invoke all agents by default. Select only the specific agents strictly necessary for the assigned task.
- **Concurrency Boundary**: Maximum 2 concurrent subagents for ordinary tasks.

---

## 2. Model Selection Policy
1. **Lead Orchestrator Model**:
   - Prefer **Gemini 3.8 Flash** (`gemini-3.8-flash-high`) as the default Lead Orchestrator model where supported (via the Antigravity chat UI model selector, or `swarm(action: "model", model: "gemini-3.8-flash-high")` inside an active swarm session).
   - When `Orchestrator (Argon)` is active in the UI, reserve **Gemini 4 Argon** planning turns strictly for complex planning, architectural decisions, and difficult debugging, and fast-exit Plan Mode on all routine requests.
2. **Gemini Flash (`Model: 'flash'`) [Mandatory Default for All 4 Specialized Agents]**:
   - Mandatory default for **all four** specialized agents: `project_researcher`, `unity_programmer`, `ui_game_designer`, and `qa_tester`.
   - Used for research, project exploration, standard C# implementation, UI reviews, routine testing, and code audits.
3. **Gemini Hydrogen / Argon (`Model: 'pro'` / `'inherit'`) [Advanced Reasoning Escalation Only]**:
   - Reserved strictly for tasks requiring advanced reasoning: complex architectural decisions, difficult debugging scenarios, or intricate multi-file gameplay implementations.
4. **Claude Models [Strict Prohibition]**:
   - **STRICT PROHIBITION**: Never invoke Claude (`claude-opus-5-5-*`, `claude-sonnet-5-5-*`) unless the user explicitly requests or approves it.

---

## 3. Codebase Exploration & Caching Rules
- **Targeted Scope**: Limit codebase exploration strictly to relevant files and their direct dependencies.
- **No Global Scans**: Never scan the entire Unity project or traverse irrelevant directories (`Library/`, `Temp/`, `Logs/`, `Packages/`) unless explicitly requested.
- **Deduplication**: Avoid duplicate research and overlapping agent responsibilities. Reuse verified findings from previous work.
- **No Redundant Reads**: Do not repeatedly reread files that have not changed during the session.

---

## 4. Subagent Communication & Output Constraints
- **Concise Briefs**: Provide subagents with tightly bounded prompts specifying exact scope, files, and expected deliverables.
- **Length Constraint**: Subagent reports must be strictly **under 300 words**.
- **Structured Report Schema**: Subagents must return only:
  1. Key Findings
  2. Affected Files / Dependencies
  3. Identified Risks
  4. Recommended Actions
- **No Verbose Progress Reports**: Suppress intermediate progress chatter. Report only meaningful updates and final verified results.

---

## 5. Unity Project Protection & Stability
- Never delete or rename files without explicit authorization.
- Never modify unrelated scripts or rewrite existing systems without technical necessity.
- Preserve all existing ScriptableObject definitions, serialized fields (`[SerializeField]`), prefab bindings, and scene references.
- Always inspect existing code before authoring modifications to prevent duplicate managers or duplicate utility logic.
