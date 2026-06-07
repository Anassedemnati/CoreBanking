#!/bin/bash
set -e

KAFKA_BIN=/opt/kafka/bin
BOOTSTRAP=kafka:9092

echo "Waiting for Kafka..."
# Already depends_on kafka:healthy, but give it a moment
sleep 2

echo "Creating topics..."

# Regular topic — savings account lifecycle events
$KAFKA_BIN/kafka-topics.sh --bootstrap-server $BOOTSTRAP \
  --create --if-not-exists \
  --topic savings-accounts.events \
  --partitions 3 \
  --replication-factor 1

# Compacted reference topic — client read model
$KAFKA_BIN/kafka-topics.sh --bootstrap-server $BOOTSTRAP \
  --create --if-not-exists \
  --topic clients.events \
  --partitions 3 \
  --replication-factor 1 \
  --config cleanup.policy=compact \
  --config min.cleanable.dirty.ratio=0.1 \
  --config segment.ms=10000

# Compacted reference topic — product read model
$KAFKA_BIN/kafka-topics.sh --bootstrap-server $BOOTSTRAP \
  --create --if-not-exists \
  --topic products.events \
  --partitions 3 \
  --replication-factor 1 \
  --config cleanup.policy=compact \
  --config min.cleanable.dirty.ratio=0.1 \
  --config segment.ms=10000

echo "Topics created successfully."
$KAFKA_BIN/kafka-topics.sh --bootstrap-server $BOOTSTRAP --list
