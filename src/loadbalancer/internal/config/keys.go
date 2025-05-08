// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

package config

//Keys that are used in the config file

const CReverseProxyHost = "reverseProxy.host"
const CReverseProxyPort = "reverseProxy.port"
const CReverseProxyTimeout = "reverseProxy.timeout"         //Timeout in seconds
const CReverseProxyTokenCutoff = "reverseProxy.tokenCutoff" //The number of minimum tokens that a model needs to have to make a request to it.

const CReverseProxyAuthKey = "reverseProxy.authKey" //The key that is used to authenticate the requests
