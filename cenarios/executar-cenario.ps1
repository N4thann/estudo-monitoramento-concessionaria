<#
.SYNOPSIS
    Executa um cenário de falha na concessionária e mede detecção e recuperação (MTTR).

.DESCRIPTION
    Pré-requisito: stack completa no ar (docker compose --profile app up -d).

    Linha do tempo de cada execução:
      1. Sobe tráfego contínuo com o k6 (cenarios/k6/trafego.js) e espera a linha de base estabilizar.
      2. T0: injeta a falha (para contêiner, injeta latência no Toxiproxy, gera carga etc.).
      3. Durante a falha, sonda o sistema como um usuário a cada 5 s (painel e planilha) e acompanha os
         alertas do Prometheus.
      4. T1: desfaz a falha (simula o conserto) e continua acompanhando até o serviço e os alertas voltarem.
      5. Coleta evidências dos 3 pilares no intervalo: métricas (Prometheus), logs (Loki) e um trace de
         erro (Tempo), e grava um relatório em cenarios/resultados/.

.EXAMPLE
    .\cenarios\executar-cenario.ps1 -Cenario relatorios-fora
.EXAMPLE
    .\cenarios\executar-cenario.ps1 -Cenario relatorios-lento -LatenciaMs 1500 -DuracaoFalhaSegundos 120
.EXAMPLE
    .\cenarios\executar-cenario.ps1 -Reprocessar .\cenarios\resultados\dados\2026-09-29_2000_relatorios-fora.json
#>
param(
    [ValidateSet('relatorios-fora', 'relatorios-lento', 'carros-fora', 'mongo-fora', 'carga-alta', 'memoria-insuficiente')]
    [string]$Cenario,

    # Refaz só a coleta de evidências e o relatório a partir dos dados de uma execução anterior
    # (cenarios/resultados/dados/*.json), sem injetar a falha de novo.
    [string]$Reprocessar,

    # Quanto tempo a falha fica ativa antes do "conserto".
    [int]$DuracaoFalhaSegundos = 150,

    # Tempo máximo esperando serviço e alertas voltarem ao normal depois do conserto.
    [int]$EsperaRecuperacaoSegundos = 240,

    # relatorios-lento: atraso injetado em cada resposta de Relatórios. Acima de 3000 estoura o timeout por tentativa.
    [int]$LatenciaMs = 4000,

    # carga-alta: requisições por segundo no pico. O sistema satura perto de 255 req/s (Relatórios a 100% de CPU).
    [int]$CargaAlvo = 350,

    # Taxa do tráfego de fundo (req/s).
    [int]$Taxa = 5
)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
Add-Type -AssemblyName System.Net.Http
[Console]::OutputEncoding = [Text.Encoding]::UTF8

$PROM = 'http://127.0.0.1:9090'
$LOKI = 'http://127.0.0.1:3100'
$TEMPO = 'http://127.0.0.1:3200'
$TOXIPROXY = 'http://127.0.0.1:8474'
$BFF = 'http://127.0.0.1:5101/api'
$GRAFANA = 'http://localhost:3000'
$K6_TRAFEGO = 'concessionaria-k6-trafego'
$K6_CARGA = 'concessionaria-k6-carga'
# O Toxiproxy recusa user agents de navegador (proteção contra CSRF), e o do Invoke-RestMethod começa com "Mozilla".
$UA_TOXIPROXY = 'executar-cenario'

# ─── Utilitários ─────────────────────────────────────────────────────────────

function Agora { [DateTimeOffset]::UtcNow }

function Escrever([string]$mensagem) {
    Write-Host ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $mensagem)
}

# docker escreve progresso no stderr; no Windows PowerShell isso vira erro com ErrorActionPreference=Stop.
function ExecutarDocker {
    $anterior = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $saida = & docker.exe @args 2>&1 | ForEach-Object { "$_" }
    $codigo = $LASTEXITCODE
    $ErrorActionPreference = $anterior
    if ($codigo -ne 0) { throw "docker $($args -join ' ') falhou (código $codigo): $($saida -join ' ')" }
    return $saida
}

function IdContainer([string]$servico) {
    $id = ExecutarDocker compose --profile app ps -q $servico | Select-Object -Last 1
    if (-not $id) { throw "Contêiner do serviço '$servico' não encontrado. A stack está no ar (docker compose --profile app up -d)?" }
    return $id.Trim()
}

# O Prometheus responde JSON em UTF-8 sem declarar o charset, e o Windows PowerShell 5.1 decodifica como Latin-1
# ("RelatÃ³rios"). Lê os bytes e decodifica como UTF-8 explicitamente.
function ObterJson([string]$url) {
    $resposta = Invoke-WebRequest $url -UseBasicParsing
    return ([Text.Encoding]::UTF8.GetString($resposta.RawContentStream.ToArray()) | ConvertFrom-Json)
}

# Conserta textos já gravados com a decodificação errada (dados de execuções anteriores).
function CorrigirTexto([string]$texto) {
    if (-not $texto -or $texto -notmatch 'Ã') { return $texto }
    return [Text.Encoding]::UTF8.GetString([Text.Encoding]::GetEncoding(28591).GetBytes($texto))
}

function ConsultarPrometheus([string]$consulta, $em = $null) {
    $url = "$PROM/api/v1/query?query=$([uri]::EscapeDataString($consulta))"
    if ($em) { $url += "&time=$(([DateTimeOffset]$em).ToUnixTimeSeconds())" }
    return @((ObterJson $url).data.result | Where-Object { $_ })
}

# O Prometheus manda frações de segundo com 9 dígitos; o .NET só aceita até 7. No PowerShell 7 o JSON já vem como data.
function ConverterData($valor) {
    if ($valor -is [datetime]) { return [DateTimeOffset]$valor.ToUniversalTime() }
    return [DateTimeOffset]::Parse(($valor -replace '(\.\d{7})\d+', '$1'), [Globalization.CultureInfo]::InvariantCulture)
}

function Segundos($inicio, $fim) {
    if ($null -eq $inicio -or $null -eq $fim) { return $null }
    return [math]::Round(($fim - $inicio).TotalSeconds)
}

function FormatarSegundos($valor) {
    if ($null -eq $valor) { return '—' }
    if ($valor -ge 60) { return ('{0} s ({1}m{2:00}s)' -f $valor, [math]::Floor($valor / 60), ($valor % 60)) }
    return "$valor s"
}

function FormatarHora($data) {
    if ($null -eq $data) { return '—' }
    return $data.ToLocalTime().ToString('HH:mm:ss')
}

function Numero($valor, [int]$casas = 0) {
    if ($null -eq $valor -or "$valor" -eq 'NaN') { return '—' }
    return ([double]$valor).ToString("N$casas", [Globalization.CultureInfo]'pt-BR')
}

function ParaIso($data) { if ($null -eq $data) { return $null } return $data.ToString('o') }

function DeIso($valor) {
    if ($null -eq $valor -or "$valor" -eq '') { return $null }
    if ($valor -is [datetime]) { return [DateTimeOffset]$valor.ToUniversalTime() }
    return [DateTimeOffset]::Parse($valor, [Globalization.CultureInfo]::InvariantCulture)
}

$estadoCarregado = $null
if ($Reprocessar) {
    $estadoCarregado = Get-Content $Reprocessar -Raw -Encoding UTF8 | ConvertFrom-Json
    $Cenario = $estadoCarregado.Cenario
    $DuracaoFalhaSegundos = $estadoCarregado.Parametros.DuracaoFalhaSegundos
    $LatenciaMs = $estadoCarregado.Parametros.LatenciaMs
    $CargaAlvo = $estadoCarregado.Parametros.CargaAlvo
    $Taxa = $estadoCarregado.Parametros.Taxa
}
elseif (-not $Cenario) { throw 'Informe -Cenario (ou -Reprocessar com o JSON de uma execução anterior).' }
# CpuAlta olha a média de 2 min e exige mais 2 min (for): a carga precisa durar mais que o padrão de 150 s.
if ($Cenario -eq 'carga-alta' -and -not $PSBoundParameters.ContainsKey('DuracaoFalhaSegundos')) { $DuracaoFalhaSegundos = 270 }

# ─── Definição dos cenários ──────────────────────────────────────────────────
# Cada cenário diz como injetar e desfazer a falha, a hipótese do que deve acontecer e quais alertas se espera.

$cenarios = @{
    'relatorios-fora'      = @{
        Titulo    = 'Relatórios fora do ar'
        Hipotese  = 'Carros e Motos não conseguem calcular indicadores nem gerar planilhas. O painel continua respondendo, mas sem indicadores (degradação controlada); as planilhas falham com 502 no BFF após os retries e o timeout total de 8 s.'
        Injecao   = 'docker compose stop relatorios'
        Conserto  = 'docker compose start relatorios'
        Esperados = @('ServicoFora', 'FalhaIntegracaoRelatorios', 'PainelDegradado', 'RetriesFrequentes', 'TaxaErro5xxAlta')
        Injetar   = { ExecutarDocker compose --profile app stop relatorios | Out-Null }
        Restaurar = { ExecutarDocker compose --profile app start relatorios | Out-Null }
    }
    'relatorios-lento'     = @{
        Titulo    = "Relatórios lento ($LatenciaMs ms por resposta)"
        Hipotese  = "Cada resposta de Relatórios passa a demorar $LatenciaMs ms (latência injetada no Toxiproxy). Acima de 3 s estoura o timeout por tentativa: Carros/Motos repetem a chamada e desistem no timeout total de 8 s, como se Relatórios estivesse fora, mas com o serviço no ar (up = 1). Abaixo de 3 s, só a latência sobe."
        Injecao   = "Toxiproxy: toxic 'latency' de $LatenciaMs ms (jitter 10%) no proxy relatorios"
        Conserto  = 'Toxiproxy: remove o toxic'
        Esperados = @('LatenciaP95Alta', 'FalhaIntegracaoRelatorios', 'PainelDegradado', 'RetriesFrequentes', 'TaxaErro5xxAlta')
        Injetar   = {
            try { Invoke-RestMethod -UserAgent $UA_TOXIPROXY -Method Delete "$TOXIPROXY/proxies/relatorios/toxics/latencia" | Out-Null } catch { }
            $corpo = @{ name = 'latencia'; type = 'latency'; stream = 'downstream'; toxicity = 1.0
                attributes = @{ latency = $LatenciaMs; jitter = [int]($LatenciaMs / 10) } } | ConvertTo-Json
            Invoke-RestMethod -UserAgent $UA_TOXIPROXY -Method Post "$TOXIPROXY/proxies/relatorios/toxics" -Body $corpo -ContentType 'application/json' | Out-Null
        }
        Restaurar = { Invoke-RestMethod -UserAgent $UA_TOXIPROXY -Method Delete "$TOXIPROXY/proxies/relatorios/toxics/latencia" | Out-Null }
    }
    'carros-fora'          = @{
        Titulo    = 'Carros fora do ar'
        Hipotese  = 'O BFF não consegue falar com Carros: rotas de carros e o painel (que depende de Carros e Motos) falham com 502. Motos continua atendendo as rotas dela normalmente (falha isolada).'
        Injecao   = 'docker compose stop carros'
        Conserto  = 'docker compose start carros'
        Esperados = @('ServicoFora', 'TaxaErro5xxAlta')
        Injetar   = { ExecutarDocker compose --profile app stop carros | Out-Null }
        Restaurar = { ExecutarDocker compose --profile app start carros | Out-Null }
    }
    'mongo-fora'           = @{
        Titulo    = 'MongoDB fora do ar'
        Hipotese  = 'Carros e Motos continuam no ar (up = 1), mas toda operação que lê o banco falha após o timeout de seleção de servidor (3 s): 500 em Carros/Motos, 502 no BFF. O /saude passa a responder Unhealthy. É uma falha que o "up" não enxerga: aparece na taxa de erro, nos logs e nos spans do MongoDB.'
        Injecao   = 'docker compose stop mongo'
        Conserto  = 'docker compose start mongo'
        Esperados = @('TaxaErro5xxAlta', 'LatenciaP95Alta')
        Injetar   = { ExecutarDocker compose --profile app stop mongo | Out-Null }
        Restaurar = { ExecutarDocker compose --profile app start mongo | Out-Null }
    }
    'carga-alta'           = @{
        Titulo    = "Carga alta (rampa até $CargaAlvo req/s)"
        Hipotese  = "O k6 sobe a carga até $CargaAlvo req/s, concentrada no painel (passa pelas 4 APIs) e nas planilhas (CPU em Relatórios). Com 1 CPU por contêiner, a CPU satura, as requisições enfileiram e a latência sobe; se piorar, surgem timeouts."
        Injecao   = "k6 (cenarios/k6/carga.js) com ALVO=$CargaAlvo"
        Conserto  = 'interrompe o k6 de carga'
        Esperados = @('CpuAlta', 'LatenciaP95Alta')
        Injetar   = {
            try { ExecutarDocker rm -f $K6_CARGA | Out-Null } catch { }
            $patamar = [math]::Max(30, $DuracaoFalhaSegundos - 30)
            ExecutarDocker compose run -d --name $K6_CARGA -e "BFF_URL=http://bff:8080" -e "ALVO=$CargaAlvo" -e "PATAMAR=$($patamar)s" k6 run --quiet /cenarios/k6/carga.js | Out-Null
        }
        Restaurar = { try { ExecutarDocker rm -f $K6_CARGA | Out-Null } catch { } }
    }
    'memoria-insuficiente' = @{
        Titulo    = 'Memória insuficiente em Relatórios'
        Hipotese  = 'O limite de memória do contêiner de Relatórios é reduzido em execução (docker update) para pouco acima do uso atual. O processo passa a trabalhar acima de 90% do limite: o kernel tira páginas de arquivo da memória, o GC coleta com mais frequência e, se o uso passar do limite, o kernel encerra o processo (OOM kill) e o Docker o reinicia.'
        Injecao   = 'docker update --memory <uso atual + 3%> relatorios'
        Conserto  = 'docker update --memory 384m relatorios'
        Esperados = @('MemoriaAlta')
        Injetar   = {
            $uso = (ConsultarPrometheus 'max(dotnet_process_memory_working_set_bytes{job="concessionaria",servico="relatorios"})')[0].value[1]
            $limite = [long]([double]$uso * 1.03)
            $script:detalheInjecao = "limite reduzido de 384 MiB para $([math]::Round($limite / 1MB)) MiB (uso no momento: $([math]::Round([double]$uso / 1MB)) MiB)"
            ExecutarDocker update --memory $limite --memory-swap $limite (IdContainer relatorios) | Out-Null
        }
        Restaurar = { ExecutarDocker update --memory 384m --memory-swap 384m (IdContainer relatorios) | Out-Null }
    }
}

$definicao = $cenarios[$Cenario]
$script:detalheInjecao = $null

# ─── Sondas: o sistema visto por um usuário ──────────────────────────────────
# Requisições assíncronas (HttpClient), para uma sonda lenta não atrasar o acompanhamento dos alertas.

$cliente = New-Object System.Net.Http.HttpClient
$cliente.Timeout = [TimeSpan]::FromSeconds(30)
$sondasPendentes = New-Object System.Collections.ArrayList
$sondas = New-Object System.Collections.ArrayList
$numeroSonda = 0

function IniciarSondas {
    foreach ($alvo in @(@{ Nome = 'painel'; Url = "$BFF/painel" }, @{ Nome = 'planilha'; Url = "$BFF/carros/relatorios/estoque" })) {
        $script:numeroSonda++
        $requisicao = New-Object System.Net.Http.HttpRequestMessage ([System.Net.Http.HttpMethod]::Get), $alvo.Url
        $idOperacao = "sonda-$Cenario-$($alvo.Nome)-$script:numeroSonda"
        $requisicao.Headers.Add('X-Id-Operacao', $idOperacao)
        $null = $sondasPendentes.Add([pscustomobject]@{
                Alvo = $alvo.Nome; Id = $idOperacao; Inicio = (Agora); Tarefa = $cliente.SendAsync($requisicao) })
    }
}

function ColetarSondas {
    foreach ($pendente in @($sondasPendentes)) {
        if (-not $pendente.Tarefa.IsCompleted) { continue }
        $status = 0; $detalhe = ''; $ok = $false
        if ($pendente.Tarefa.IsFaulted -or $pendente.Tarefa.IsCanceled) {
            $detalhe = 'sem resposta (timeout ou conexão recusada)'
        }
        else {
            $resposta = $pendente.Tarefa.Result
            $status = [int]$resposta.StatusCode
            $ok = $status -eq 200
            if ($pendente.Alvo -eq 'painel' -and $ok) {
                $corpo = $resposta.Content.ReadAsStringAsync().Result
                # Painel sem indicadores = degradado: responde, mas incompleto para o usuário.
                if ($corpo -match '"quantidadeVendas"\s*:\s*null') { $ok = $false; $detalhe = 'degradado (sem indicadores)' }
            }
            $resposta.Dispose()
        }
        $null = $sondas.Add([pscustomobject]@{
                Alvo = $pendente.Alvo; Id = $pendente.Id; Inicio = $pendente.Inicio; Status = $status; Ok = $ok; Detalhe = $detalhe
                Ms = [math]::Round(((Agora) - $pendente.Inicio).TotalMilliseconds) })
        $sondasPendentes.Remove($pendente)
    }
}

# ─── Alertas: quando cada um ficou pendente, disparou e resolveu ─────────────

$alertas = @{}

function AcompanharAlertas {
    $ativos = @((ObterJson "$PROM/api/v1/alerts").data.alerts | Where-Object { $_ })
    $vistos = @{}
    foreach ($alerta in $ativos) {
        $rotulos = $alerta.labels
        $partes = @($rotulos.servico, $rotulos.operacao, $rotulos.pipeline_name) | Where-Object { $_ }
        $chave = "$($rotulos.alertname)|$($partes -join ' / ')"
        $vistos[$chave] = $true
        if (-not $alertas.ContainsKey($chave)) {
            $alertas[$chave] = [pscustomobject]@{
                Alerta = $rotulos.alertname; Alvo = ($partes -join ' / '); Severidade = $rotulos.severidade
                Pendente = (ConverterData $alerta.activeAt); Disparou = $null; Resolveu = $null; Valor = $alerta.value
                Descricao = $alerta.annotations.descricao
            }
        }
        $registro = $alertas[$chave]
        $registro.Resolveu = $null
        if ($alerta.state -eq 'firing' -and -not $registro.Disparou) {
            $registro.Disparou = Agora
            $registro.Descricao = $alerta.annotations.descricao
            Escrever "  ALERTA disparou: $chave"
        }
    }
    foreach ($registro in $alertas.Values) {
        $chave = "$($registro.Alerta)|$($registro.Alvo)"
        if (-not $vistos.ContainsKey($chave) -and -not $registro.Resolveu) {
            $registro.Resolveu = Agora
            if ($registro.Disparou) { Escrever "  alerta resolvido: $chave" }
        }
    }
    return $ativos.Count
}

# Laço principal: sonda a cada 5 s, alertas a cada 5 s, coleta de sondas a cada 200 ms (a duração da sonda é medida na coleta).
function Acompanhar([int]$segundos, [scriptblock]$parar = $null) {
    $fim = (Agora).AddSeconds($segundos)
    $proximaSonda = Agora
    while ((Agora) -lt $fim) {
        if ((Agora) -ge $proximaSonda) {
            IniciarSondas
            $ativos = AcompanharAlertas
            $proximaSonda = $proximaSonda.AddSeconds(5)
            if ($parar -and (& $parar $ativos)) { return }
        }
        ColetarSondas
        Start-Sleep -Milliseconds 200
    }
}

# ─── Execução ────────────────────────────────────────────────────────────────

$pastaResultados = Join-Path $PSScriptRoot 'resultados'
$pastaDados = Join-Path $pastaResultados 'dados'

if ($estadoCarregado) {
    Escrever "Reprocessando: $($definicao.Titulo) ($Reprocessar)"
    $T0 = DeIso $estadoCarregado.T0; $T1 = DeIso $estadoCarregado.T1; $T2 = DeIso $estadoCarregado.T2
    $script:detalheInjecao = $estadoCarregado.Detalhe
    foreach ($a in @($estadoCarregado.Alertas | Where-Object { $_ })) {
        $alertas["$($a.Alerta)|$($a.Alvo)"] = [pscustomobject]@{
            Alerta = $a.Alerta; Alvo = $a.Alvo; Severidade = $a.Severidade; Descricao = (CorrigirTexto $a.Descricao); Valor = $null
            Pendente = DeIso $a.Pendente; Disparou = DeIso $a.Disparou; Resolveu = DeIso $a.Resolveu
        }
    }
    foreach ($s in @($estadoCarregado.Sondas | Where-Object { $_ })) {
        $null = $sondas.Add([pscustomobject]@{
                Alvo = $s.Alvo; Id = $s.Id; Inicio = DeIso $s.Inicio; Status = [int]$s.Status; Ok = [bool]$s.Ok; Detalhe = $s.Detalhe; Ms = [int]$s.Ms })
    }
}
else {
Escrever "Cenário: $($definicao.Titulo)"
try { Invoke-WebRequest "$BFF/painel" -UseBasicParsing -TimeoutSec 20 | Out-Null }
catch { throw "O BFF não respondeu em $BFF. Suba a stack completa antes: docker compose --profile app up -d" }

Escrever 'Subindo tráfego de fundo (k6)...'
try { ExecutarDocker rm -f $K6_TRAFEGO | Out-Null } catch { }
$duracaoTrafego = 90 + 120 + $DuracaoFalhaSegundos + $EsperaRecuperacaoSegundos + 60
ExecutarDocker compose run -d --name $K6_TRAFEGO -e "TAXA=$Taxa" -e "DURACAO=$($duracaoTrafego)s" k6 run --quiet /cenarios/k6/trafego.js | Out-Null

try {
    Escrever 'Aguardando a linha de base: nenhum alerta ativo e 60 s de tráfego normal...'
    $limiteBase = (Agora).AddSeconds(300)
    while (((Invoke-RestMethod "$PROM/api/v1/alerts").data.alerts).Count -gt 0) {
        if ((Agora) -gt $limiteBase) { throw 'Ainda há alertas ativos de uma execução anterior. Aguarde eles resolverem e rode de novo.' }
        Start-Sleep -Seconds 5
    }
    Start-Sleep -Seconds 60

    $T0 = Agora
    Escrever "T0 - injetando a falha: $($definicao.Injecao)"
    & $definicao.Injetar
    if ($script:detalheInjecao) { Escrever "  $($script:detalheInjecao)" }

    Acompanhar $DuracaoFalhaSegundos

    $T1 = Agora
    Escrever "T1 - desfazendo a falha: $($definicao.Conserto)"
    & $definicao.Restaurar

    # Para quando o serviço está bem (últimas sondas OK) e não há mais alerta ativo.
    Acompanhar $EsperaRecuperacaoSegundos {
        param($ativos)
        $recentes = @($sondas | Where-Object { $_.Inicio -gt $T1 } | Select-Object -Last 4)
        return ($ativos -eq 0 -and $recentes.Count -eq 4 -and -not ($recentes | Where-Object { -not $_.Ok }))
    }
    Start-Sleep -Seconds 2
    ColetarSondas
    $T2 = Agora
}
finally {
    try { ExecutarDocker rm -f $K6_TRAFEGO | Out-Null } catch { }
    if ($Cenario -eq 'carga-alta') { try { ExecutarDocker rm -f $K6_CARGA | Out-Null } catch { } }
}

# Guarda o que foi medido, para refazer evidências e relatório depois com -Reprocessar.
$estado = [ordered]@{
    Cenario    = $Cenario
    Parametros = [ordered]@{ DuracaoFalhaSegundos = $DuracaoFalhaSegundos; LatenciaMs = $LatenciaMs; CargaAlvo = $CargaAlvo; Taxa = $Taxa }
    Detalhe    = $script:detalheInjecao
    T0         = ParaIso $T0; T1 = ParaIso $T1; T2 = ParaIso $T2
    Alertas    = @($alertas.Values | ForEach-Object { [ordered]@{
                Alerta = $_.Alerta; Alvo = $_.Alvo; Severidade = $_.Severidade; Descricao = $_.Descricao
                Pendente = ParaIso $_.Pendente; Disparou = ParaIso $_.Disparou; Resolveu = ParaIso $_.Resolveu } })
    Sondas     = @($sondas | ForEach-Object { [ordered]@{
                Alvo = $_.Alvo; Id = $_.Id; Inicio = ParaIso $_.Inicio; Status = $_.Status; Ok = $_.Ok; Detalhe = $_.Detalhe; Ms = $_.Ms } })
}
New-Item -ItemType Directory -Force $pastaDados | Out-Null
$arquivoDados = Join-Path $pastaDados ("{0}_{1}.json" -f $T0.ToLocalTime().ToString('yyyy-MM-dd_HHmm'), $Cenario)
[IO.File]::WriteAllText($arquivoDados, ($estado | ConvertTo-Json -Depth 5), (New-Object System.Text.UTF8Encoding($false)))
Escrever "Dados da execução: $arquivoDados"
}

# ─── Cálculo das medidas ─────────────────────────────────────────────────────

$sondasOrdenadas = @($sondas | Sort-Object Inicio)
$primeiraFalhaUsuario = ($sondasOrdenadas | Where-Object { $_.Inicio -ge $T0 -and -not $_.Ok } | Select-Object -First 1).Inicio
$ultimaFalha = $sondasOrdenadas | Where-Object { -not $_.Ok } | Select-Object -Last 1
$recuperado = $null
if ($ultimaFalha) {
    $recuperado = ($sondasOrdenadas | Where-Object { $_.Inicio -gt $ultimaFalha.Inicio -and $_.Ok } | Select-Object -First 1).Inicio
}
$disparos = @($alertas.Values | Where-Object { $_.Disparou -and $_.Disparou -ge $T0 } | Sort-Object Disparou)
$primeiroPendente = ($alertas.Values | Where-Object { $_.Pendente -ge $T0.AddSeconds(-5) } | Sort-Object Pendente | Select-Object -First 1).Pendente
$deteccao = if ($disparos.Count) { $disparos[0].Disparou } else { $null }
$ultimoResolvido = ($alertas.Values | Where-Object { $_.Resolveu } | Sort-Object Resolveu | Select-Object -Last 1).Resolveu
$duracaoFalha = [int](Segundos $T0 $T1)

# ─── Evidências: métricas ────────────────────────────────────────────────────

Escrever 'Coletando evidências (Prometheus, Loki, Tempo)...'
$rotaReal = 'job="concessionaria",http_route!~"/metrics|/saude"'
$janela = "$($duracaoFalha)s"
$porServico = [ordered]@{}
foreach ($s in 'bff', 'carros', 'motos', 'relatorios') { $porServico[$s] = [ordered]@{ Req = $null; Erros5xx = $null; P95Base = $null; P95Falha = $null; Cpu = $null; Memoria = $null } }
function Preencher([string]$campo, [string]$consulta, $em) {
    foreach ($r in (ConsultarPrometheus $consulta $em)) {
        if ($porServico.Contains($r.metric.servico)) { $porServico[$r.metric.servico][$campo] = $r.value[1] }
    }
}
$indisponivel = @(); $polly = @(); $negocio = @()
try {
Preencher 'Req' "sum by (servico) (increase(http_server_request_duration_seconds_count{$rotaReal}[$janela]))" $T1
Preencher 'Erros5xx' "sum by (servico) (increase(http_server_request_duration_seconds_count{$rotaReal,http_response_status_code=~`"5..`"}[$janela]))" $T1
Preencher 'P95Base' "histogram_quantile(0.95, sum by (servico, le) (rate(http_server_request_duration_seconds_bucket{$rotaReal}[60s])))" $T0
Preencher 'P95Falha' "histogram_quantile(0.95, sum by (servico, le) (rate(http_server_request_duration_seconds_bucket{$rotaReal}[$janela])))" $T1
Preencher 'Cpu' "max_over_time((sum by (servico) (rate(dotnet_process_cpu_time_seconds_total{job=`"concessionaria`"}[1m])) / on (servico) max by (servico) (dotnet_process_cpu_count{job=`"concessionaria`"}))[$($janela):15s])" $T1
Preencher 'Memoria' "max_over_time((max by (servico) (dotnet_process_memory_working_set_bytes{job=`"concessionaria`"}) / on (servico) max by (servico) (concessionaria_processo_memoria_limite_bytes{job=`"concessionaria`"} > 0))[$($janela):15s])" $T1
$indisponivel = ConsultarPrometheus "count_over_time((up{job=`"concessionaria`"} == 0)[$($janela):15s]) * 15" $T1
$polly = ConsultarPrometheus "sum by (servico, pipeline_name, event_name) (increase(resilience_polly_strategy_events_total{job=`"concessionaria`",event_name=~`"OnRetry|OnTimeout|OnCircuitOpened`"}[$janela])) > 0" $T1
foreach ($contador in 'concessionaria_relatorios_falhas_integracao_total', 'concessionaria_painel_degradado_total') {
    foreach ($r in (ConsultarPrometheus "sum by (servico) (increase($contador{job=`"concessionaria`"}[$janela]))" $T1)) {
        $negocio += [pscustomobject]@{ Nome = $contador; Servico = $r.metric.servico; Valor = $r.value[1] }
    }
}
}
catch { Escrever "  aviso: falha ao consultar o Prometheus: $($_.Exception.Message) $($_.InvocationInfo.PositionMessage)" }

# ─── Evidências: logs ────────────────────────────────────────────────────────

$inicioNs = "$($T0.ToUnixTimeMilliseconds())000000"
$fimNs = "$($T1.AddSeconds(30).ToUnixTimeMilliseconds())000000"
$consultaLogs = '{service_namespace="concessionaria"} | severity_text=~"Warning|Error|Fatal"'
# Contagem por serviço, nível e modelo da mensagem numa consulta de métrica do Loki (count_over_time): baixar as
# linhas em si é pesado, porque uma falha gera milhares de logs de vários KB cada (com os metadados estruturados).
$gruposLogs = @()
$totalLogs = 0
$exemplosLogs = @()
try {
    $segundosLogs = [int](Segundos $T0 $T1.AddSeconds(30))
    $contagem = "sum by (service_name, severity_text, message_template_text) (count_over_time($consultaLogs [$($segundosLogs)s]))"
    $resultado = @((Invoke-RestMethod "$LOKI/loki/api/v1/query?query=$([uri]::EscapeDataString($contagem))&time=$fimNs").data.result | Where-Object { $_ })
    $totalLogs = ($resultado | ForEach-Object { [int]$_.value[1] } | Measure-Object -Sum).Sum
    $gruposLogs = @($resultado | Sort-Object { [int]$_.value[1] } -Descending | Select-Object -First 8 | ForEach-Object {
            [pscustomobject]@{ Quantidade = [int]$_.value[1]; Servico = $_.metric.service_name; Nivel = $_.metric.severity_text; Modelo = $_.metric.message_template_text } })

    # Primeira ocorrência de cada serviço, com o trace_id que liga o log ao trace.
    foreach ($servico in 'bff', 'carros', 'motos', 'relatorios') {
        $consultaServico = $consultaLogs -replace '^\{service_namespace="concessionaria"\}', "{service_namespace=`"concessionaria`", service_name=`"$servico`"}"
        $resposta = Invoke-RestMethod -Headers @{ 'X-Loki-Response-Encoding-Flags' = 'categorize-labels' } `
            "$LOKI/loki/api/v1/query_range?query=$([uri]::EscapeDataString($consultaServico))&start=$inicioNs&end=$fimNs&limit=1&direction=forward"
        foreach ($fluxo in @($resposta.data.result | Where-Object { $_ })) {
            foreach ($valor in $fluxo.values) {
                $meta = $valor[2].structuredMetadata
                $exemplosLogs += [pscustomobject]@{
                    Servico = $servico; Nivel = $meta.severity_text; Linha = $valor[1]; TraceId = $meta.trace_id; IdOperacao = $meta.IdOperacao
                    Hora = [DateTimeOffset]::FromUnixTimeMilliseconds([long]("$($valor[0])".Substring(0, 13)))
                }
            }
        }
    }
}
catch { Escrever "  aviso: não foi possível consultar o Loki ($($_.Exception.Message))" }

# ─── Evidências: traces ──────────────────────────────────────────────────────

# Busca 20 e fica com os mais longos que têm span raiz (a busca também devolve pedaços de trace ainda
# incompletos, sem nome de operação). O Tempo omite os zeros à esquerda do traceID: PadLeft devolve os 32.
function BuscarTraces([string]$traceql, [int]$limite = 3) {
    $url = "$TEMPO/api/search?q=$([uri]::EscapeDataString($traceql))&start=$($T0.ToUnixTimeSeconds())&end=$($T1.AddSeconds(30).ToUnixTimeSeconds())&limit=20"
    try {
        $encontrados = @((Invoke-RestMethod $url).traces | Where-Object { $_ -and $_.traceID -and $_.rootTraceName })
        foreach ($t in $encontrados) { $t.traceID = $t.traceID.PadLeft(32, '0') }
        return @($encontrados | Sort-Object { [double]$_.durationMs } -Descending | Select-Object -First $limite)
    }
    catch { Escrever "  aviso: falha ao buscar traces no Tempo: $($_.Exception.Message)"; return @() }
}
# Erro de verdade = 5xx ou chamada sem resposta (sem status HTTP). Os 400/409 da mistura do k6 também marcam o
# span de cliente do BFF como erro, e não interessam aqui.
$consultaTracesErro = '{ resource.service.namespace = "concessionaria" && status = error && (span.http.response.status_code >= 500 || span.http.response.status_code = nil) }'
$consultaTracesLentos = '{ resource.service.namespace = "concessionaria" && kind = server && duration > 1s }'
$tracesErro = BuscarTraces $consultaTracesErro 3
$tracesLentos = BuscarTraces $consultaTracesLentos 3

function ResumirExcecao([string]$tipo, [string]$mensagem) {
    if (-not $tipo -and -not $mensagem) { return $null }
    $inicio = ($mensagem -split '\r?\n')[0]
    if ($inicio.Length -gt 110) { $inicio = $inicio.Substring(0, 110) + '…' }
    return (@($tipo, $inicio) | Where-Object { $_ }) -join ': '
}

# Dentro de um trace de erro, lista os spans com erro do mais profundo (causa) ao mais externo (efeito).
$spansErro = @()
$traceAnalisado = $null
if ($tracesErro.Count) {
    $traceAnalisado = $tracesErro[0].traceID
    try {
        $trace = Invoke-RestMethod "$TEMPO/api/traces/$traceAnalisado"
        foreach ($lote in $trace.batches) {
            $servico = ($lote.resource.attributes | Where-Object key -eq 'service.name').value.stringValue
            foreach ($escopo in $lote.scopeSpans) {
                foreach ($span in $escopo.spans) {
                    if ($span.status.code -ne 'STATUS_CODE_ERROR') { continue }
                    $excecao = ($span.events | Where-Object name -eq 'exception' | Select-Object -First 1).attributes |
                        Where-Object key -eq 'exception.message' | ForEach-Object { $_.value.stringValue }
                    $atributo = { param($chave) ($span.attributes | Where-Object key -eq $chave | Select-Object -First 1).value |
                            ForEach-Object { @($_.stringValue, $_.intValue) | Where-Object { $_ } | Select-Object -First 1 } }
                    $destino = @((& $atributo 'server.address'), (& $atributo 'server.port')) | Where-Object { $_ }
                    $rotulo = $span.name
                    $urlCompleta = & $atributo 'url.full'
                    if ($urlCompleta) { $rotulo += " $(([uri]$urlCompleta).AbsolutePath)" }
                    if ($destino) { $rotulo += " → $($destino -join ':')" }
                    elseif (& $atributo 'db.collection.name') { $rotulo += " (Mongo: $(& $atributo 'db.collection.name'))" }
                    $spansErro += [pscustomobject]@{
                        Servico = $servico; Span = $rotulo
                        Inicio = [decimal]$span.startTimeUnixNano
                        Ms = [math]::Round(([decimal]$span.endTimeUnixNano - [decimal]$span.startTimeUnixNano) / 1000000)
                        # O driver do Mongo põe a exceção em atributos do próprio span (exception.type/.message),
                        # com a mensagem gigante (estado do cluster e stack trace): fica só o começo.
                        Mensagem = (@($span.status.message, $excecao, (ResumirExcecao (& $atributo 'exception.type') (& $atributo 'exception.message'))) |
                            Where-Object { $_ } | Select-Object -First 1)
                    }
                }
            }
        }
        $spansErro = @($spansErro | Sort-Object Inicio -Descending)
    }
    catch { Escrever "  aviso: não foi possível abrir o trace $traceAnalisado ($($_.Exception.Message))" }
}

# ─── Relatório ───────────────────────────────────────────────────────────────

function LinkExplorarTrace([string]$traceId) {
    $painel = @{ t = @{ datasource = 'tempo'; queries = @(@{ refId = 'A'; datasource = @{ type = 'tempo'; uid = 'tempo' }; queryType = 'traceql'; query = $traceId }) } } | ConvertTo-Json -Depth 6 -Compress
    return "$GRAFANA/explore?schemaVersion=1&orgId=1&panes=$([uri]::EscapeDataString($painel))"
}
function Celula([string]$texto) { if (-not $texto) { return '' } return ($texto -replace '\|', '\|' -replace '\r?\n', ' ') }

$r = New-Object System.Text.StringBuilder
function L([string]$linha = '') { [void]$r.AppendLine($linha) }

L "# Cenário: $($definicao.Titulo)"
L
L "Executado em $($T0.ToLocalTime().ToString('dd/MM/yyyy HH:mm:ss')) com ``cenarios/executar-cenario.ps1 -Cenario $Cenario``."
L "Tráfego de fundo: k6 a $Taxa req/s (painel, listagens, planilhas, vendas inválidas e revendas)."
L
L '## Hipótese'
L
L $definicao.Hipotese
L
L "- **Falha injetada (T0):** $($definicao.Injecao)$(if ($script:detalheInjecao) { " — $($script:detalheInjecao)" })"
L "- **Conserto (T1):** $($definicao.Conserto)"
L "- **Alertas esperados:** $($definicao.Esperados -join ', ')"
L
L '## Linha do tempo e MTTR'
L
L '| Marco | Hora | Desde T0 |'
L '|---|---|---|'
L "| T0: falha injetada | $(FormatarHora $T0) | 0 s |"
L "| Primeira falha vista pelo usuário (sonda) | $(FormatarHora $primeiraFalhaUsuario) | $(FormatarSegundos (Segundos $T0 $primeiraFalhaUsuario)) |"
L "| Primeiro alerta pendente (condição verdadeira) | $(FormatarHora $primeiroPendente) | $(FormatarSegundos (Segundos $T0 $primeiroPendente)) |"
L "| **Detecção: primeiro alerta disparado** | $(FormatarHora $deteccao) | **$(FormatarSegundos (Segundos $T0 $deteccao))** |"
L "| T1: falha desfeita (conserto) | $(FormatarHora $T1) | $(FormatarSegundos (Segundos $T0 $T1)) |"
L "| **Serviço recuperado (sondas OK de novo)** | $(FormatarHora $recuperado) | **$(FormatarSegundos (Segundos $T0 $recuperado))** |"
L "| Último alerta resolvido | $(FormatarHora $ultimoResolvido) | $(FormatarSegundos (Segundos $T0 $ultimoResolvido)) |"
L
L "- **TTD (tempo até detectar):** $(FormatarSegundos (Segundos $T0 $deteccao))"
L "- **Tempo de reparo (T0 → T1, simulado):** $(FormatarSegundos (Segundos $T0 $T1))"
L "- **Recuperação após o conserto (T1 → serviço OK):** $(FormatarSegundos (Segundos $T1 $recuperado))"
L "- **MTTR (T0 → serviço OK):** $(FormatarSegundos (Segundos $T0 $recuperado))"
L "- **Alertas resolvidos após o conserto:** $(FormatarSegundos (Segundos $T1 $ultimoResolvido)) (as regras de evento olham os últimos 2 min, por isso resolvem depois do serviço)"
L
L '> Numa operação real o reparo não é fixo: ele é o tempo de diagnóstico + correção. As seções abaixo mostram o'
L '> que a observabilidade entrega para encurtar o diagnóstico: o alerta aponta o sintoma, o dashboard e os logs'
L '> localizam o serviço, e o trace mostra o span exato onde a falha começou.'
L
L '## Alertas'
L
if ($alertas.Count) {
    L '| Alerta | Alvo | Severidade | Pendente | Disparou | Resolveu | Esperado? |'
    L '|---|---|---|---|---|---|---|'
    foreach ($a in ($alertas.Values | Sort-Object Pendente)) {
        $esperado = if ($definicao.Esperados -contains $a.Alerta) { 'sim' } else { 'não' }
        L "| $($a.Alerta) | $(Celula $a.Alvo) | $($a.Severidade) | T0 + $(FormatarSegundos (Segundos $T0 $a.Pendente)) | $(if ($a.Disparou) { "T0 + $(FormatarSegundos (Segundos $T0 $a.Disparou))" } else { 'não chegou a disparar' }) | $(if (-not $a.Resolveu) { 'ativo ao fim' } elseif ($a.Resolveu -lt $T1) { "T0 + $(FormatarSegundos (Segundos $T0 $a.Resolveu)) (antes do conserto)" } else { "T1 + $(FormatarSegundos (Segundos $T1 $a.Resolveu))" }) | $esperado |"
    }
    $naoDisparados = @($definicao.Esperados | Where-Object { $nome = $_; -not ($alertas.Values | Where-Object { $_.Alerta -eq $nome -and $_.Disparou }) })
    if ($naoDisparados.Count) { L; L "Esperados que **não** dispararam: $($naoDisparados -join ', ')." }
    L
    L 'Mensagens dos alertas disparados:'
    L
    foreach ($a in ($alertas.Values | Where-Object Disparou | Sort-Object Disparou)) { L "- **$($a.Alerta)** ($(Celula $a.Alvo)): $($a.Descricao)" }
}
else { L 'Nenhum alerta ficou ativo durante o cenário.' }
L
L '## Sondas (visão do usuário)'
L
$sondasFalha = @($sondasOrdenadas | Where-Object { $_.Inicio -ge $T0 -and $_.Inicio -lt $T1 })
foreach ($alvo in 'painel', 'planilha') {
    $grupo = @($sondasFalha | Where-Object Alvo -eq $alvo)
    $falhas = @($grupo | Where-Object { -not $_.Ok })
    $tempos = @($grupo | ForEach-Object Ms | Sort-Object)
    $p95 = if ($tempos.Count) { $tempos[[math]::Min($tempos.Count - 1, [math]::Floor($tempos.Count * 0.95))] } else { $null }
    $status = ($falhas | Group-Object { if ($_.Detalhe) { "$($_.Status) $($_.Detalhe)" } else { "$($_.Status)" } } | ForEach-Object { "$($_.Name) × $($_.Count)" }) -join '; '
    $rotulo = if ($alvo -eq 'painel') { 'GET /api/painel' } else { 'GET /api/carros/relatorios/estoque' }
    L "- **$rotulo** durante a falha: $($falhas.Count) de $($grupo.Count) sondas com problema; p95 $(Numero $p95) ms$(if ($status) { " ($status)" })."
}
$exemplo = $sondasFalha | Where-Object { -not $_.Ok } | Select-Object -First 1
if ($exemplo) { L "- Exemplo para seguir nos logs: ``X-Id-Operacao: $($exemplo.Id)`` (buscar ``IdOperacao`` no Loki)." }
L
L '## Métricas durante a falha (Prometheus)'
L
L "Janela: T0 → T1 ($duracaoFalha s). Linha de base do p95: 60 s antes de T0."
L
L '| Serviço | Requisições | Respostas 5xx | % 5xx | p95 antes | p95 na falha | CPU máx. | Memória máx. (% do limite) |'
L '|---|---|---|---|---|---|---|---|'
foreach ($s in $porServico.Keys) {
    $m = $porServico[$s]
    $pct = if ($m.Req -and [double]$m.Req -gt 0 -and $null -ne $m.Erros5xx) { Numero (100 * [double]$m.Erros5xx / [double]$m.Req) 1 } else { '—' }
    $p95a = if ($null -ne $m.P95Base -and "$($m.P95Base)" -ne 'NaN') { "$(Numero (1000 * [double]$m.P95Base)) ms" } else { '—' }
    $p95f = if ($null -ne $m.P95Falha -and "$($m.P95Falha)" -ne 'NaN') { "$(Numero (1000 * [double]$m.P95Falha)) ms" } else { '—' }
    $cpu = if ($null -ne $m.Cpu) { "$(Numero (100 * [double]$m.Cpu) 1)%" } else { '—' }
    $mem = if ($null -ne $m.Memoria) { "$(Numero (100 * [double]$m.Memoria) 1)%" } else { '—' }
    L "| $s | $(Numero $m.Req) | $(Numero $m.Erros5xx) | $pct | $p95a | $p95f | $cpu | $mem |"
}
L
foreach ($i in $indisponivel) { if ([double]$i.value[1] -gt 0) { L "- ``up == 0`` em **$($i.metric.servico)** por cerca de $(Numero $i.value[1]) s da janela." } }
foreach ($p in $polly) { L "- Polly: **$(Numero $p.value[1])** eventos ``$($p.metric.event_name)`` de $($p.metric.servico) no pipeline ``$($p.metric.pipeline_name)``." }
foreach ($n in $negocio) {
    if ([double]$n.Valor -gt 0) { L "- ``$($n.Nome)`` ($($n.Servico)): +$(Numero $n.Valor)." }
}
L
L '> Os números de "increase" do Prometheus são extrapolados a partir das amostras (scrape de 15 s), por isso'
L '> podem ter casas decimais ou diferir levemente da contagem exata.'
L
L '## Logs de aviso e erro (Loki)'
L
L "Consulta: ``$consultaLogs``, de T0 a T1 + 30 s. Total: **$(Numero $totalLogs)** linhas."
L
if ($gruposLogs.Count) {
    L 'Mensagens mais frequentes (agrupadas pelo modelo do Serilog, `message_template_text`):'
    L
    L '| Qtd. | Serviço | Nível | Modelo da mensagem |'
    L '|---|---|---|---|'
    foreach ($g in $gruposLogs) { L "| $(Numero $g.Quantidade) | $($g.Servico) | $($g.Nivel) | $(Celula $g.Modelo) |" }
    L
    L 'Primeira ocorrência de cada serviço (o `trace_id` abre o trace no Tempo; o `IdOperacao` é o mesmo do ProblemDetails):'
    L
    L '```'
    foreach ($e in ($exemplosLogs | Sort-Object Hora)) {
        L "$($e.Hora.ToLocalTime().ToString('HH:mm:ss')) [$($e.Servico)] [$($e.Nivel)] $($e.Linha)"
        L "         trace_id=$($e.TraceId) IdOperacao=$($e.IdOperacao)"
    }
    L '```'
}
else { L 'Nenhum log de aviso ou erro no intervalo.' }
L
L '## Traces (Tempo)'
L
L "- Com erro (5xx ou sem resposta): ``$consultaTracesErro``"
L "- Lentos: ``$consultaTracesLentos``"
L
if ($tracesErro.Count -or $tracesLentos.Count) {
    L '| Tipo | Trace | Início | Serviço raiz | Operação | Duração |'
    L '|---|---|---|---|---|---|'
    foreach ($par in @(@('com erro', $tracesErro), @('lento (> 1 s)', $tracesLentos))) {
        foreach ($t in $par[1]) {
            $inicioTrace = [DateTimeOffset]::FromUnixTimeMilliseconds([long]([decimal]$t.startTimeUnixNano / 1000000)).ToLocalTime().ToString('HH:mm:ss')
            L "| $($par[0]) | [$($t.traceID.PadLeft(32, '0').Substring(0, 12))…]($(LinkExplorarTrace $t.traceID.PadLeft(32, '0'))) | $inicioTrace | $($t.rootServiceName) | $(Celula $t.rootTraceName) | $(Numero $t.durationMs) ms |"
        }
    }
}
else { L 'Nenhum trace com erro ou acima de 1 s no intervalo.' }
if ($spansErro.Count) {
    L
    L "**Causa raiz pelo trace ``$traceAnalisado``**: spans com erro, do mais interno (onde a falha nasceu) ao mais externo (efeito visto pelo usuário):"
    L
    L '| Serviço | Span | Duração | Mensagem |'
    L '|---|---|---|---|'
    foreach ($s in ($spansErro | Select-Object -First 10)) { L "| $($s.Servico) | $(Celula $s.Span) | $(Numero $s.Ms) ms | $(Celula $s.Mensagem) |" }
}
L
L '## Onde ver no Grafana'
L
$de = $T0.AddMinutes(-2).ToUnixTimeMilliseconds(); $ate = $T2.AddMinutes(1).ToUnixTimeMilliseconds()
L "- Dashboard no intervalo do cenário: $GRAFANA/d/concessionaria-visao-geral?orgId=1&from=$de&to=$ate"
L "- Alertas: $PROM/alerts"

New-Item -ItemType Directory -Force $pastaResultados | Out-Null
$arquivo = Join-Path $pastaResultados ("{0}_{1}.md" -f $T0.ToLocalTime().ToString('yyyy-MM-dd_HHmm'), $Cenario)
[IO.File]::WriteAllText($arquivo, $r.ToString(), (New-Object System.Text.UTF8Encoding($false)))

Escrever "Relatório: $arquivo"
Escrever ("TTD {0} | recuperação após conserto {1} | MTTR {2} | alertas disparados: {3}" -f
    (FormatarSegundos (Segundos $T0 $deteccao)), (FormatarSegundos (Segundos $T1 $recuperado)),
    (FormatarSegundos (Segundos $T0 $recuperado)), (($disparos | ForEach-Object Alerta | Select-Object -Unique) -join ', '))
