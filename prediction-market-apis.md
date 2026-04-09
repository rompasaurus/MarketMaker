# Prediction Market APIs & Real-Time Trend Data — Technical Reference

*Research document — MarketMaker project*
*Last updated: 2026-04-08*

---

## 1. Overview

This document catalogs every actionable prediction market API, real-time trend data source, and cross-platform aggregator relevant to MarketMaker. Each entry includes base URLs, authentication methods, rate limits, data freshness, historical data availability, pricing, and integration notes. The goal is to map a concrete path from "zero data" to "unified real-time prediction market + discourse feed" with the least cost and friction.

---

## 2. Primary Prediction Market APIs

### 2.1 Polymarket — Largest by Volume, Best API Surface

Polymarket is a blockchain-based (Polygon) prediction market and currently the highest-volume platform globally. It exposes four distinct APIs plus WebSocket streaming, and all read-only endpoints are unauthenticated.

**Architecture:**

| API | Base URL | Auth Required | Purpose |
|-----|----------|---------------|---------|
| Gamma | `https://gamma-api.polymarket.com` | No | Market metadata, event descriptions, enriched info |
| Data | `https://data-api.polymarket.com` | No | Positions, trades, portfolio data |
| CLOB | `https://clob.polymarket.com` | Read: No / Write: Yes | Orderbook, prices, price history, order placement |
| Bridge | `https://bridge.polymarket.com` | Yes | Deposits and withdrawals |

**Key REST Endpoints (CLOB):**

```
GET  /markets         → list markets with metadata
GET  /prices          → current token prices
GET  /book            → orderbook depth for a token
GET  /midpoint        → midpoint price
GET  /price-history   → historical price series
POST /order           → place order (authenticated)
DELETE /order         → cancel order (authenticated)
```

**Authentication (two-tier, only required for trading):**

- **L1 (Wallet):** EIP-712 signature from an Ethereum private key
- **L2 (API Key):** HMAC-SHA256 credentials — requires 5 headers per request:
  - `POLY_API_KEY`
  - `POLY_SIGNATURE`
  - `POLY_TIMESTAMP`
  - `POLY_NONCE`
  - `POLY_PASSPHRASE`

**WebSocket Streaming:**

| Channel | Endpoint | Data |
|---------|----------|------|
| Market | `wss://ws-subscriptions-clob.polymarket.com/ws/market` | Orderbook updates, trades |
| User | `wss://ws-subscriptions-clob.polymarket.com/ws/user` | Order fills, position changes |

WebSocket connections do not count against REST rate limits.

**Rate Limits:** ~10–100 requests/second depending on endpoint. Order placement: ~5–10/sec.

**Historical Data:** Full price history available via `/price-history`. Gamma API provides enriched event/market metadata for historical reconstruction.

**SDKs:**

| Language | Package | Notes |
|----------|---------|-------|
| Python | `py-clob-client` | 500K+ monthly PyPI downloads |
| TypeScript | `@polymarket/clob-client` | Official |
| Rust | `rs-clob-client` | Official |

**Pricing:** Free for all data endpoints. No paid tiers. Only trading fees (~2% on open interest) apply when executing trades.

**Data Freshness:** Real-time via WebSocket. REST is near-real-time.

**Integration Notes:**
- Best-documented prediction market API available
- CLOB (Central Limit Order Book) architecture means real orderbook depth data, not just last price
- Prices represent probabilities directly (0.00–1.00)
- Polymarket acquired Dome (aggregator) in early 2026 — may expand data surface

---

### 2.2 Kalshi — CFTC-Regulated, Institutional-Grade

Kalshi is the only federally regulated (CFTC) prediction market exchange in the US. Offers both REST and WebSocket APIs with a full demo sandbox environment.

**Environments:**

| Environment | REST Base URL | WebSocket |
|-------------|---------------|-----------|
| Production | `https://api.elections.kalshi.com/trade-api/v2` | `wss://api.elections.kalshi.com/trade-api/ws/v2` |
| Demo/Sandbox | `https://demo-api.kalshi.co/trade-api/v2` | `wss://demo-api.kalshi.co/trade-api/ws/v2` |

**Key REST Endpoints:**

```
GET  /markets                 → list all markets (ticker, price, volume, status)
GET  /markets/{ticker}        → single market details
GET  /markets/{ticker}/orderbook → order book snapshot
GET  /events                  → event groupings
GET  /series                  → market series
POST /portfolio/orders        → place limit order
GET  /portfolio/orders        → list your orders
DELETE /portfolio/orders/{id} → cancel order
GET  /portfolio/balance       → cash balance (in cents)
GET  /portfolio/positions     → current holdings
GET  /portfolio/settlements   → settlement history and P&L
GET  /historical/markets      → settled markets (older than 3 months)
GET  /historical/trades       → historical trade feed
GET  /historical/fills        → user's historical fills
GET  /historical/orders       → older order records
GET  /account/limits          → check your rate tier
```

**Authentication:** RSA-PSS (Probabilistic Signature Scheme) with 2048-bit PKCS#8 keys. Three custom headers per request:

```
KALSHI-ACCESS-KEY        → API key ID
KALSHI-ACCESS-SIGNATURE  → RSA-PSS signature (base64), signing "{timestamp}{HTTP_METHOD}{path}"
KALSHI-ACCESS-TIMESTAMP  → Unix timestamp in milliseconds
```

**WebSocket Channels:**

| Type | Channels |
|------|----------|
| Public | `orderbook_delta`, `ticker`, `trade`, `market_lifecycle_v2` |
| Private | `fill`, `user_orders`, `market_positions`, `order_group_updates` |

Note: WebSocket connection requires auth handshake even for public channels.

**Rate Limits (Tiered):**

| Tier | Read/sec | Write/sec | Qualification |
|------|----------|-----------|---------------|
| Basic | 20 | 10 | Free on signup |
| Advanced | 30 | 30 | Application form |
| Premier | 100 | 100 | 3.75% monthly exchange volume + tech demo |
| Prime | 400 | 400 | 7.5% monthly exchange volume + tech demo |

**Historical Data:** Available via `/historical/*` endpoints. Data partitioned by a cutoff timestamp (query via `GET /historical/cutoff`).

**Price Format (March 2026 update):** Prices are now fixed-point dollar strings with up to 4 decimal places (e.g., `"0.6500"`). Legacy integer cent fields have been removed. Some markets support subpenny ticks ($0.001) and fractional contracts.

**SDKs:** Python (sync/async), TypeScript, community Go client.

**Pricing:** Free API access. No API fees — only trading fees apply.

**Regulatory Notes:**
- CFTC-regulated — US residents can trade
- No geographic restrictions on data API access
- Trading requires KYC-verified account
- FIX 4.4 protocol available for institutional low-latency trading

**Integration Notes:**
- RSA-PSS auth is more complex than HMAC but well-documented
- Demo sandbox is excellent for development — mirrors production exactly
- Regulated status means data is high-trust (no wash trading concerns)
- Historical endpoints are the best of any prediction market for backtesting

---

### 2.3 Manifold Markets — Fully Open, Play-Money, Best for Prototyping

Manifold is a play-money prediction market (Mana currency). Fully open-source. The most permissive API for development and testing.

**Base URL:** `https://api.manifold.markets/v0`

*(Legacy `https://manifold.markets/api` is deprecated)*

**Key REST Endpoints:**

```
GET  /v0/markets              → list all markets (paginated, sortable)
GET  /v0/market/{id}          → single market with answers
GET  /v0/slug/{slug}          → market by URL slug
GET  /v0/search-markets       → search/filter with 12+ sort options
GET  /v0/market/{id}/prob     → current probability (1s cache)
GET  /v0/market-probs         → batch probabilities for multiple markets
POST /v0/market               → create market (costs M$25–250)
POST /v0/bet                  → place bet or limit order
POST /v0/multi-bet            → batch YES bets on multi-choice
POST /v0/market/{id}/sell     → sell shares
GET  /v0/bets                 → list bets (max 1000)
GET  /v0/market/{id}/positions → market positions
GET  /v0/users                → list users
GET  /v0/me                   → authenticated user info
GET  /v0/comments             → list comments
GET  /v0/txns                 → transactions
```

**Authentication:** `Authorization: Key {api_key}` header. Key generated from user profile settings. Many endpoints are public (no auth needed for read).

**WebSocket Streaming:**

| Endpoint | `wss://api.manifold.markets/ws` |
|----------|------|
| Global topics | `global/new-bet`, `global/new-contract`, `global/new-comment`, `global/updated-contract` |
| Per-market | `contract/{id}/new-bet`, `contract/{id}/new-comment`, `contract/{id}/orders` |
| Per-user | `user/{id}` |

Requires ping every 30–60 seconds (disconnects after 60s without ping).

**Rate Limits:** 500 requests/minute per IP.

**Historical Data:** Full bet history and market history available via API. Portfolio history with daily/weekly/monthly/allTime granularity.

**Pricing:** Completely free. Play money only.

**Data Licensing:** Commercial AI/ML training prohibited without license. Contact `data@manifold.markets`.

**Integration Notes:**
- Best API for building and testing your normalization layer before connecting paid sources
- Play-money, but represents real crowd wisdom — useful as a signal
- Fully open-source: you can read their API implementation if you need to understand behavior
- WebSocket requires keepalive pings — handle in your connection manager

---

### 2.4 Metaculus — Forecasting Aggregation Platform

Metaculus is a forecasting platform, not a trading market. It aggregates community predictions with track record scoring. Useful for long-horizon forecast data and calibration benchmarking.

**Base URL:** `https://www.metaculus.com/api2/`

**API Docs:** OpenAPI spec at `https://www.metaculus.com/api2/schema/redoc/`

**Key Endpoints:**

```
GET  /questions/                → list questions (filterable by publish_time, resolve_time, project)
GET  /questions/{id}/           → single question with forecast data
POST /questions/{id}/predict/   → submit prediction
POST /questions/bulk-predict/   → batch predictions
GET  /predictions/              → list predictions
GET  /rankings/                 → user rankings
GET  /categories/               → question categories
GET  /projects/                 → projects
GET  /projectstats/             → project statistics
GET  /comments/                 → comments
GET  /question-summaries/{id}/  → AI-generated summaries
GET  /user-profiles/            → user list
```

**Authentication:** Three methods supported:
- Token auth: `Authorization: Token <token>` (primary)
- Basic auth
- Cookie auth

Many read endpoints are public.

**Rate Limits:** Not formally documented. Community reports suggest moderate limits. Contact `api-requests@metaculus.com` for high-volume access.

**Historical Data:** Full question history with forecast distributions and resolution data.

**Pricing:** Free for non-commercial research. Contact for commercial/high-volume use.

**Data Freshness:** Near-real-time for community median forecasts. No order book or tick data (not a trading market).

**Integration Notes:**
- Best used as a calibration reference — compare your model's forecasts against Metaculus community median
- Forecast distributions (not just point estimates) are available
- Long-horizon questions (months to years) complement Polymarket/Kalshi's shorter-term markets
- No trading mechanics — purely informational

---

### 2.5 PredictIt — Survived CFTC Shutdown, Limited API

PredictIt received an amended CFTC no-action letter in July 2025 and relaunched with a redesigned platform in March 2026. API remains minimal.

**Base URL:** `https://www.predictit.org/api/marketdata/`

**Endpoints:**

```
GET /all/           → all markets with current prices
GET /markets/{id}   → single market data
```

**Authentication:** None required (public read-only).

**Rate Limits:** Strict — approximately 1 request per second.

**Historical Data:** Not available via API. Requires CSV downloads from the website.

**Pricing:** Free but very limited.

**Limitations:**
- Read-only — no programmatic trading
- No WebSocket — polling only
- Updates approximately every 60 seconds
- Non-commercial use only — must attribute PredictIt as source

**Integration Notes:**
- Weakest API of the five primary platforms
- Useful only as a supplementary cross-reference data point
- Political market focus — good for election/policy markets specifically
- Do not build critical functionality around this API

---

## 3. Emerging Prediction Market Platforms

### 3.1 Drift Protocol (B.E.T) — Solana

Decentralized prediction markets on Solana via the Drift DEX.

| Detail | Value |
|--------|-------|
| Access | SDK-based (`driftpy` for Python, `@drift-labs/sdk` for TypeScript, `drift-rs` for Rust) |
| Data API Playground | `https://www.drift.trade/developers/data-api` |
| Self-hosted Gateway | `https://github.com/drift-labs/gateway` |
| Market Type | Binary outcome (YES=1, NO=0) |
| Rate Limits | Not formally documented |

No dedicated prediction market REST API — access via SDK or self-hosted gateway. Blockchain-native complexity.

### 3.2 Predict.fun — BNB Chain

| Detail | Value |
|--------|-------|
| Testnet API | `https://api-testnet.predict.fun` (no credentials needed) |
| Mainnet API Key | Request via Discord |
| SDKs | Python (`predict-sdk`), TypeScript (`@predictdotfun/sdk`) |
| Rate Limit | 240 requests/minute |
| Features | WebSocket support, order book access |

### 3.3 Opinion — Blockchain-Native

- Crypto wallet authentication required
- Taker-only fee model (makers pay zero)
- $20M funding (Hack VC, Jump Crypto)
- Focus: macro events, crypto, culture, geopolitical outcomes

**Assessment:** These platforms are lower priority. Smaller markets, blockchain-native complexity, and less mature APIs. Defer to Phase 3+ unless targeting crypto-native users.

---

## 4. Cross-Platform Aggregator APIs

These services unify multiple prediction markets into a single normalized feed. They can significantly reduce integration work on the normalization layer.

### 4.1 Prediction Hunt v2

| Detail | Value |
|--------|-------|
| Base URL | `https://www.predictionhunt.com/api/v2/` |
| Covers | Kalshi, Polymarket, PredictIt, ProphetX, Opinion |
| Auth | Bearer token API key; demo endpoints free without auth |
| Free Tier | 1,000 requests/month |
| Key Features | Cross-platform market matching, arbitrage detection, normalized schemas |

### 4.2 FinFeedAPI

| Detail | Value |
|--------|-------|
| Covers | Polymarket, Kalshi, Myriad, Manifold Markets |
| Data | Normalized market metadata, order book snapshots, trades/quotes, OHLCV history |
| Access | REST API, read-only (no trade execution) |

### 4.3 PMXT (Launched January 2026)

| Detail | Value |
|--------|-------|
| Covers | Multiple venues |
| Key Feature | **Actual trade execution** across venues (limit orders, balance checks) |
| Data | Candles, order books, trade history, real-time price streaming |
| Status | Newest entrant — still maturing |

### 4.4 Oddpool

| Detail | Value |
|--------|-------|
| Covers | Cross-venue odds aggregation |
| Data | Live odds, spreads, liquidity, orderbook depth |
| Key Feature | Arbitrage opportunity detection |
| Historical | Yes |

**Integration Note:** Dome (previously an independent aggregator) was acquired by Polymarket in early 2026 and no longer operates independently.

**Recommendation:** Prediction Hunt v2 or FinFeedAPI could serve as MarketMaker's initial normalization layer, reducing the work needed to unify Polymarket + Kalshi + Manifold into a common schema. Evaluate whether their normalization matches your target schema before building a custom one.

---

## 5. Real-Time Trend Data APIs

These pair with prediction market data to build the discourse-precedes-price signal layer described in `trend-analysis.md`.

### 5.1 GDELT (Global Database of Events, Language, and Tone)

The highest-value free source for global discourse monitoring.

**Classic API:**

| Detail | Value |
|--------|-------|
| Base URL | `https://api.gdeltproject.org/` |
| Auth | None |
| Update Frequency | Every 15 minutes |
| Coverage | 65+ languages, 215+ countries |
| Historical | Back to 1979 |
| Cost | Free |

**GDELT Cloud (2025+):**

| Detail | Value |
|--------|-------|
| Base URL | `https://docs.gdeltcloud.com` |
| Auth | API key |
| Update Frequency | Hourly |
| Coverage | 100+ languages, entities linked to Wikipedia with sentiment |
| Historical | Back to January 2025 |
| Tiers | Free, Analyst (paid), Professional (paid) |

Key data points per event: `AvgTone`, `GoldsteinScale`, `NumArticles`, `NumSources`, `NumMentions`, `Actor1/Actor2`, `EventCode` (CAMEO taxonomy).

### 5.2 Google Trends

No reliable official public API exists. The alpha API launched in 2025 has limited endpoints.

**Best Programmatic Access Options:**

| Tool | Type | Cost | Notes |
|------|------|------|-------|
| **trendspyg** | Open-source Python library | Free | Modern replacement for pytrends, most reliable free option |
| **SerpApi Google Trends** | Commercial API | ~$75/mo (5K credits) | Structured JSON, ~$0.015/query |
| **Glimpse** | Commercial API | Enterprise pricing | Most reliable provider, used by Fortune 50, native Python support |
| **Trends MCP** | API | Free tier: 100 req/day | 15+ sources beyond just Google Trends |

**Note:** `pytrends` (the old standby) is effectively deprecated — breaks 1–4 times per year when Google changes its frontend. Do not depend on it.

### 5.3 X (Twitter) API

**Pricing (February 2026 change):** Replaced fixed tiers with pay-per-use for new signups.

| Tier | Cost | Volume | Notes |
|------|------|--------|-------|
| Free | $0 | Severely limited | Not viable for market intelligence |
| Basic (legacy) | $100/mo | 10K tweet reads | Minimum viable for cashtag tracking |
| Pro (legacy) | $5,000/mo | 1M tweet reads | Required for meaningful sentiment analysis |
| Enterprise (legacy) | $42,000+/mo | Full firehose | Institutional only |
| Pay-per-use (new) | Variable | Capped at 2M reads/mo | Cost-effective for write-heavy; for read-heavy market intelligence, likely $100–5,000/mo range |

### 5.4 Reddit API

| Detail | Value |
|--------|-------|
| Base URL | `https://oauth.reddit.com/` |
| Auth | OAuth 2.0 required for all access |
| Free Tier | 100 requests/minute, non-commercial use only |
| Commercial | ~$12,000/year for 100 RPM |

**Integration Notes:**
- Free tier is workable for MVP if polling selectively (r/wallstreetbets, r/stocks, r/CryptoCurrency)
- At 100 req/min, you can poll ~6 subreddits every 10 seconds — sufficient for mention velocity signals
- Commercial license required if MarketMaker is ever monetized

### 5.5 Other Trend Sources (from trend-analysis.md)

| Source | API | Cost | Latency |
|--------|-----|------|---------|
| StockTwits | REST API | Free + paid tiers | Seconds |
| HackerNews | Algolia API | Free | Minutes |
| Wikipedia edits | MediaWiki API | Free | Minutes |
| SEC EDGAR | Full-text search API | Free | Minutes |
| Benzinga | REST API | $50–200/mo | Seconds |
| Unusual Whales | REST API | Paid | Minutes |

---

## 6. Data Normalization Strategy

All prediction market sources express probabilities differently. The normalization layer must convert everything to a common schema.

**Target Schema:**

```
{
  "market_id":       string,       // internal unique ID
  "source":          enum,         // polymarket | kalshi | manifold | metaculus | predictit
  "source_id":       string,       // platform-native ID
  "question":        string,       // human-readable question text
  "category":        string,       // politics | crypto | economics | sports | science | other
  "probability":     float,        // 0.0 to 1.0 (YES probability)
  "volume_usd":      float | null, // 24h volume in USD (null for play-money)
  "liquidity_usd":   float | null, // available liquidity in USD
  "open_interest":   float | null, // total open interest in USD
  "last_trade_price": float,       // 0.0 to 1.0
  "bid":             float,        // best bid
  "ask":             float,        // best ask
  "spread":          float,        // ask - bid
  "resolution_date": datetime,     // expected resolution
  "status":          enum,         // open | closed | resolved
  "resolved_value":  float | null, // 1.0 (YES) or 0.0 (NO) or null if unresolved
  "timestamp":       datetime,     // data capture time (UTC)
  "metadata":        object        // source-specific extra fields
}
```

**Source-Specific Conversions:**

| Source | Native Format | Conversion |
|--------|---------------|------------|
| Polymarket | Token price 0.00–1.00 | Direct mapping |
| Kalshi | Dollar string `"0.6500"` | Parse as float |
| Manifold | Probability 0.0–1.0 | Direct mapping |
| Metaculus | Community median (various distributions) | Extract point estimate |
| PredictIt | Cents 1–99 → share price | Divide by 100 |

---

## 7. Recommended Integration Roadmap

### Phase 1 — Foundation (Free, Start Immediately)

All free, no API keys needed for read-only:

1. **Polymarket CLOB + Gamma APIs** — richest data, best documented, real-time WebSocket
2. **Kalshi demo sandbox** — build and test against regulated data with zero risk
3. **Manifold Markets** — fully open, test normalization layer cheaply
4. **GDELT classic API** — 15-minute global news sentiment, free, no auth
5. **PredictIt** `/api/marketdata/all/` — minimal effort supplementary source

**Deliverable:** Unified data ingestion workers pulling from all five sources into TimescaleDB with normalized schema. Real-time WebSocket connections to Polymarket and Kalshi. GDELT polling on 15-minute cadence.

### Phase 2 — Signal Depth

6. **Prediction Hunt v2 or FinFeedAPI** — evaluate as cross-platform normalization layer (may replace custom normalizer)
7. **trendspyg** — Google Trends data for search interest signals
8. **Reddit (free tier)** — PRAW streaming for r/wallstreetbets, r/stocks, r/CryptoCurrency
9. **Metaculus** — forecast aggregation and calibration benchmarking
10. **Kalshi production API** — switch from sandbox to live data

**Deliverable:** Cross-platform market matching (same question across platforms). Divergence detection (Polymarket says 0.65, Kalshi says 0.58 → signal). Google Trends + Reddit mention velocity overlaid on prediction market odds charts.

### Phase 3 — Full Coverage (Requires Budget)

11. **X/Twitter API** — cashtag stream ($100–5K/mo depending on volume)
12. **Reddit commercial tier** — if MarketMaker is monetized (~$12K/year)
13. **StockTwits + Benzinga** — financial-native social and news
14. **GDELT Cloud** — higher update frequency, richer entity linking
15. **Drift / Predict.fun / Opinion** — blockchain-native markets for crypto-focused users

**Deliverable:** Full discourse-to-prediction-market correlation engine. Cross-platform consensus signals. Prediction market odds as a leading indicator overlaid on price charts with social sentiment.

---

## 8. API Access Summary Matrix

| Platform | Free Read | Free Trade | WebSocket | Historical | Auth Complexity | Rate Limit (Free) | Priority |
|----------|-----------|------------|-----------|------------|-----------------|-------------------|----------|
| Polymarket | Yes | No (wallet needed) | Yes | Yes | None (read) / HMAC (write) | ~10–100/sec | **P1** |
| Kalshi | Yes | Sandbox only | Yes | Yes | RSA-PSS | 20 read/sec | **P1** |
| Manifold | Yes | Yes (play money) | Yes | Yes | API key header | 500/min | **P1** |
| Metaculus | Yes | N/A | No | Yes | Token header | Moderate | P2 |
| PredictIt | Yes | No | No | No (via API) | None | ~1/sec | P2 |
| Prediction Hunt | Demo free | N/A | No | No | Bearer token | 1K/month | P2 |
| GDELT Classic | Yes | N/A | No | Yes (1979+) | None | Unlisted (generous) | **P1** |
| Google Trends | Via workarounds | N/A | No | Yes | Varies | Varies | P2 |
| Reddit | Yes | N/A | No | Yes | OAuth 2.0 | 100/min | P2 |
| X / Twitter | Paid only | N/A | Yes (filtered) | Yes | OAuth 2.0 | Tier-dependent | P3 |

---

## 9. Key Takeaways

1. **You can build the entire MVP prediction market data layer for free.** Polymarket, Kalshi (sandbox), Manifold, GDELT, and PredictIt all offer free read access with no API keys needed (or free keys).

2. **Polymarket is the primary source.** Best documentation, highest volume, real-time WebSocket, full orderbook depth, historical price data, and three official SDKs.

3. **Aggregator APIs exist and may save significant normalization work.** Prediction Hunt v2 and FinFeedAPI already cross-match markets across platforms — evaluate before building a custom normalizer.

4. **GDELT is the discourse backbone.** Free, global, 15-minute updates, covers 65+ languages, proven Sharpe ratios in academic backtests. No other free source comes close for news/event sentiment.

5. **Google Trends has no reliable free API.** Use `trendspyg` (free, open-source) or SerpApi ($75/mo). Do not depend on `pytrends`.

6. **X/Twitter is expensive but important.** The fastest-decaying sentiment signals live here. Budget $100–5K/mo depending on volume needs. Defer to Phase 3.

7. **Cross-platform odds divergence is a signal itself.** When Polymarket says 0.65 and Kalshi says 0.58 on the same question, one of them is wrong — or an arbitrage opportunity exists. The aggregator APIs can detect this automatically.

---

*Document version: 0.1 — Initial API research*
*Last updated: 2026-04-08*
