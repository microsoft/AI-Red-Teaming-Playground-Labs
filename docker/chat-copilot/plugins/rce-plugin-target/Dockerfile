# docker build -f docker/plugins/rce-plugin-target/Dockerfile -t rce-plugin-target .

FROM mcr.microsoft.com/devcontainers/python:dev-3.12-bullseye

RUN adduser --system --no-create-home coderunner

WORKDIR /app
COPY src/chat-copilot/plugins/rce-plugin/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY src/chat-copilot/plugins/rce-plugin .

RUN sed -i "s/localhost:5000/rce-challenge-service:8000/g" static/.well-known/ai-plugin.json

# Copy the second flag
COPY docker/plugins/rce-plugin-target/flag.txt .

USER coderunner
EXPOSE 5000
CMD [ "python", "app.py" ]

