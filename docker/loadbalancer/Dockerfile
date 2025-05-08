FROM mcr.microsoft.com/oss/go/microsoft/golang:1.22-cbl-mariner2.0 AS builder

WORKDIR /app
COPY src/loadbalancer/go.mod ./
RUN go mod download

COPY src/loadbalancer/ ./
RUN CGO_ENABLED=0 GOOS=linux GOARCH=amd64 go build -ldflags="-w -s" -o /loadbalancer cmd/server/main.go

FROM scratch
# Add the required certificates
COPY --from=builder /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/
COPY --from=builder /loadbalancer /loadbalancer
COPY --from=builder /app/config-example.yaml /config.yaml

EXPOSE 8080
ENTRYPOINT [ "/loadbalancer" ]