# MyQuiz

MyQuiz is an AI-powered platform for creating and practicing quizzes. Users upload a document (PDF, DOCX, PPTX or TXT) and the system turns it into a deck of quiz questions. Users can then take quizzes, study with flashcards, share decks with others, and discover and rate decks from the community. Paid subscription plans unlock higher AI usage limits and extra features, with payment by bank-transfer QR code.

This repository is a monorepo with two apps:

- **`backend/`**: an ASP.NET Core 8 Web API built with Clean Architecture and CQRS
- **`frontend/`**: an Angular 21 single-page app with server-side rendering (SSR)

---

## Table of contents

- [Features](#features)
- [Business rules](#business-rules)
- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [Repository layout](#repository-layout)
- [Getting started](#getting-started)
- [Building with Docker](#building-with-docker)
- [History](#history)

---

## Features

### For learners and creators

| Area | What users can do |
|---|---|
| **AI deck generation** | Upload a document and get a deck with a name, description, tags and questions. Progress streams to the browser in real time over Server-Sent Events (SSE), so questions appear as each part of the document is processed. |
| **Manual editing** | Create a deck from scratch, or review and edit an AI-generated deck before saving it. Decks can be saved as a **draft** or **published**. |
| **Question types** | Multiple choice (one or more correct answers), true/false, and fill-in-the-blank, each with an optional hint and explanation. |
| **Quiz mode** | Take a quiz with configurable options: number of questions, question types, shuffle, timer and hints. Answers are graded on the server. |
| **Learn mode** | Flashcard-style study. Each card moves through *new → learning → mastered* until the whole deck is mastered. |
| **History** | Every quiz attempt is stored with its score and time taken. Users can open any past attempt and review each answer. |
| **Library** | One place for the user's own decks, drafts, decks shared with them, decks they saved, and decks they have attempted. |
| **Explore and search** | Search public decks, browse personalized **recommended** decks, **trending** decks and **hot tags**. |
| **Sharing** | Set a deck to public, private or shared, and invite people by email. |
| **Ratings** | Rate public decks from 1 to 5 stars. |
| **PDF export** | Download a deck as a formatted PDF, on plans that include it. |
| **Dashboard** | Personal stats and recently viewed decks. |
| **Account** | Register with email (with email verification) or sign in with Google. Forgot/reset password, change password, and profile and avatar updates. |

### For administrators

- **Dashboard:** summary statistics, a revenue chart, and how users are spread across plans
- **Users:** search and filter users, ban and unban them, and assign roles
- **Subscription plans:** create, update, deactivate and reorder plans
- **Decks and transactions:** browse all decks and every payment transaction

---

## Business rules

### Decks: status and visibility

Each deck has a **status** and a **visibility**, and the server checks both every time someone opens a deck:

| | Who can view it |
|---|---|
| `Draft` (any visibility) | Only the owner. A draft has not been published yet. |
| `Published` + `Public` | Everyone. Only these decks show up in search, recommendations, trending and hot tags. |
| `Published` + `Shared` | The owner and members who accepted an invitation |
| `Published` + `Private` | Only the owner |

### Sharing by invitation

- Only the deck owner can send invitations. An owner cannot invite themselves, someone who is already a member, or an email that already has a pending invitation.
- The invitee receives an email with a link containing a unique token. The invitation **expires after 7 days**.
- Only a signed-in user **whose email matches the invitation** can accept it. Accepting the invitation makes them a `DeckMember`, which gives them access to the shared deck.

### AI question generation

1. **Quota check:** the user's active plan is looked up first (see [Subscriptions](#subscriptions-and-quotas)). If the user has already used all of today's generations, the request is rejected before any AI call is made.
2. **Text extraction:** text is pulled from the upload with PdfPig (PDF) and OpenXml (DOCX/PPTX), or read directly (TXT).
3. **Chunking:** the text is split into chunks of about 15,000 characters, breaking at paragraph boundaries where possible.
4. **Generation:**
   - The first chunk produces the deck's metadata (name, description, tags) plus the first questions.
   - The remaining chunks are processed **in parallel, up to 5 at a time**.
   - Calls to OpenAI are retried with Polly (exponential backoff, and the server's `Retry-After` header is respected).
5. **Early stop:** once the plan's `MaxQuestionsPerGenerate` is reached, the remaining chunks are skipped and the result is trimmed to the limit.
6. **Post-generation validation:** every question is cleaned up after generation:
   - Questions whose type does not match their options are reclassified, for example a "multiple choice" question whose only options are True and False.
   - True/false options are normalized.
   - Options are removed from fill-in-the-blank questions.
   - Each correct answer must be one of the question's options.
7. **Usage counting:** the daily usage counter is **incremented only after generation succeeds**, so a failed AI call does not use up quota.
8. **Nothing is saved automatically.** The user reviews and edits the generated deck in the frontend, then saves it as a draft or publishes it.

The SSE endpoint (`POST /api/decks/generate/stream`) sends these events in order: `start` (number of chunks, question limit), `metadata`, a `questions` event for each processed chunk, and finally `complete`. If anything fails, an `error` event is sent instead.

### Subscriptions and quotas

A **subscription plan** defines:

| Field | Meaning |
|---|---|
| `DailyGenerateLimit` | AI generations allowed per day (`-1` = unlimited) |
| `MaxQuestionsPerGenerate` | Maximum questions per generation (`-1` = unlimited) |
| `HasExportToPdf` | Whether PDF export is allowed |
| `Price`, `Duration` | Price, and length of the subscription in days |
| `Order` | Plan tier, where a higher number means a higher plan |
| `IsActive` | Whether the plan is offered for purchase |

- If a user holds several subscriptions that are active at the same time, the one with the **highest `Order`** applies.
- Daily usage is counted in **Redis** with the key `gen_limit:{userId}:{yyyy-MM-dd}`. The key expires automatically at **midnight UTC**, so quotas reset every day.
- Users can check their remaining quota at any time (`GET /api/quota`).

### Payments (bank transfer via SePay)

1. **Create an order:** the user picks a paid plan, and the backend creates a `Pending` transaction with a unique order code (`MQSUB` + timestamp + random digits) that **expires after 15 minutes**.
   - Free plans cannot be purchased.
   - **Downgrades are blocked:** a user cannot buy a plan of the same or a lower tier while their current plan is still active.
   - If the user already has a pending, unexpired order for the same plan, that order is reused and its expiry is extended. No duplicate order is created.
2. **Pay:** the frontend shows a bank-transfer QR code. The order code is the transfer description.
3. **Confirm:** SePay calls the webhook (`POST /api/payments/sepay-webhook`, protected by an API key). The backend then:
   - ignores outgoing transfers
   - reads the order code from the transfer description
   - rejects the payment if the order has expired or the amount received is less than the plan price
   - otherwise marks the transaction `Completed` and starts a new subscription that lasts `Duration` days
4. **Expire:** a background job runs every 5 minutes and marks unpaid orders past their expiry as `Expired`.

### Quiz attempts and grading

- Answers are graded **on the server**. An answer counts as correct only if the set of answers given exactly matches the set of correct answers (order does not matter).
- Each stored answer keeps a **snapshot** of the question at the time of the attempt (content, type, options, correct answers, hint, explanation). Past attempts therefore still display correctly after the deck is edited or a question is deleted. Questions are soft-deleted.

### Discovery ranking

- **Recommended decks:**
  - **Interest profile:** built from the user's recent activity. Each action gives weight to the tags of the deck involved: views ×1, saves ×3, quiz attempts ×4. Activity from the last 7 or 30 days counts extra (×1.5 or ×1.2).
  - **Candidates:** public, published decks owned by other users.
  - **Ranking:** decks are ordered by how well their tags match the interest profile. A popularity score (saves, views, attempts, ratings) plus a freshness bonus breaks ties.
- **Trending decks:**
  - **Recent engagement:** views, saves and attempts from the last 30 days carry the most weight.
  - **Overall popularity:** all-time views, saves, attempts and ratings count as well.
  - **Bonuses:** decks tagged with a current hot tag, and recently updated decks, score higher.
- **Hot tags:** the most frequent tags across public, published decks.

### Authentication and security

- **Accounts:** ASP.NET Identity with the roles `Admin` and `User`. Email must be confirmed before a user can sign in.
- **Tokens:**
  - **Access tokens:** short-lived JWTs.
  - **Refresh tokens:** stored in **Redis** (`rt:{token}`) with their own expiry and **rotated** on every refresh.
- **Logout:** revokes the refresh token and adds the access token's ID (JTI) to a Redis **blacklist** until that token would have expired.
- **Frontend handling:** an HTTP interceptor catches `401` responses, refreshes the token silently, and retries the original request.
- **Banning:** banned users are locked out through Identity lockout.
- **Error handling:** errors are mapped to consistent JSON responses by a global exception middleware.

---

## Tech stack

| | Backend | Frontend |
|---|---|---|
| Framework | ASP.NET Core 8 (.NET 8) | Angular 21 (standalone components, signals, SSR) |
| Architecture | Clean Architecture + CQRS with MediatR | Lazy-loaded routes, guards, HTTP interceptors |
| Data | PostgreSQL (EF Core 8, Npgsql), Redis | n/a |
| Auth | ASP.NET Identity, JWT bearer, Google Sign-In | `@abacritt/angularx-social-login` |
| AI | OpenAI | n/a |
| Documents | PdfPig, DocumentFormat.OpenXml (input), QuestPDF (PDF export) | n/a |
| Storage | AWS S3 (uploaded documents, thumbnails, avatars) | n/a |
| Other | MailKit (email), Polly (retries), Swagger | Tailwind CSS 4, Chart.js, SweetAlert2, Vitest |

---

## Architecture

### Backend: Clean Architecture

```
Presentation  ──►  Application  ──►  Domain
      │                 ▲
      └──► Infrastructure ┘
```

| Layer | Responsibility |
|---|---|
| **Domain** | Entities (`Deck`, `Question`, `QuizAttempt`, `UserAnswer`, `DeckMember`, `DeckInvitation`, `DeckRating`, `SavedDeck`, `DeckView`, `SubscriptionPlan`, `UserSubscriptionPlan`, `PaymentTransaction`), enums and constants. No dependencies. |
| **Application** | Use cases written as MediatR **commands and queries**, grouped by feature (`Decks`, `QuizAttempts`, `Payments`, `Admin`, ...), plus DTOs, application exceptions and the interfaces Infrastructure implements (`IAiService`, `IDocumentService`, `IRateLimitService`, repositories, ...). |
| **Infrastructure** | EF Core `DbContext`, migrations and repositories, Identity and token services, OpenAI, S3, email, PDF and Redis implementations, and background jobs. |
| **Presentation** | Thin controllers that send requests to MediatR, middleware (exception handling, request logging), the standard `ApiResponse` wrapper, and `Program.cs`. |

### Frontend

- **`pages/`:** one folder per screen, all loaded lazily through `app.routes.ts`
- **`layouts/`:** the main app layout and the admin layout
- **`services/`:** API clients (`auth`, `deck`, `quiz`, `payment`, `admin`, `google-auth`)
- **`guards/`:**
  - `authGuard`: pages that require sign-in
  - `noAuthGuard`: pages for signed-out users only, such as login and register
  - `adminGuard`: admin pages
  - `unsavedChangesGuard`: warns before leaving a deck that is being edited
- **`interceptors/`:**
  - `base-url`: prepends the API base URL to requests
  - `auth`: attaches the access token and refreshes it on `401`

---

## Repository layout

```
myquiz-monorepo/
├── backend/
│   ├── MyQuizGenerator.Domain/           # Entities, enums, constants
│   ├── MyQuizGenerator.Application/      # Commands/queries (MediatR), DTOs, interfaces
│   ├── MyQuizGenerator.Infrastructure/   # EF Core, Identity, OpenAI, S3, email, Redis, jobs
│   ├── MyQuizGenerator.Presentation/     # Controllers, middleware, Program.cs, Dockerfile
│   └── MyQuizGenerator.sln
└── frontend/
    ├── src/app/
    │   ├── pages/                        # dashboard, library, search, add-deck, quiz, learn, admin, ...
    │   ├── services/  guards/  interceptors/  layouts/  models/
    │   └── app.routes.ts
    ├── Dockerfile
    └── vercel.json
```

---

## Getting started

### Prerequisites

- .NET 8 SDK
- Node.js 22+ and npm
- PostgreSQL and Redis
- Optional, depending on which features you want to try: an OpenAI API key, an AWS S3 bucket, an SMTP account, a Google OAuth client ID, and a SePay account

### Backend

1. Configure `backend/MyQuizGenerator.Presentation/appsettings.json`. Keep secrets out of git by putting them in `appsettings.Development.json` or in [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets).

   | Section | Contents |
   |---|---|
   | `ConnectionStrings` | `DefaultConnection` (PostgreSQL) and `Redis` |
   | `JwtSettings` | Secret key, issuer, audience, token lifetimes |
   | `EmailSettings` | SMTP settings |
   | `GoogleSettings` | Google OAuth client ID |
   | `StorageSettings` | S3 bucket, region, access keys |
   | `OpenAiSettings` | API key and model |
   | `PaymentSettings` | Receiving bank account details and the SePay webhook API key |
   | `AppSettings.ClientBaseUrl` | Frontend URL, used for CORS and for links in emails |

2. Create the database and run the API:

   ```bash
   cd backend
   dotnet ef database update -p MyQuizGenerator.Infrastructure -s MyQuizGenerator.Presentation
   dotnet run --project MyQuizGenerator.Presentation
   ```

   The API runs at `http://localhost:5230`, with Swagger UI at `http://localhost:5230/swagger`. On first startup it seeds the default roles and an admin account (see `Infrastructure/Persistence/DatabaseSeeder.cs`). **Change the admin password** in any shared environment.

### Frontend

```bash
cd frontend
npm install
npm start
```

Then open `http://localhost:4200`. The API base URL and the Google client ID are set in `src/environments/environment.development.ts` for development and in `environment.ts` for production builds.

Run the unit tests with `npm test` (Vitest).

---

## Building with Docker

- **Backend:** build the image from the `backend/` folder. The container listens on port `8080`.
  ```bash
  docker build -f MyQuizGenerator.Presentation/Dockerfile -t myquiz-api ./backend
  ```
- **Frontend:** build the image from `frontend/Dockerfile` to run the SSR build on Node. The app can also be deployed to Vercel with `frontend` as the root directory, using the included `vercel.json`.

---

## History

This monorepo merges the two original repositories, `myquiz-be` and `myquiz-fe`. The full commit history of both is preserved under `backend/` and `frontend/`.
