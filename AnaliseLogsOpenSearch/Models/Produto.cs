namespace AnaliseLogsOpenSearch.Models
{
    public class Produto
    {
        public int Id { get; set; }
        public string ProdutoNome { get; set; } = string.Empty;
        public string DescricaoResumida { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public int Estoque { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public string? TraceId { get; set; }
        public string? SpanId { get; set; }
    }
}
