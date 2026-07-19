using Dapper;
using Microsoft.Data.Sqlite;
using OpenSearch.Client;
using AnaliseLogsOpenSearch.Models;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System;

namespace AnaliseLogsOpenSearch.Services;

public class ProdutoService : IProdutoService
{
    protected const string OPENSEARCH_INDEX = "produtos";
    private readonly OpenSearchClient _opensearchClient;
    private readonly string _connectionString;

    public ProdutoService(OpenSearchClient opensearchClient, IConfiguration configuration)
    {
        _opensearchClient = opensearchClient;
        _connectionString = configuration.GetConnectionString("SqliteConnection") ?? "Data Source=produtos.db";
        CreateTableIfNotExists();
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    private void CreateTableIfNotExists()
    {
        using var connection = CreateConnection();
        var sql = @"
            CREATE TABLE IF NOT EXISTS Produtos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProdutoNome TEXT,
                DescricaoResumida TEXT,
                Preco NUMERIC,
                Estoque INTEGER,
                DataCriacao TEXT
            );";
        connection.Execute(sql);
    }

    public async Task<Produto?> InsertAsync(Produto? produto)
    {
        Activity? activity = Activity.Current;
        activity?.SetTag("db.statement", "INSERT");

        if (produto == null) return null;

        // CORREÇÃO: Compara usando o valor padrão de inicialização de um DateTime (DateTime.MinValue)
        if (produto.DataCriacao == DateTime.MinValue)
        {
            produto.DataCriacao = DateTime.Now;
        }

        using (var connection = CreateConnection())
        {
            // O Dapper converte o DateTime do C# automaticamente para salvar no campo TEXT do SQLite
            var sql = @"INSERT INTO Produtos (ProdutoNome, DescricaoResumida, Preco, Estoque, DataCriacao) 
                        VALUES (@ProdutoNome, @DescricaoResumida, @Preco, @Estoque, @DataCriacao);
                        SELECT last_insert_rowid();";

            int insertedId = await connection.ExecuteScalarAsync<int>(sql, produto);
            produto.Id = insertedId;
        }

        if (activity != null)
        {
            produto.TraceId = activity.TraceId.ToString();
            produto.SpanId = activity.SpanId.ToString();
        }

        var response = await _opensearchClient.IndexAsync(produto, id => id
            .Index(OPENSEARCH_INDEX)
            .Id(produto.Id.ToString())
        );

        if (!response.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Falha ao indexar no OpenSearch");
            return null;
        }

        return produto;
    }

    public async Task<Produto?> GetByIdAsync(string id)
    {
        Activity? activity = Activity.Current;
        activity?.SetTag("db.statement", "SELECT WHERE Id");
        activity?.SetTag("produto.id", id);

        if (!int.TryParse(id, out int numericId)) return null;

        using var connection = CreateConnection();
        var sql = "SELECT Id, ProdutoNome, DescricaoResumida, Preco, Estoque, DataCriacao FROM Produtos WHERE Id = @Id;";
        return await connection.QueryFirstOrDefaultAsync<Produto>(sql, new { Id = numericId });
    }

    public async Task<IReadOnlyCollection<Produto>?> GetAllAsync()
    {
        Activity? activity = Activity.Current;
        activity?.SetTag("db.system", "sqlite");
        activity?.SetTag("db.statement", "SELECT ALL");

        using var connection = CreateConnection();
        var sql = "SELECT Id, ProdutoNome, DescricaoResumida, Preco, Estoque, DataCriacao FROM Produtos;";
        var produtos = await connection.QueryAsync<Produto>(sql);
        return produtos.ToImmutableList();
    }

    public async Task<bool> UpdateAsync(string id, Produto produto)
    {
        Activity? activity = Activity.Current;
        activity?.SetTag("db.statement", "UPDATE");
        activity?.SetTag("produto.id", id);

        if (!int.TryParse(id, out int numericId)) return false;
        produto.Id = numericId;

        using (var connection = CreateConnection())
        {
            // CORREÇÃO: Busca a data original tipando o Query para DateTime? para evitar erros de conversão implicitada
            var dataOriginal = await connection.QueryFirstOrDefaultAsync<DateTime?>(
                "SELECT DataCriacao FROM Produtos WHERE Id = @Id;", new { Id = numericId });

            // Atribui o DateTime original ou gera a data e hora locais atuais
            produto.DataCriacao = dataOriginal ?? DateTime.Now;

            var sql = @"UPDATE Produtos 
                        SET ProdutoNome = @ProdutoNome, 
                            DescricaoResumida = @DescricaoResumida, 
                            Preco = @Preco, 
                            Estoque = @Estoque 
                        WHERE Id = @Id;";

            var rowsAffected = await connection.ExecuteAsync(sql, produto);
            if (rowsAffected == 0) return false;
        }

        if (activity != null)
        {
            produto.TraceId = activity.TraceId.ToString();
            produto.SpanId = activity.SpanId.ToString();
        }

        var response = await _opensearchClient.IndexAsync(produto, i => i
            .Index(OPENSEARCH_INDEX)
            .Id(id)
        );

        if (!response.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Falha ao atualizar no OpenSearch");
            return false;
        }

        return response.IsValid;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        Activity? activity = Activity.Current;
        activity?.SetTag("db.statement", "DELETE");
        activity?.SetTag("produto.id", id);

        if (!int.TryParse(id, out int numericId)) return false;

        using var connection = CreateConnection();
        var sql = "DELETE FROM Produtos WHERE Id = @Id;";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = numericId });

        if (rowsAffected == 0) return false;

        var response = await _opensearchClient.DeleteAsync<Produto>(id, d => d.Index(OPENSEARCH_INDEX));

        if (!response.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Falha ao deletar no OpenSearch");
            return false;
        }

        return response.IsValid;
    }

    public async Task<IReadOnlyCollection<Produto>?> FilterAsync(string text)
    {
        Activity? activity = Activity.Current;
        activity?.SetTag("search.term", text);

        var request = new SearchRequest(OPENSEARCH_INDEX)
        {
            From = 0,
            Size = 10
        };

        if (string.IsNullOrWhiteSpace(text) || text.Trim() == "*")
        {
            request.Query = new MatchAllQuery();
        }
        else
        {
            request.Query = new MatchQuery { Field = "produtoNome", Query = text } ||
                            new MatchQuery { Field = "descricaoResumida", Query = text };
        }

        ISearchResponse<Produto> response = await _opensearchClient.SearchAsync<Produto>(request);

        if (!response.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Falha na query do OpenSearch");
            return null;
        }

        return response.Documents.ToImmutableList();
    }
}