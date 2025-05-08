// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

package orchestrator

import (
	"loadbalancer/internal/config"
	service "loadbalancer/internal/services"
	"log"
	"net/url"
	"strings"
	"sync"
	"time"
)

var singleton *Orchestrator

type Orchestrator struct {
	shutdown     chan bool
	nextEndpoint int
	endpoints    []endpoint
	modelNames   map[string]bool

	// time when the capacity will be refreshed
	tokenCutoff int
	lock        sync.Mutex
}

func (o *Orchestrator) Register() {

}

func (o *Orchestrator) Init() {
	o.shutdown = make(chan bool)
	o.modelNames = make(map[string]bool)
	o.nextEndpoint = 0
	o.tokenCutoff = config.GetInt(config.CReverseProxyTokenCutoff)

	//Load the endpoints from the config file
	configEndpoints := config.GetEndpoints()
	id := 1
	for _, configEndpoint := range configEndpoints {
		url, err := url.Parse(configEndpoint.Url)
		if err != nil {
			log.Fatalf("[Orchestrator] -> Invalid endpoint URL: %s", configEndpoint.Url)
		}

		endpoint := endpoint{
			connection: EndpointConnection{Url: url, Key: configEndpoint.Key, Id: id},
			models:     make(map[string]*modelCapacity),
		}
		for _, model := range configEndpoint.Models {
			key := strings.ToLower(model.Name)
			endpoint.models[key] = &modelCapacity{
				capacityToken:       model.CapacityToken,
				capacityRequest:     model.CapacityRequest,
				tokenAvailable:      0,
				requestAvailable:    0,
				nextCapacityRefresh: time.Now(),
			}

			//Add the model name to the hashset of model names
			o.modelNames[key] = true
		}

		o.endpoints = append(o.endpoints, endpoint)
		id++
	}

	singleton = o
}

func (o *Orchestrator) Start() {
	log.Printf("[Orchestrator] -> Starting orchestrator service")
	go o.run()
}

func (o *Orchestrator) Shutdown() {
	log.Printf("[Orchestrator] -> Stopping orchestrator service")
	close(o.shutdown)
}

func (o *Orchestrator) run() {
	ticker := time.NewTicker(time.Second * 1)
	defer service.Closed()
	defer ticker.Stop()

	for {
		select {
		case <-o.shutdown:
			log.Printf("[Orchestrator] -> Orchestrator service closed")
			return

		case <-ticker.C:
			log.Printf("[Orchestrator] -> Tick")
			o.lock.Lock()
			current := time.Now()
			for i := range o.endpoints {
				for modelName, model := range o.endpoints[i].models {
					if model.nextCapacityRefresh.Before(current) {
						log.Printf("[Orchestrator] -> Refreshing capacity for model %s|%s tokenAvailable=%d requestAvailable=%d", o.endpoints[i].connection.Url.Host, modelName, model.tokenAvailable, model.requestAvailable)
						model.tokenAvailable = model.capacityToken
						model.requestAvailable = model.capacityRequest
						model.nextCapacityRefresh = current.Add(time.Second * 60)
					}
				}
			}
			o.lock.Unlock()
		}
	}
}
