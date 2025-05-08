// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

package graceful

import (
	"log"
	"os"
	"os/signal"
	"syscall"
)

type subscriberStruct struct {
	f    func()
	name string
}

var subscriber []subscriberStruct
var closed chan bool

// Register a function to be called when sigterm is raised
func Register(f func(), name string) {
	subscriber = append(subscriber, subscriberStruct{
		f:    f,
		name: name})
}

// ListenSIG register a thread to listen to a SIGTERM signal returns a signal to wait untill the functions are all called
func ListenSIG() chan bool {
	c := make(chan os.Signal)
	closed = make(chan bool)
	signal.Notify(c, os.Interrupt, syscall.SIGTERM)
	go func() {
		<-c
		onSIGTERM()
		os.Exit(0)
	}()
	return closed
}

func onSIGTERM() {
	for _, sub := range subscriber {
		log.Printf("Stopping %s", sub.name)
		sub.f()
	}
	close(closed)
}
