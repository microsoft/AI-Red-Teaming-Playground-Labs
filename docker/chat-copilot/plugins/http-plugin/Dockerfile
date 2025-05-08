# docker build -f docker/plugins/http-plugin/Dockerfile -t http-plugin .

# http plugin builder
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder-http-plugin
ARG PAT
WORKDIR /source
COPY src/chat-copilot/plugins/http-plugin http-plugin
RUN cd http-plugin && \
  dotnet restore --use-current-runtime && \
  dotnet publish --use-current-runtime --self-contained false --no-restore -o /app && \
  sed -i -e 's/localhost:5084/http-plugin-service:4000/g' wwwroot/.well-known/ai-plugin.json

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=builder-http-plugin /app .
COPY --from=builder-http-plugin /source/http-plugin/wwwroot /app/wwwroot

RUN useradd -u 1000 aspnet
USER aspnet
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Kestrel__Endpoints__Http__Url=http://0.0.0.0:4000

ENTRYPOINT ["./HttpPlugin"]