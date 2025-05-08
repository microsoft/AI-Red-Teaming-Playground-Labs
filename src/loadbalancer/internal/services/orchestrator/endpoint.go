// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

package orchestrator

import (
	"log"
	"net/url"
	"time"
)

type EndpointChoice struct {
	NotAvailable bool // If true, the endpoint is not available, but it was the best choice at the time.
	Endpoint     EndpointConnection
}

type EndpointConnection struct {
	Url *url.URL
	Key string
	Id  int
}

type modelCapacity struct {
	capacityToken       int
	capacityRequest     int
	tokenAvailable      int
	requestAvailable    int
	nextCapacityRefresh time.Time
}

type endpoint struct {
	connection EndpointConnection
	models     map[string]*modelCapacity
	// TODO maybe do a bit of health monitoring on the endpoints and expontentially back off
}

func GetEndpoint(modelName string) EndpointChoice {
	o := singleton
	o.lock.Lock()
	choice := EndpointChoice{}
	defer o.lock.Unlock()

	i := o.nextEndpoint
	j := 0
	endpoint := o.endpoints[i]
	for !isEndpointValid(&endpoint, modelName) && j < len(o.endpoints) {
		i = (i + 1) % len(o.endpoints)
		j++
	}

	if j == len(o.endpoints) {
		// We have tried all the endpoints and none of them are valid. We give up and return the original endpoint
		log.Printf("[Orchestrator] -> No endpoint available with capacity for model %s", modelName)
		choice.NotAvailable = true
	} else {
		choice.NotAvailable = false
	}

	//Check if the request available is greater than zero
	o.endpoints[i].models[modelName].requestAvailable--
	o.nextEndpoint = (i + 1) % len(o.endpoints)
	choice.Endpoint = o.endpoints[i].connection

	return choice
}

func SetEndpointToken(endpointId int, modelName string, tokenAvailable int, requestAvailable int) {
	o := singleton
	o.lock.Lock()
	defer o.lock.Unlock()

	model := o.endpoints[endpointId-1].models[modelName]
	model.tokenAvailable = tokenAvailable
	model.requestAvailable = requestAvailable
	model.nextCapacityRefresh = time.Now().Add(time.Second * 60)
}

func SetEndpointTokenNextRefresh(endpoint int, modelName string, tokenAvailable int, requestAvailable int, nextRefresh time.Time) {
	o := singleton
	o.lock.Lock()
	defer o.lock.Unlock()

	model := o.endpoints[endpoint-1].models[modelName]
	model.tokenAvailable = tokenAvailable
	model.requestAvailable = requestAvailable
	model.nextCapacityRefresh = nextRefresh

}

func IsValidModel(modelName string) bool {
	o := singleton
	//No sync since this hashset is always read from
	_, ok := o.modelNames[modelName]
	return ok
}

func isEndpointValid(endpoint *endpoint, modelName string) bool {
	o := singleton
	return endpoint.models[modelName].requestAvailable > 0 &&
		endpoint.models[modelName].tokenAvailable-o.tokenCutoff > 0
}
