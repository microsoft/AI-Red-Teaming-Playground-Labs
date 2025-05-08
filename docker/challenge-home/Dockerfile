# builder
FROM mcr.microsoft.com/devcontainers/javascript-node:20-bookworm AS builder-webapp
ARG PAT
WORKDIR /app
COPY src/challenge-home/webapp/ .
RUN yarn install \
  --prefer-offline \
  --frozen-lockfile \
  --non-interactive \
  --production=false
RUN yarn build

# python
FROM mcr.microsoft.com/cbl-mariner/base/python:3
WORKDIR /app
RUN tdnf install -y ca-certificates-microsoft && tdnf clean all
COPY src/challenge-home/webapi/ .
RUN pip install --no-cache-dir --upgrade -r requirements.txt &&  \
  pip install gunicorn && \
  mkdir -p build
COPY --from=builder-webapp /app/build /app/build
COPY docker/challenge-home/entrypoint.py /app/entrypoint.py
ENTRYPOINT ["python3", "-u", "entrypoint.py"]