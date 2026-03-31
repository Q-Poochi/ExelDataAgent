# DataAgent Automated Reporting

This repository contains the backend components for DataAgent, a system designed to automate analysis, reporting, and emailing processes using a combination of ASP.NET Core, Python, and n8n workflows.

## Architecture

- **`DataAgent.API`**: Core backend written in ASP.NET Core (.NET 8.0). Handles job queuing, SignalR notifications, Webhooks, and rate limiting.
- **`chart-service`**: Python microservice (FastAPI + WeasyPrint). Responsible for converting tabular data and generating beautifully formatted PDF reports.
- **`n8n`**: Automated workflow engine used to integrate LLM providers (Anthropic Claude/Google Gemini) and execute the actual analysis orchestration.
- **Support Services**: Microsoft SQL Server, Redis, and MinIO (S3-compatible storage).

## Local Development Setup

We use Docker Compose to spin up the entire ecosystem quickly.

### Prerequisites
- [Docker & Docker Compose](https://docs.docker.com/get-docker/) installed.
- Git.

### Setup Instructions

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd DAagent
   ```

2. **Configure Environment Variables:**
   Copy the provided `.env.example` file to `.env`:
   ```bash
   cp .env.example .env
   ```
   Open the `.env` file and fill in your actual credentials (Passwords, API Keys, Email settings).

3. **Start the Infrastructure:**
   Run the following command in the root of the project to build and start all 6 services:
   ```bash
   docker-compose up -d --build
   ```

4. **Verify the Services:**
   - **C# API (Swagger)**: `http://localhost:8080/swagger`
   - **Chart Service**: `http://localhost:8000`
   - **n8n Dashboard**: `http://localhost:5678`
   - **MinIO Console**: `http://localhost:9001` (Login with `admin` / `S3cureP@ssword!`)

5. **Initialize Storage:**
   Log into the MinIO Console (`http://localhost:9001`) and create a bucket named: `data-agent-reports`. Make sure the access policy is set to `public` if you want direct download links.

### Shutting Down
To stop all services and preserve data:
```bash
docker-compose down
```
To stop all services and **wipe all data** (delete volumes):
```bash
docker-compose down -v
```

## Production Deployment (Kubernetes)

For staging and production environments, we provide Kubernetes manifests located in the `infra/k8s` directory.

### Quick Start (K8s)
Ensure you have `kubectl` configured pointing to your cluster.

1. **Apply Namespace:**
   ```bash
   kubectl apply -f infra/k8s/namespace.yaml
   ```
2. **Setup Configurations:**
   Edit `infra/k8s/configmap.yaml` and `infra/k8s/secrets.yaml` with your production values, then apply:
   ```bash
   kubectl apply -f infra/k8s/configmap.yaml
   kubectl apply -f infra/k8s/secrets.yaml
   ```
3. **Deploy Support Infrastructure:**
   ```bash
   kubectl apply -f infra/k8s/sqlserver-statefulset.yaml
   kubectl apply -f infra/k8s/minio-statefulset.yaml
   kubectl apply -f infra/k8s/redis-deployment.yaml
   ```
4. **Deploy Application Services:**
   ```bash
   kubectl apply -f infra/k8s/n8n-deployment.yaml
   kubectl apply -f infra/k8s/chart-service-deployment.yaml
   kubectl apply -f infra/k8s/api-deployment.yaml
   ```
5. **Setup Networking:**
   ```bash
   kubectl apply -f infra/k8s/networkpolicy.yaml
   kubectl apply -f infra/k8s/ingress.yaml
   ```

## CI/CD Pipeline
A GitHub Actions workflow is defined in `.github/workflows/ci.yml`. It will automatically trigger on pushes and Pull Requests to the `main` branch to evaluate code health and verify Docker build integrity.
