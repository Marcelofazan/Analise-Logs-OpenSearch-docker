using Microsoft.AspNetCore.Mvc;
using AnaliseLogsOpenSearch.Services;
using AnaliseLogsOpenSearch.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AnaliseLogsOpenSearch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutosController : ControllerBase
    {
        private readonly IProdutoService _produtoService;
        private readonly ILogger<ProdutosController> _logger;

        public ProdutosController(IProdutoService produtoService, ILogger<ProdutosController> logger)
        {
            _produtoService = produtoService;
            _logger = logger;
        }

        // POST: api/produtos
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Produto? request)
        {
            // CORREÇÃO CS8602: Valida se o corpo está nulo no topo para garantir segurança nos logs e propriedades
            if (request == null)
            {
                _logger.LogWarning("Recebida requisição para criar produto com o corpo vazio (null).");
                return BadRequest("O corpo da requisição não pode ser nulo.");
            }

            _logger.LogInformation("Recebida requisição para criar um novo produto: {ProdutoNome}", request.ProdutoNome);

            try
            {

                request.DataCriacao = DateTime.Now;

                var result = await _produtoService.InsertAsync(request);
                if (result == null)
                {
                    _logger.LogWarning("Falha ao inserir o produto: {ProdutoNome}", request.ProdutoNome);
                    return BadRequest("Erro ao inserir o produto.");
                }

                _logger.LogInformation("Produto criado com sucesso. ID gerado: {ProdutoId}", result.Id);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico ao criar o produto.");
                return StatusCode(500, "Erro interno no servidor.");
            }
        }

        // GET: api/produtos
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Recebida requisição para listar todos os produtos.");

            try
            {
                var result = await _produtoService.GetAllAsync();
                _logger.LogInformation("Produtos retornados com sucesso. Total: {Count}", result?.Count ?? 0);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar listagem de produtos.");
                return StatusCode(500, "Erro interno no servidor.");
            }
        }

        // GET: api/produtos/search?text=teclado
        [HttpGet("search")]
        public async Task<IActionResult> Filter([FromQuery] string text)
        {
            _logger.LogInformation("Recebida requisição de busca textual no OpenSearch com o termo: '{Termo}'", text);

            try
            {
                var result = await _produtoService.FilterAsync(text);
                if (result == null)
                {
                    _logger.LogWarning("Nenhum produto encontrado ou falha no OpenSearch para o termo: '{Termo}'", text);
                    return NotFound("Nenhum produto encontrado ou erro na busca.");
                }

                _logger.LogInformation("Busca concluída. Itens encontrados: {Count}", result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar busca textual por '{Termo}'.", text);
                return StatusCode(500, "Erro interno no servidor.");
            }
        }

        // GET: api/produtos/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            _logger.LogInformation("Recebida requisição para buscar o produto com ID: {ProdutoId}", id);

            try
            {
                var result = await _produtoService.GetByIdAsync(id);
                if (result == null)
                {
                    _logger.LogWarning("Produto com ID {ProdutoId} não foi localizado.", id);
                    return NotFound($"Produto com ID {id} não encontrado.");
                }

                _logger.LogInformation("Produto {ProdutoId} recuperado com sucesso.", id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar o produto ID {ProdutoId}.", id);
                return StatusCode(500, "Erro interno no servidor.");
            }
        }

        // PUT: api/produtos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] Produto? request)
        {
            if (request == null)
            {
                _logger.LogWarning("Recebida requisição para atualizar produto com corpo nulo.");
                return BadRequest("O corpo da requisição não pode ser nulo.");
            }

            _logger.LogInformation("Recebida requisição para atualizar o produto com ID: {ProdutoId}", id);

            try
            {
                var updated = await _produtoService.UpdateAsync(id, request);
                if (!updated)
                {
                    _logger.LogWarning("Não foi possível atualizar. Produto com ID {ProdutoId} não encontrado.", id);
                    return NotFound($"Não foi possível atualizar. Produto com ID {id} não encontrado.");
                }

                _logger.LogInformation("Produto {ProdutoId} atualizado com sucesso no banco de dados e OpenSearch.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico ao atualizar o produto ID {ProdutoId}.", id);
                return StatusCode(500, "Erro interno no servidor.");
            }
        }

        // DELETE: api/produtos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogInformation("Recebida requisição para remover o produto com ID: {ProdutoId}", id);

            try
            {
                var deleted = await _produtoService.DeleteAsync(id);
                if (!deleted)
                {
                    _logger.LogWarning("Não foi possível deletar. Produto com ID {ProdutoId} não encontrado.", id);
                    return NotFound($"Não foi possível deletar. Produto com ID {id} não encontrado.");
                }

                _logger.LogInformation("Produto {ProdutoId} removido com sucesso do banco de dados e OpenSearch.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico ao deletar o produto ID {ProdutoId}.", id);
                return StatusCode(500, "Erro interno no servidor.");
            }
        }
    }
}