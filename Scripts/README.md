## DevTools-DQL 

#### Logs de hoje
```text
GET /opensearch-net-2026-07-18/_search
```

#### Busca refinada procurando especificamente pelo nome do seu controlador para ignorar os ruídos do sistema
```text
GET /opensearch-net-2026-07-18/_search?q=logger:opensearch_net.Controllers.ProdutosController
```

#### Descobrir como os dados estão estruturados
```text 
GET /opensearch-net-*/_search
{
  "size": 3,
  "query": {
    "match_all": {}
  },
  "sort": [
    {
      "@timestamp": {
        "order": "desc"
      }
    }
  ]
}
```

#### Último documento bruto inserido
```text
GET /opensearch-net-*/_search
{
  "size": 1,
  "query": {
    "match_all": {}
  },
  "sort": [
    {
      "@timestamp": {
        "order": "desc"
      }
    }
  ]
}
```

#### Query de Agrupamento (Aggregation) por Tipo de Erro/Log
- Descubra a quantidade exata de mensagens divididas por nível (Info, Warning, Error) para criar gráficos de pizza ou barras:
```text
GET /opensearch-net-*/_search
{
  "size": 0,
  "aggs": {
    "volumetria_por_nivel": {
      "terms": {
        "field": "level.keyword"
      }
    }
  }
}
```

#### Verificar se o Log de Fato Existe no Índice Geral
```text
GET /opensearch-net-*/_search
{
  "size": 5,
  "query": {
    "match_all": {}
  }
}
```

#### Limpar e recriar o índice de forma perfeita
```text
DELETE /opensearch-net-2026-07-18
```

#### Criar o Pipeline de Extração JSON
```text
PUT _ingest/pipeline/descompactar-json-nlog
{
  "description": "Extrai as propriedades do JSON contido na string message do NLog",
  "processors": [
    {
      "json": {
        "field": "message",
        "target_field": "dados"
      }
    },
    {
      "script": {
        "description": "Move os campos extraídos para a raiz do documento",
        "lang": "painless",
        "source": """
          if (ctx.dados != null) {
            for (entry in ctx.dados.entrySet()) {
              ctx[entry.getKey()] = entry.getValue();
            }
            ctx.remove('dados');
          }
        """
      }
    }
  ]
}
```

#### Testar a Busca com o Pipeline Ativo
```text
POST /opensearch-net-*/_search?pipeline=descompactar-json-nlog
{
  "query": {
    "match": {
      "message": "1b71c1240bf9c1c30855a9200e3a87b3"
    }
  }
}
```

#### Tornar Pipeline Extração Automático para Sempre
```text
PUT /opensearch-net-*/_settings
{
  "index.default_pipeline": "descompactar-json-nlog"
}
```


#### Testar a extração simulando um documento
```text
POST _ingest/pipeline/descompactar-json-nlog/_simulate
{
  "docs": [
    {
      "_source": {
        "@timestamp": "2026-07-18T17:27:29.1714948-03:00",
        "level": "Info",
        "message": "{\"timestamp\":\"2026-07-18T17:27:29.171-03:00\",\"level\":\"Info\",\"message\":\"Executed endpoint\",\"traceId\":\"1b71c1240bf9c1c30855a9200e3a87b3\"}"
      }
    }
  ]
}
```

#### Ativar o Pipeline de forma definitiva no Índice
```text
PUT /opensearch-net-*/_settings
{
  "index.default_pipeline": "descompactar-json-nlog"
}
```

#### Atualizar os logs antigos 
```text
POST /opensearch-net-*/_update_by_query?pipeline=descompactar-json-nlog
{
  "query": {
    "match_all": {}
  }
}
```

#### Faça uma nova chamada na sua API C# para gerar um novo ID e tente novamente a sua busca original
```text
GET /opensearch-net-*/_search
{
  "query": {
    "term": {
      "traceId.keyword": "COLE_O_NOVO_ID_AQUI"
    }
  }
}
``` 

#### Buscar por esse spanId específico
```text
GET /opensearch-net-*/_search
{
  "query": {
    "term": {
      "spanId.keyword": "1d4e9bdb49309c46"
    }
  }
}
```

#### Busca combinanda (Trace e Span)
```text
GET /opensearch-net-*/_search
{
  "query": {
    "bool": {
      "must": [
        { "term": { "traceId.keyword": "9bc83e794e4cd58f78d544160fbb02e2" } },
        { "term": { "spanId.keyword": "1d4e9bdb49309c46" } }
      ]
    }
  }
}
```

#### Agrupar logs para descobrir a jornada da requisição
```text
GET /opensearch-net-*/_search
{
  "query": {
    "term": {
      "traceId.keyword": "9bc83e794e4cd58f78d544160fbb02e2"
    }
  },
  "sort": [
    { "@timestamp": "asc" }
  ]
}
```

#### Contar quantos Spans diferentes cada rota gera (Métrica)
```text
GET /opensearch-net-*/_search
{
  "size": 0,
  "aggs": {
    "rotas_da_api": {
      "terms": { "field": "requestPath.keyword" },
      "aggs": {
        "total_de_spans_gerados": {
          "cardinality": { "field": "spanId.keyword" }
        }
      }
    }
  }
}
```

#### Descobrir gargalos de desempenho (Spans lentos)
```text
GET /opensearch-net-*/_search
{
  "size": 5,
  "query": {
    "exists": { "field": "ElapsedMilliseconds" }
  },
  "sort": [
    { "ElapsedMilliseconds": "desc" }
  ]
}
```
