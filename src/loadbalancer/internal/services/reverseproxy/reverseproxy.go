// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

package reverseproxy

import (
	"bytes"
	"context"
	"fmt"
	"io"
	"loadbalancer/internal/config"
	service "loadbalancer/internal/services"
	"loadbalancer/internal/services/orchestrator"
	"log"
	"net"
	"net/http"
	"regexp"
	"strconv"
	"strings"
	"time"

	"github.com/google/uuid"
)

type ReverseProxy struct {
	h       *http.Server
	client  *http.Client
	timeout time.Duration

	azureRegex *regexp.Regexp
	retryCount int
	authKey    string
}

func (rp *ReverseProxy) ServeHTTP(rw http.ResponseWriter, req *http.Request) {
	id := uuid.New()
	log.Printf("[ReverseProxy] -> [Id:%s] %s %s %s", id.String(), req.RemoteAddr, req.Method, req.URL)
	if !rp.preProcessing(&id, rw, req) {
		return
	}

	//Get the model name from the request
	path := strings.ToLower(req.URL.Path)
	matches := rp.azureRegex.FindStringSubmatch(path)
	if len(matches) < 2 {
		rw.WriteHeader(http.StatusBadRequest)
		_, _ = fmt.Fprint(rw, "Invalid request. Model unrecognized")
		return
	}

	modelName := matches[1]
	//Check if model name is valid
	if !orchestrator.IsValidModel(modelName) {
		rw.WriteHeader(http.StatusBadRequest)
		_, _ = fmt.Fprint(rw, "Invalid request. Model unrecognized")
		return
	}

	body, err := io.ReadAll(req.Body)
	if err != nil {
		log.Printf("[ReverseProxy] [Id:%s] -> Error reading request body: %v", id.String(), err)
		rw.WriteHeader(http.StatusBadRequest)
		_, _ = fmt.Fprint(rw, "Invalid request. Body not readable")
		return
	}
	rp.forwardRequest(&id, modelName, rw, req, body, 1)
}

// Run the reverse proxy returns true if the request succeeded or false if it failed ie 429
func (rp *ReverseProxy) forwardRequest(id *uuid.UUID, modelName string, rw http.ResponseWriter, req *http.Request, body []byte, attempt int) bool {
	if attempt > rp.retryCount {
		log.Printf("[ReverseProxy] [Id:%s] -> Request failed after %d attempts", id.String(), rp.retryCount)
		rw.Header()["X-Endpoint-Not-Available"] = []string{"true"}
		rw.WriteHeader(http.StatusTooManyRequests)
		return false
	}

	endpointChoice := orchestrator.GetEndpoint(modelName)
	endpoint := endpointChoice.Endpoint
	log.Printf("[ReverseProxy] [Id:%s] -> Received request at: %s. Using model: %s On endpoint: %s", id.String(), time.Now(), modelName, endpoint.Url.Host)

	req.Host = endpoint.Url.Host
	req.URL.Host = endpoint.Url.Host
	req.URL.Scheme = endpoint.Url.Scheme
	req.RequestURI = ""

	req.Header.Set("api-key", endpoint.Key) // Make sure we use the right api-key
	req.Body = io.NopCloser(bytes.NewBuffer(body))

	// send a request to the origin server
	response, err := rp.client.Do(req)
	if err != nil {
		log.Printf("[ReverseProxy] [Id:%s] -> Error while sending request to origin server: %v. Attempting retry.", id.String(), err)
		return rp.forwardRequest(id, modelName, rw, req, body, attempt+1)
	}

	remainingTokens := response.Header.Get("X-Ratelimit-Remaining-Tokens")
	remainingTokensInt := 0

	if remainingTokens != "" {
		remainingTokensInt, _ = strconv.Atoi(remainingTokens)
	}

	remainingRequests := response.Header.Get("X-Ratelimit-Remaining-Requests")
	remainingRequestsInt := 0

	if remainingRequests != "" {
		remainingRequestsInt, _ = strconv.Atoi(remainingRequests)
	}

	resetTokens := response.Header.Get("X-Ratelimit-Reset-Tokens")
	resetTokensInt := 0
	if resetTokens != "" {
		resetTokensInt, _ = strconv.Atoi(resetTokens)
	}

	resetRequests := response.Header.Get("X-Ratelimit-Reset-Requests")
	resetRequestsInt := 0
	if resetRequests != "" {
		resetRequestsInt, _ = strconv.Atoi(resetRequests)
	}

	if remainingRequestsInt == 0 || remainingTokensInt == 0 {
		log.Printf("[ReverseProxy] [Id:%s] -> Endpoint %s has no more tokens or requests available", id.String(), endpoint.Url.Host)

		//Look for backoff time
		var backoffTime int
		if resetTokensInt > resetRequestsInt {
			backoffTime = resetTokensInt
		} else {
			backoffTime = resetRequestsInt
		}

		if backoffTime == 0 {
			backoffTime = 59 //Default backoff time is 60 seconds (59+1)
		}
		nextRefresh := time.Now().Add(time.Second * time.Duration(backoffTime+1))
		orchestrator.SetEndpointTokenNextRefresh(endpoint.Id, modelName, remainingTokensInt, remainingRequestsInt, nextRefresh)
	} else {
		orchestrator.SetEndpointToken(endpoint.Id, modelName, remainingTokensInt, remainingRequestsInt)
	}

	if (response.StatusCode < 200 || response.StatusCode >= 300) && response.StatusCode != http.StatusBadRequest {
		log.Printf("[ReverseProxy] [Id:%s] -> Request failed with status code %d", id.String(), response.StatusCode)
		return rp.forwardRequest(id, modelName, rw, req, body, attempt+1)
	}

	// Don't retry if the status code is 400. This is usually a content filter error.
	if response.StatusCode == http.StatusBadRequest {
		log.Printf("[ReverseProxy] [Id:%s] -> Request failed with status code %d. Not retrying for bad request!", id.String(), response.StatusCode)
	}

	//Add headers to the response
	for key, value := range response.Header {
		rw.Header()[key] = value
	}

	rw.Header()["X-Endpoint-Url"] = []string{endpoint.Url.Host}
	if endpointChoice.NotAvailable {
		// We set the header to let us know that this endpoint was a last resort.
		rw.Header()["X-Endpoint-Not-Available"] = []string{"true"}
	}

	rw.WriteHeader(response.StatusCode)

	io.Copy(rw, response.Body)
	log.Printf("[ReverseProxy] [Id:%s] -> Request completed at: %s", id.String(), time.Now())
	return true
}

func (rp *ReverseProxy) Register() {

}

func (rp *ReverseProxy) Init() {
	netTransport := &http.Transport{
		Dial: (&net.Dialer{
			Timeout: 5 * time.Second, //Taken from OpenAI documentaion
		}).Dial,
		TLSHandshakeTimeout: 5 * time.Second,
	}

	rp.timeout = time.Duration(config.GetInt(config.CReverseProxyTimeout)) * time.Second

	rp.client = &http.Client{
		Timeout:   rp.timeout,
		Transport: netTransport,
	}

	//Compile the regex for the azure endpoint
	rp.azureRegex = regexp.MustCompile(`\/openai\/deployments\/(.+?)\/`)
	rp.retryCount = 3 //TODO export as a config parameter
	rp.authKey = config.GetString(config.CReverseProxyAuthKey)
}

func (rp *ReverseProxy) Start() {
	log.Printf("[ReverseProxy] -> Starting Reverse Proxy Server")

	go rp.run()
}

func (rp *ReverseProxy) Shutdown() {
	log.Printf("[ReverseProxy] -> Shutting down Reverse Proxy Server")
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()
	rp.h.Shutdown(ctx)
}

func (rp *ReverseProxy) run() {
	defer service.Closed()

	host := config.GetAddr(config.CReverseProxyHost, config.CReverseProxyPort)

	rp.h = &http.Server{
		Addr:         host,
		Handler:      rp,
		ReadTimeout:  rp.timeout,
		WriteTimeout: rp.timeout,
	}

	log.Printf("[ReverseProxy] -> Server is started on %s", host)

	err := rp.h.ListenAndServe()
	if err != nil {
		errString := err.Error()
		if errString != "http: Server closed" {
			log.Fatal("[ReverseProxy] -> ", err)
		}
	}
}
