# Configurações básicas
$ApiUrl = "http://localhost:5122/api/Produtos"
$TotalItens = 20

# Arrays alinhados com exatamente 7 itens cada
$Nomes = @("Teclado Mecânico", "Mouse Gamer", "Monitor 144Hz", "Headset Wireless", "Cadeira Ergonômica", "Mousepad XL", "Gabinete ATX")
$Descricoes = @("RGB Switch Red", "Sensor Óptico 16000 DPI", "Painel IPS 1ms", "Som Surround 7.1", "Pistão Classe 4", "Superfície Speed", "Lateral Vidro Temperado")

Write-Host "🚀 Iniciando inserção automatizada de $TotalItens itens via PowerShell..." -ForegroundColor Cyan

1..$TotalItens | ForEach-Object {
    # Garante que o índice sorteado existe igualmente em ambos os arrays
    $Index = Get-Random -Minimum 0 -Maximum $Nomes.Count
    
    # Obtém a data atual em UTC para bater com o padrão 'Z' do Swagger
    $DataAtual = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")

    # Sorteia o preço flutuante inicial
    $PrecoSorteado = Get-Random -Minimum 49.90 -Maximum 1499.90
    
    # GARANTE DECIMAIS NO SQLITE: Força o ponto decimal invariantecultura e converte para double.
    # Isso impede o PowerShell de truncar zeros à direita (ex: mantendo 40.0 no JSON).
    $PrecoDecimal = [double]($PrecoSorteado.ToString("F2", [System.Globalization.CultureInfo]::InvariantCulture))

    # Monta o objeto e converte para JSON garantindo que a string final use UTF8 no pipeline
    $Body = @{
        produtoNome       = "$($Nomes[$Index]) - PS $_"
        descricaoResumida = $Descricoes[$Index]
        preco             = $PrecoDecimal
        estoque           = Get-Random -Minimum 5 -Maximum 120
        dataCriacao       = $DataAtual
    } | ConvertTo-Json -Depth 10

    try {
        # Converte explicitamente a string para bytes UTF-8 para evitar problemas com acentuação
        $BodyBytes = [System.Text.Encoding]::UTF8.GetBytes($Body)

        # Faz a chamada HTTP POST síncrona passando os bytes tratados
        $Response = Invoke-RestMethod -Uri $ApiUrl -Method Post -ContentType "application/json; charset=utf-8" -Body $BodyBytes
        Write-Host "✔️ Sucesso! Item $_ cadastrado. ID: $($Response.id) | TraceId: $($Response.traceId)" -ForegroundColor Green
    }
    catch {
        # Extrai a mensagem real enviada pela API (.NET/OpenSearch) em caso de erro 400/500
        $ErrorMessage = $_.Exception.Message
        if ($_.Exception.Response) {
            $ResponseStream = $_.Exception.Response.GetResponseStream()
            $ReadStream = New-Object System.IO.StreamReader($ResponseStream)
            $ErrorMessage = $ReadStream.ReadToEnd()
        }
        Write-Host "❌ Falha ao inserir o item $_ : $ErrorMessage" -ForegroundColor Red
    }
}

# Finalização com cor padrão aceita pelo console
Write-Host "✨ Carga de dados finalizada com sucesso!" -ForegroundColor Yellow
