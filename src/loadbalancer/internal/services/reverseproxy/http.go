// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.	

package reverseproxy

import (
	"log"
	"net/http"

	"github.com/google/uuid"
)

func healthz(rw http.ResponseWriter) {
	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("OK"))
}

func (rp *ReverseProxy) authenticate(rw http.ResponseWriter, req *http.Request) bool {
	//Authentication

	//Extract the api-key from the header
	apiKeyHeader := req.Header.Get("api-key")
	if apiKeyHeader == "" {
		//Check if the api-key is in the Authorization header (OpenAI Standard)
		apiKeyHeader = req.Header.Get("Authorization")
		if apiKeyHeader != "" {
			//We need to remove the Bearer prefix if there's one
			if len(apiKeyHeader) >= 7 && apiKeyHeader[:7] == "Bearer " {
				apiKeyHeader = apiKeyHeader[7:]
			}
		}
	}

	//Check if the api key is empty
	if apiKeyHeader == "" {
		rw.WriteHeader(http.StatusUnauthorized)
		rw.Write([]byte("Unauthorized to access this resource. No api-key was provided."))
		return false
	}

	// Check if the api-key is valid
	if apiKeyHeader != rp.authKey {
		rw.WriteHeader(http.StatusUnauthorized)
		rw.Write([]byte("Unauthorized to access this resource"))
		return false
	}

	//TODO perform ctfd token base authentication
	return true
}

func (rp *ReverseProxy) preProcessing(id *uuid.UUID, rw http.ResponseWriter, req *http.Request) bool {
	//Is it a health check?
	if req.URL.Path == "/healthz" {
		healthz(rw)
		return false
	}

	//Authentication
	if !rp.authenticate(rw, req) {
		log.Printf("[ReverseProxy] -> [Id:%s] Unauthorized request from %s", id.String(), req.RemoteAddr)
		return false
	}

	return true
}
