# MarketMaker — Project Plan

## Vision

A comprehensive, real-time market intelligence platform that unifies prediction markets, traditional financial markets, and social/media signals into a single customizable dashboard. The platform provides deep historical analysis, trend and pattern matching, event mapping, and a full-featured trading simulator with realistic fee modeling and market-impact estimation.

---

## Feature & Functionality List

### 1. Data Ingestion & Integration

#### 1.1 Prediction Market Data
- **Polymarket** — real-time and historical odds via REST + WebSocket
- **Kalshi** — regulated US prediction market data
- **Manifold Markets** — community prediction markets (open API)
- **PredictIt** — political/event markets
- **Metaculus** — aggregate forecasting platform
- Unified normalization layer: convert all probability formats to a common 0–1 float scale

#### 1.2 Traditional Financial Markets
- **Equities** — real-time quotes, OHLCV, order book depth (Alpaca, Polygon.io, Yahoo Finance fallback)
- **Crypto** — spot prices, perpetuals, funding rates (Binance, Coinbase Advanced, Kraken, CoinGecko)
- **Forex** — major and exotic pairs (OANDA, ExchangeRate.host)
- **Commodities** — gold, oil, nat gas, agricultural (Quandl / FRED)
- **Indices** — S&P 500, NASDAQ, Dow, VIX, global indices
- **Options** — Greeks, open interest, implied volatility surface (Tradier, Unusual Whales)
- **Derivatives** — futures open interest, funding rates

#### 1.3 Macro & Economic Data
- FRED (Federal Reserve Economic Data) — CPI, unemployment, GDP, M2, Fed Funds Rate
- US Treasury yield curve (daily)
- Central bank calendars (FOMC, ECB, BOJ, BOE)
- Earnings calendar (EPS estimates vs actuals)
- IPO and secondary offering calendar

#### 1.4 Social & Media Signals
- **Reddit** — subreddit post volume, sentiment, upvote velocity (r/wallstreetbets, r/stocks, r/CryptoCurrency, etc.)
- **X (Twitter)** — keyword/cashtag mention volume, engagement metrics, trending topics
- **StockTwits** — bullish/bearish sentiment ratio, message volume
- **YouTube** — video publish volume per ticker/topic, comment sentiment
- **Google Trends** — search interest over time per ticker/keyword
- **News aggregation** — RSS ingest from Reuters, Bloomberg headlines, AP, CoinDesk, The Block
- **NLP sentiment pipeline** — classify media and social content as positive / neutral / negative with confidence score

---

### 2. Dashboard & Visualization

#### 2.1 Customizable Layout
- Drag-and-drop widget system (grid-based, resizable panels)
- Save/load named dashboard layouts (profiles)
- Light / dark / high-contrast themes
- Full-screen / focus mode per widget
- Multi-monitor / multi-tab awareness

#### 2.2 Core Widgets
| Widget | Description |
|---|---|
| Price Chart | Candlestick / line / Heikin-Ashi with multi-timeframe (1m → 1W) |
| Prediction Market Odds | Probability curve over time per question/market |
| Order Book Depth | Real-time L2 bid/ask visualization |
| Volume Profile | Volume at price (VAP) histogram |
| Sentiment Gauge | Composite social/media sentiment score (–100 to +100) |
| News Feed | Filtered, real-time news with inline sentiment tag |
| Event Timeline | Overlay macroeconomic, earnings, political events on any chart |
| Correlation Matrix | Heatmap of asset correlations over selectable window |
| Options Flow | Unusual options activity feed with size and strike |
| Macro Dashboard | CPI, yield curve, VIX, Fed Funds tracker |
| Watchlist | Multi-asset list with live P&L, % change, sparklines |
| Heatmap | Sector/market cap heatmap (like Finviz) |
| Pattern Scanner | Detected technical and behavioral patterns across assets |

#### 2.3 Cross-Asset Overlays
- Overlay prediction market odds on top of price charts (dual Y-axis)
- Overlay social sentiment score on price chart
- Overlay media event markers (news, tweets, Reddit spikes) directly on candles
- Correlation view: show how two assets move relative to each other over time

---

### 3. Historical Data

#### 3.1 Storage
- Time-series database (TimescaleDB or InfluxDB) for OHLCV, odds, sentiment scores
- PostgreSQL relational store for metadata, events, user data
- S3-compatible object storage for raw media/news archives
- Data retention: tick-level 30 days, 1-minute 2 years, daily unlimited

#### 3.2 Historical Backreference
- Full historical odds for all supported prediction markets
- Reconstructed order book snapshots (where available)
- Social volume and sentiment history aligned to price timeline
- News/event database with timestamps and asset tags

#### 3.3 Export
- CSV / JSON / Parquet export for any dataset or time range
- API endpoint for external tooling access

---

### 4. Trend Analysis & Pattern Matching

#### 4.1 Technical Patterns
- Classic chart patterns: head & shoulders, double top/bottom, cup & handle, wedges, flags, triangles
- Candlestick patterns: engulfing, doji, hammer, shooting star, morning/evening star
- Support & resistance zone detection (automatic horizontal levels)
- Fibonacci retracement and extension auto-draw
- Multi-timeframe trend detection (primary, intermediate, short)

#### 4.2 Statistical & Quantitative Analysis
- Rolling correlation (price vs. sentiment, price vs. odds, two assets)
- Autocorrelation and seasonality detection
- Volatility regime detection (low / normal / high / crisis)
- Momentum and mean-reversion signal indicators
- Z-score anomaly detection on price, volume, and sentiment
- Granger causality test between any two data series

#### 4.3 Behavioral Patterns
- "Crowd surge" detection — rapid social volume + engagement spike preceding price move
- Pump-and-dump signature scanning (volume spike → rapid reversal)
- Prediction market divergence — odds move before price moves (or vice versa)
- Earnings whisper drift — price drift into earnings vs. consensus
- VIX crush / volatility event patterns

#### 4.4 Pattern Library & Alerts
- User-buildable custom pattern definitions
- Alert on pattern confirmation (push notification, email, in-app)
- Historical hit rate / precision / recall for each detected pattern

---

### 5. Media & Social Event Mapping

#### 5.1 Event Tagging
- Each media item (article, tweet, Reddit post, YouTube video) is tagged with:
  - Detected asset tickers / topics
  - Sentiment score
  - Reach/engagement score (views, upvotes, retweets)
  - Source credibility weight
- Events are stored with precise UTC timestamp

#### 5.2 Price Impact Attribution
- For a selected asset and time window, surface the top N media events that coincide with significant price moves (>X% in Y minutes)
- Show pre-event vs. post-event price trajectory
- Engagement-weighted event impact score
- Compare event impact across similar historical events ("last 5 times this account tweeted about TSLA…")

#### 5.3 Social Momentum Indicators
- **Mention Velocity** — rate of change in ticker mentions over rolling window
- **Sentiment Momentum** — change in sentiment score over time (bullish trending vs. bearish trending)
- **Influencer Signal** — weighted mention score from accounts with high historical price correlation
- **Cross-platform consensus** — when Reddit + X + news align on a direction

---

### 6. Trading Simulator

#### 6.1 Paper Trading Environment
- Simulated portfolio with configurable starting capital (any currency)
- Supports: equities, crypto, prediction market positions, options
- Real-time P&L with unrealized and realized tracking
- Position sizing (shares / contracts / dollars / % of portfolio)
- Long and short positions

#### 6.2 Order Types
- Market order (fills at best available ask/bid)
- Limit order (queued, fills when price crosses limit)
- Stop-loss / stop-limit
- Trailing stop (absolute $ or % based)
- Bracket orders (entry + take-profit + stop-loss in one)
- Time-in-force: GTC, DAY, IOC, FOK

#### 6.3 Fee & Cost Modeling

##### Equities
| Venue | Commission | SEC Fee | FINRA TAF |
|---|---|---|---|
| Robinhood / Webull | $0 | $0.000008 × proceeds | $0.000145 × shares |
| TD Ameritrade / Schwab | $0 | same | same |
| Interactive Brokers | $0.005/share (min $1) | same | same |
| Custom | User-defined | — | — |

##### Crypto
| Exchange | Maker | Taker | Withdrawal |
|---|---|---|---|
| Coinbase Advanced | 0.00%–0.40% | 0.05%–0.60% | Network fee |
| Binance US | 0.00%–0.10% | 0.10% | Network fee |
| Kraken | 0.00%–0.16% | 0.10%–0.26% | Network fee |
| Custom | User-defined | — | — |

##### Prediction Markets
- Kalshi: 5–7% fee on winnings
- Polymarket: ~2% on open interest
- User-configurable for any platform

#### 6.4 Market Impact Modeling
- **Trade Size vs. Market Cap ratio** — flag when order size exceeds configurable % of daily volume or market cap
- **Slippage estimation** — modeled from order book depth at time of order; linear and square-root market impact models available
- **Price impact tiers:**

| Trade Size / ADV | Estimated Slippage Model |
|---|---|
| < 0.1% | Negligible (< 1 bps) |
| 0.1% – 1% | Linear interpolation from spread |
| 1% – 5% | Square-root impact model |
| > 5% | Almgren-Chriss optimal execution model |

- **Prediction market depth** — model impact of large YES/NO orders against the CLOB or AMM curve
- Show "expected fill price" vs. "mid price" before order submission
- Show estimated post-trade price impact (shift in market after your trade)

#### 6.5 Portfolio Analytics
- Overall return (absolute and %, time-weighted and money-weighted)
- Sharpe ratio, Sortino ratio, Calmar ratio
- Max drawdown and drawdown duration
- Beta to SPY / BTC (user-selectable benchmark)
- Win rate, average winner/loser, profit factor
- Trade log with full entry/exit details, fees paid, slippage experienced
- Equity curve chart with drawdown overlay

#### 6.6 Simulation Modes
- **Real-time simulation** — execute against live market data as if trading live
- **Historical backtest** — replay a date range, execute orders against historical OHLCV
- **Stress test** — inject scenario (2008 crash, COVID drop, Luna collapse) and measure portfolio impact
- **Monte Carlo** — run N randomized scenarios based on asset volatility to project outcome distribution

---

### 7. Alerts & Notifications

- Price level alerts (cross above/below)
- Percentage move alerts (X% in Y minutes)
- Prediction market odds threshold alerts
- Sentiment score threshold alerts
- Pattern detection alerts
- Volume spike alerts
- News keyword alerts
- Delivery: in-app, browser push, email, optional Webhook/Slack/Discord integration

---

### 8. User Accounts & Personalization

- Auth: email/password + OAuth (Google, GitHub)
- Multiple watchlists (named, tagged)
- Saved dashboard layouts
- Alert history and management
- Portfolio history across sessions
- API key management (user's own API keys for data sources)
- Usage tier (free / pro / institutional)

---

### 9. Technical Architecture (Proposed)

```
┌─────────────────────────────────────────────────────────┐
│                     Frontend (SPA)                      │
│   React + TypeScript │ TradingView Charting Library     │
│   Zustand state │ React Query │ Tailwind CSS            │
└────────────────────────┬────────────────────────────────┘
                         │ REST + WebSocket (SignalR)
┌────────────────────────▼────────────────────────────────┐
│                    API Gateway / BFF                    │
│              C# / ASP.NET Core + SignalR                │
└──┬──────────────────────────────────────────────────────┘
   │
   ├── Data Ingestion Workers (C# hosted services)
   │   ├── Market Data Worker  (Polygon, Alpaca, Binance WS)
   │   ├── Prediction Market Worker  (Polymarket, Kalshi)
   │   ├── Social Media Worker  (Reddit, Twitter API v2)
   │   ├── News/RSS Worker
   │   └── Macro/FRED Worker
   │
   ├── Analytics Engine (C# + Python interop where needed)
   │   ├── Pattern detection (TA-Lib.NET + custom)
   │   ├── NLP sentiment pipeline (ML.NET / OpenAI API)
   │   ├── Correlation & causality engine (MathNet.Numerics)
   │   └── Market impact calculator (MathNet + custom)
   │
   ├── Trading Simulator & Backtest Engine (C#)
   │   ├── LEAN engine (QuantConnect, open source C#)
   │   ├── Order matching, fee modeling, slippage
   │   └── Monte Carlo & stress test runner
   │
   └── Databases
       ├── TimescaleDB  (time-series: OHLCV, odds, sentiment)
       ├── PostgreSQL   (users, events, alerts, portfolios)
       ├── Redis        (caching, pub/sub for real-time)
       └── S3           (raw archives, exports)
```

---

### 10. Development Phases

#### Phase 1 — Foundation (MVP)
- [ ] Project scaffolding (monorepo: React frontend + ASP.NET Core backend + worker services)
- [ ] Database schema design (TimescaleDB + PostgreSQL via Npgsql / EF Core)
- [ ] Core data workers: equities (Polygon/Alpaca), crypto (Binance), 1 prediction market (Polymarket)
- [ ] Basic REST + SignalR WebSocket API
- [ ] Frontend: login, basic price chart widget, watchlist
- [ ] Paper trading: market/limit orders, fee model, basic P&L

#### Phase 2 — Data Breadth
- [ ] Add remaining prediction market sources
- [ ] Social media ingestion (Reddit + X)
- [ ] News/RSS pipeline + basic sentiment tagging
- [ ] Historical data backfill pipeline
- [ ] Event timeline overlay on charts
- [ ] Expanded fee model for all venues

#### Phase 3 — Analysis Layer
- [ ] Technical pattern detection engine
- [ ] Social sentiment dashboard widgets
- [ ] Price-to-event attribution view
- [ ] Correlation matrix widget
- [ ] Market impact model (slippage estimator in simulator)
- [ ] Alert system (in-app + email)

#### Phase 4 — Advanced Simulation
- [ ] Historical backtest engine
- [ ] Options simulation (Greeks, IV surface)
- [ ] Stress test scenarios
- [ ] Monte Carlo projection
- [ ] Full portfolio analytics (Sharpe, drawdown, etc.)

#### Phase 5 — Polish & Scale
- [ ] Customizable dashboard drag-and-drop
- [ ] Dashboard layout save/load
- [ ] Multi-user support, API key management, usage tiers
- [ ] Mobile-responsive layout
- [ ] Performance optimization (data pagination, virtual lists)
- [ ] Export to CSV/Parquet

---

### 11. Open Questions / Decisions

- **Primary charting library**: TradingView Lightweight Charts (free) vs. full TradingView widget (paid) vs. Recharts + custom
- **Primary language for backend**: C# / ASP.NET Core — chosen for performance, quant finance ecosystem (MathNet.Numerics, LEAN), and SignalR for real-time WebSocket
- **Prediction market depth data**: Polymarket provides CLOB via API; Kalshi requires account — need to confirm access
- **X (Twitter) API tier**: Basic ($100/mo) vs. Pro ($5000/mo) — sentiment depth depends on tier
- **Self-hosted NLP vs. API**: Running a local FinBERT model vs. calling OpenAI API for sentiment — cost vs. latency tradeoff
- **Regulatory considerations**: Simulator must be clearly labeled as non-financial-advice; no actual order routing

---

*Document version: 0.1 — Initial planning draft*
*Last updated: 2026-03-05*
