## Install Dapr


```
helm upgrade --install dapr dapr/dapr \
--version=1.15 \
--namespace dapr-system \
--create-namespace 
helm install dapr-dashboard dapr/dapr-dashboard --namespace dapr-system

```
## Install ingress-controller
Para acessar facilmente os dashboards

```
helm install nginx oci://ghcr.io/nginx/charts/nginx-ingress -n nginx --create-namespace
```

## Install Kafka and Redis
```
helm install kafka oci://registry-1.docker.io/bitnamicharts/kafka
helm install redis oci://registry-1.docker.io/bitnamicharts/redis

CLIENT_PASSWORD=$(kubectl get secret kafka-user-passwords -o jsonpath="{.data.client-passwords}" | base64 --decode)

helm repo add kafka-ui https://provectus.github.io/kafka-ui-charts
helm upgrade kafka-ui kafka-ui/kafka-ui --install -f k8s/kafka.ui.values.yaml --set 'envs.secret.KAFKA_CLUSTERS_0_PROPERTIES_SASL_JAAS_CONFIG=org.apache.kafka.common.security.plain.PlainLoginModule required username="user1" password="'"$CLIENT_PASSWORD"'";'
```

## Install Jaeger 

```
    helm repo add jaegertracing https://jaegertracing.github.io/helm-charts

    helm upgrade --install jaeger jaegertracing/jaeger \
        --namespace devops \
        --create-namespace \
        --history-max 3 \
        --values k8s/jaeger.values.yaml

    helm repo add open-telemetry https://open-telemetry.github.io/opentelemetry-helm-charts

    helm install otel-collector open-telemetry/opentelemetry-collector \
        --namespace devops \
        --create-namespace \
        --values k8s/otel.collector.values.yaml 
``` 


## Create dapr components

```
kubectl apply -f k8s/dapr.kafka.component.yaml 
kubectl apply -f k8s/dapr.redis.component.yaml
kubectl apply -f k8s/dapr.tracing.yaml

```

## Create apps

```
kubectl apply -f k8s/dapr.app.consumer.yaml
kubectl apply -f k8s/dapr.app.publisher.yaml
```