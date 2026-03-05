# Trend Analysis & Internet-Wide Discourse Tracking

*Research document — MarketMaker project*
*Last updated: 2026-03-05*

---

## 1. The Core Thesis

The hypothesis underlying this entire system: **discourse precedes price**. Before a significant price move, there is almost always a detectable shift in the pattern of human attention, language, and sentiment across the internet. The challenge is not whether this signal exists — it does, and there is substantial academic evidence for it — the challenge is:

1. Collecting the signal at sufficient breadth and speed
2. Separating genuine signal from noise
3. Running it through a model that understands financial context
4. Delivering it fast enough to matter

This document covers all four layers.

---

## 2. Where the Signal Lives — Data Source Landscape

### 2.1 Tier 1: High-Signal, Low-Noise (Financial Context Native)

These sources are already oriented toward markets. High SNR, easier to extract value from.

| Source | Type | Access | Latency | Notes |
|---|---|---|---|---|
| **StockTwits** | Social / micro-blog | Free + paid API | Seconds | Bullish/bearish tagged by users. Small but highly finance-specific |
| **Unusual Whales** | Options flow + Twitter aggregate | Paid API | Minutes | Aggregates financial Twitter/X, flags unusual activity |
| **Seeking Alpha** | News + analysis | Paid API | Minutes | Author sentiment, earnings previews |
| **Bloomberg Terminal** | News + sentiment | Enterprise | Seconds | Proprietary but gold standard for institutional |
| **Reuters / Dow Jones Newswires** | Wire service | Paid API | Seconds | Machine-readable news feeds specifically for algo trading |
| **Benzinga** | News | Paid API ($50–$200/mo) | Seconds | Good mid-tier financial news firehose |
| **Polymarket / Kalshi** | Prediction market odds | REST + WS | Seconds | Odds shifts ARE the crowd's sentiment signal |

### 2.2 Tier 2: High-Volume, Moderate-Signal (General Social)

These require more processing but contain early-warning signals not present in financial media.

| Source | Type | Access | Latency | Notes |
|---|---|---|---|---|
| **Reddit** | Forums | PRAW / official API | Minutes | r/wallstreetbets, r/stocks, r/CryptoCurrency, r/investing. Volume and velocity matter more than any single post |
| **X (Twitter/X)** | Microblog | API v2 Basic ($100/mo) or Pro ($5k/mo) | Seconds–Minutes | Cashtag tracking ($TSLA, $BTC), influencer signal, topic velocity |
| **YouTube** | Video | Data API v3 (free quota) | Hours | Video publish volume per ticker, comment sentiment. Slower but predicts longer-term narrative |
| **Telegram (public channels)** | Messaging | MTProto API (Telethon) | Seconds | Crypto communities, political prediction channels. Not officially scraped but technically accessible |
| **Discord (public servers)** | Chat | Discord API (rate limited) | Seconds | Crypto/trading servers. Harder to access at scale |
| **HackerNews** | Tech-focused social | Algolia API (free, real-time) | Minutes | Useful for tech sector signals (AI stocks, semiconductors, SaaS) |

### 2.3 Tier 3: Internet-Wide Firehose Sources

These are the deepest layer — global internet discourse captured at scale. This is where the project can differentiate from retail tools.

#### GDELT (Global Database of Events, Language, and Tone)
This is the single most underused data source in retail trading. It is **free, real-time, and covers the entire global internet news ecosystem**.

- Monitors news media in 65+ languages across 215+ countries
- Within **15 minutes** of a news article publishing, GDELT has translated it, identified all events, people, organizations, locations, themes, emotions, and Goldstein tone scores
- Updated every 15 minutes as a live open firehose
- **Goldstein scale**: each event gets a −10 to +10 conflict/cooperation score
- **AvgTone**: average emotional tone of all articles covering an event
- Available via BigQuery (Google) for SQL querying at scale — no infrastructure needed

**Academic evidence that GDELT works as a trading signal:**
- GDELT + FinBERT → Sharpe ratios of 5.87 (EUR/USD) and 4.65 (USD/JPY) in out-of-sample backtests (2025 paper, "Interpretable Machine Learning for Macro Alpha")
- GDELT sentiment predicts Chinese stock market returns and volatility (ScienceDirect, 2022)
- GDELT-based ESG sentiment strategies outperformed STOXX 600 index 2015–2022
- Italian sovereign bond yield spreads forecast improved by Gradient Boosting on GDELT features

Key GDELT features to extract for trading:
```
- AvgTone           → average emotional tone across all articles on a topic
- GoldsteinScale    → conflict/cooperation rating of event type
- NumArticles       → media attention volume
- NumSources        → source diversity (breadth vs. echo chamber)
- NumMentions       → mention frequency
- Actor1/Actor2     → who is involved (company, country, person)
- EventCode         → CAMEO event taxonomy (e.g., "Express intent to cooperate")
```

#### Common Crawl
- 2.16 billion pages crawled as of December 2025
- Not real-time (batch crawls), but useful for building training datasets
- GDELT vs. Common Crawl comparison: GDELT wins for news timeliness, CC wins for breadth

#### Pushshift (Reddit Historical)
- Post-2023 note: Pushshift lost firehose API access when Reddit closed free access
- Historical data (pre-2023) is available for research
- For current Reddit data: use the official Reddit API (PRAW) — 100 req/min on free tier, higher on paid

### 2.4 Tier 4: Alternative & Emerging Sources

| Source | Signal Type | Access | Notes |
|---|---|---|---|
| **Google Trends** | Search intent volume | Unofficial API (pytrends) | Weekly granularity free; hourly via enterprise |
| **Wikipedia edit velocity** | Narrative attention proxy | MediaWiki API (free) | Sudden spike in edits on a company/person page = news event |
| **GitHub Stars / Issues** | Tech sector proxy | GitHub API | Useful for crypto projects, AI companies |
| **Wayback Machine / Archive.org** | Historical context | CDX API (free) | Cross-reference historical narrative with price |
| **SEC EDGAR filings** | Regulatory/fundamental | EDGAR full-text search (free) | NLP on 10-K/10-Q tone changes |
| **PACER (US courts)** | Legal risk events | $0.10/page | Lawsuit filings against companies = risk signal |
| **FEC filings** | Political money flow | FEC API (free) | Lobbying and PAC activity → regulatory risk/opportunity |

---

## 3. Pipeline Architecture — Capturing Internet-Wide Discourse in Real-Time

### 3.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    SOURCE INGESTION LAYER                       │
│                                                                 │
│  GDELT (15min)  Reddit (stream)  X/Twitter  StockTwits          │
│  News RSS       YouTube          Telegram   HackerNews          │
│  Google Trends  SEC EDGAR        Benzinga   Polymarket          │
└────────────────────────┬────────────────────────────────────────┘
                         │ raw events
┌────────────────────────▼────────────────────────────────────────┐
│               KAFKA MESSAGE BUS (topic per source)              │
│  Topics: gdelt.raw | reddit.raw | twitter.raw | news.raw        │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────────┐
│              FLINK STREAM PROCESSING ENGINE                     │
│                                                                 │
│  1. Entity extraction   → ticker/company/person tagging         │
│  2. Deduplication       → content hash, source normalization    │
│  3. Reach scoring       → upvotes, retweets, engagement weight  │
│  4. Language detection  → route non-English to translation      │
│  5. Windowed aggregates → mention velocity, sentiment deltas    │
└────────────────────────┬────────────────────────────────────────┘
                         │ enriched events
┌────────────────────────▼────────────────────────────────────────┐
│              SENTIMENT SCORING SERVICE (C# + Model)             │
│                                                                 │
│  Fine-tuned small LLM (see Section 5)                           │
│  Input:  cleaned text + entity tags                             │
│  Output: {sentiment: float, confidence: float, tone: enum}      │
│  Throughput target: <200ms per item                             │
└────────────────────────┬────────────────────────────────────────┘
                         │ scored events
┌────────────────────────▼────────────────────────────────────────┐
│                   SIGNAL AGGREGATION LAYER                      │
│                                                                 │
│  Per-entity rolling windows: 5m, 15m, 1h, 4h, 24h              │
│  Composite score = Σ(sentiment × reach_weight × source_weight)  │
│  Velocity = dScore/dt (rate of change)                          │
│  Divergence = score vs 30-day baseline                          │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────────┐
│                   TIMESCALEDB STORAGE                           │
│  hypertable: sentiment_scores (entity, timestamp, score, ...)   │
│  hypertable: mention_counts  (entity, source, window, count)    │
│  hypertable: discourse_events (raw + enriched)                  │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Kafka Topic Design

```
gdelt.raw.events          → raw GDELT event records (15-min batches → simulate stream)
reddit.raw.posts          → new posts from tracked subreddits
reddit.raw.comments       → comment stream (higher volume, lower weight)
twitter.raw.cashtags      → cashtag-matched tweets
twitter.raw.accounts      → tweets from tracked high-influence accounts
news.raw.articles         → parsed news articles from RSS + wire feeds
stocktwits.raw.stream     → StockTwits message stream
processed.scored          → all sources post-sentiment-scoring
signals.velocity          → windowed velocity alerts
signals.divergence        → divergence from baseline alerts
```

### 3.3 Entity Extraction — Mapping Discourse to Tickers

The hardest part of the pipeline. "Tesla" must map to $TSLA. "the Fed" must trigger macro signals. Approach:

- Maintain a **financial entity dictionary**: tickers, company names, aliases, common nicknames ("spy" → SPY, "mag7", "jerome" → Jerome Powell → macro event)
- Use a fast NER (Named Entity Recognition) model — `bert-base-NER` or spaCy with a custom financial vocabulary
- Fuzzy match + context rules for ambiguous cases
- Output: `{entity_id, entity_type: [ticker|person|macro|sector|event], confidence}`

---

## 4. Discourse Pattern Types & Trading Signals

### 4.1 Proven Signal Patterns (Research-Backed)

**Mention Velocity Spike**
- Sudden acceleration in mention rate (>3σ above baseline) preceding price moves
- Reddit specifically: post velocity on r/wallstreetbets has been shown to precede short squeezes
- Implementation: rolling z-score on 15-minute mention counts vs. 30-day mean

**Sentiment Divergence**
- Price moves up while sentiment is declining → bearish divergence (distribution)
- Price moves down while sentiment is improving → bullish divergence (accumulation)
- Prediction market odds divergence from price → one of them is wrong

**Cross-Platform Consensus**
- When Reddit + X + news + prediction markets all align on a direction simultaneously, the signal is stronger than any single platform
- Research shows cross-platform alignment precedes larger moves
- Implementation: correlation score across N sources within a time window

**Narrative Shift Detection**
- A topic that was being discussed in one context suddenly appears in a new context
- Example: "NVDA" appears in AI research discourse → migrates to retail investor forums → price move follows
- Implementation: track which subreddits/communities a ticker is appearing in, flag community migrations

**Influencer Signal**
- Weighted mention score from accounts with historically high price correlation
- Pre-compute per-account Granger causality: does @account's mention of a ticker Granger-cause its price?
- Weight current mentions by that historical correlation coefficient

**GDELT Tone Trajectory**
- Multi-day decline in AvgTone for a company → institutional narrative degradation
- NumSources diversity drop (fewer sources covering something) → story fading
- Goldstein scale shift toward conflict → geopolitical/regulatory risk emerging

### 4.2 Behavioral Pattern Library

| Pattern | Detection Method | Historical Hit Rate (literature) |
|---|---|---|
| Crowd Surge | Mention velocity >3σ + positive sentiment spike | ~62% precision for >2% move in 24h |
| Pump Signature | Volume spike + rapid sentiment reversal | ~71% for crypto, lower for equities |
| Narrative Fade | NumSources decline + engagement drop | Useful for short signals |
| Earnings Whisper Drift | Sentiment trend into earnings vs. consensus | Well-documented pre-earnings drift |
| VIX Crush Setup | Macro discourse tension → resolution language | Pairs well with VIX options |
| Prediction Market Lead | Odds move before price move | Polymarket has 4–6 hour lead on some events |
| Regulatory Fear Cascade | PACER/EDGAR filings → news → social spread | Strong for biotech, crypto |

---

## 5. LLM Research for Financial Sentiment — State of the Art

### 5.1 What Exists (Key Models & Papers)

**FinBERT** (2019, still widely used)
- BERT fine-tuned on 1.8M financial news articles (Thomson Reuters TRC2, 2008–2010)
- Further fine-tuned on Financial Phrasebank dataset
- Outputs: positive / negative / neutral classification
- Limitation: outdated training data, binary classification too coarse, struggles with irony and context

**FinLlama** (2024, ACM Conference on AI in Finance)
- Fine-tuned Llama 2 7B on four labeled financial sentiment datasets
- Key result: **outperformed FinBERT cumulative returns by 44.7%**
- Higher Sharpe ratio and lower annualized volatility than FinBERT
- More robust during turbulent market conditions (FinBERT degrades, FinLlama holds)
- Classifies both sentiment valence AND strength (more nuanced than binary)

**FinGPT** (2023–2024, ongoing)
- Open-source financial LLM family from Columbia/Yale researchers
- Fine-tuned GPT variants on financial news, SEC filings, earnings calls
- Used as the sentiment backbone in several RL trading papers

**Finance-LLaMA-2** (ScienceDirect, 2024)
- Finance-specific LLaMA 2 variants
- Trading strategies using LLaMA-2 sentiments produce significantly higher buy-and-hold returns than FinBERT
- LLaMA-2 sentiment signals show strong correlation with cumulative abnormal returns (CARs)

**QF-LLM** (2025, ACM AI in Finance)
- Quantized LLMs for financial sentiment — 4-bit and 8-bit quantization
- Reduces VRAM by ~75%, speeds up inference significantly
- Near-parity accuracy with full-precision models on financial tasks

**Alpha-R1** (2025, arxiv)
- 8B-parameter reasoning model trained via RL for alpha screening
- Reasons over factor logic AND real-time news simultaneously
- Context-aware alpha evaluation under changing market conditions
- This is the closest to what MarketMaker's LLM layer should eventually become

### 5.2 LLM + Reinforcement Learning Pipeline (Cutting Edge)

The most advanced pattern emerging in 2025 research:

```
News/Social → LLM Sentiment Score → RL Agent → Portfolio Action
```

Key papers:
- **"Integrating LLMs and RL for Sentiment-Driven Quantitative Trading"** (arxiv 2510.10526): Long-short strategies on LLM sentiment + technical indicators exhibit returns not explained by conventional factors
- **"HARLF"** (arxiv 2507.18560): Hierarchical RL + lightweight LLM → 26% annualized return, Sharpe 1.2 (2018–2024 backtest)
- **"Adaptive Alpha Weighting with PPO"** (arxiv 2509.01393): Uses Proximal Policy Optimization to dynamically weight multiple LLM-generated alpha signals under varying market conditions
- **"Language Model Guided RL in Quantitative Trading"** (arxiv 2508.02366): LLM provides context about market regimes to guide RL agent's exploration

### 5.3 Can a Small LLM Do This Job? Yes.

Research strongly suggests a purpose-built small LLM is both viable and preferable to a large general-purpose model for this task:

**Evidence:**
- FinLlama at 7B parameters outperforms FinBERT and matches or beats general GPT models on financial classification
- Phi-3-mini (3.8B) achieves quality on par with Mixtral 8x7B on several reasoning tasks
- QF-LLM (2025): 4-bit quantized models achieve near full-precision performance with 75% VRAM reduction
- Fine-tuning with LoRA (Low-Rank Adaptation) allows cheap, fast specialization — train on a single A100 in hours

**Why small wins over large here:**
- Inference latency: need <200ms per document for real-time pipeline. GPT-4 via API: ~1–3 seconds. Local 7B model: ~50–150ms
- Cost: running GPT-4 on a firehose of millions of daily social posts = prohibitive
- Specialization: a 7B model fine-tuned specifically on financial text with market context will outperform a 70B general model on financial sentiment classification
- Control: local model means you can run it 24/7, no API rate limits, no data privacy concerns

---

## 6. Recommended Small LLM Architecture for MarketMaker

### 6.1 Model Selection

**Primary recommendation: Qwen2.5-7B or Qwen3-8B fine-tuned on financial corpus**

Why Qwen:
- 32k token context window — can process long articles, earnings call transcripts
- Strong multilingual capability — important for GDELT's global coverage
- Competitive with Llama 3 on most benchmarks at same parameter count
- Active open-source development, good quantization support

**Alternative: Phi-3-mini (3.8B) for ultra-low latency path**
- If inference speed is the priority (real-time ticker sentiment scoring)
- Smaller model = faster, but less nuanced reasoning
- Acceptable for binary/ternary sentiment classification
- Run via ONNX Runtime or llama.cpp for max speed on CPU/GPU

**For the reasoning/alpha layer: use a larger model on a slower cadence**
- Qwen2.5-72B or Claude API for daily macro synthesis, not per-tweet scoring
- Reserve heavy models for high-value, low-frequency tasks

### 6.2 Fine-Tuning Dataset Construction

Build a training corpus from:

```
Source                          Volume          Labels
──────────────────────────────────────────────────────
Financial Phrasebank            5k sentences    pos/neg/neutral (gold standard)
FiQA (Financial QA)             10k items       sentiment + opinion targets
StockTwits historical           500k+ messages  bullish/bearish (user-labeled)
FinSent (Reddit WSB)            200k posts      price-correlated labels
Twitter/X cashtag history       1M+ tweets      label via next-day price move
GDELT AvgTone corpus            Large           tone score as continuous label
News articles + price delta     Custom          label = abnormal return in T+1, T+7
Earnings call transcripts       10k calls       tone shift + stock reaction
```

Labeling strategy for unlabeled data: use **price correlation labeling**
- Article at time T → if stock moves >1.5% in next 4 hours = strong positive/negative signal
- This creates a weakly supervised dataset grounded in actual market outcomes

### 6.3 Training Pipeline

```
1. Base model:     Qwen2.5-7B (or Llama 3.1 8B)
2. Method:         LoRA fine-tuning (rank=16, alpha=32)
3. Quantization:   4-bit QLoRA via BitsAndBytes
4. Framework:      HuggingFace Transformers + PEFT
5. Hardware:       Single A100 80GB (or 2x RTX 4090)
6. Training time:  ~6–12 hours for initial fine-tune
7. Output:         {sentiment: [-1.0, 1.0], confidence: [0,1], regime: enum}
```

### 6.4 Inference Architecture in C#

```csharp
// Option A: ONNX Runtime (fastest, C# native)
// Export fine-tuned model to ONNX, run via Microsoft.ML.OnnxRuntime

// Option B: Python microservice via gRPC
// C# API calls a Python FastAPI/vLLM service
// Decouples model serving from business logic

// Option C: Hugging Face Text Generation Inference (TGI) Docker container
// Call via HTTP from C#, TGI handles batching and throughput optimization

// Recommended: Option B or C
// Reason: Python has better LLM tooling; C# handles everything else
```

Throughput target:
- With batched inference on a single A100: ~500–2000 texts/second for a 7B model
- With 4-bit quantization on RTX 4090: ~200–800 texts/second
- Twitter/X at $100/mo tier gives ~500k tweets/month = ~6 tweets/second average (very manageable)

---

## 7. Signal Quality & Validation Framework

### 7.1 Backtesting Sentiment Signals

Before going live, every signal type needs backtesting:

```
1. Collect historical sentiment scores (or reconstruct from archives)
2. Align to price data at matching timestamps
3. Test strategy: long when sentiment > threshold, short below
4. Measure: Sharpe, Sortino, max drawdown, win rate
5. Test for data leakage (ensure no future price info in features)
6. Out-of-sample test on held-out time period
```

Academic baseline from "Backtesting Sentiment Signals for Trading" (ACL Anthology, 2025):
- All models on Dow Jones 30 produced positive returns
- Best model: 50.63% return over 28 months, outperforming Buy & Hold

### 7.2 Signal Decay Analysis

Sentiment signals have varying decay rates:
- Twitter/X mention velocity: decays within 1–4 hours
- Reddit discourse: decays within 4–24 hours
- News sentiment: decays within 1–7 days
- GDELT macro tone: decays within 1–4 weeks

Match signal horizon to trade horizon. Using a 4-hour signal to make a 3-month position bet is a category error.

### 7.3 Granger Causality Testing

For each source and each asset, test whether the sentiment time series Granger-causes the price time series:
- If yes: the source has predictive power for that asset
- Use this to dynamically weight sources per asset (don't apply Reddit weights to Treasury bonds)
- Retest quarterly — relationships change as markets evolve

---

## 8. Known Risks & Limitations

| Risk | Description | Mitigation |
|---|---|---|
| **Crowded trade** | If everyone is using the same Reddit signal, it's already priced in | Use diverse, non-standard sources (GDELT, Wikipedia, HackerNews) |
| **Adversarial manipulation** | Coordinated pumping campaigns on social media | Detect anomalous source concentration; flag when signal comes from <5 accounts |
| **API access degradation** | Twitter/Reddit have been closing free access since 2023 | Multi-source redundancy; GDELT as free fallback |
| **Model staleness** | FinBERT trained on 2008–2010 data; new slang not understood | Retrain periodically; include current slang in fine-tune data |
| **Latency vs. signal decay** | By the time you score and act, the signal may be consumed | Prioritize source speed over depth for fastest-decaying signals |
| **Overfitting to historical regimes** | Strategies backtested on 2018–2024 may fail in new regimes | Include stress tests: COVID, 2022 rate hike cycle, Luna collapse |
| **Regulatory scrutiny** | Using non-public information (e.g., leaked docs) is illegal | Only public data sources; document provenance for all inputs |

---

## 9. Implementation Roadmap for MarketMaker

### Phase 1 (MVP Sentiment)
- [ ] GDELT integration — 15-minute polling, parse AvgTone + NumMentions per entity
- [ ] Reddit PRAW streaming — r/wallstreetbets, r/stocks, r/CryptoCurrency
- [ ] Entity tagging pipeline — map discourse to tickers
- [ ] FinBERT inference via Python microservice (fast to stand up, known baseline)
- [ ] Store scored sentiment in TimescaleDB
- [ ] Display as Sentiment Gauge widget in dashboard

### Phase 2 (Signal Depth)
- [ ] Add X/Twitter cashtag stream
- [ ] StockTwits integration
- [ ] Mention velocity + rolling z-score calculation in Flink or C# Channels
- [ ] Cross-platform consensus signal
- [ ] Basic Granger causality test per asset/source pair

### Phase 3 (LLM Upgrade)
- [ ] Build financial sentiment fine-tune dataset (Financial Phrasebank + StockTwits + price-correlated news)
- [ ] Fine-tune Qwen2.5-7B with QLoRA
- [ ] Deploy as Python gRPC microservice
- [ ] A/B test FinBERT vs. fine-tuned model on live signal quality
- [ ] Replace FinBERT in pipeline if quality improves

### Phase 4 (Alpha Layer)
- [ ] Implement RL-based signal weighting (PPO over multiple sentiment alphas)
- [ ] HARLF-style hierarchical integration with trading simulator
- [ ] Alpha-R1 style reasoning: LLM reasons over factor logic + news context together
- [ ] Monte Carlo + sentiment scenario injection in stress tester

---

## 10. Key Research References

- [FinLlama: LLM-Based Financial Sentiment Analysis for Algorithmic Trading](https://dl.acm.org/doi/10.1145/3677052.3698696) — ACM, 2024
- [FinLlama arxiv preprint](https://arxiv.org/html/2403.12285v1)
- [Interpretable ML for Macro Alpha: GDELT + FinBERT](https://arxiv.org/html/2505.16136v1) — Sharpe 5.87 on FX
- [Integrating LLMs and RL for Sentiment-Driven Trading](https://arxiv.org/html/2510.10526v1)
- [HARLF: Hierarchical RL + Lightweight LLM Portfolio Optimization](https://arxiv.org/html/2507.18560v1)
- [Alpha-R1: RL-Trained 8B Reasoning Model for Alpha Screening](https://arxiv.org/abs/2512.23515v1)
- [Adaptive Alpha Weighting with PPO](https://arxiv.org/html/2509.01393v1)
- [Language Model Guided RL in Quantitative Trading](https://arxiv.org/html/2508.02366v1)
- [LLMs in Equity Markets: Applications & Insights (84-paper review)](https://pmc.ncbi.nlm.nih.gov/articles/PMC12421730/)
- [Backtesting Sentiment Signals for Trading](https://aclanthology.org/2025.jeptalnrecital-industrielle.2/)
- [Finance-Specific LLaMA 2: Advancing Sentiment & Return Prediction](https://www.sciencedirect.com/science/article/abs/pii/S0927538X24003846)
- [QF-LLM: Quantized LLMs for Financial Sentiment](https://dl.acm.org/doi/10.1145/3764727.3764731)
- [Fine-tuning Lightweight LLMs on Heterogeneous Financial Text](https://arxiv.org/html/2512.00946)
- [GDELT Project](https://www.gdeltproject.org/)
- [GDELT 2.0 Real-Time Description](https://blog.gdeltproject.org/gdelt-2-0-our-global-world-in-realtime/)
- [GDELT Bond Market Sentiment Paper](https://blog.gdeltproject.org/big-data-financial-sentiment-analysis-in-the-european-bond-markets/)
- [Generating Alpha: Hybrid AI Trading System (ComSIA 2026)](https://arxiv.org/html/2601.19504v1)

---

---

## 11. Simulated Trading, Modeling & Backtesting — Deep Reference

This section covers the full engineering and mathematical stack required to build a simulation engine that produces trustworthy, non-deceptive results. The single most dangerous output of a poorly built simulator is a backtest that looks great and fails live. Everything here is oriented toward avoiding that outcome.

---

### 11.1 Two Modes of Simulation

All simulation falls into two fundamental categories that must never be conflated:

| Mode | Definition | Primary Risk | Use Case |
|---|---|---|---|
| **Paper Trading** | Real-time strategy execution against live market data with no real capital | Fill quality assumptions, latency illusion | Strategy validation before going live |
| **Historical Backtest** | Strategy replay against recorded historical data | Look-ahead bias, overfitting, data quality | Strategy development and hypothesis testing |

Both require the same core engine. The difference is whether the data feed is live or recorded.

---

### 11.2 Engine Architecture — Event-Driven Simulation

**Why event-driven, not vectorized:**

Vectorized backtesting (apply a function across a DataFrame) is fast to write but fundamentally dishonest. It allows your logic to implicitly see future bars, cannot model partial fills, ignores intra-bar price paths, and makes realistic order types impossible. Event-driven simulation is slower to build but produces results that translate to live trading.

```
┌─────────────────────────────────────────────────────────────────┐
│                      DATA FEED LAYER                            │
│                                                                 │
│  Live: WebSocket streams (Binance, Alpaca, Polymarket)          │
│  Backtest: TimescaleDB replay — ordered by timestamp, strict    │
│  Output: chronologically ordered MarketEvent stream             │
└────────────────────────┬────────────────────────────────────────┘
                         │ MarketEvent (tick | bar | orderbook)
┌────────────────────────▼────────────────────────────────────────┐
│                      EVENT BUS (C# Channel<T>)                  │
│                                                                 │
│  MarketEvent → Strategy → SignalEvent                           │
│  SignalEvent → RiskManager → OrderEvent                         │
│  OrderEvent → ExecutionEngine → FillEvent                       │
│  FillEvent → Portfolio → PortfolioUpdateEvent                   │
└─────────────────────────────────────────────────────────────────┘
```

**Core event types:**

```csharp
// Base event
public abstract record SimEvent(DateTimeOffset Timestamp, string Symbol);

// Data events
public record TickEvent(DateTimeOffset Timestamp, string Symbol,
    decimal Bid, decimal Ask, decimal BidSize, decimal AskSize)
    : SimEvent(Timestamp, Symbol);

public record BarEvent(DateTimeOffset Timestamp, string Symbol,
    decimal Open, decimal High, decimal Low, decimal Close,
    long Volume, BarInterval Interval)
    : SimEvent(Timestamp, Symbol);

public record OrderBookEvent(DateTimeOffset Timestamp, string Symbol,
    IReadOnlyList<(decimal Price, decimal Size)> Bids,
    IReadOnlyList<(decimal Price, decimal Size)> Asks)
    : SimEvent(Timestamp, Symbol);

// Strategy events
public record SignalEvent(DateTimeOffset Timestamp, string Symbol,
    SignalDirection Direction,   // Long | Short | Exit
    decimal Strength,           // 0.0 to 1.0 — conviction score
    string StrategyId)
    : SimEvent(Timestamp, Symbol);

// Order events
public record OrderEvent(DateTimeOffset Timestamp, string Symbol,
    OrderType Type,             // Market | Limit | StopLimit | TrailingStop
    OrderSide Side,             // Buy | Sell
    decimal Quantity,
    decimal? LimitPrice,
    decimal? StopPrice,
    TimeInForce Tif,            // GTC | DAY | IOC | FOK
    string OrderId)
    : SimEvent(Timestamp, Symbol);

// Fill events
public record FillEvent(DateTimeOffset Timestamp, string Symbol,
    OrderSide Side,
    decimal Quantity,
    decimal FillPrice,          // after slippage model
    decimal Commission,
    decimal Slippage,
    string OrderId)
    : SimEvent(Timestamp, Symbol);
```

**The simulation clock:**

In backtest mode, time is driven exclusively by the data. No wall-clock time. The engine processes events in strict timestamp order. Any strategy that reads the current time must use the simulation clock, never `DateTime.UtcNow`.

```csharp
public interface ISimulationClock
{
    DateTimeOffset Now { get; }
    void Advance(DateTimeOffset to);
}

// Backtest: clock advances with each event
// Live: clock reads DateTimeOffset.UtcNow
```

---

### 11.3 Execution Engine — Realistic Fill Modeling

This is where most simulators lie. Assuming every order fills instantly at the last price is not a simulation — it is a fantasy.

#### 11.3.1 Fill Latency

In live trading, there is always latency between signal generation and fill confirmation. Model it:

```csharp
public record LatencyModel(
    TimeSpan SignalToOrderLatency,   // strategy → broker: typically 1–50ms
    TimeSpan OrderToFillLatency,     // broker → exchange: typically 1–10ms
    bool RandomizeWithJitter         // add ±20% random variation
);
```

In backtesting, fills should not be processed at the same bar as the signal. Minimum delay: signal at bar N, earliest fill at bar N+1. This is the most commonly violated rule.

#### 11.3.2 Bid-Ask Spread

Never fill at the midpoint. Fill direction:
- **Buy market order**: fills at the Ask
- **Sell market order**: fills at the Bid
- **Difference = spread cost**, which can be 0.01% (liquid large cap) to 2%+ (illiquid small cap or exotic prediction market)

```csharp
decimal GetFillPrice(OrderSide side, decimal bid, decimal ask)
    => side == OrderSide.Buy ? ask : bid;
```

For prediction markets specifically, the spread is often 2–5 cents on a $0.50 market and must be modeled per market.

#### 11.3.3 Slippage Models

Four tiers based on order size relative to Average Daily Volume (ADV):

**Tier 1: Negligible (< 0.1% ADV)**
```
Slippage ≈ 0
Fill price = ask/bid + tiny random noise (±0.5 bps)
```

**Tier 2: Linear (0.1% – 1% ADV)**
```
Slippage = spread × (order_size / adv) × liquidity_factor
```

**Tier 3: Square-Root Impact (1% – 5% ADV)**
```
Market Impact = σ × (order_size / ADV)^0.5 × participation_rate^0.5
σ = daily volatility of the asset
```
This is the Kyle (1985) / Grinold-Kahn square-root model. Widely used by institutional desks.

**Tier 4: Almgren-Chriss Optimal Execution (> 5% ADV)**

For large positions that must be liquidated over time. The Almgren-Chriss model minimizes the tradeoff between market impact cost and timing risk:

```
Objective: minimize E[Cost] + λ × Var[Cost]

Where:
  E[Cost] = temporary_impact + permanent_impact
  Var[Cost] = execution risk (price moves against you while executing)
  λ = risk aversion parameter (set by user or calibrated to Sharpe target)

Temporary impact: h(v) = η × σ × (v/V)^0.6
  v = trading rate (shares/time)
  V = average daily volume
  η = market impact coefficient (calibrated per asset)

Permanent impact: g(v) = γ × σ × (v/V)
  γ = permanent impact coefficient

Optimal execution trajectory:
  x(t) = X × sinh[κ(T-t)] / sinh[κT]
  κ = sqrt(λγ/Σ)  (decay rate of urgency)
  X = initial position size
  T = total execution horizon
```

Output: a time-weighted execution schedule. The simulator respects this schedule and fills each tranche with appropriate impact.

```csharp
public class AlmgrenChrissModel
{
    public ExecutionSchedule Compute(
        decimal positionSize,
        decimal adv,
        decimal dailyVolatility,
        TimeSpan executionHorizon,
        decimal riskAversion)
    {
        // Returns list of (time, quantity, expectedSlippage) tuples
        // spanning the execution horizon
    }
}
```

#### 11.3.4 Partial Fills

Limit orders may not fill completely, especially in illiquid markets. The engine must model queue position and available liquidity:

```csharp
// Limit order fill logic
decimal availableLiquidity = GetLiquidityAtPrice(limitPrice, orderBook);
decimal fillQuantity = Math.Min(order.Quantity, availableLiquidity * fillFraction);
bool isPartialFill = fillQuantity < order.Quantity;
```

For prediction markets (CLOB-based like Kalshi), fill probability is a function of your position in the order queue. The simulator must approximate this.

#### 11.3.5 Order Type Implementations

```csharp
// Market order: fills next bar at bid/ask + slippage
// Limit order: fills when price crosses limit (buy: ask ≤ limit, sell: bid ≥ limit)
// Stop-loss: triggers when last price crosses stop, then becomes market order
// Trailing stop: stop price follows best price by fixed amount/%, triggers on reversal
// Bracket: entry + take-profit limit + stop-loss stop, as atomic group

// Time-in-force enforcement:
// GTC (Good Till Cancelled): persists until filled or manually cancelled
// DAY: cancelled at market close if unfilled
// IOC (Immediate or Cancel): fills available quantity immediately, cancels rest
// FOK (Fill or Kill): fills entirely or not at all
```

---

### 11.4 Fee & Cost Modeling — Full Breakdown

Every cost must be modeled. Ignoring any single cost layer will produce returns that cannot be replicated in live trading.

#### 11.4.1 Cost Layer Stack

```
Gross P&L
  − Commission (broker fee)
  − Exchange fee / taker fee (for crypto)
  − SEC fee (for equity sells)
  − FINRA TAF (for equity trades)
  − Bid-ask spread cost
  − Slippage / market impact
  − Borrow cost (for short positions)
  − Overnight funding rate (for crypto perps: funding rate)
  − Currency conversion cost (for multi-currency portfolios)
  − Withdrawal / transfer fee (for crypto)
= Net P&L
```

#### 11.4.2 Fee Schedules by Venue

**Equities:**

```csharp
public record EquityFeeModel(
    decimal CommissionPerShare,     // IBKR: $0.005/share (min $1)
    decimal SecFeeRate,             // $0.000008 × proceeds (sell only)
    decimal FinraTafRate,           // $0.000145 × shares (sell only, max $7.27)
    decimal MinCommission           // IBKR: $1.00
);

// Zero-commission brokers: Robinhood, Webull, Schwab
// Still pay SEC fee + FINRA TAF on sells
// PFOF (Payment for Order Flow) = indirect cost via worse fills
```

**Crypto (maker/taker model):**

```csharp
public record CryptoFeeModel(
    decimal MakerFee,       // Coinbase Advanced: 0.00%–0.40%
    decimal TakerFee,       // Coinbase Advanced: 0.05%–0.60%
    decimal WithdrawalFee,  // network gas fee — variable
    bool HasBnbDiscount     // Binance: 25% discount if BNB held
);

// Fee tier lookup: most exchanges discount fees by trailing 30-day volume
// At <$10k/month: pay max taker fee
// At >$1M/month: near-maker rates even for takers
```

**Prediction Markets:**

```csharp
public record PredictionMarketFeeModel(
    string Platform,
    decimal FeeOnWinnings,          // Kalshi: 5–7%
    decimal FeeOnOpenInterest,      // Polymarket: ~2% annualized
    decimal? CloseOutFee,           // some platforms charge to close early
    FeeStructure Structure          // OnWin | OnOpenInterest | OnTrade
);
```

**Crypto Perpetuals (Funding Rates):**

```csharp
// Funding rate paid every 8 hours on open positions
// Positive rate: longs pay shorts (market is bullish/overextended)
// Negative rate: shorts pay longs (market is bearish/overextended)
// Typical range: -0.1% to +0.1% per 8h interval
// Accumulates significantly on long-held positions

decimal FundingCost(decimal notional, decimal fundingRate, TimeSpan holdTime)
    => notional * fundingRate * (holdTime.TotalHours / 8.0m);
```

#### 11.4.3 Borrow Cost for Short Positions

```csharp
// Hard-to-borrow stocks: 10%–100%+ annualized borrow rate
// Easy-to-borrow (liquid large cap): 0.25%–1% annualized
// Borrow rate is dynamic — can spike overnight if shorts pile in (GME, AMC events)
// Model: deduct borrow cost daily on open short positions

decimal BorrowCost(decimal positionValue, decimal annualBorrowRate, TimeSpan holdTime)
    => positionValue * annualBorrowRate * (holdTime.TotalDays / 365.0m);
```

---

### 11.5 Portfolio & Position Management

#### 11.5.1 Position Sizing Models

```csharp
// 1. Fixed Dollar Amount
decimal quantity = targetDollarAmount / currentPrice;

// 2. Fixed Fractional (Kelly-adjacent)
// Bet a fixed % of portfolio per trade
decimal quantity = (portfolio.TotalValue * fixedFraction) / currentPrice;

// 3. Kelly Criterion (theoretically optimal, practically dangerous)
// f* = (edge) / (odds)
// f* = (p × b - q) / b
// p = win probability, q = 1-p, b = win/loss ratio
// In practice: use half-Kelly (f*/2) to reduce variance
decimal kellyFraction = (winRate * avgWin - lossRate * avgLoss) / avgWin;
decimal halfKelly = kellyFraction / 2.0m;

// 4. Volatility-Scaled Position Sizing
// Target constant dollar volatility per position regardless of asset volatility
decimal targetDailyVolatilityDollars = portfolio.TotalValue * 0.01m; // 1% of portfolio
decimal dailyVolatility = GetHistoricalVolatility(symbol, lookback: 20);
decimal quantity = targetDailyVolatilityDollars / (currentPrice * dailyVolatility);

// 5. Maximum Drawdown Budget
// Size positions so that if the max historical drawdown repeats,
// total portfolio loss stays below user-defined threshold
```

#### 11.5.2 Risk Checks (Pre-Order Validation)

Every order must pass these checks before reaching the execution engine:

```csharp
public class RiskManager
{
    bool CheckOrder(OrderEvent order, Portfolio portfolio)
    {
        // 1. Position concentration limit
        // Single position ≤ 20% of portfolio (configurable)
        if (GetPositionWeight(order.Symbol) + order.NotionalValue / portfolio.TotalValue > maxPositionWeight)
            return Reject("Concentration limit exceeded");

        // 2. Sector/asset class concentration
        // Crypto ≤ 40%, equities ≤ 80%, etc.
        if (GetSectorWeight(order.Symbol) > maxSectorWeight)
            return Reject("Sector concentration limit exceeded");

        // 3. Daily loss limit (circuit breaker)
        if (portfolio.TodayPnL < -maxDailyLoss * portfolio.StartOfDayValue)
            return Reject("Daily loss limit reached — trading halted");

        // 4. Drawdown circuit breaker
        if (portfolio.CurrentDrawdown > maxDrawdownThreshold)
            return Reject("Max drawdown threshold — trading halted");

        // 5. Buying power / margin check
        if (order.NotionalValue > portfolio.BuyingPower)
            return Reject("Insufficient buying power");

        // 6. Liquidity check
        // Don't enter a position larger than N% of ADV
        if (order.Quantity > GetADV(order.Symbol) * maxAdvParticipation)
            return Reject("Order exceeds ADV participation limit");

        return Approve();
    }
}
```

---

### 11.6 Backtesting Engine — Correctness & Anti-Bias

#### 11.6.1 The Seven Ways Backtests Lie

**1. Look-Ahead Bias**
Using information at bar N that was not available until bar N+1 or later.
- OHLC bars: the High and Low of a bar are only known at bar close, not at bar open
- Earnings data: reported on date X, but 10-Q is filed 2–4 days later. Must use filing date, not earnings date
- Index composition: S&P 500 membership changes. A backtest in 2026 using "current S&P 500 members" tests on companies that may not have been in the index in 2018
- Fix: timestamp everything with the date it became known (as-of date), never the date it refers to

**2. Survivorship Bias**
Testing only on assets that still exist today excludes companies that went bankrupt, were delisted, or were acquired — which are often the worst performers.
- A 2010–2020 backtest on "current Nasdaq 100 members" excludes the dozens of companies that fell out of the index
- Fix: use a point-in-time universe with full delisted history (Sharadar, CRSP, Norgate data)

**3. Overfitting / Data Snooping**
Testing many parameter combinations and reporting the best one.
- If you test 1,000 strategies and 50 look good by chance (p<0.05), you have found nothing
- The more parameters a strategy has, the more degrees of freedom to fit noise
- Fix: Deflated Sharpe Ratio (DSR), Probability of Backtest Overfitting (PBO), strict out-of-sample testing

**4. Transaction Cost Underestimation**
Using zero or minimal costs. Frequently turns a losing strategy into a winning one on paper.
- A strategy with 10% gross alpha and 0.1% round-trip costs at 1 trade/day loses 25% annually to costs alone
- Fix: model every cost layer as described in Section 11.4

**5. Ignoring Market Impact**
Assuming your orders don't move the price. Valid for retail-sized positions in liquid assets. Invalid for any meaningful size.
- A backtest that buys 5% of daily volume at the prior close price is not credible
- Fix: apply the slippage tier model from Section 11.3.3

**6. Instant Fill Assumption**
Assuming limit orders fill the moment price touches the limit.
- In reality, you are in a queue. A limit buy at $100 may not fill if price only briefly touches $100 with other orders ahead of you
- Fix: conservative fill rule — assume your limit order fills only if price trades through your limit by at least one tick

**7. Regime Ignorance**
A strategy optimized on 2010–2020 bull market conditions will look great but fail in a rate-hiking cycle, a liquidity crisis, or a correlation breakdown.
- Fix: segment backtests by market regime. Test separately across: bull trend, bear trend, high-vol/crisis, low-vol/range

#### 11.6.2 Walk-Forward Validation

The standard defense against overfitting:

```
Timeline split for walk-forward:
─────────────────────────────────────────────────────────
 Train₁  │  Test₁  │
          Train₂    │  Test₂  │
                     Train₃    │  Test₃  │
                                Train₄    │  Test₄  │
─────────────────────────────────────────────────────────

Each training window: optimize parameters
Each test window: evaluate performance on unseen data
Final report: aggregate Test₁ + Test₂ + Test₃ + Test₄
```

- Training window: 2–4 years
- Test window: 3–6 months
- Re-optimize at each roll
- If the strategy degrades consistently on test windows, it is overfit

#### 11.6.3 Combinatorial Purged Cross-Validation (CPCV)

Newer than walk-forward, shown to be superior in 2024–2025 research. Uses combinatorial splitting with embargo periods between train/test to eliminate leakage from time-series autocorrelation:

```
1. Split time series into N groups (e.g., 6 groups of 2 years each)
2. Choose k groups as test (e.g., k=2)
3. Purge: remove samples within an embargo period of test boundaries
   from training set (eliminates feature leakage from overlapping returns)
4. Train on remaining N-k groups
5. Evaluate on k test groups
6. Repeat for all C(N,k) combinations
7. Report Probability of Backtest Overfitting (PBO) across combinations

PBO < 0.1 → low overfitting risk
PBO > 0.5 → high probability your backtest is finding noise
```

#### 11.6.4 The Deflated Sharpe Ratio (DSR)

When you have run N strategy variations and pick the best, the observed Sharpe is inflated by selection. The DSR corrects for this:

```
DSR = SR_observed × correction_factor

correction_factor = Φ[(1 - γ_E)√(T-1) × SR_observed] / SR_benchmark

Where:
  Φ = normal CDF
  γ_E = Euler-Mascheroni constant ≈ 0.5772
  T = number of observations
  SR_benchmark = expected max Sharpe from N independent trials

Rule: if DSR < 1.0, reject the strategy — selection bias explains your results
```

---

### 11.7 Historical Backtest Engine Implementation (C#)

#### 11.7.1 Core Loop

```csharp
public class BacktestEngine
{
    private readonly IDataFeed _feed;
    private readonly IStrategy _strategy;
    private readonly ExecutionEngine _execution;
    private readonly Portfolio _portfolio;
    private readonly RiskManager _risk;
    private readonly ISimulationClock _clock;

    public async Task<BacktestResult> RunAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct)
    {
        await foreach (var evt in _feed.StreamAsync(start, end, ct))
        {
            // 1. Advance simulation clock
            _clock.Advance(evt.Timestamp);

            // 2. Update portfolio with latest market prices (mark-to-market)
            _portfolio.UpdateMarketValue(evt);

            // 3. Process pending orders against new market data
            var fills = _execution.ProcessPendingOrders(evt);
            foreach (var fill in fills)
                _portfolio.ApplyFill(fill);

            // 4. Feed market event to strategy
            var signals = _strategy.OnMarketEvent(evt);

            // 5. Convert signals to orders, validate through risk manager
            foreach (var signal in signals)
            {
                var order = _strategy.SizeOrder(signal, _portfolio);
                if (order is not null && _risk.CheckOrder(order, _portfolio))
                    _execution.SubmitOrder(order);
            }

            // 6. Record state snapshot for analytics
            _portfolio.RecordSnapshot(_clock.Now);
        }

        return BuildResult();
    }
}
```

#### 11.7.2 Data Feed — Avoiding Look-Ahead

```csharp
public class TimescaleDataFeed : IDataFeed
{
    // CRITICAL: query is strictly ordered by timestamp
    // No future data can be in the stream at any point
    public async IAsyncEnumerable<SimEvent> StreamAsync(
        DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        // Bars are emitted only after they are closed
        // A 1-hour bar for 09:00–10:00 is emitted at 10:00:00, not 09:00:00
        // High and Low of bar are only available at bar close

        await foreach (var row in _db.StreamBarsAsync(start, end, ct))
        {
            yield return new BarEvent(
                Timestamp: row.CloseTime,  // NOT row.OpenTime
                Symbol: row.Symbol,
                Open: row.Open,
                High: row.High,
                Low: row.Low,
                Close: row.Close,
                Volume: row.Volume,
                Interval: row.Interval
            );
        }
    }
}
```

#### 11.7.3 LEAN Engine Integration

QuantConnect's LEAN engine (open source, written in C#) provides a production-grade event-driven backtesting framework. Rather than building from scratch, integrate it:

```csharp
// LEAN provides out of the box:
// - Multi-asset backtesting (equities, options, futures, crypto, forex)
// - Reality models: slippage, fill, fee, margin
// - Benchmark comparison
// - Full statistics report

// Integration path:
// 1. Install Lean.Engine NuGet package
// 2. Write algorithm as class inheriting from QCAlgorithm
// 3. Use LEAN's data infrastructure, feed in your custom data
// 4. Override reality models with MarketMaker's fee/slippage models
// 5. Export results to MarketMaker's TimescaleDB for display

public class MarketMakerAlgorithm : QCAlgorithm
{
    public override void Initialize()
    {
        SetStartDate(2022, 1, 1);
        SetEndDate(2026, 1, 1);
        SetCash(100_000);
        AddEquity("AAPL", Resolution.Minute);
        SetSlippageModel(new MarketMakerSlippageModel());
        SetFeeModel(new MarketMakerFeeModel());
    }

    public override void OnData(Slice data)
    {
        // Strategy logic here
        // Signal generation, order submission
    }
}
```

---

### 11.8 Monte Carlo Simulation

Monte Carlo answers the question: *given uncertainty about future returns, what is the distribution of possible outcomes?*

#### 11.8.1 Return-Sampling Monte Carlo

The simplest and most interpretable approach:

```csharp
public class MonteCarlo
{
    public MonteCarloResult Run(
        IReadOnlyList<decimal> historicalReturns,   // daily return series from backtest
        int numSimulations,                          // e.g., 10,000
        int horizonDays,                             // e.g., 252 (1 year)
        decimal initialPortfolioValue,
        int seed = 42)
    {
        var rng = new Random(seed);
        var endValues = new decimal[numSimulations];

        for (int sim = 0; sim < numSimulations; sim++)
        {
            decimal value = initialPortfolioValue;
            for (int day = 0; day < horizonDays; day++)
            {
                // Resample from historical return distribution (bootstrap)
                int idx = rng.Next(historicalReturns.Count);
                value *= (1 + historicalReturns[idx]);
            }
            endValues[sim] = value;
        }

        Array.Sort(endValues);

        return new MonteCarloResult(
            Median:          endValues[numSimulations / 2],
            Percentile5:     endValues[(int)(numSimulations * 0.05)],
            Percentile25:    endValues[(int)(numSimulations * 0.25)],
            Percentile75:    endValues[(int)(numSimulations * 0.75)],
            Percentile95:    endValues[(int)(numSimulations * 0.95)],
            ProbabilityOfRuin: endValues.Count(v => v <= 0) / (decimal)numSimulations,
            ExpectedValue:   endValues.Average()
        );
    }
}
```

#### 11.8.2 Parametric Monte Carlo (GBM)

When historical return series is too short, use Geometric Brownian Motion parameterized from volatility estimates:

```csharp
// dS = μ S dt + σ S dW
// Discretized: S(t+dt) = S(t) × exp((μ - σ²/2)dt + σ√dt × Z)
// Z ~ N(0,1)

decimal SimulateGBM(
    decimal S0, decimal mu, decimal sigma,
    int steps, double dt, Random rng)
{
    decimal S = S0;
    for (int i = 0; i < steps; i++)
    {
        double Z = SampleStandardNormal(rng);
        S *= (decimal)Math.Exp(
            ((double)(mu - sigma * sigma / 2m)) * dt
            + (double)sigma * Math.Sqrt(dt) * Z);
    }
    return S;
}
```

**Limitation of GBM**: assumes log-normal returns, no fat tails, no volatility clustering. For realistic tail risk, use:
- **Student-t distributed innovations** (heavier tails)
- **GARCH(1,1) volatility** (volatility clustering)
- **Regime-switching model** (alternates between calm and crisis regimes)

#### 11.8.3 Stress Test Scenarios

Inject known historical crisis parameters rather than sampling randomly:

```csharp
public static class StressScenarios
{
    // Apply a scenario as a shock to the portfolio
    public static IReadOnlyList<decimal> Apply(
        StressScenario scenario,
        Portfolio portfolio)
    {
        return scenario switch
        {
            StressScenario.GFC2008 => new[] {
                // S&P 500 peak-to-trough: -56% over 17 months
                // VIX peak: 89.53
                // Credit spreads: +600bps
                // Daily return sequence: replay Sep 2008 – Mar 2009
            },
            StressScenario.Covid2020 => new[] {
                // -34% in 33 days (fastest bear market in history)
                // Crypto: BTC -60% in 2 days (March 12–13, 2020)
            },
            StressScenario.LunaTerra2022 => new[] {
                // UST depeg → LUNA hyperinflation → -99.9% in 72 hours
                // BTC -30% in 1 week as contagion spread
            },
            StressScenario.RateHike2022 => new[] {
                // US Treasuries: -20% (worst year in 240 years of bond history)
                // Tech stocks (ARKK): -75%
                // BTC: -75% over 12 months
            },
            StressScenario.FlashCrash2010 => new[] {
                // Dow: -1,000 points in minutes, recovered same day
                // Tests intraday circuit breakers and stop-loss triggers
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
```

---

### 11.9 Portfolio Analytics — Full Metrics Suite

All metrics must be computed from the equity curve (time series of portfolio value):

```csharp
public class PortfolioAnalytics
{
    public AnalyticsReport Compute(
        IReadOnlyList<(DateTimeOffset Date, decimal Value)> equityCurve,
        IReadOnlyList<(DateTimeOffset Date, decimal Value)> benchmarkCurve,
        decimal riskFreeRate = 0.05m)  // current 3-month T-bill rate
    {
        var returns = ComputeDailyReturns(equityCurve);
        var benchmarkReturns = ComputeDailyReturns(benchmarkCurve);

        return new AnalyticsReport(
            // Return metrics
            TotalReturn:        TotalReturn(equityCurve),
            CAGR:               CAGR(equityCurve),
            TWR:                TimeWeightedReturn(equityCurve),
            MWR:                MoneyWeightedReturn(equityCurve),   // IRR

            // Risk metrics
            AnnualizedVolatility: StdDev(returns) * Math.Sqrt(252),
            MaxDrawdown:          MaxDrawdown(equityCurve),
            MaxDrawdownDuration:  MaxDrawdownDuration(equityCurve),
            VaR95:               Percentile(returns, 0.05m),        // 1-day 95% VaR
            CVaR95:              CVaR(returns, 0.05m),               // Expected Shortfall

            // Risk-adjusted return metrics
            SharpeRatio:  (CAGR(equityCurve) - riskFreeRate) / (StdDev(returns) * Math.Sqrt(252)),
            SortinoRatio: (CAGR(equityCurve) - riskFreeRate) / (DownsideDeviation(returns) * Math.Sqrt(252)),
            CalmarRatio:  CAGR(equityCurve) / MaxDrawdown(equityCurve),
            OmegaRatio:   OmegaRatio(returns, riskFreeRate / 252m),

            // Relative performance
            Alpha:        Alpha(returns, benchmarkReturns, riskFreeRate),
            Beta:         Beta(returns, benchmarkReturns),
            InformationRatio: (CAGR(equityCurve) - CAGR(benchmarkCurve)) / TrackingError(returns, benchmarkReturns),

            // Trade-level metrics
            WinRate:      WinRate(trades),
            AverageWin:   AverageWin(trades),
            AverageLoss:  AverageLoss(trades),
            ProfitFactor: Math.Abs(TotalGains(trades) / TotalLosses(trades)),
            PayoffRatio:  AverageWin(trades) / Math.Abs(AverageLoss(trades)),
            ExpectedValue: WinRate(trades) * AverageWin(trades) - LossRate(trades) * Math.Abs(AverageLoss(trades))
        );
    }

    // Formulas:
    // Sortino: use only returns below MAR (minimum acceptable return) in denominator
    // Calmar: CAGR / |MaxDrawdown| — penalizes worst peak-to-trough loss
    // Omega: ratio of gains above threshold to losses below threshold
    // CVaR (Expected Shortfall): mean of worst (1-confidence)% of returns — more conservative than VaR
    // Information Ratio: excess return vs. benchmark per unit of tracking error
    // Profit Factor: gross profit / gross loss — >1.5 is good, >2.0 is excellent
}
```

#### 11.9.1 Drawdown Analysis

```csharp
public (decimal MaxDrawdown, TimeSpan MaxDuration, TimeSpan CurrentRecoveryTime)
    DrawdownAnalysis(IReadOnlyList<(DateTimeOffset Date, decimal Value)> curve)
{
    decimal peak = curve[0].Value;
    decimal maxDD = 0;
    TimeSpan maxDuration = TimeSpan.Zero;
    DateTimeOffset drawdownStart = curve[0].Date;

    foreach (var (date, value) in curve)
    {
        if (value > peak)
        {
            // New high-water mark: check if previous drawdown was longest
            var duration = date - drawdownStart;
            if (duration > maxDuration) maxDuration = duration;
            peak = value;
            drawdownStart = date;
        }

        decimal dd = (peak - value) / peak;
        if (dd > maxDD) maxDD = dd;
    }

    return (maxDD, maxDuration, /* current recovery time */ ...);
}
```

---

### 11.10 Live Paper Trading vs Backtest — Reconciliation

A strategy that backtests well must be validated in live paper trading before any real capital is committed. The reconciliation process:

```
1. Run backtest on data up to T-90 days
2. Deploy strategy in paper trading from T-90 days to T (present)
3. Compare paper trading results to what the backtest would have predicted
   for that same period

Expected differences (acceptable):
  - Fill prices differ slightly (market microstructure noise)
  - Costs are slightly higher in paper trading (backtest may have underestimated)
  - Small timing differences in signal generation

Red flags (strategy is broken):
  - Paper trading return is less than 50% of backtest return for same period
  - Sharpe ratio drops by more than 1 full point
  - Strategy places dramatically different trades in paper vs backtest
  → Indicates look-ahead bias, data mismatch, or implementation bug

4. Only after 60–90 days of paper trading with results consistent with
   backtest predictions should live capital be considered
```

#### 11.10.1 Implementation Consistency Checklist

Before going live, verify:
- [ ] Simulation clock never uses `DateTime.UtcNow` — always `ISimulationClock.Now`
- [ ] Bar timestamps use close time, not open time
- [ ] Fundamental data uses filing date, not period end date
- [ ] Universe is point-in-time (includes delisted symbols)
- [ ] All order processing happens at bar N+1 minimum after signal at bar N
- [ ] Limit orders use conservative queue-position fill assumption
- [ ] All fee layers applied (commission + spread + slippage + borrow + funding)
- [ ] Stress test against 2008, 2020, 2022 scenarios passes drawdown budget
- [ ] Walk-forward validation completed with DSR > 1.0
- [ ] At least 200 independent trades in backtest (below this: insufficient sample)
- [ ] Out-of-sample period exists and was never looked at during development

---

### 11.11 Research References — Simulation & Backtesting

- [Almgren-Chriss Optimal Execution Model (original paper)](https://www.smallake.kr/wp-content/uploads/2016/03/optliq.pdf)
- [QuestDB: Almgren-Chriss Execution Strategies](https://questdb.com/glossary/optimal-execution-strategies-almgren-chriss-model/)
- [The Deflated Sharpe Ratio (Bailey, López de Prado)](https://www.davidhbailey.com/dhbpapers/deflated-sharpe.pdf)
- [Probability of Backtest Overfitting (Bailey et al.)](https://www.researchgate.net/publication/318600389_The_probability_of_backtest_overfitting)
- [Walk-Forward Optimization: Introduction (QuantInsti)](https://blog.quantinsti.com/walk-forward-optimization-introduction/)
- [TRADES: Diffusion Models for Realistic Order Flow Simulation](https://arxiv.org/html/2502.07071v2)
- [MarS: Market Simulation Engine via Generative Foundation Model](https://arxiv.org/abs/2409.07486)
- [QuantConnect LEAN Engine Docs](https://www.quantconnect.com/docs/v2/lean-engine)
- [LEAN GitHub Repository (C#)](https://github.com/QuantConnect/Lean)
- [Slippage: Non-Linear Modeling with ML](https://quantjourney.substack.com/p/slippage-a-comprehensive-analysis)
- [Monte Carlo VaR Under Basel III (MDPI, 2025)](https://www.mdpi.com/2227-9091/13/8/146)
- [Backtesting Sentiment Signals for Trading (ACL Anthology, 2025)](https://aclanthology.org/2025.jeptalnrecital-industrielle.2/)
- [Interpretable Walk-Forward Validation Framework (arxiv, 2025)](https://arxiv.org/html/2512.12924v1)
- [Backtest Overfitting in the ML Era (ScienceDirect)](https://www.sciencedirect.com/science/article/abs/pii/S0950705124011110)
- [Seven Sins of Quantitative Investing](https://bookdown.org/palomar/portfoliooptimizationbook/8.2-seven-sins.html)
- [Right Place, Right Time: RL for Execution Optimisation](https://arxiv.org/html/2510.22206v1)

---

---

## 12. Market Movers — Identification, Profiling & Prediction Framework

The central thesis of this section: **specific people, outlets, and communities have a measurable, historically quantifiable relationship with price movements**. The goal is not to build a surveillance tool — it is to build a calibrated trust and predictive-power registry: a live ledger of who moves markets, how reliably, in what direction, and with what lag.

---

### 12.1 Taxonomy of Market Movers

Market movers fall into distinct categories with very different mechanisms of influence. Each requires a different data collection and modeling approach.

#### 12.1.1 Individual People

| Category | Examples | Mechanism | Asset Classes |
|---|---|---|---|
| **Regulatory / Government** | Fed Chair (Jerome Powell), SEC Chair, Treasury Secretary, FOMC members | Policy signals, rate expectations | All — especially bonds, indices, forex |
| **Corporate Insiders** | CEOs (Elon Musk, Jensen Huang, Tim Cook), CFOs, activist investors | Product announcements, guidance, buybacks | Single stocks |
| **Macro Commentators** | Ray Dalio, Michael Burry, Cathie Wood, Nouriel Roubini | Narrative amplification, credibility spillover | Broad market, sectors |
| **Crypto KOLs** | Michael Saylor, Arthur Hayes, Vitalik Buterin, Changpeng Zhao (CZ) | Token sentiment, ecosystem confidence | Crypto |
| **Retail Amplifiers** | WSB moderators, top Reddit contributors, large Twitter/X fintwit accounts | Coordination, retail flow | Small/mid-cap equities, meme coins |
| **Political Figures** | US Presidents, legislators on finance/tech committees | Regulatory risk or opportunity | Sector-specific |
| **Short Sellers** | Hindenburg Research, Muddy Waters, Citron Research | Targeted negative reports, short pressure | Single stocks |
| **Analysts** | Goldman Sachs analysts, Morgan Stanley, JPMorgan price target changes | Institutional flow, index inclusion triggers | Equities |

#### 12.1.2 Platforms & Forums

| Platform | Mover Type | Signal Characteristics |
|---|---|---|
| **r/wallstreetbets** | Retail coordination | High velocity, high noise, extreme moves in small caps and meme stocks. Most moves are short-lived |
| **r/stocks, r/investing** | Retail accumulation | Lower velocity, more fundamental-oriented, longer time horizon |
| **r/CryptoCurrency** | Crypto sentiment | Sentiment leads price by hours to days on mid/small-cap crypto |
| **r/Bitcoin, r/ethereum** | Asset-specific communities | Narrative formation for the largest crypto assets |
| **X (Twitter) — FinTwit** | Mixed institutional + retail | The fastest signal propagation. Breaking news, CEO tweets, analyst commentary all arrive here first |
| **Telegram (public channels)** | Crypto: high-signal pump communities | Very high noise. Pump signals are frequent but directional for 30–120 minutes. Requires pump/dump filter |
| **Discord (trading servers)** | Mid-size retail coordination | Harder to access but sometimes precedes Reddit trends by hours |
| **TradingView ideas** | Retail technical analysis | Contrarian signal at extremes: when everyone is bullish on an idea, often signals exhaustion |
| **Substack / newsletters** | Macro commentary (e.g., Doomberg, Macro Compass) | Slower, higher credibility. Subscriber count proxies reach |
| **YouTube (finance channels)** | Retail amplification | Lags price action, confirms narrative. Useful for identifying late-stage hype |
| **4chan /biz/** | Early-stage crypto signals, memes | Extremely noisy. Occasionally first-mover on small caps before Reddit. Not reliable standalone |

#### 12.1.3 News Outlets & Publications

| Outlet | Tier | Latency | Asset Coverage | Notes |
|---|---|---|---|---|
| **Bloomberg Terminal / Wire** | 1 | Seconds | All | Gold standard. Machine-readable feeds available |
| **Reuters** | 1 | Seconds | All | Wire service, algo-readable |
| **Dow Jones / WSJ** | 1 | Seconds–Minutes | Equities, macro | "WSJ Effect" — DJ Newswires moves markets |
| **Financial Times** | 1 | Minutes | Global macro, equities | European market hours esp. important |
| **CNBC** | 2 | Minutes | Equities, crypto | "Cramer Effect" — documented inverse signal |
| **CoinDesk / The Block** | 2 | Minutes | Crypto | First with regulatory, exchange news |
| **Politico / Axios** | 2 | Minutes | Regulatory, macro | Political risk signals |
| **Benzinga** | 3 | Minutes | Equities | Good pre-market scanner |
| **Seeking Alpha** | 3 | Minutes–Hours | Equities | Author ratings predictive for small caps |
| **ZeroHedge** | 3 | Minutes | Macro | Extreme framing but fast on macro data |
| **Business Insider** | 3 | Minutes | General | Amplifier, not originator |

---

### 12.2 The Mover Profile Schema

Every tracked entity (person, outlet, community) gets a structured profile that is continuously updated:

```csharp
public record MoverProfile
{
    // Identity
    string MoverId { get; init; }       // e.g., "elon_musk_twitter"
    string DisplayName { get; init; }
    MoverType Type { get; init; }       // Individual | Publication | Community | Analyst
    MoverCategory Category { get; init; } // CEO | Regulator | Commentator | KOL | ShortSeller | ...

    // Reach Metrics (updated daily)
    long FollowerCount { get; init; }
    double AvgEngagementRate { get; init; }     // (likes + comments + shares) / followers
    double EngagementVelocityP90 { get; init; } // 90th percentile engagement burst
    string[] PrimaryPlatforms { get; init; }

    // Asset Affinity (which assets does this mover discuss?)
    Dictionary<string, double> AssetMentionFrequency { get; init; }  // symbol → mentions/week
    string[] PrimaryAssets { get; init; }

    // Predictive Power Registry (the core of the profile)
    Dictionary<string, MoverAssetStats> AssetStats { get; init; }  // per-asset calibration

    // Psychological / Behavioral Profile
    NarrativeProfile NarrativeStyle { get; init; }
    CredibilityScore Credibility { get; init; }
    BiasProfile Bias { get; init; }
}

public record MoverAssetStats(
    string Symbol,

    // Historical signal → price correlation
    double GrangerPValue,           // p < 0.05: mover content Granger-causes price
    double SignalLeadHours,         // how many hours before price move the signal appears
    double PriceImpact1h,           // average abs% price change in 1h after mover content
    double PriceImpact24h,          // average abs% price change in 24h
    double PriceImpact7d,           // 7-day drift following content

    // Directional accuracy
    double DirectionalAccuracy,     // % of positive signals followed by positive moves
    double CallAccuracy,            // when they make a directional claim, how often correct?
    int TotalSignalsTracked,

    // Decay curve
    double HalfLifeHours,          // time for signal to decay to 50% of initial impact

    // Regime breakdown
    double AccuracyBullMarket,
    double AccuracyBearMarket,
    double AccuracyHighVolatility
);
```

---

### 12.3 Psychological & Behavioral Profiling

Beyond market impact stats, each mover has a behavioral profile that informs how to interpret their content.

#### 12.3.1 Narrative Style Dimensions

```csharp
public record NarrativeProfile
{
    // Communication style
    double CertaintyScore;      // how often they use definitive language ("will", "definitely") vs hedged
    double HyperboleFrequency;  // use of superlatives ("massive", "catastrophic", "moon")
    double TechnicalDepth;      // domain vocabulary density — signals expertise vs. hype
    double EmotionalValence;    // mean emotional tone across all posts
    double ConsistencyScore;    // how consistent is their position over time — flip-flopper vs. committed

    // Posting behavior
    double PostingRegularity;       // consistent scheduler vs. burst poster
    double EventReactivity;         // do they post more during price events?
    double OriginalContentRatio;    // original analysis vs. reposting/reacting

    // Audience relationship
    double EchoChamberScore;       // how much does their audience reinforce vs. challenge them?
    double CrossPlatformReach;     // do their posts spread across platforms?

    // Potential red flags
    double PumpProbability;         // ML score: likelihood this is coordinated pumping
    double UndisclosedPositionRisk; // does posting pattern correlate with own holdings?
}
```

#### 12.3.2 Credibility Score — Multi-Factor Calculation

The credibility score is not a single number — it is a vector of dimensions that are combined into a composite:

```
CredibilityScore = weighted_sum([
    PredictionAccuracy × 0.35,      // ground truth: were their calls right?
    ConsistencyOverTime × 0.20,     // do they maintain positions or reverse when convenient?
    TransparencyScore × 0.15,       // do they disclose holdings? acknowledge being wrong?
    SourceQuality × 0.15,           // do they cite primary sources, or just opine?
    AudienceDiversity × 0.10,       // broad or insular audience?
    LongevityScore × 0.05           // years of tracked history
])

Range: 0.0 (no trust) to 1.0 (extremely reliable)
```

Numeric thresholds:
- **0.85–1.0**: Tier 1 — treat signal with high weight in composite score
- **0.65–0.84**: Tier 2 — moderate weight, require cross-platform confirmation
- **0.40–0.64**: Tier 3 — low weight, treat as noise unless corroborated
- **< 0.40**: Flagged — potential manipulator, adversarial actor, or consistently wrong
- **Contrarian flag**: if credibility < 0.30 AND directional accuracy < 0.40, apply as *inverse* signal

#### 12.3.3 Bias Profile

Every mover has systematic biases. Modeling them is more useful than ignoring them:

```csharp
public record BiasProfile
{
    double StructuralBull;      // persistently optimistic regardless of conditions
    double StructuralBear;      // persistently pessimistic
    double SelfInterestBias;    // their calls align with their known/rumored positions
    double RecencyBias;         // overweights recent events, ignores long-term base rates
    double NarrativeBias;       // fits all data to a single thesis
    double ConfirmationBias;    // selectively amplifies confirming data

    // Detected position alignment
    bool? KnownLongPosition;    // known or suspected to hold long position in discussed asset
    bool? KnownShortPosition;   // known or suspected to hold short position
    double PositionAlignmentScore; // how often their calls align with price action that benefits them
}
```

---

### 12.4 Signal Extraction Pipeline — From Content to Market Signal

```
┌──────────────────────────────────────────────────────────────────┐
│              CONTENT INGESTION (per tracked mover)               │
│                                                                  │
│  X/Twitter: stream filtered by mover account IDs                 │
│  Reddit: stream filtered by top-karma accounts + mods            │
│  News outlets: RSS / wire feed parsing                           │
│  YouTube: Data API, video title + description + transcript       │
└───────────────────────────┬──────────────────────────────────────┘
                            │ raw content
┌───────────────────────────▼──────────────────────────────────────┐
│                   CONTENT PROCESSING                             │
│                                                                  │
│  1. Entity extraction    → which assets are mentioned?           │
│  2. Claim extraction     → is this a directional prediction?     │
│     ("I think BTC hits $200k by Q4" → YES, directional)         │
│     ("Here's today's CPI reading" → NO, factual report)          │
│  3. Sentiment scoring    → fine-tuned LLM (Section 5–6)          │
│  4. Certainty scoring    → "might" vs. "will" vs. "guaranteed"   │
│  5. Deception markers    → linguistic deception features         │
│  6. Reach score          → followers × engagement rate × spread  │
└───────────────────────────┬──────────────────────────────────────┘
                            │ structured signal
┌───────────────────────────▼──────────────────────────────────────┐
│                   MOVER REGISTRY LOOKUP                          │
│                                                                  │
│  Load MoverProfile for author                                    │
│  Apply credibility weight to signal                              │
│  Apply bias correction (subtract known structural bull/bear)     │
│  Lookup AssetStats for mentioned symbol                          │
│  Retrieve historical signal→price lag (SignalLeadHours)          │
└───────────────────────────┬──────────────────────────────────────┘
                            │ weighted, bias-corrected signal
┌───────────────────────────▼──────────────────────────────────────┐
│                   SIGNAL SCORING OUTPUT                          │
│                                                                  │
│  {                                                               │
│    mover_id, asset, timestamp,                                   │
│    raw_sentiment: float,          // [-1, 1]                     │
│    credibility_weight: float,     // [0, 1]                      │
│    bias_corrected_sentiment: float,                              │
│    reach_score: float,                                           │
│    is_directional_claim: bool,                                   │
│    certainty_level: enum,         // Speculative|Moderate|High   │
│    predicted_price_direction: enum,                              │
│    expected_lag_hours: float,     // from historical profile      │
│    composite_impact_score: float  // final weighted signal       │
│  }                                                               │
└──────────────────────────────────────────────────────────────────┘
```

---

### 12.5 Claim Tracking & Ground Truth Verification

Every directional claim made by a tracked mover is stored as a falsifiable record and resolved against actual price data:

```csharp
public record TrackedClaim
{
    string ClaimId { get; init; }
    string MoverId { get; init; }
    string Symbol { get; init; }
    DateTimeOffset ClaimTimestamp { get; init; }

    // The claim itself
    string RawText { get; init; }
    ClaimDirection Direction { get; init; }    // Bullish | Bearish | Neutral
    ClaimHorizon Horizon { get; init; }        // Intraday | Days | Weeks | Months | Unspecified
    decimal? PriceTarget { get; init; }        // explicit target if stated
    CertaintyLevel Certainty { get; init; }    // Speculative | Moderate | High | Guaranteed

    // Ground truth resolution (filled in after horizon passes)
    decimal PriceAtClaim { get; init; }
    decimal PriceAtResolution { get; init; }
    DateTimeOffset ResolutionTimestamp { get; init; }
    bool ClaimCorrect { get; init; }           // did price move in stated direction?
    double ActualMovePercent { get; init; }    // how large was the actual move?
    double PredictedVsActual { get; init; }    // for numeric targets: deviation

    // Derived metrics updated to mover profile
    bool UpdatedToProfile { get; init; }
}
```

**Claim resolution logic:**

```
For "Bullish on BTC next week":
  - Horizon = 7 days
  - Resolution: if BTC price at T+7 > T+0, claim = CORRECT
  - PredictedVsActual = (P_t7 - P_t0) / P_t0

For "BTC hits $200k by Q4 2026":
  - Horizon = end of Q4 2026
  - Resolution: did BTC ever touch $200k before Dec 31 2026?
  - Store as PENDING until horizon passes

For "Sell everything, crash incoming" (no specific target):
  - Direction = Bearish
  - Horizon = Unspecified → use 30-day window
  - Resolution: if SPX down >5% in next 30 days, claim = CORRECT
```

All resolved claims feed back into `MoverAssetStats.CallAccuracy` and `MoverAssetStats.DirectionalAccuracy`.

---

### 12.6 Numerical Trust & Predictive Power Registry

The full data model for a mover's trustworthiness registry, continuously updated as claims resolve:

```
┌──────────────────────────────────────────────────────────────────┐
│                    MOVER TRUST REGISTRY                          │
├──────────────────────────────────────────────────────────────────┤
│  Mover: @ExampleAnalyst                                          │
│  Period: 2024-01-01 to 2026-03-05                                │
│  Total claims tracked: 147                                       │
│  Resolved claims: 131   (89%)                                    │
├──────────────────────────────────────────────────────────────────┤
│  OVERALL STATS                                                   │
│  ─────────────────────────────────────────────────────────────  │
│  Directional accuracy (all assets):      61.8%                  │
│  vs. random baseline (50%):              +11.8pp                │
│  Calibration score (confidence vs acc):  0.74  [0–1]            │
│  Credibility score (composite):          0.71  [0–1]            │
├──────────────────────────────────────────────────────────────────┤
│  PER-ASSET BREAKDOWN                                             │
│  ─────────────────────────────────────────────────────────────  │
│  Asset   │ Calls │ Acc%  │ AvgImpact│ Lead  │ Granger p         │
│  TSLA    │  34   │ 70.6% │ +2.1%    │ 4.2h  │ 0.031 *           │
│  BTC     │  28   │ 57.1% │ +1.8%    │ 2.1h  │ 0.089             │
│  SPX     │  21   │ 47.6% │ +0.9%    │ N/A   │ 0.41              │
│  NVDA    │  18   │ 66.7% │ +3.4%    │ 6.8h  │ 0.018 *           │
│  (*)  = statistically significant Granger causality             │
├──────────────────────────────────────────────────────────────────┤
│  BIAS PROFILE                                                    │
│  ─────────────────────────────────────────────────────────────  │
│  Structural bull bias:       0.68  (persistent optimism)         │
│  Self-interest alignment:    0.71  (calls align with positions)  │
│  Recency bias:               0.55  (moderate)                   │
│  Certainty miscalibration:  +0.18  (overconfident)              │
├──────────────────────────────────────────────────────────────────┤
│  REGIME PERFORMANCE                                              │
│  ─────────────────────────────────────────────────────────────  │
│  Bull market accuracy:       74.2%                               │
│  Bear market accuracy:       41.2%  (degrades sharply)          │
│  High volatility accuracy:   48.9%                               │
│  Recommendation: weight signal highly in low-vol bull regime     │
│                  reduce to 30% weight in bear/high-vol regimes   │
└──────────────────────────────────────────────────────────────────┘
```

---

### 12.7 Deception & Manipulation Detection

Legitimate signal must be separated from coordinated manipulation. Key detection dimensions:

#### 12.7.1 Linguistic Deception Markers

NLP research identifies language patterns statistically associated with deceptive financial content (Frontiers in AI, 2025; CFA Institute, 2021):

```
Deception-correlated features:
  - Excessive certainty in speculative contexts
  - Use of passive voice to obscure agency ("it is widely known that...")
  - Selective omission (high sentiment score but mentions only positives)
  - Inconsistency between stated belief and posting behavior
  - Sudden urgency language ("buy NOW before it's too late")
  - False scarcity framing ("this opportunity closes in 24 hours")
  - Appeal to unnamed authority ("sources inside the company say...")
  - Hedging removals over successive posts (starts uncertain, becomes certain)

Features extracted as numeric scores by the sentiment/NLP pipeline,
combined into a PumpProbability score [0.0–1.0]
```

#### 12.7.2 Behavioral Pattern Flags

```csharp
public static class ManipulationDetector
{
    // Flag: high posting volume about asset preceding unusual volume spike
    bool IsVolumePreludeSuspect(string moverId, string symbol, TimeSpan window);

    // Flag: multiple accounts with correlated posting timing (coordinated pumping)
    bool IsCoordinatedActivity(IEnumerable<string> moverIds, string symbol, TimeSpan window);

    // Flag: mover's known position directly benefits from call direction
    bool HasObviousSelfInterest(string moverId, string symbol, ClaimDirection direction);

    // Flag: claim accuracy drops to near 0 (they may be deliberately wrong / fading their audience)
    bool IsPotentiallyAdversarial(string moverId, string symbol);

    // Flag: engagement comes from accounts with suspicious patterns (bot network)
    double BotEngagementRatio(string moverId);
}
```

#### 12.7.3 Pump-and-Dump Signature

Pattern that indicates coordinated manipulation rather than genuine signal:

```
1. Quiet accumulation phase:
   - Mover begins mentioning asset with positive framing
   - Volume and mentions below normal
   - Credibility score may appear high (early calls were accurate to build trust)

2. Escalation phase:
   - Posting frequency increases 3–10x
   - Language certainty increases sharply
   - Urgency framing appears
   - Multiple correlated accounts begin posting simultaneously

3. Peak / distribution:
   - Maximum hype / maximum posting frequency
   - Price peaks
   - Mover goes quiet or shifts to "profit taking is normal"

4. Reversal:
   - Price drops rapidly
   - Mover either disappears or blames external factors

Detection signal:
  - PostingVelocityZScore > 3.0 + CertaintyDelta > 0.3 + PriceAlreadyUp > 20%
  → Raise PumpProbability flag, reduce signal weight to 0 or invert
```

---

### 12.8 Composite Market Mover Signal — Aggregation

Multiple movers posting about the same asset at the same time produces a composite signal. The aggregation formula:

```
CompositeSignal(asset, window) =
  Σ over all movers i posting about asset in window:
    (
      BiasCorrectSentiment(i) ×
      CredibilityScore(i) ×
      ReachScore(i) ×
      AssetSpecificAccuracy(i) ×
      RegimeWeight(i, currentMarketRegime) ×
      (1 - PumpProbability(i))     ← discounts suspected manipulators
    )
  / Σ(ReachScore(i))               ← reach-weighted normalization

Output:
  Composite score ∈ [-1.0, 1.0]
  Where:
    -1.0 = extremely strong bearish consensus from high-credibility movers
     0.0 = neutral / conflicting
    +1.0 = extremely strong bullish consensus from high-credibility movers
```

This composite is fed into the wider signal aggregation layer (Section 4) alongside technical indicators and GDELT sentiment.

---

### 12.9 Dashboard Integration — Mover Intelligence Widgets

Widgets to expose this system in the MarketMaker UI:

| Widget | Description |
|---|---|
| **Mover Feed** | Real-time stream of content from tracked movers, ranked by composite impact score |
| **Mover Leaderboard** | Ranked list of movers by directional accuracy for a selected asset and time range |
| **Claim Tracker** | Live ledger of open directional claims, status (pending/resolved), and accuracy rate |
| **Credibility Heatmap** | Matrix of mover × asset, color-coded by historical accuracy |
| **Manipulation Alert** | Banner when PumpProbability > threshold for any asset in watchlist |
| **Mover Profile Card** | Full profile view: stats, bias, regime performance, recent claims |
| **Consensus Gauge** | Composite mover signal dial for selected asset, real-time |

---

### 12.10 Mover Registry — Initial Tracked Entities

A starting list, structured by category:

**Regulatory / Central Bank**
- Federal Reserve Chair (Jerome Powell) — @federalreserve, FOMC statement transcripts
- FOMC minutes (all voting members) — EDGAR/Fed website
- SEC Chair — press releases, enforcement actions
- CFTC Chair — crypto derivatives oversight

**Corporate — High Market Impact**
- Elon Musk (@elonmusk) — TSLA, DOGE, X/Twitter company news, SpaceX
- Jensen Huang (NVDA) — AI, semiconductor narrative
- Jamie Dimon (JPM) — macro/banking sentiment
- Larry Fink (BlackRock) — ETF flows, institutional sentiment
- Michael Saylor (MicroStrategy/@saylor) — BTC accumulation signal
- Cathie Wood (ARK Invest) — high-growth tech, genomics, crypto

**Macro Commentators**
- Ray Dalio — Bridgewater macro views
- Michael Burry (@michaeljburry) — contrarian short thesis, high impact when active
- Nouriel Roubini — crisis signal (known bear, useful as narrative gauge)
- Peter Schiff — gold/anti-crypto signal

**Crypto KOLs**
- Arthur Hayes (BitMEX founder) — macro-crypto thesis, well-calibrated historically
- Raoul Pal (Real Vision) — macro-crypto institutional
- Willy Woo — on-chain analytics signal
- PlanB (@100trillionUSD) — BTC stock-to-flow model, predictive claims trackable
- Vitalik Buterin — ETH ecosystem signal

**Short Sellers / Investigative**
- Hindenburg Research — targeted short reports, very high directional accuracy on target
- Muddy Waters Research (Carson Block) — same
- Citron Research — high historical accuracy, now more balanced

**Reddit Communities (tracked as entities)**
- r/wallstreetbets — meme stock coordination signal
- r/CryptoCurrency — crypto retail sentiment
- r/stocks — equity retail accumulation signal
- r/ValueInvesting — contrarian longer-term signal

**News Outlets (algorithmic feed)**
- Bloomberg News (wire feed) — Tier 1 speed, all assets
- Dow Jones Newswires — Tier 1, equities focus
- CoinDesk — Tier 2, crypto regulatory/exchange news
- Politico Pro — regulatory risk signal

---

### 12.11 Research References — Market Movers & Profiling

- [Explainable Assessment of Financial Experts' Credibility (arxiv, 2024)](https://arxiv.org/abs/2406.11924)
- [Power of 280: Measuring Elon Musk Tweet Impact on Stock Market](https://www.researchgate.net/publication/372833861_Power_of_280_Measuring_the_Impact_of_Elon_Musk_s_Tweets_on_the_Stock_Market)
- [Elon Musk's Influence on Stock Markets: The Musk Effect](https://press.farm/elon-musks-influence-stock-markets-musk-effect/)
- [Investor Sentiment and Market Movements: Granger Causality Perspective (arxiv, 2025)](https://arxiv.org/pdf/2510.15915)
- [Information Fusion of Stock Prices and Sentiment Using Granger Causality (IEEE)](https://ieeexplore.ieee.org/document/8170390/)
- [Psycholinguistic NLP Framework for Forensic Text Analysis (Frontiers in AI, 2025)](https://www.frontiersin.org/journals/artificial-intelligence/articles/10.3389/frai.2025.1669542/pdf)
- [Fraud and Deception Detection: Text-Based Analysis (CFA Institute)](https://blogs.cfainstitute.org/investor/2021/02/15/fraud-and-deception-detection-text-based-analysis/)
- [AI in Behavioral Finance: Investor Bias Through ML](https://www.researchgate.net/publication/393785984_AI_in_Behavioral_Finance_Understanding_Investor_Bias_Through_Machine_Learning)
- [Natural Language Processing in Finance: A Survey (ScienceDirect)](https://www.sciencedirect.com/article/abs/pii/S1566253524005335)
- [AI Narrative Drift Detection in Influencer Content (2025)](https://www.influencers-time.com/ai-for-catching-narrative-drift-in-influencer-contracts-2025/)

---

---

## 13. Retroactive Movement Attribution — Who Called It, Who Was Wrong, and Why

This section covers the reverse problem: given that a significant price move has already happened, **who predicted it before it occurred, with what lead time, and what does that tell us about their predictive signal going forward?** It also covers the complementary pattern: movers whose content is consistently *contradicted* by price action, which is equally valuable as an inverse or fade signal.

---

### 13.1 The Two Signal Regimes

Every tracked mover, at any point in time, falls into one of two signal regimes:

```
┌───────────────────────────────────────────────────────────────┐
│                   SIGNAL REGIME CLASSIFICATION                │
│                                                               │
│  INFORMATIVE (follow signal)                                  │
│  ────────────────────────────────────────────────────────     │
│  Content precedes price in consistent direction               │
│  Granger test: p < 0.05                                       │
│  Directional accuracy: > 55% (statistically above chance)     │
│  Lead time: measurable and consistent                         │
│  Example: short-seller publishing research report             │
│                                                               │
│  CONTRARIAN / INVERSE (fade the signal)                       │
│  ────────────────────────────────────────────────────────     │
│  Content PRECEDES price in OPPOSITE direction                 │
│  Market consistently does the opposite of what mover says     │
│  Directional accuracy: < 45% (statistically below chance)     │
│  Lead time: measurable, but direction must be inverted        │
│  Example: late-stage retail amplifier posting at sentiment     │
│           exhaustion peaks (Cramer effect, top-tick media)    │
│                                                               │
│  NOISE (discard or heavily discount)                          │
│  ────────────────────────────────────────────────────────     │
│  No measurable relationship, p > 0.1, accuracy near 50%      │
│  High posting volume, zero predictive value                   │
└───────────────────────────────────────────────────────────────┘
```

The system must continuously recalibrate which regime each mover is in. A mover can shift regimes as markets change — a bull market megaphone who was informative in 2021 may be noise or contrarian in a 2022–2023 bear market.

---

### 13.2 Retroactive Move Attribution Pipeline

When a significant price move occurs, the system automatically runs a retroactive attribution query:

```
Event trigger:
  Price moves ≥ threshold (configurable, e.g., ±2% in 4h, ±5% in 24h)

Attribution query window:
  Scan all mover content from T-72h to T-1h before the move began

Attribution steps:
  1. Collect all mover signals for the affected asset in the window
  2. Classify each signal: bullish / bearish / neutral
  3. Compare signal direction to actual price move direction
  4. Score each mover: predicted correctly (1) or incorrectly (0)
  5. Note lead time: hours between signal and move onset
  6. Store as MoveAttributionRecord
  7. Update mover's AssetStats with this new data point
```

```csharp
public record MoveAttributionRecord
{
    string Symbol { get; init; }
    DateTimeOffset MoveOnset { get; init; }
    decimal MoveMagnitude { get; init; }        // % change
    MoveDirection Direction { get; init; }       // Up | Down

    // All movers who signaled about this asset in the pre-move window
    IReadOnlyList<MoverAttribution> Attributions { get; init; }
}

public record MoverAttribution
{
    string MoverId { get; init; }
    DateTimeOffset SignalTimestamp { get; init; }
    double LeadTimeHours { get; init; }          // hours before move onset
    SignalDirection SignalDirection { get; init; }
    bool CorrectlyPredicted { get; init; }
    double SignalStrength { get; init; }          // certainty × reach
    string ContentSnippet { get; init; }          // the actual text fragment
}
```

#### 13.2.1 Lead Time Histogram

For each mover × asset pair that has a statistically significant Granger relationship, maintain a histogram of how many hours before the move the signal appeared:

```
@SampleMover → NVDA: Move attribution histogram (N=47 events)

Lead time distribution:
  0–1h:    ██████ 12 events (25.5%)   ← mostly reactive, not predictive
  1–4h:    ████████████ 24 events (51.1%)  ← primary signal window
  4–12h:   ████ 8 events (17.0%)
  12–24h:  ██ 3 events (6.4%)
  >24h:    0 events

Median lead time: 2.8 hours
P90 lead time:   8.1 hours
→ Set alert window to T-6h for this mover/asset pair
```

This lead time profile is used to set the retrospective scan window per mover (different movers have different characteristic lead times).

---

### 13.3 Informed Trading Detection — Pre-Move Signals

Beyond content, there are behavioral and market-data signals that suggest someone is acting on information *before* it becomes public. These are distinct from discourse signals but can be correlated with them:

#### 13.3.1 Options Market Pre-Event Detection

Options markets are frequently the first place informed traders act because leverage magnifies returns and the identity of option buyers is obscured:

```
Abnormal options activity flags:
  - Open interest spike: OI increases ≥ 3σ above rolling 20-day average
  - Volume / OI ratio: daily volume > 2× existing open interest
    (indicates new positioning, not closing existing trades)
  - Out-of-the-money call surge: far OTM calls being bought in volume
    ahead of anticipated positive event
  - OTM put accumulation: before negative event
  - Implied volatility divergence: IV rising while price is flat
    (someone buying protection before a move)

Research finding: abnormal options volume accurately predicts event
outcomes for corporate events, FDA meetings, earnings surprises.
Options inform trading tends to cluster 1–5 days pre-event.
```

```csharp
public class InformedTradingDetector
{
    public AbnormalOptionsSignal Detect(
        string symbol,
        DateTimeOffset analysisTime,
        OptionsChain chain,
        HistoricalOptionsData baseline)
    {
        double oiZScore = ZScore(chain.TotalOpenInterest, baseline.OpenInterestSeries);
        double volumeOiRatio = chain.DailyVolume / chain.TotalOpenInterest;
        bool otmCallSurge = DetectOtmDirectionalSurge(chain, OptionType.Call);
        bool otmPutSurge = DetectOtmDirectionalSurge(chain, OptionType.Put);
        double ivDisconnect = ComputeIvPriceDisconnect(chain, baseline);

        return new AbnormalOptionsSignal(
            Symbol: symbol,
            Timestamp: analysisTime,
            OiZScore: oiZScore,
            VolumeOiRatio: volumeOiRatio,
            SuspectedDirection: otmCallSurge ? Direction.Up :
                                otmPutSurge  ? Direction.Down : Direction.Neutral,
            InformedTradingScore: CompositeScore(oiZScore, volumeOiRatio, ivDisconnect)
        );
    }
}
```

#### 13.3.2 Prediction Market Pre-Move Detection

Prediction markets show a documented pattern of odds shifting 4–6 hours before the corresponding financial market reacts. This can be measured and used as a lead signal:

```
Cross-market lead detection:

For each prediction market question linked to a financial asset:
  1. Monitor odds for shifts ≥ 5pp in a 15-minute window
  2. Record: direction of odds shift, magnitude, time
  3. After each price move in the linked asset, measure:
     - Did odds shift precede price move? (Y/N)
     - Lead time: hours
     - Correlation between odds magnitude and price magnitude

Build a historical calibration:
  "Polymarket 'Fed hike in March' moves from 40% to 65% odds"
  → historically associated with:
    - 10yr Treasury yield rising 8–15bps within 4 hours (N=12 events)
    - USD/EUR +0.4% within 8 hours (N=9 events)
```

#### 13.3.3 Blockchain / On-Chain Pre-Move Detection (Crypto)

For crypto assets, on-chain data provides the equivalent of options flow:

```
Pre-move on-chain signals:
  - Large wallet accumulation: whale wallets receiving significant inflows
    from exchanges (withdrawing to cold storage = bullish conviction)
  - Exchange inflow surge: assets moving TO exchanges = selling pressure imminent
  - Miner outflows: miners sending BTC to exchanges before price drops
  - Funding rate shifts: perpetual funding rate moving from positive to negative
    (longs being liquidated, or shorts accumulating)
  - Open interest spike on perps: leveraged positioning building
  - Liquidation cascade risk: measure leveraged positions at risk at current price ± X%

Combine with discourse signals:
  On-chain accumulation + positive KOL sentiment = strong bull signal
  On-chain exchange inflows + negative KOL sentiment = strong bear confirmation
  On-chain accumulation + negative KOL sentiment = potentially contrarian (whales
    accumulating while retail is scared — historically bullish)
```

---

### 13.4 The Inverse Signal — When Movers Are Wrong

The Cramer Effect is the canonical example: Jim Cramer's bullish calls are followed by a short-term price gain of ~3%, then a reversion, with systematic inverse correlation on longer time horizons. Research confirms:

- Average Cramer-mentioned stock gains ~3% the next session
- Then drifts back toward pre-show levels
- Directional accuracy over 3–30 day horizon: statistically below 50%
- Systematic inverse alpha exists, though small (0.05%), and mostly captures crowding/mean-reversion

This is not unique to Cramer. The phenomenon applies to any mover who:
1. Has a large retail audience
2. Posts when a thesis is already mature (not early)
3. Functions as a late-stage amplifier rather than early discoverer

#### 13.4.1 Crowding Detection Model

The root cause of inverse signals is **crowding**: when a narrative has been amplified to the point that everyone who will act on it has already acted, the remaining flow is exhausted. Future price movement is now determined by mean-reversion, profit-taking, or news that contradicts the narrative.

```
CrowdingScore(asset, t) = f(
    MentionVelocityPercentile,      // where are we in the mention velocity distribution?
    SentimentExtreme,               // is sentiment at a multi-month extreme?
    PositioningConcentration,       // are futures/options positioned one-sided?
    FundingRateExtreme,             // crypto: is funding rate at an extreme?
    RetailFlowVsInstitutional,      // are retail buyers dominant while institutions sell?
    SourceConcentration             // is signal coming from few amplifiers, not diverse sources?
)

When CrowdingScore > threshold:
  Apply inverse multiplier to signal from known amplifier-type movers
  Flag asset as "crowded" in dashboard
  Log as potential contrarian setup
```

#### 13.4.2 Systematic Inverse Signal Conditions

The following conditions, when all co-occurring, historically produce inverse (fade) signals:

```
FADE SETUP CHECKLIST:
  [ ] Mover credibility < 0.40 OR InverseRegime flag = true
  [ ] CrowdingScore > 0.75
  [ ] Content certainty level = High (definitive bullish/bearish calls)
  [ ] Sentiment at 90th percentile extreme (rolling 60-day)
  [ ] Multiple mainstream retail amplifiers all saying the same thing
  [ ] Price has already moved ≥ X% in the direction of the narrative
  [ ] On-chain OR options data shows institutional distribution
      while retail sentiment is at extreme

When all checked:
  → Signal direction = OPPOSITE of mover content
  → Expected trade: mean-reversion entry on the asset
```

Real-world documented examples to seed the historical database:

| Event | Mover Content | Actual Outcome | Lead Time |
|---|---|---|---|
| CNBC BTC $100k coverage surge (Dec 2017) | Extreme bullish, mainstream retail entry | BTC -83% over next 12 months | 2 weeks |
| Jim Cramer "never sell FAANG" (Nov 2021) | Strong buy on Meta, Amazon, Netflix | -65%, -50%, -75% over 12 months | 1 month |
| WSB GME MOASS calls (Jan 2021 peak) | Maximum bullish, "infinity squeeze" | -90% in 30 days from peak | Days |
| Jim Cramer bearish on BTC (June 2022) | Extreme bearish | BTC +200% over next 18 months | 1 month |
| Mass crypto Twitter "supercycle" thesis (2021) | Universal bullish consensus | Major correction followed | 2 weeks |

---

### 13.5 Correlation Analysis — Content vs. Movement

Beyond individual movers, the system maintains a continuous correlation matrix between:
- **Content signals** (mover signals, aggregated by type, source, credibility tier)
- **Price series** (returns, volatility, volume)
- **Market microstructure** (options flow, prediction market odds, on-chain)

#### 13.5.1 Rolling Correlation Matrix

```
For each (mover_signal_type × asset) pair:
  - Compute Pearson correlation between daily signal score and T+1, T+7, T+30 returns
  - Compute Spearman rank correlation (for non-linear relationships)
  - Update rolling on new data with exponential decay (recent more relevant)
  - Flag correlation flips: when a historically positive correlation goes negative
    → regime change signal — the mover has shifted from informative to inverse

Stored as:
  correlation_matrix[mover_id][symbol][lag_days] = {
    pearson: float,
    spearman: float,
    p_value: float,
    n_observations: int,
    last_updated: DateTimeOffset,
    trend: 'strengthening' | 'weakening' | 'inverting'
  }
```

#### 13.5.2 Event Study Framework

For every significant market move, run an automated event study:

```
Event study methodology (standard finance):

1. Define event: price move ≥ ±2σ in specified window
2. Define estimation window: T-120 to T-30 days before event
3. Define event window: T-5 to T+5 days
4. Compute expected return: using market model (CAPM or Fama-French)
5. Compute abnormal return: actual return - expected return
6. Cumulative abnormal return (CAR): sum over event window
7. Test statistical significance: t-test on CAR

Apply this per mover per event:
  - Was there a significant signal from this mover in T-120 to T-30?
  - Did the signal direction match the CAR direction?
  - What was the lag between signal and abnormal return onset?

Over N events, build the mover's event study profile:
  AverageCAR_following_signal:  % return attributable to mover signal
  SignalLeadTime:              hours/days mover signal preceded event
  StatisticalSignificance:     p-value across all events
```

#### 13.5.3 Opposite Direction Event Registry

Every time a mover's content goes in the *opposite* direction of the subsequent price move, it is logged as an Inverse Event:

```csharp
public record InverseEvent
{
    string MoverId { get; init; }
    string Symbol { get; init; }
    DateTimeOffset ContentTimestamp { get; init; }
    SignalDirection ContentDirection { get; init; }  // what they said
    DateTimeOffset MoveOnset { get; init; }
    MoveDirection ActualMoveDirection { get; init; } // what market did
    double MoveMagnitude { get; init; }
    double LeadTimeHours { get; init; }
    string ContentSnippet { get; init; }

    // Context at time of content
    double CrowdingScoreAtTime { get; init; }
    double SentimentPercentileAtTime { get; init; }
    bool WasAtSentimentExtreme { get; init; }
}
```

When a mover accumulates ≥ 10 inverse events with:
- Consistent directionality (content bullish → price falls, content bearish → price rises)
- Statistical significance (binomial test p < 0.05)

→ Automatically flag mover as `InverseRegime = true` in their profile
→ All future signals from this mover are tagged with inverse signal type
→ Dashboard shows "INVERSE SIGNAL" badge on mover's content

---

### 13.6 The Predictive Pre-Move Registry

A running ledger of movers who called specific moves *before* they happened, updated after every significant event:

```
┌──────────────────────────────────────────────────────────────────────┐
│              PRE-MOVE ATTRIBUTION LEDGER (sample)                    │
├──────────────────────────────────────────────────────────────────────┤
│  Move: NVDA +18% on earnings beat (Nov 2024)                         │
│  ─────────────────────────────────────────────────────────────────   │
│  Called correctly (pre-move):                                        │
│    @AnalystA          Bullish × 0.9 certainty    Lead: 6.2h          │
│    r/WallStreetBets   Bullish × 0.7 certainty    Lead: 12h           │
│    OptionFlow #4452   OTM call surge              Lead: 2 days        │
│  Called incorrectly:                                                  │
│    @MacroBear42       Bearish × 0.8 certainty    (INVERSE flag)      │
│    CNBC_Headline_A    Bearish × 0.6 certainty    (added to inverse)  │
├──────────────────────────────────────────────────────────────────────┤
│  Move: BTC -22% in 48h (Luna contagion, May 2022)                    │
│  ─────────────────────────────────────────────────────────────────   │
│  Called correctly:                                                    │
│    @ArthurHayes       Bearish × 0.85 certainty   Lead: 4 days        │
│    On-chain signal    Exchange inflows spike       Lead: 18h          │
│    Polymarket odds    "BTC below $30k" 60% → 82%  Lead: 6h           │
│  Called incorrectly:                                                  │
│    @PlanB             Bullish × 0.9 (S2F model)   (model fail flag)  │
│    Multiple KOLs      "Buy the dip" × 0.8          (consensus wrong) │
├──────────────────────────────────────────────────────────────────────┤
│  Cumulative correct-call leaders (all events, last 12 months):       │
│  ─────────────────────────────────────────────────────────────────   │
│  Rank  Mover                   Events  Acc%   Avg Lead   Asset Focus  │
│  1     @HighCredAnalyst        41      73.2%  5.1h       NVDA, AMD   │
│  2     Polymarket odds shifts  89      68.5%  4.8h       All         │
│  3     Hindenburg Research     12      91.7%  3 days     Short Eq    │
│  4     On-chain OI signal      67      66.2%  14.2h      BTC, ETH    │
│  5     r/WSB (contrarian)      22      69.8%  18h        Meme stocks │
│        (used as INVERSE signal for these 22 events)                  │
└──────────────────────────────────────────────────────────────────────┘
```

---

### 13.7 Regime-Aware Signal Direction

A critical insight from the research: **the same mover can be informative in one market regime and contrarian in another**. The system must detect regime and apply appropriate signal direction:

```
PlanB (@100trillionUSD) — Stock-to-Flow BTC model:
  2019–2021 (bull regime):  model predictions directionally correct → FOLLOW
  2022–2023 (bear regime):  predictions consistently too bullish → FADE / DISCOUNT
  Status: credibility score dropped from 0.78 to 0.31 after 2022

@CathieWood (ARK Invest):
  2017–2021 (low-rate growth regime):  high conviction calls correct → FOLLOW
  2022 (rate-hike regime):  growth thesis collapses → calls wrong → FADE
  Status: regime dependency flag set; signal weight = high in low-rate, low in rate-hike

Rule: when RegimeShift detected → re-evaluate all mover credibility scores
      apply regime tag to each score: bull_accuracy, bear_accuracy, high_vol_accuracy
      route signal through regime-conditional weight lookup
```

---

### 13.8 Implementation — Retroactive Attribution Engine (C#)

```csharp
public class RetroactiveAttributionEngine
{
    // Triggered after any significant price move is detected
    public async Task<MoveAttributionRecord> AttributeMoveAsync(
        string symbol,
        DateTimeOffset moveOnset,
        decimal moveMagnitude,
        MoveDirection direction,
        TimeSpan lookbackWindow,     // how far back to scan content
        CancellationToken ct)
    {
        // 1. Fetch all content signals in lookback window
        var signals = await _signalStore.QueryAsync(
            symbol: symbol,
            from: moveOnset - lookbackWindow,
            to: moveOnset.AddHours(-1),      // exclude noise right at move start
            ct: ct);

        // 2. Score each signal against actual move
        var attributions = signals.Select(s => new MoverAttribution(
            MoverId: s.MoverId,
            SignalTimestamp: s.Timestamp,
            LeadTimeHours: (moveOnset - s.Timestamp).TotalHours,
            SignalDirection: s.Direction,
            CorrectlyPredicted: s.Direction == ToSignalDirection(direction),
            SignalStrength: s.CredibilityWeight * s.ReachScore,
            ContentSnippet: s.TextSummary
        )).ToList();

        // 3. Update mover profiles with new data points
        foreach (var attribution in attributions)
            await _profileRegistry.UpdateWithAttributionAsync(attribution, ct);

        // 4. Detect inverse regime shifts
        await _regimeClassifier.ReevaluateAsync(
            attributions.Select(a => a.MoverId).Distinct(), symbol, ct);

        var record = new MoveAttributionRecord(
            Symbol: symbol,
            MoveOnset: moveOnset,
            MoveMagnitude: moveMagnitude,
            Direction: direction,
            Attributions: attributions
        );

        await _attributionStore.SaveAsync(record, ct);
        return record;
    }
}
```

---

### 13.9 Research References — Movement Attribution & Inverse Signals

- [Detecting Informed Trading in Options Markets (ResearchGate)](https://www.researchgate.net/publication/228207854_Detecting_Informed_Trading_Activities_in_the_Options_Markets)
- [Detecting Abnormal Trading Activities in Option Markets (ScienceDirect)](https://www.sciencedirect.com/science/article/abs/pii/S0927539815000262)
- [Informed Options Trading Before FDA Advisory Meetings (ScienceDirect)](https://www.sciencedirect.com/science/article/abs/pii/S092911992300144X)
- [Informed Options Strategies Before Corporate Events (ScienceDirect)](https://www.sciencedirect.com/science/article/abs/pii/S1386418122000568)
- [Uncovering Insiders and Alpha on Polymarket with AI (Hacker News / arxiv)](https://news.ycombinator.com/item?id=47091557)
- [Inverse Cramer Strategy — Quiver Strategies](https://www.quiverquant.com/strategies/s/Inverse%20Cramer/)
- [Can (Inverse) Jim Cramer Generate Alpha? — Finance Club](https://www.financeclub.ch/blog/can-inverse-jim-cramer-generate-alpha)
- [Investor Sentiment and Market Movements: Granger Causality (arxiv, 2025)](https://arxiv.org/pdf/2510.15915)
- [AI-Powered Detection of Insider Trading Activities (ResearchGate, 2025)](https://www.researchgate.net/publication/390764456_AI-Powered_Detection_of_Insider_Trading_Activities_in_Financial_Market)
- [Shadow Trading Detection: Graph-Based Surveillance (ScienceDirect, 2025)](https://www.sciencedirect.com/article/pii/S1544612325017787)
- [Explainable Assessment of Financial Experts' Credibility (arxiv, 2024)](https://arxiv.org/abs/2406.11924)
- [Going Against the Crowd: Contrarian Strategies (Bookmap)](https://bookmap.com/blog/going-against-the-crowd-in-trading-contrarian-strategies-for-market-success)

---

*Document version: 0.4 — Added retroactive attribution, inverse signals, and pre-move detection sections*
*Last updated: 2026-03-05*
