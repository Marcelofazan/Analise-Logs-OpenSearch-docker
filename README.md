## 📈 AnaliseLogs-OpenSearch-docker
Exemplo de API Analise de Logs com OpenSearch e NLog em C# ASP.NET Core 10 Dapper com banco de dados SQLite.

#### 📋 O que voçê vai ver nesse Projeto
| Tecnologia | Descrição |
|-----------|-----------|
| **NLog**  |Frameworks de registro de logs, facilitando a manutenção e a auditoria de softwares |
| **OpenSearch**  | Motor de Busca utilizado para armazenamento e busca de logs centralizados. |
| **Trace Analytics**  | Ferramenta para coletar, armazenar e visualizar dados de rastreamento de ponta a ponta. |

-  Sua aplicação escreve logs usando o NLog. Um plugin ou target do NLog envia os logs diretamente para o OpenSearch (usando bibliotecas como o SocketLabs.NLog.Targets.OpenSearch ou componentes similares)
- Em sistemas distribuídos, TraceId e SpanId são identificadores usados para rastreabilidade e correlação. Eles permitem rastrear o caminho completo de uma requisição que passa por vários microsserviços e conectar seus logs (gerados via NLog) às métricas de desempenho detalhadas no OpenSearch.
-  Ideal para reter grandes volumes de logs históricos, fazer buscas avançadas baseadas em termos/palavras-chave e construir gráficos complexos de erros ou acessos usando o OpenSearch Dashboards

💬 Requisitos do Projeto
- Necessário **Docker** instalado.


#### 🔄 Executar a aplicação Docker
VSCode Terminal [1]

- Criar Container
```bash
docker-compose up --build
```
 
VSCode Terminal [2]

- Fechar Container
```bash
docker compose down 
```

#### 🔄 Executar a aplicação Desenvolvimento local
VSCode Terminal [1.1]

```bash
dotnet build 
cd AnaliseLogsOpenSearch
dotnet run
```

| Tecnologia | Host |
|-----------|-----------|
| **API**  | http://localhost:8080/swagger/index.html |
| **OpenSearch**  | http://localhost:5601/  |


#### 🌐 OpenSearch Interface Web (UI)
- **Passo 1** - Criar Index Pattern
- **Passo 1.1** Criando o Index Pattern (Padrão de Índice) Abra o painel do seu OpenSearch Dashboards no navegador.
- **Passo 1.2** Acesse o menu lateral esquerdo e vá em Management ➔ Index Management ➔  Clique em Index (Confira se existe analiselogsopensearch-AAAA-MM-DD).

- **Passo 1.3** - Acesse o menu lateral esquerdo novamente vá em Dashboards 
- **Passo 1.4** - Clique no botão azul Create index pattern no canto superior direito.
- **Passo 1.5** - No campo de texto Index pattern name digite:
```text
analiselogsopensearch-*
```

- **Passo 1.6** - De um Nome no Display Name **Produtos Logs** e Tique a opção Include system and hidden indices, vá em Next step
- **Passo 1.7** - No campo Time field (Filtro de tempo), selecione a propriedade @timestamp na lista suspensa.
- **Passo 1.8** - Clique no botão final Create index pattern.


- **Passo 2** - Validando a Correlação dos Logs
- **Passo 2.1** - Vá para a aba Discover no menu esquerdo. Selecione o padrão analiselogsopensearch-* que você acabou de criar.
- **Passo 2.2** Se você fizer um novo POST de produto, copie o traceId retornado e cole na barra de pesquisa do Discover. Caso Não visualizar, será necessário um pipeline de descompactação geral. Seguir passos do arquivo DQL na pasta **scripts**. 

- POST
```text
{
  "produtoNome": "Teclado",
  "descricaoResumida": "Teclado",
  "preco": 40.80,
  "estoque": 50,
  "dataCriacao": "2026-07-19T10:13:30.096"
}
```


- **Passo 3** - Menu Dashboard
- **Passo 3.1** - Dashboard -> Create New Dashboard -> Create New -> Escolha Modelo desejado (por exemplo, Vertical Bar para barras, Line para linhas ou Pie para pizza ->  selecione o Index Pattern
- **Passo 3.2** - No painel lateral direito, configure os eixos do seu gráfico:
- **Passo 3.3** - AZMetrics (Eixo Y): Escolha o tipo de agregação (ex: Count, Average ou Max) e o campo numérico que deseja analisar.
- **Passo 3.4** - Buckets (Eixo X / Divisões): Selecione uma agregação (ex: Date Histogram para ver a evolução no tempo ou Terms para agrupar por categorias/status).
- **Passo 3.5** - Após configurar o gráfico, clique em Save no canto superior direito.Dê um nome para a sua visualização e clique em Save and return (ou Save and go to dashboard para adicioná-la a um painel geral)

