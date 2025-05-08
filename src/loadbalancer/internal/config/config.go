// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

package config

import (
	"fmt"
	"log"

	"github.com/spf13/viper"
)

var configItem ConfigItem

type ConfigItem struct {
	Endpoints []EndpointItem `yaml:"endpoints"`
}

type EndpointItem struct {
	Url    string      `yaml:"url"`
	Key    string      `yaml:"key"`
	Type   string      `yaml:"type"`
	Models []ModelItem `yaml:"models"`
}

type ModelItem struct {
	Name            string `yaml:"name"`
	CapacityToken   int    `yaml:"capacityToken"`
	CapacityRequest int    `yaml:"capacityRequest"`
}

func Init() {
	log.Printf("Loading config file")
	setupFile()
	setupDefault()
	validate()
	log.Printf("Config file loaded successfully")
}

func setupFile() {
	viper.SetConfigName("config")
	viper.SetConfigType("yaml")
	viper.AddConfigPath(".")
	viper.AddConfigPath("/etc/loadbalancer/")

	if err := viper.ReadInConfig(); err != nil {
		if _, ok := err.(viper.ConfigFileNotFoundError); ok {
			log.Fatal("Error: The config file was not found. Make sure that a config file is available")
		} else {
			// Config file was found but another error was produced
			panic(err)
		}
	}

	// Load the endpoints
	configItem = ConfigItem{}
	err := viper.Unmarshal(&configItem)
	if err != nil {
		log.Printf("Error unmarshalling config: %v", err)
		panic(err)
	}
}

func setupDefault() {
	viper.SetDefault(CReverseProxyHost, "")
	viper.SetDefault(CReverseProxyPort, "8000")
	viper.SetDefault(CReverseProxyTimeout, "600")
	viper.SetDefault(CReverseProxyTokenCutoff, "500")
}

func GetString(key string) string {
	return viper.GetString(key)
}

func GetInt(key string) int {
	return viper.GetInt(key)
}

func GetInt64(key string) int64 {
	return viper.GetInt64(key)
}

func GetAddr(hostname string, portname string) string {
	return fmt.Sprintf("%s:%d", GetString(hostname), GetInt(portname))
}

func GetEndpoints() []EndpointItem {
	return configItem.Endpoints
}

func validate() {
	//Check if the port number is ok
	if GetInt(CReverseProxyPort) < 1 || GetInt(CReverseProxyPort) > 65535 {
		panic("Error: Invalid port number")
	}

	//Check if the timeout is ok
	if GetInt(CReverseProxyTimeout) < 1 {
		panic("Error: Invalid timeout value")
	}

	//Check if the tokenCutoff is ok
	if GetInt(CReverseProxyTokenCutoff) < 1 {
		panic("Error: Invalid tokenCutoff value")
	}

	//Check if the autnenitcation key is ok
	if GetString(CReverseProxyAuthKey) == "" {
		panic("Error: Invalid authentication key")
	}

	//Check if the endpoints are ok
	if len(configItem.Endpoints) == 0 {
		panic("Error: No endpoints found in the config file")
	}

	models_name := make(map[string]bool)
	for i, endpoint := range configItem.Endpoints {
		//Look if type is either openai or azure
		if endpoint.Type != "openai" && endpoint.Type != "azure" {
			panic(fmt.Sprintf("Error: Invalid endpoint type: %s", endpoint.Type))
		}

		//Check if the URL is ok
		if endpoint.Url == "" {
			panic("Error: Invalid URL")
		}

		//Check if models are not empty
		if len(endpoint.Models) == 0 {
			panic("Error: No models found")
		}

		//Check if we have the right number of models
		if i > 0 && len(endpoint.Models) != len(configItem.Endpoints[0].Models) {
			panic(fmt.Sprintf("Error: Invalid number of models for endpoint %s", endpoint.Url))
		}

		for _, model := range endpoint.Models {
			if i > 0 {
				//Check if the model is not present in the map
				if _, ok := models_name[model.Name]; !ok {
					panic(fmt.Sprintf("Error: Model %s is not present in every endpoints", model.Name))
				}
			} else {
				models_name[model.Name] = true
			}

			//Check if the capacityToken is ok
			if model.CapacityToken < 1 {
				panic("Error: Invalid capacityToken value")
			}

			//Check if the capacityRequest is ok
			if model.CapacityRequest < 1 {
				panic("Error: Invalid capacityRequest value")
			}
		}
	}
}
