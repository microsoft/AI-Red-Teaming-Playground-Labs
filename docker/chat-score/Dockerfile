# docker build -f docker/chat-score/Dockerfile -t chat-score .

FROM mcr.microsoft.com/devcontainers/javascript-node:20-bookworm AS builder-webapp
ARG PAT
WORKDIR /app
COPY src/chat-score/webapp/package.json chat-score/webapp/package-lock.json* ./
RUN npm ci

COPY src/chat-score/webapp/ .
RUN rm -f .env && \
  npm run build

# python
FROM mcr.microsoft.com/cbl-mariner/base/python:3
WORKDIR /app
RUN tdnf install -y ca-certificates-microsoft && tdnf clean all
COPY src/chat-score/webapi/ .
RUN pip install --no-cache-dir --upgrade -r requirements.txt &&  \
  pip install gunicorn && \
  mkdir -p build
COPY --from=builder-webapp /app/build /app/build
COPY docker/chat-score/entrypoint.py /app/entrypoint.py
ENTRYPOINT ["python3", "-u", "entrypoint.py"]