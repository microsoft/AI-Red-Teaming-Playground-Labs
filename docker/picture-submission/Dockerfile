# docker build -f docker/picture-submission/Dockerfile -t picture-submission .

FROM mcr.microsoft.com/devcontainers/javascript-node:20-bookworm AS builder-webapp
ARG PAT
WORKDIR /app
COPY src/picture-submission/webapp/package.json picture-submission/webapp/package-lock.json* ./
RUN npm ci

COPY src/picture-submission/webapp/ .
RUN rm -f .env && \
  npm run build

# python
FROM mcr.microsoft.com/cbl-mariner/base/python:3
WORKDIR /app
RUN tdnf install -y ca-certificates-microsoft file-libs && tdnf clean all
COPY src/picture-submission/webapi/ .
RUN pip install --no-cache-dir --upgrade -r requirements.txt &&  \
  pip install gunicorn && \
  mkdir -p build
COPY --from=builder-webapp /app/build /app/build
COPY docker/picture-submission/entrypoint.py /app/entrypoint.py
ENTRYPOINT ["python3", "-u", "entrypoint.py"]