# Setup

## Install cert manager for certs

helm repo add jetstack https://charts.jetstack.io
helm repo update
kubectl create ns cert-manager
helm upgrade --install cert-manager jetstack/cert-manager  --create-namespace  --namespace cert-manager --create-namespace --version v1.13.3 --set installCRDs=true

## Apply lets-encrypt config
kubectl -n cert-manager apply -f conf/ClusterIssuer.yaml

## Setup nginx ingress
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update
kubectl create ns ingress-basic
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx --create-namespace --namespace ingress-basic --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz

## For a local endpoint instead
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx --create-namespace --namespace ingress-basic --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-internal"=true --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-ipv4"=10.224.1.50 --set controller.allowSnippetAnnotations=true

### Get IP address with
kubectl get service --namespace ingress-basic ingress-nginx-controller

### Set a cloudapp domain in azure
Use the azure portal for that.

### Update/Deploy ingress.yaml
kubectl apply -f conf/ingress.yaml

### Run python script to configure everything