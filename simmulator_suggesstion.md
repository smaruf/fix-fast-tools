If you've already implemented the **DSE/Nasdaq FIX client side**, and your goal is simply:

```text
Run an executable
      ↓
Accept FIX connection
      ↓
Receive NewOrderSingle
      ↓
Generate ExecutionReport
      ↓
Optionally perform matching
```

then you don't need another FIX library. You need a **simulated exchange server**.

A few practical options:

### Option 1: B2BITS FIX Antenna Demo Server

[B2BITS FIX Antenna Demo](https://b2bits.com/fixopaedia/fix-antenna-java/?utm_source=chatgpt.com)

Pros:

* Ready-made FIX acceptor.
* Supports order routing simulation.
* Commonly used in capital markets testing.

Cons:

* Commercial product.
* Demo limitations.

---

### Option 2: QuickFIX/J Executor Example

[QuickFIX/J Examples](https://github.com/quickfix-j/quickfixj/tree/master/quickfixj-examples/executor?utm_source=chatgpt.com)

This is probably the closest to what you want.

It acts as:

```text
FIX Client
    |
    | NewOrderSingle
    v
Executor Application
    |
    +-- Ack
    +-- Fill
    +-- Reject
```

Many people use it to test OMS/FIX integrations before connecting to a real exchange.

---

### Option 3: Exchange-Core + QuickFIX/J Gateway

[Exchange Core](https://github.com/exchange-core/exchange-core?utm_source=chatgpt.com)

If you want actual matching:

```text
Client A Buy
Client B Sell
        ↓
Matching
        ↓
ExecutionReport(Fill)
```

This is the closest to a real exchange.

---

### Option 4: Nasdaq Testing Simulator

Since DSE's FIX gateway is Nasdaq-based, another possibility is using Nasdaq's certification/UAT environment if you have access through DSE. Those environments typically provide:

* Logon
* Order entry
* Rejects
* Execution reports
* Session management

without requiring a live exchange connection.

---

### My recommendation for your situation

Because you've already built the FIX client and are debugging messages like:

```text
35=D
35=8
35=3
40=1
40=2
```

the fastest path is:

1. Clone the QuickFIX/J executor example.
2. Configure it as an Acceptor (`FGW`).
3. Point your DSE-FIX client (`FCSLFIX1`) at it.
4. Customize the execution logic to:

   * ACK all orders
   * Reject invalid OrdType values
   * Generate partial fills/full fills

This gives you a lightweight "exchange" in under an hour.

If you need something that behaves more like the **DSE Nasdaq matching engine** (price-time priority, order book, partial fills, cancel/replace), then Exchange-Core with a FIX gateway is the better fit. It requires more setup but produces realistic exchange behavior.
