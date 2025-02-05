# Build stage
FROM golang:1.22-alpine3.19 AS builder
WORKDIR /app
COPY . .
RUN go build -o main main.go

# Run stage
FROM alpine:3.19
WORKDIR /app
COPY --from=builder /app/main .
COPY app.env .
COPY start.sh .
COPY wait-for.sh .
COPY db/migration ./db/migration

RUN apk add --no-cache dos2unix netcat-openbsd

RUN chmod +x start.sh && \
    chmod +x wait-for.sh && \
    dos2unix start.sh && \
    dos2unix wait-for.sh

EXPOSE 8080 9090
CMD [ "/app/main" ]
ENTRYPOINT [ "/app/start.sh" ]
