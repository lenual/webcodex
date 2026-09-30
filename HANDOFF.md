# HANDOFF｜Instruction System / Agent Harness 持续优化方案探索续接说明

> **当前 handoff，更新时间：2026-09-30。**
>
> 本文件只记录当前任务状态、研究判断和续接方式，**不是质量标准本身，也不是方案 authority**。
> 当前唯一现行质量规范是根目录：
>
> `INSTRUCTION-SYSTEM-QUALITY-STANDARD.md`
>
> 除用户当前明确要求、平台硬约束和安全边界外，不得让旧 `OBJECTIVE.md`、`FINAL-SOLUTION.md`、`SOLUTION-STATE.md`、`RUN-OPTIMIZER.md`、旧 optimizer contract、旧架构比较文档或本文件本身覆盖该标准。

---

## 0. 新会话先看这里

### 0.0 最新执行续接（2026-09-30）

- 用户最新明确要求：**不再使用 A11yChat，也不再使用 WebTunnel / WebCodex tools；可以使用子代理。** 这覆盖下面历史阶段的工具选择。
- 本地 matched pilot 位于 `runs/2026-09-30-rrsi-vs-modularrsi/`，以该目录 `PROTOCOL.md` 的执行更新和限制为准。
- 两个优化器的选定 Instruction System 已完成并冻结：RRSI `561430c961e79df423c6e2ae9d61c7e03c3d53d3`；ModularRSI `d39cc7c6c5046db2d108371fb9e7f2beb7f5f4e6`。
- 下游执行用两个无父会话历史的独立子代理，以及 `holdout/arm-A`、`holdout/arm-B` 两份代码副本；只改副本，不修改真实项目或冻结指令。
- 原始 H1 的 30 条用户输入已恢复，阶段 1 先只提供 T01；原来简写的 H1 包不能替代 T01 的 21 条完整约束。
- 评价标准已预先保存为 `evaluator-private/SCORECARD.md`。不按 reviewer 投票、不凭小幅文本评分决定 winner。
- 这是本地适配试验：优化阶段使用预期行为而非真实 rollout reward；旧 baseline 可能已有 H1 历史知识；代码快照不是已证明的 SVN r24256；缺少游戏/编辑器运行环境。不能宣称原论文完整复现或长期最终 winner。

### 0.1 当前任务进入本地证据复核

用户先要求：

> **“找到最好的就行”**

基于 21 份完整方案记录与统一 comparison matrix，RRSI 是 paper/research comparison 的：

> **provisional leader：RRSI — Regularized Recursive Self-Improvement of Agent Harnesses**

随后用户补充了新的明确要求：

1. 已开放 `C:\Users\admin\.codex\sessions`，必须利用其中与项目对应的真实会话证据；
2. 其它方案也要对比；
3. 需要隔离上下文时可以用 A11yChat：
   - 明确的一次性会话，任务完成后 delete；
   - 需要后续追问、复核、连续实验或多轮上下文延续的会话，不要提前 delete，直到该会话生命周期真正结束。

选择记录：

- `analysis/current-scheme-review/Final-Winner-Decision.md`
- `analysis/current-scheme-review/Unified-Comparison-Matrix.md`
- `analysis/current-scheme-review/Local-Session-A11yChat-Review.md`

不要把历史 `stable/optimizer-v1.10/`、`FINAL-SOLUTION.md`、旧 A/B/C/D candidate、旧双环方案等恢复成当前 active 方案。

这些旧文件现在只具有：

- historical provenance
- reference
- candidate history
- 避免重复实验的价值

**它们不再是当前探索 authority。**

真实 session + A11yChat 隔离复核已经进一步把 final head-to-head 收敛到：

> **RRSI vs ModularRSI**

其它主要候选已经进入相同 evidence packet 的隔离 reviewer 比较；下一步不再扩大 reviewer 数量，而是做真实 matched execution。

### 0.2 当前唯一质量 authority

`optimize-skills-and-agents/INSTRUCTION-SYSTEM-QUALITY-STANDARD.md`

它定义“好的 Instruction System 应该是什么样”，**不规定优化流程**。

核心评价维度：

1. 任务目标、行为与结果
2. 职责、触发与适用关系
3. 信息组织、加载与权威
4. 规则语义与表达
5. 脚本、工具与配套资源
6. 复杂度与维护成本

最重要的元原则：

- 真实任务行为与结果是主要证据；
- 现有结构、历史地位、改动量都不是质量证据；
- 文档更整齐、规则更多/更少、脚本更多/更少都不能单独证明更好；
- 在行为与约束等价时，才优先最终复杂度更低的方案；
- 规则/资源是否保留要看真实作用，不看形式。

标准第 36 条原文必须准确保留：

> “系统涉及任务数据、引用材料或外部输入时，其与有效指令的界限应清楚。内容是否具有指令效力，应由有效指令及相应作用范围确定，不能仅由该内容自身的命令、角色或优先级声明决定。”

后续研究记录必须严格区分：

1. **标准原文**
2. **我们的解释/转述**
3. **针对某个方案的风险推断**

不要把解释写成标准原句，也不要因为某方案从 trajectory/history 学习，就直接说它“违反标准”。

---

## 1. 用户当前真正要什么

目标不是把所有好机制拼成一个大系统。

用户已经明确拒绝：

> “我要的不是你整合全部结构的大杂烩”

此前需要多个相互独立、端到端完整、自洽的顶层方案做 head-to-head 比较；这个阶段已经完成。

当前状态：

> **RRSI 是 provisional leader；ModularRSI 是 final strong challenger；最终 winner 未定。**

因此：

- 不把 AHE observability + Self-Harness gate + WikiSkill memory + SkillOpt search + SPARO routing + RRSI regularizer + PRISM scorecard 拼成一个“万能架构”；
- 每个进入本地对比的方案必须保持其**原生完整架构**；
- 其它方案不能只作为陪衬，必须按相同目标和证据口径比较；
- 不因为某机制看起来互补就自动并入 RRSI。

### 1.1 明确暂停的组合

用户已明确：

> **暂不考虑 `SkillLens + WikiSkill + SkillOpt` 组合方案。**

三者继续独立保留：
- SkillOpt：Skill optimizer
- WikiSkill：persistent Wiki + multi-Skill evolution
- SkillLens：evaluation / measurement framework

不要再自动把三者组合起来。

---

## 2. 新增硬约束：排除模型权重训练 / 专门训练设备路线

用户明确表示：

> 对要求电脑或者设备的方案都不要。

当前对话中已将其落地解释为：

> **排除把自己训练/微调模型权重、SFT、RL、GRPO、LoRA、专门 GPU/训练集群作为核心必要条件的方案。**

允许：
- 现成模型/API
- 普通本地软件运行
- Python
- Docker / benchmark container
- 普通任务环境
- 多次 API rollout

只要这些不是为了训练模型权重。

如果用户以后进一步说连 Docker/本地环境也不要，再收紧；当前不要自行扩大限制。

### 2.1 已因此排除

- **Harness-R1**
  - 核心是 RL/post-train 一个专门的 harness engineer model。
  - 不再正式审。

- **JIT-Agent**
  - 核心是训练专用 harness intelligence model。
  - 排除。

- **HELIX 完整闭环**
  - verified sibling trajectories 进一步进入 SFT / critic / preference model update。
  - 当前完整方案不纳入。

- **HarnessX 的 model–harness co-evolution / GRPO 分支**
  - 权重训练部分排除。
  - 只保留不训练权重的 harness adaptation 证据。

---

## 3. 研究方法：每个方案怎么审

对每个独立 candidate 都按同一证据顺序：

### 3.1 原始 / 最新论文

必须看：
- 真正的循环是什么
- editable surface
- 数据 split
- candidate 数 / budget
- accept / reject / rollback
- evaluator
- search/test 是否泄漏
- held-out / OOD
- repeated run / seeds
- limitation
- 论文自己做了哪些消融

必须区分：
- 论文事实
- 作者解释
- 我们的推断

### 3.2 官方实现

核对：
- GitHub / official project / docs
- 代码里的实际 loop
- candidate 数
- proposer / judge
- retry
- rollback
- executable scope
- data split
- failure semantics
- current repo 与 paper 的差异

特别注意：

> **当前 main repo 的 post-paper 新功能不能倒灌成 paper 已经证明过的能力。**

复现论文时要 pin commit / release / tag，不要拿当前 main 跑完就说“复现论文”。

### 3.3 独立第三方证据

优先：
- independent reproduction
- issue
- benchmark replay
- code audit
- Pith 等方法学审阅
- 失败案例
- cross-model / cross-domain replication

不要用“热度”替代证据。

### 3.4 Zhihu / secondary synthesis

本地有：

`7 篇自动优化 agent harness论文对比.html`

只当：
- secondary synthesis
- 作者观点
- 线索来源

不能当 authority。

任何二手材料都要拆：
- fact restatement
- reasonable analysis
- subjective judgment
- unsupported inference

### 3.5 最后才按当前质量标准独立判断

始终回答三层：

> **它实际是什么 → 什么证据证明 → 是否适合我们的目标**

不要先决定“喜欢这个方案”，再找证据。

---

## 4. 当前项目与工具上下文

本地项目：

- 根目录：`D:\WebCodex`
- 主要研究目录：`D:\WebCodex\optimize-skills-and-agents`
- WebTunnel project id：
  `agent:desktop-s9hk717-44b94f1222f74de2:webcodex`
- Runner client：
  `desktop-s9hk717-44b94f1222f74de2`

当前工作方式：
- 使用 WebTunnel 读写项目文件；
- 用户之前不希望切换到 Work mode；
- 不要为了普通续接重复要求换执行环境；
- 用户不想被技术细节淹没，最终沟通应先给结论，细节落到 research record。

研究记录目录：

`optimize-skills-and-agents/analysis/current-scheme-review/`

这里的 Markdown：
> **都是 research evidence records，不是 current authority。**

---

## 5. 当前已完成的 21 份方案记录

当前目录里已经有：

1. `AHE.md`
2. `Adaptive-Auto-Harness.md`
3. `Continual-Harness-harness-only.md`
4. `EvolveNet.md`
5. `HarnessX.md`
6. `LIFE-HARNESS.md`
7. `MemoHarness.md`
8. `Meta-Harness.md`
9. `ModularRSI.md`
10. `PRISM-Beyond-Prompts.md`
11. `RHI.md`
12. `RHO.md`
13. `RRSI.md`
14. `SPARO.md`
15. `Self-Harness.md`
16. `SkillLens.md`
17. `SkillOpt.md`
18. `TTHE.md`
19. `WikiSkill.md`
20. `Living-Harness.md`
21. `Autogenesis.md`

**不要下一次又从头审这些文件。**

其中 `EvolveNet.md`、`Living-Harness.md`、`Autogenesis.md` 都已经完整审完。

最后一轮资格筛选另有：

- `analysis/current-scheme-review/Final-Qualification-Screen.md`

其中确认：

- Living-Harness：通过；
- Autogenesis：只保留 fixed-model / reflection-textual-resource branch，通过；
- Harness-of-Harness：优化的是 software deliverable，不进入顶层 harness-optimizer pool；
- RobustSGPO：属于 SGPO search-space-control 强化，不作为新的独立 top-level architecture。

---

## 6. 当前候选分类总览

> 这里不是排名，只是当前分类。

### 6.1 Strong / core finalist 级别

#### Meta-Harness
状态：
> **core finalist / no winner**

原生特征：
- fixed base model
- coding-agent proposer
- full historical filesystem
- candidate source + score + raw traces
- Pareto accuracy × context
- local edit / rewrite

优点：
- whole-harness search能力强

主要问题：
- TB2 search/final同任务
- evaluator/proxy不稳
- per-candidate non-regression gate不强
- long-term continual未证明

#### AHE
状态：
> **core finalist / no winner**

特征：
- full traces
- Debugger
- manifest
- predicted fixes
- at-risk regressions
- rollback
- 多层observability
- mutable surface很宽

问题：
- regression blindness论文里precision/recall很低
- infra重
- TB2仍存在同任务search/final
- 长期cleanup弱

#### Self-Harness
状态：
> **core finalist / no winner**

特征：
- same model solve + propose
- bounded K edits
- held-in + held-out
- hard acceptance gate

问题：
- held-out每轮参与promotion，不是真正最终 untouched test
- merge risk
- bounded surface可能漏掉架构级改动

#### RHO
状态：
> **core finalist / no winner**

特征：
- DPP coreset
- multiple rollouts
- self-validation
- multiple candidate harnesses
- pairwise self-preference
- independent final heldout

问题：
- promotion依赖模型自我偏好
- same backbone bias
- label-free不等于无需可靠final evaluator
- historical trajectory可能有authority/provenance风险

#### HarnessX（仅非权重训练部分）
状态：
> **core finalist / no winner**

保留：
- typed HarnessConfig
- processor hooks / slots
- AEGIS evolver
- deterministic seesaw gate
- variant isolation

排除：
- GRPO / model co-evolution训练分支

问题：
- headline evolution无真正final heldout
- global single-harness长期恶化
- reward hack可通过gate
- typed contract不等于semantic correctness
- variant proliferation

#### LIFE-HARNESS
状态：
> **core finalist / strong applicability limitation / no winner**

特征：
- Environment Contract
- Procedural Skill
- Action Realization
- Trajectory Regulation
- runtime interface adaptation
- deterministic/rule-governed environment中特别强

证据：
- 7 environments × 18 models = 126 settings
- 116/126提升
- 同环境未见tasks + 未见models transfer
- 四层ablation

问题：
- 不是 unseen environment transfer
- residual reasoning failure不在interface修复范围
- 原论文长期/多轮证据弱
- current repo post-paper新gate不能倒灌论文

#### Adaptive Auto-Harness
状态：
> **core finalist / no winner**
>
> 当前最直接研究 long-running open-ended task stream 的方案之一。

特征：
- open-ended chronological stream
- stateful multi-agent Evolver
- harness tree
- solve-time routing
- temporal-reveal feedback
- HITL可提供credential/source，不提供答案

重要结果：
- dense shared harness会先峰值后退化
- routing/tree有时有帮助，有时会伤
- oracle routing仍有明显headroom

问题：
- 把prompt bloat转成branch/routing complexity
- 没有充分merge/delete/stale retirement
- 没有完全冻结的post-stream future test
- routing不是trigger semantic proof

#### ModularRSI
状态：
> **strong core finalist / no winner**

核心：
- benchmark-disjoint evolution data
- same-task success/failure contrast
- five modular runtime surfaces
- separate evolution + cross-module integration
- freeze后测 unseen task/domain/model

最硬证据：
- modular > joint / non-modular
- five individual modules都高于baseline
- cross-domain point estimate双向正
- cross-model transfer正

问题：
- contrastive mechanism未单独消融
- difficulty mix selection时间线不够清
- no multi-seed/CI
- 只用240/2000 main evolution tasks
- static pool，不是长期stream
- semantic authority / Skill trigger弱

#### RRSI
状态：
> **strong core finalist / no winner**

核心：
- open-ish edit surface
- proposal-side + selection-side regularization
- annealed edit budget
- full edit ledger
- stall exploration
- leakage critic
- explicit evaluator noise band
- complexity-aware cost
- prune pressure

最关键证据：
- unregularized evolve score更高，但 OOD更差、token更膨胀
- RRSI OOD最好
- 多个 held-out/OOD 都正向

问题：
- 无多个完整 evolution seeds + CI
- 细粒度regularizer贡献未拆
- 固定evolve set
- hyperparameter多
- authority/version/trigger弱

#### EvolveNet
状态：
> **strong core finalist for distributed / heterogeneous experience aggregation / no winner**

核心：
- 多client各自在本地workload上演化
- raw tasks / raw trajectories不集中
- 上传program delta + behavioral evidence
- server做scope typing：GLOBAL / HOME(domain)
- aggregate executable program
- validation
- revise once / rollback
- redistribute

最强证据：
- 5 benchmarks held-out全部提升
- paired significance + Holm correction
- composition > select-best / routing / global-only
- DS-1000 local capability union中18/20（90%）被merged保留
- 未见Pytorch domain 8/15 → 10/15

主要风险：
- 只有 T=3
- bloat非常严重：
  - BIRD 14 → 253 lines
  - DS-1000 17 → 409 lines
- tie可以accept，长期会持续增厚
- 无size/latency gate
- GLOBAL threshold手工
- domain key必须可观察
- data locality不是privacy guarantee
- labeled server validation仍必需

#### Autogenesis（仅 fixed-model reflection branch）
状态：
> **strong core finalist candidate for versioned multi-resource self-evolution / no winner**

核心：
- RSPL：prompt / agent / tool / environment / memory 作为 versioned resources
- SEPL：Reflect → Select → Improve → Evaluate → Commit
- explicit lifecycle / lineage / rollback
- frozen/learnable boundary
- broad editable resource surface
- official runnable implementation

排除：
- GRPO / Reinforce++ 等模型权重训练分支

主要风险：
- evaluator/objective仍可能错
- versioning/rollback不等于semantic correctness
- Environment/Memory evolution实证较弱
- GAIA等结果不足以证明严格long-term untouched scorecard
- 缺multiple full-evolution seeds/CI
- infra/maintenance较重
- authority/provenance semantics仍未完整解决

#### Living-Harness
状态：
> **strong specialist/core finalist for persistent procedural repair in interactive task streams / no winner**

核心：
- fixed model
- frozen tools / base context
- evaluated trajectory → Evolution-SOP
- gated episodic-memory + state-graph update
- future-task retrieval
- cross-cycle persistent procedural state

关键证据：
- tau2-Bench average：83.09 vs strongest interactive baseline 73.02
- MultiWOZ-2.4 average：65.50 vs 55.59
- evolved state有cross-backbone retrieval-only transfer证据

主要风险：
- mutable surface只到memory/state graph
- 无完整rollback
- 无stale retirement
- 无systematic per-update regression suite
- simulator-heavy
- domain-level Evolution-SOP需人工定义
- paper-only implementation maturity

---

## 6.2 Skill-system / specialist finalist

### SkillOpt
状态：
> **core specialist finalist / strong single-Skill text optimization evidence / 不足以独自覆盖完整Instruction System**

核心：
- frozen target model
- one Markdown Skill
- failure/success reflection
- structured textual edits
- textual learning rate
- strict selection gate
- rejected edit buffer
- slow/meta update
- train / selection / untouched test

优点：
- 数据split纪律很强
- cross-model / cross-harness / cross-benchmark transfer有正证据
- 真实可部署artifact轻量

缺口：
- 不优化真实trigger/routing
- 不做Skill split/merge
- 不改scripts/tools/assets/references
- 无authority/version/project scope

### WikiSkill
状态：
> **core Skill-system finalist**

核心：
- Raw immutable trajectories
- persistent Wiki
- active multi-Skill set
- Wiki Maintainer
- Skill Proposer
- strict validation gate
- rollback Skill、不rollback Wiki
- rejected proposal进入impact history

关键证据：
- proposer有Wiki vs无Wiki约 +15pp
- multi-Skill create/patch
- macro average普遍高于SkillOpt

重要边界：
- 论文把所有active Skills全量塞prompt
- **没有真正测试Skill trigger/retrieval**
- 没有delete/merge/split/deprecate
- Wiki无自动prune
- paper实际只演化SKILL.md/PURPOSE.md，不是完整filesystem Skill

### SPARO
状态：
> **strong Skill-system finalist / trigger-routing evidence最强之一 / 非whole Instruction-System finalist**

核心：
- global prompt
- Skill library
- real Skill router
- positive/negative triggers
- representative queries
- counterfactual attribution：
  - 只修prompt
  - 只修Skill
  - 只改routing
- where-first, how-second

关键证据：
- CLUTRR：
  - random component 41.0
  - round-robin 38.0
  - prompt-only 28.0
  - attribution-guided 69.0
- 去routing显著下降
- 25/25 model-task setting point estimate最高

问题：
- paper-only，当前没官方repo
- 无OOD/cross-domain
- 12-step offline search
- validation被adaptive reuse
- 不改scripts/tools/resources
- authority/project scope弱

### PRISM
状态：
> **strong specialist optimizer for tool-agent harness**
>
> 不进入通用core finalist。

范围：
- system prompt
- constrained tool-boundary middleware

优点：
- failure-surface routing
- pattern-constrained middleware
- 真实tool behavior
- 多次独立run reliability

问题：
- scope窄
- 不覆盖whole Instruction System
- Telecom的RelLift95有non-native pass4 caveat

---

## 6.3 Evaluation / measurement 候选，不是 optimizer

### SkillLens
状态：
> **core evaluation / measurement candidate / 非continual optimizer**

最重要结论：

- Skill quality必须看真实consumer-specific downstream utility
- 同一个Skill对不同consumer可能正/负迁移
- 约25%的 extractor×target 组合出现negative transfer
- LLM只读Skill文本判断谁更好：
  - 46.4%
- utility gap更大时，unguided judge甚至更差
- 经utility校准的rubric可到73.8%，但仍不能替代真实execution

用途：
> 防止把“看起来更好的Skill”误判成真的更好。

### Beyond Prompts / PRISM evaluation protocol
状态：
> **strong core evaluation / selection protocol candidate**

核心：
- Repair / Gate / Scorecard三分离
- Scorecard完全不参与search/selection
- 评价实际 selected harness，不看 oracle-best candidate
- MeanLift
- WorstLift
- RR0
- lower-tail RelLift95
- search cost

以后做 head-to-head 时非常值得作为中立评价原则，但不要把它拼进某个optimizer当成“组合方案”。

---

## 6.4 重要 specialist / 证据暂不足进入 strong core

### MemoHarness
状态：
> **important adaptive-harness finalist candidate / evidence provisional**

核心：
- training阶段学global harness + experience bank
- deployment时：
  - 当前case
  - global harness
  - 相似成功/失败历史
  - global patterns
  → case-specific harness `W(x)`
- 当前case不读label、不再search

优点：
- per-case adaptation
- dual-layer experience
- cross-model point estimate六个额外model全正

问题：
- Terminal headline只有18 held-out tasks
- 无强CI/significance
- 核心component几乎没拆开消融
- cost高度依赖cache
- test-time bank冻结，不是真online continual
- authority/version/staleness弱

### Continual Harness（harness-only）
状态：
> **important reset-free continual specialist / 非通用core finalist**

只保留fixed-model harness-only分支。

核心：
- 同一长episode不reset
- 每F步Refiner直接修改：
  - prompt
  - subagents
  - executable Skills
  - memory

独特点：
> mid-run self-repair

问题：
- **没有candidate gate / accept-reject / rollback**
- 直接写入正在运行的harness
- Flash-Lite会越改越差
- inherited subagents会被后续新组件挤掉并回归
- create-and-forget tail长
- only Pokémon ecosystem
- reset-free vs reset-based没有公平head-to-head

### TTHE
状态：
> **important test-time / label-free executable-harness candidate / 暂不进 strong core**

核心：
- test-time unlabeled traces
- executable Python harness
- population branches
- execution health
- round-trip consistency
- public tests
- judge commit
- persistent batches

最重要证据问题：
> **headline是 transductive。**

同一批test inputs：
- 先用于无标签adapt/search/select
- 再在同一批上用gold计分

没有关键 prequential 结果：
> batch t只学习，batch t+1先评分再适应。

BIRD oracle audit：
- actual judge 50
- pool oracle 64
- all-generated oracle 70

说明：
- selection gap很大
- coverage gap也存在
- proxy promotion仍未解决

---

## 6.5 Specialized / control candidate

### RHI
状态：
> **specialized/control candidate，不是core general finalist**

特点：
- per-task task-specific prompt/workflow specialization
- immediate predecessor comparison
- self-comparison history

问题：
- prompt-level only
- no tools/scripts
- no shared persistent cross-task system
- independent reproduction有一组几乎退回baseline

用户特别强调：
> 关心的是 optimizer 自己反复运行跨不同任务，而不是同一task重复。

因此不要把RHI当长期共享系统主候选。

---

## 7. 已完成的重要最新文件修改

当前 exploration 新增/更新的核心研究记录包括：

- `analysis/current-scheme-review/SkillOpt.md`
- `analysis/current-scheme-review/WikiSkill.md`
- `analysis/current-scheme-review/SkillLens.md`
- `analysis/current-scheme-review/ModularRSI.md`
- `analysis/current-scheme-review/RRSI.md`
- `analysis/current-scheme-review/PRISM-Beyond-Prompts.md`
- `analysis/current-scheme-review/SPARO.md`
- `analysis/current-scheme-review/MemoHarness.md`
- `analysis/current-scheme-review/Continual-Harness-harness-only.md`
- `analysis/current-scheme-review/TTHE.md`
- `analysis/current-scheme-review/EvolveNet.md`
- `analysis/current-scheme-review/Final-Qualification-Screen.md`
- `analysis/current-scheme-review/Living-Harness.md`
- `analysis/current-scheme-review/Autogenesis.md`

以及此前已经完成：

- `Meta-Harness.md`
- `AHE.md`
- `Self-Harness.md`
- `RHO.md`
- `RHI.md`
- `HarnessX.md`
- `LIFE-HARNESS.md`
- `Adaptive-Auto-Harness.md`

本文件 `HANDOFF.md` 已被重新写成当前 handoff，**替代旧 handoff 的当前续接用途**。

---

## 8. 当前关键决策及原因

### 8.1 不选winner

当前明确：
> **还没有架构winner。**

不要在下个会话因为某一篇刚看起来很强，就宣布总冠军。

必须等：
- 候选池收敛
- 统一比较
- 必要的 matched experiments

之后再判断。

### 8.2 不做 integration soup

原因：
- 机制“理论互补”不是行为收益证据；
- 两个optimizer叠加可能只是重复；
- 更多机制本身会增加routing、authority、maintenance、failure surface。

### 8.3 评估真实 selected result，不只看oracle best

受 SkillLens / Beyond Prompts 等证据影响，后续统一比较应优先关注：

- 实际selected candidate
- independent final scorecard / untouched test
- 多次独立optimizer run
- mean
- worst
- lower-tail reliability
- regression rate
- search cost
- runtime cost
- 长期复杂度

不要只报：
> “历史里曾经出现过一个最高分candidate”。

### 8.4 LLM读文档打分不能当主gate

SkillLens已经给出很强负证据：
> surface plausibility judge很弱。

所以：
- LLM review可以做screening
- 可以做rubric proxy
- 不能替代真实task execution / verifier

### 8.5 Label-free不等于 evaluator-free

RHO / TTHE都说明：
- 没gold可以搜索
- 但最终仍需要可靠behavioral evaluation

不要把：
> “不需要label”
误写成：
> “不需要任何可信评估”。

### 8.6 trigger metadata不等于 routing已证明

例如 WikiSkill：
- 会写 When to Apply
- 但paper full-inject所有Skills
- 所以trigger能力没有实测

SPARO才真正把routing/trigger放进runtime与优化闭环。

### 8.7 当前 repo ≠ paper implementation

多篇都存在 post-paper repo增强，例如：
- LIFE-HARNESS current repo
- SkillOpt-Sleep / main
- MemoHarness telemetry fixes
- Continual Harness current main

不要把后续工程增强写回论文实验结论。

---

## 9. 历史项目证据仍可参考，但不能当当前authority

### 9.1 SkillOpt matched experiment（历史）

历史实验曾比较：
- Direct arm
- pinned SkillOpt arm

一个关键发现：

> 相同 baseline / byte-identical artifact，在独立 LLM judge 尝试中平均分可以差约0.19。

这说明：
> one-shot LLM judge variance非常大。

因此不能从那次实验简单说：
> Direct普遍优于SkillOpt。

这条历史证据仍值得用于“为什么必须noise calibration / repeated evaluation”。

不要把旧 `FINAL-RESULT.md` 等历史产物覆盖掉。

---

## 10. 当前仍未解决的问题

### 10.1 候选池是否已经足够

最近正在做最后一轮严格扩池。

目标：
> 只找真正独立、完整、fixed-model、software/API-only 的方案。

如果剩余方案只是：
- 现有方案的小补丁
- 权重训练
- 只有抽象proposal无实证
- 证据明显弱于现有finalists

就应停止扩池，进入统一比较。

### 10.2 长期 continual 证据仍分散在不同问题设定

目前不同方案分别回答：

- Adaptive Auto-Harness：
  - task之间chronological open-ended stream

- RRSI：
  - 多轮固定反馈下如何抑制adaptive overfit与complexity

- ModularRSI：
  - evolve后freeze，能否迁移到unseen task/domain/model

- EvolveNet：
  - 多个local experience source如何协同merge shared harness

- MemoHarness：
  - 新case开始前如何按历史做case-specific adaptation

- Continual Harness：
  - 同一个未结束episode里mid-run自修

- TTHE：
  - test-time无label时如何靠execution proxy改executable harness

不能因为都叫“continual/self-improving”就混为一个问题。

### 10.3 authority/version/project scope 几乎所有方案都弱

这是当前候选池的共性缺口。

很多方案很会：
- 改prompt
- 改tool logic
- 改Skill
- 改memory

但几乎没有原生完整处理：

- canonical authority
- project vs Skill scope
- task data vs valid instruction
- source provenance strength
- valid-from / expires / supersedes
- environment/version-conditioned applicability

后面统一比较时，这一项必须单独打出来，不能被benchmark分数掩盖。

### 10.4 长期复杂度仍是大问题

明显负例：

- Adaptive Auto-Harness：branch/routing complexity
- EvolveNet：3轮就14→253 lines、17→409 lines
- WikiSkill：Wiki不prune、Skill无delete
- Continual Harness：create-and-forget tail
- HarnessX：variant proliferation
- RRSI虽有cost/prune，但最终仍比H0更重

最终方案如果真的会不断运行，必须回答：
> 什么长期保留？为什么？什么时候退役？

---

## 11. 最后一轮资格筛选已完成

本轮已一次性筛完：

- **Living-Harness** → 通过并已正式审
- **Harness-of-Harness** → 不进入顶层 harness-optimizer pool
- **RobustSGPO** → 保留为 search-space-control 机制证据，不作为独立 top-level architecture
- **Autogenesis** → fixed-model / reflection branch 通过并已正式审；权重训练 optimizer 分支排除

记录：

- `analysis/current-scheme-review/Final-Qualification-Screen.md`
- `analysis/current-scheme-review/Living-Harness.md`
- `analysis/current-scheme-review/Autogenesis.md`

### 当前扩池决定

> **停止继续扩大 candidate pool。**

原因：

- 已经覆盖 whole-harness search、observability repair、bounded gated edit、self-preference、typed harness、interface adaptation、open-ended stream、modular evolution、regularized RSI、distributed merge、Skill evolution/routing、test-time adaptation、persistent procedural repair、versioned multi-resource evolution 等明显不同的 top-level families；
- 最后一轮剩余候选中，真正独立且证据足够的两项已纳入；
- 继续扩池更可能进入小 patch、弱实证、权重训练路线或问题设定错位。

下一阶段不再以“找更多论文”为默认动作。

---

## 12. 下一步计划

### Step 1：统一 comparison matrix 已完成

记录：

- `analysis/current-scheme-review/Unified-Comparison-Matrix.md`

当前横向结论：

- 没有单一现成方案同时覆盖宽 editable surface、untouched final evaluation、长期 continual、trigger/routing、authority/version 与低复杂度；
- headline benchmark 数字不能直接跨论文排序；
- editable surface 越宽不自动越好，必须同时看 evaluator coverage、regression、authority corruption 与 complexity；
- 最干净的 final-evaluation isolation 主要来自 SkillOpt / RHO / ModularRSI / LIFE-HARNESS 等；
- 最直接的 long-horizon 问题证据来自 Adaptive Auto-Harness / Living-Harness / Continual Harness / RRSI / TTHE 等，但它们研究的是不同时间尺度；
- trigger/routing 最直接的实证来自 SPARO 与 Adaptive Auto-Harness；
- version/lifecycle/rollback substrate 目前 Autogenesis 最直接，但仍没有证明完整 semantic authority；
- authority/provenance/version 仍是整个候选池的共同缺口；
- complexity / pruning / retirement 仍没有任何候选完整解决。

这个 matrix 是比较工具，不是机制拼接。

至少比较：

- editable surface
- state / persistent state
- proposal/search mechanism
- candidate isolation
- promotion gate
- rollback
- evaluator
- train/val/test/scorecard isolation
- repeated-run reliability
- OOD / cross-domain / cross-model
- long-horizon evidence
- trigger/routing
- authority/provenance/version
- scripts/tools/resources coverage
- complexity/pruning/retirement
- search cost
- runtime cost
- implementation maturity
- independent replication
- user硬件约束

注意：
> 这个matrix是比较工具，不是把方案机制拼起来。

### Step 2：paper comparison 的 provisional leader

> **RRSI**

记录：

- `analysis/current-scheme-review/Final-Winner-Decision.md`

核心理由：

- editable surface 足够宽；
- proposal-side 与 selection-side regularization 都有 behavioral ablation；
- held-out / OOD transfer 是主评价目标；
- matched comparison 中不追求最高 evolve score，而是获得更好的 OOD；
- evaluator noise 显式校准；
- complexity/token cost 进入 promotion；
- git worktree isolation + edit ledger + accept/reject/rollback 适合长期审计；
- fixed model，不需要权重训练或专门训练硬件；
- 官方实现与论文机制映射清楚。

### Step 3：当前下一步——RRSI vs ModularRSI matched local execution

不再比较更多论文。

真实 session / A11yChat review 已完成第一轮：

- 读取并定位 `C:\Users\admin\.codex\sessions` 中与本项目对应的历史 session；
- Host-native / Direct 恢复为 strong deployment baseline，不恢复为 winner；
- SkillOpt 保留为 single-Skill specialist/control；
- 三个 fresh A11yChat reviewer 分别给出：
  - RRSI > ModularRSI > Adaptive；
  - ModularRSI > RRSI；
  - RRSI > ModularRSI > Autogenesis；
- reviewer 分歧说明 text-level comparison 已到边界，不能再靠加 reviewer 数量解决。

现在只验证并对比：

1. RRSI 与 ModularRSI 使用相同真实 target / task stream；
2. 相同基础模型；
3. 相同或明确预算的 token / tool / wall-time；
4. final scorecard 对两边均 untouched；
5. 比较真实行为、regression、complexity、throughput、transfer；
6. selected candidate 而不是 oracle best 才算方案结果。

优先使用：
- 同一target
- 同一任务池
- 同一final scorecard
- 同一或明确记录的budget
- 多次独立run
- selected candidate而非oracle best
- behavior + complexity + regression
- untouched final evaluation

不要从一开始就造新组合架构。

---

## 13. 已踩过的坑：后续不要重复

### 13.1 不要把历史文档当当前authority

特别是：
- `OBJECTIVE.md`
- `FINAL-SOLUTION.md`
- `SOLUTION-STATE.md`
- `RUN-OPTIMIZER.md`
- 旧 `HANDOFF.md`
- 旧 optimizer contracts
- 旧 architecture comparison docs

除非为了：
- 理解历史candidate
- 查历史证据
- 避免重复实验

否则不要读它们来决定当前路线。

### 13.2 不要因为文件存在就恢复旧架构

“stable/optimizer-v1.10还在”不等于现在选了它。

### 13.3 不要组合所有好机制

这是用户已经明确拒绝的。

### 13.4 不要把 secondary synthesis 当论文事实

本地知乎/HTML文章只是线索。

### 13.5 不要把 paper abstract 当完整机制

必须看：
- algorithm
- split
- implementation
- limitations
- appendices
- ablations

### 13.6 不要把当前 repo 新功能写成论文已验证

这是目前最容易犯的研究错误之一。

### 13.7 不要把 “OOD” 说得过强

例如：
- LIFE主要是same-environment unseen tasks + unseen models
- RRSI是within broad domain across benchmark/environment
- SPARO基本没有OOD
- TTHE主结果是transductive
- MemoHarness transfer是选择性的

必须写清具体 population / environment / time / split。

### 13.8 不要把 small held-out point estimate 当显著结果

尤其：
- MemoHarness Terminal只有18 tasks
- TTHE hard slices小
- 很多cross-model +1 task级别差异

### 13.9 不要把“会写trigger”当“routing正确”

WikiSkill是典型例子。

### 13.10 不要把 typed/module structure 当质量自动成立

HarnessX、ModularRSI、MemoHarness都说明：
> 结构能改善credit assignment，但semantic correctness仍需要证据。

### 13.11 不要把“label-free”当“无需可信评估”

TTHE / RHO已经证明 selection proxy 会错。

### 13.12 不要把“更多search budget”当单调更好

TTHE明确出现更多round更差；
其他方案也可能noise chase。

### 13.13 不要把“严格每轮必须涨分”当万能解决方案

EvolveNet replay显示：
- strict tie reject会丢掉后续组合有价值的中间机制。

所以长期价值不能只看当前一轮即时分数。

### 13.14 不要把“能delete”当长期治理已解决

Continual Harness：
- 有CRUD
- 但仍会替换掉好的旧subagent

WikiSkill：
- 原生甚至没有delete/merge/split

RRSI：
- prune只是“标记删除候选 → 生成candidate → 再验证”，不是自动垃圾回收。

### 13.15 不要把数据不集中说成privacy guarantee

EvolveNet：
- raw tasks不上传
- 但program diff仍可能带本地敏感pattern

### 13.16 不要再把用户关心的问题解释成“同一task反复”

用户关心的是：
> optimizer会在长期中反复运行，面对变化的任务与环境。

RHI类task-local recurrence不是同一个目标。

---

## 14. 当前最重要的中立比较原则

后续横向比较建议至少保留这些，不要把它们当某个方案私有机制：

### 来自 SkillLens 的测量教训
- 真实downstream behavior > 文本plausibility
- consumer-specific utility
- negative transfer必须显式检查
- LLM judge只能作为proxy

### 来自 Beyond Prompts 的选择教训
- repair / gate / final scorecard分离
- 比实际selected candidate
- independent optimizer runs
- MeanLift / WorstLift / repeatability / lower-tail
- 不看oracle best
- final scorecard不参与selection

### 来自 RRSI 的统计教训
- evaluator noise必须校准
- 微小涨分可能只是噪声
- complexity / token growth不能长期白送

### 来自 ModularRSI 的泛化教训
- evolve data和最终评价尽量隔离
- freeze后再看未见task/domain/model

这些是**评价原则**，不是要求所有optimizer必须内部实现相同机制。

---

## 15. 当前状态一句话总结

> **论文扩池和 text-level reviewer 阶段都已停止。真实 Codex session + 三个一次性 A11yChat reviewer 已把最终 head-to-head 收敛到 RRSI vs ModularRSI：RRSI 仍是 provisional leader，但 reviewer 2 因更干净的 benchmark-disjoint evolve → freeze → unseen 证据选择 ModularRSI。下一步只做两者的 matched local execution；最终目标仍是找到能够长期、稳定、高吞吐、可迁移地持续提升 Skill / AI 指令系统真实行为质量的最佳方法。**
