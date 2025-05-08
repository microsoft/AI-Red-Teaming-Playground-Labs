# docker build -f docker/webapi/Dockerfile -t chat-copilot-webapi .

# webapp builder
FROM mcr.microsoft.com/devcontainers/javascript-node:20-bookworm AS builder-webapp
ARG PAT
WORKDIR /app
COPY src/chat-copilot/webapp/ .
RUN rm -f .env && \
  yarn install \
  --prefer-offline \
  --frozen-lockfile \
  --non-interactive \
  --production=false
RUN yarn build

# webapi builder
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS builder-webapi
ARG PAT
WORKDIR /source
# copy everything else and build app
COPY src/chat-copilot/webapi webapi
COPY src/chat-copilot/shared shared
RUN cd webapi && \
  dotnet restore --use-current-runtime && \
  apt-get update && apt-get install -y wget && \
  wget -P data https://raw.githubusercontent.com/tesseract-ocr/tessdata/main/eng.traineddata && \
  dotnet publish --use-current-runtime --self-contained false --no-restore -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=builder-webapi /app .
RUN apt-get update && \
  apt-get install -y libleptonica-dev libtesseract-dev libc6-dev libjpeg62-turbo-dev libgdiplus && \
  ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5 x64/libleptonica-1.82.0.so && \
  ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.4.0.1 x64/libtesseract50.so && \
  mkdir -p /app/wwwroot
COPY --from=builder-webapp /app/build /app/wwwroot

ENTRYPOINT ["./CopilotChatWebApi"]