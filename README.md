# DataAgent Platform 🚀

DataAgent is an end-to-end automated platform that processes user raw data (CSV/Excel) and generates comprehensive PDF analytical reports. The platform integrates a C# back-end for data ingestion, an N8N workflow orchestrator for interacting with AI (Gemini/Claude), and a Python visualization microservice, delivered through a sleek React frontend.

## 🏗 Architecture Diagram

```ascii
                      +---------------------------------+
                      |         React Frontend          |
                      |   (Vite + TS + Tailwind CSS)    |
                      +---------------------------------+
                             |    ^
               REST API (Upload)  |  SignalR (Progress / WebSocket)
                             v    |
   +-----------------------------------------------------------+
   |                       DataAgent API                       |
   |              (ASP.NET Core 8 Web API / C#)                |
   |                                                           |
   |  - Controllers: File, Analysis, Health Check              |
   |  - Services: Rate Limiting, Serilog, SignalR Hub          |
   |  - Storage: SQL Server (EF Core), MinIO (S3)              |
   +-----------------------------------------------------------+
             |                               ^ (Callback)
             v (Trigger Workflow via HTTP)   |
   +-----------------------------------------------------------+
   |                       n8n Orchestrator                    |
   |                                                           |
   | 1. Receives File URL + User Prompt                        |
   | 2. Connects to LLM (Anthropic / Gemini) to write Python   |
   | 3. Triggers Chart Service with Python code                |
   | 4. Generates PDF Report string / Returns via Webhook      |
   +-----------------------------------------------------------+
             |
             v (Send Code to run safely)
   +-----------------------------------------------------------+
   |                     Chart Microservice                    |
   |             (Python FastAPI + WeasyPrint)                 |
   |                                                           |
   | - Executes sandboxed Python code by AI                    |
   | - Renders charts using Matplotlib / Seaborn               |
   | - Compiles HTML/CSS to PDF using WeasyPrint               |
   +-----------------------------------------------------------+

                           Infrastructure
   [ SQL Server ]   [ Redis ]    [ MinIO Storage ]   [ Hangfire ]
```

## 📋 Prerequisites

Before you start, ensure you have the following installed on your machine:
- **Docker & Docker Compose** (For running infrastructure services)
- **.NET 8 SDK** (For building and running the Backend API)
- **Node.js 20+ & npm** (For the React Frontend)
- **Python 3.12+** (For the Chart Service, if running outside Docker)

## ⚡ Quick Start

You can bring up the entire environment entirely locally using these 3 commands:

**1. Start Infrastructure (Background)**
```bash
docker-compose up -d sqlserver redis minio n8n setup-minio
```
*Wait ~10 seconds for SQL Server and MinIO to fully initialize.*

**2. Start the Backend API**
```bash
cd src/DataAgent.API
dotnet run
```

**3. Start the Frontend Application**
```bash
# In a separate terminal
cd src/DataAgent.Frontend
npm install
npm run dev
```
👉 Open your browser at: `http://localhost:5173`

*(Note: The `chart-service` can also be brought up via docker using `docker-compose up -d chart-service`)*

## 🔐 Environment Variables (`.env` Reference)

A `.env` file should be located at the root of your project or injected during deployment. Example configuration:

```env
# Database
SA_PASSWORD=YourStrong@Passw0rd
REDIS_PASSWORD=YourRedisPass

# MinIO
MINIO_ROOT_USER=root
MINIO_ROOT_PASSWORD=Th1s1sS3cureP@ssword!
MINIO_ENDPOINT=localhost:9000

# N8N & Application Secrets
N8N_ENCRYPTION_KEY=my-encryption-key
WEBHOOK_URL=http://host.docker.internal:5678/webhook/analysis-trigger
N8N_AUTH_TOKEN=my-secret-token
API_CALLBACK_SECRET=SUPER_SECRET_HMAC_KEY_123456789012345

# AI API Keys
ANTHROPIC_API_KEY=sk-ant-xxxxxxxxxx
```

## 🔌 API Endpoints
*Full documentation is available via Swagger at `http://localhost:5196/swagger` when running in Development mode.*

- **Health Checks**
  - `GET /health` : Liveness check. Returns 200 OK.
  - `GET /health/ready` : Readiness check. Verifies SQL Server, Redis, and MinIO connectivity.
- **File Management** *(Rate Limit: 10 req / min)*
  - `POST /api/files/upload` : Uploads an Excel/CSV file to MinIO and returns `jobId` & `fileUrl`.
- **Analysis** *(Rate Limit: 5 req / min)*
  - `POST /api/analysis/start` : Triggers the N8N workflow processing.
  - `GET /api/analysis/{jobId}/status` : Get execution status & SignalR progress.
  - `POST /api/analysis/callback` : Secured entrypoint for N8N Webhook callbacks.
- **Reporting** *(Rate Limit: 20 req / hour)*
  - `POST /api/analysis/{jobId}/send-email` : Queues an email delivery task with Hangfire.

## 🛠 Troubleshooting

**1. Port Conflicts**
If you see an error that port `1433` (SQL) or `5196` (API) is already in use, verify active processes using `netstat -ano | findstr <port>` and terminate them, or update the `docker-compose.yml` to map to alternative ports (e.g., `1434:1433`).

**2. SignalR Failing to Negotiate**
Ensure the CORS policy inside `Program.cs` allows `http://localhost:5173` and `AllowCredentials` is enabled. You may see a fallback to LongPolling if WebSockets is blocked by antivirus or VPNs.

**3. MinIO Bucket Refuses Uploads**
If `setup-minio` container failed in Docker Compose, the `dataagent` bucket won't exist. Manually access `http://localhost:9001` (login: root / Th1s1sS3cureP@ssword!) and create a bucket named `dataagent`. Set its access policy to `public`.

**4. Migrations Not Applied**
If you receive SQL Exceptions about missing tables, ensure Entity Framework ran its update. Stop the API, run:
`dotnet ef database update --project ..\DataAgent.Infrastructure --startup-project .`

---
*Built securely. Engineered for performance.*
