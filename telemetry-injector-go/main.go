package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"sync"
	"sync/atomic"
	"time"

	amqp "github.com/rabbitmq/amqp091-go"
)

type TelemetryData struct {
	DeviceID    string                 `json:"device_id"`
	Timestamp   time.Time              `json:"timestamp"`
	Metrics     map[string]interface{} `json:"metrics"`
	DeviceType  string                 `json:"device_type"`
	Location    string                 `json:"location"`
	CPUUsage    float64                `json:"cpu_usage"`
	MemoryUsage float64                `json:"memory_usage"`
	DiskUsage   float64                `json:"disk_usage"`
	NetworkIn   int64                  `json:"network_in"`
	NetworkOut  int64                  `json:"network_out"`
}

const (
	workerCount      = 5
	channelBuffer    = 1000
	rabbitMQQueue    = "telemetry_queue"
	rabbitMQExchange = "telemetry_exchange"
)

var (
	telemetryChan = make(chan TelemetryData, channelBuffer)
	workerWg      sync.WaitGroup
	rabbitMQConn  *amqp.Connection
	rabbitMQChan  *amqp.Channel
	totalIngested atomic.Int64
	activeDevices atomic.Int64
	startTime     = time.Now()
)

func main() {
	initRabbitMQ()
	defer cleanupRabbitMQ()

	startWorkers()

	http.HandleFunc("/health", healthHandler)
	http.HandleFunc("/ingest", ingestHandler)
	http.HandleFunc("/metrics", metricsHandler)

	port := ":8080"
	log.Printf("Telemetry Injector Service starting on port %s", port)
	log.Printf("Worker Pool initialized with %d workers", workerCount)
	log.Fatal(http.ListenAndServe(port, nil))
}

func initRabbitMQ() {
	rabbitMQURL := os.Getenv("RABBITMQ_URL")
	if rabbitMQURL == "" {
		rabbitMQURL = "amqp://guest:guest@localhost:5672/"
	}

	var err error
	for i := 0; i < 5; i++ {
		rabbitMQConn, err = amqp.Dial(rabbitMQURL)
		if err == nil {
			break
		}
		log.Printf("Failed to connect to RabbitMQ (attempt %d/5): %v", i+1, err)
		time.Sleep(5 * time.Second)
	}
	if err != nil {
		log.Fatalf("Failed to connect to RabbitMQ after 5 attempts: %v", err)
	}

	rabbitMQChan, err = rabbitMQConn.Channel()
	if err != nil {
		log.Fatalf("Failed to open RabbitMQ channel: %v", err)
	}

	err = rabbitMQChan.ExchangeDeclare(
		rabbitMQExchange,
		"topic",
		true,
		false,
		false,
		false,
		nil,
	)
	if err != nil {
		log.Fatalf("Failed to declare RabbitMQ exchange: %v", err)
	}

	_, err = rabbitMQChan.QueueDeclare(
		rabbitMQQueue,
		true,
		false,
		false,
		false,
		nil,
	)
	if err != nil {
		log.Fatalf("Failed to declare RabbitMQ queue: %v", err)
	}

	err = rabbitMQChan.QueueBind(
		rabbitMQQueue,
		"telemetry.#",
		rabbitMQExchange,
		false,
		nil,
	)
	if err != nil {
		log.Fatalf("Failed to bind RabbitMQ queue: %v", err)
	}

	log.Println("RabbitMQ connection established successfully")
}

func cleanupRabbitMQ() {
	if rabbitMQChan != nil {
		rabbitMQChan.Close()
	}
	if rabbitMQConn != nil {
		rabbitMQConn.Close()
	}
}

func startWorkers() {
	for i := 0; i < workerCount; i++ {
		workerWg.Add(1)
		go worker(i)
	}
}

func worker(id int) {
	defer workerWg.Done()
	log.Printf("Worker %d started", id)

	for data := range telemetryChan {
		if err := publishToRabbitMQ(data); err != nil {
			log.Printf("Worker %d: Failed to publish telemetry for device %s: %v", id, data.DeviceID, err)
		} else {
			totalIngested.Add(1)
			log.Printf("Worker %d: Successfully published telemetry for device %s", id, data.DeviceID)
		}
	}
}

func publishToRabbitMQ(data TelemetryData) error {
	body, err := json.Marshal(data)
	if err != nil {
		return fmt.Errorf("failed to marshal telemetry: %w", err)
	}

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	err = rabbitMQChan.PublishWithContext(
		ctx,
		rabbitMQExchange,
		"telemetry."+data.DeviceType,
		false,
		false,
		amqp.Publishing{
			ContentType:  "application/json",
			Body:         body,
			DeliveryMode: amqp.Persistent,
			Timestamp:    time.Now(),
		},
	)
	if err != nil {
		return fmt.Errorf("failed to publish message: %w", err)
	}

	return nil
}

func validateTelemetry(data TelemetryData) error {
	if data.DeviceID == "" {
		return fmt.Errorf("device_id is required")
	}
	if data.DeviceType == "" {
		return fmt.Errorf("device_type is required")
	}
	if data.CPUUsage < 0 || data.CPUUsage > 100 {
		return fmt.Errorf("cpu_usage must be between 0 and 100")
	}
	if data.MemoryUsage < 0 || data.MemoryUsage > 100 {
		return fmt.Errorf("memory_usage must be between 0 and 100")
	}
	if data.DiskUsage < 0 || data.DiskUsage > 100 {
		return fmt.Errorf("disk_usage must be between 0 and 100")
	}
	if data.NetworkIn < 0 {
		return fmt.Errorf("network_in must be non-negative")
	}
	if data.NetworkOut < 0 {
		return fmt.Errorf("network_out must be non-negative")
	}
	return nil
}

func healthHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]interface{}{
		"status":  "healthy",
		"service": "telemetry-injector",
		"rabbitmq": rabbitMQConn != nil && !rabbitMQConn.IsClosed(),
	})
}

func ingestHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		w.WriteHeader(http.StatusMethodNotAllowed)
		return
	}

	var data TelemetryData
	if err := json.NewDecoder(r.Body).Decode(&data); err != nil {
		w.WriteHeader(http.StatusBadRequest)
		json.NewEncoder(w).Encode(map[string]string{"error": err.Error()})
		return
	}

	if err := validateTelemetry(data); err != nil {
		w.WriteHeader(http.StatusBadRequest)
		json.NewEncoder(w).Encode(map[string]string{"error": err.Error()})
		return
	}

	data.Timestamp = time.Now()

	select {
	case telemetryChan <- data:
		activeDevices.Add(1)
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusAccepted)
		json.NewEncoder(w).Encode(map[string]interface{}{
			"message":    "telemetry accepted for processing",
			"device_id":  data.DeviceID,
			"timestamp":  data.Timestamp,
			"queue_size": len(telemetryChan),
		})
	default:
		w.WriteHeader(http.StatusServiceUnavailable)
		json.NewEncoder(w).Encode(map[string]string{
			"error": "worker pool queue is full, please retry later",
		})
	}
}

func metricsHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]interface{}{
		"total_ingested":  totalIngested.Load(),
		"active_devices":  activeDevices.Load(),
		"uptime_seconds":  time.Since(startTime).Seconds(),
		"queue_size":      len(telemetryChan),
		"worker_count":    workerCount,
		"rabbitmq_status": rabbitMQConn != nil && !rabbitMQConn.IsClosed(),
	})
}
