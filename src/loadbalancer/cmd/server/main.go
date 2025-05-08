// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

package main

import (
	"loadbalancer/internal/config"
	service "loadbalancer/internal/services"
	"loadbalancer/internal/services/orchestrator"
	"loadbalancer/internal/services/reverseproxy"
	"loadbalancer/pkg/graceful"
	"log"
)

func main() {
	config.Init()
	graceful.Register(service.ShutdownAll, "Services")
	handleGraceful := graceful.ListenSIG()

	log.Printf("Starting services")
	registerServices()
	service.StartAll()

	<-handleGraceful
}

func registerServices() {
	service.Add(&reverseproxy.ReverseProxy{})
	service.Add(&orchestrator.Orchestrator{})
}
