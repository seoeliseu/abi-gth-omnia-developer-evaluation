using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Common.Idempotencia;
using Ambev.DeveloperEvaluation.Application.Products.Contracts;
using Ambev.DeveloperEvaluation.Application.Sales.Contracts;
using Ambev.DeveloperEvaluation.Application.Sales.Repositories;
using Ambev.DeveloperEvaluation.Application.Users.Contracts;
using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Sales.Services;

public sealed class SalesApplicationService : ISalesApplicationService
{
    private const string EscopoCriacao = "sales:create";
    private const string EscopoCancelamentoVenda = "sales:cancel";
    private const string EscopoCancelamentoItem = "sales:cancel-item";

    private readonly ISaleRepository _saleRepository;
    private readonly IUsersService _usersService;
    private readonly IProductsService _productsService;
    private readonly IIdempotencyStore _idempotencyStore;

    public SalesApplicationService(
        ISaleRepository saleRepository,
        IUsersService usersService,
        IProductsService productsService,
        IIdempotencyStore idempotencyStore)
    {
        _saleRepository = saleRepository;
        _usersService = usersService;
        _productsService = productsService;
        _idempotencyStore = idempotencyStore;
    }

    public async Task<Result<SaleDetail>> CriarAsync(CreateSaleRequest requisicao, string? chaveIdempotencia, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validacao = ValidarCriacao(requisicao);
        if (validacao is not null)
        {
            return validacao;
        }

        var fingerprint = GerarFingerprintCriacao(requisicao);
        var resultadoIdempotente = ObterResultadoIdempotente(EscopoCriacao, chaveIdempotencia, fingerprint);
        if (resultadoIdempotente is not null)
        {
            return resultadoIdempotente;
        }

        var usuario = await _usersService.ObterPorIdAsync(requisicao.ClienteId, cancellationToken);
        if (usuario.IsFailure || usuario.Value is null)
        {
            return Result<SaleDetail>.NotFound([new ResultError("cliente_nao_encontrado", "O cliente informado não foi encontrado.")]);
        }

        var produtos = await _productsService.ListarPorIdsAsync(requisicao.Itens.Select(item => item.ProductId).Distinct().ToArray(), cancellationToken);
        if (produtos.IsFailure || produtos.Value is null)
        {
            return Result<SaleDetail>.NotFound([new ResultError("produto_nao_encontrado", "Um ou mais produtos informados não foram encontrados.")]);
        }

        var produtosPorId = produtos.Value.ToDictionary(produto => produto.Id);
        if (requisicao.Itens.Any(item => !produtosPorId.ContainsKey(item.ProductId)))
        {
            return Result<SaleDetail>.NotFound([new ResultError("produto_nao_encontrado", "Um ou mais produtos informados não foram encontrados.")]);
        }

        try
        {
            var sale = new Sale(
                requisicao.Numero,
                requisicao.DataVenda,
                requisicao.ClienteId,
                usuario.Value.NomeUsuario,
                requisicao.FilialId,
                requisicao.FilialNome);

            foreach (var item in requisicao.Itens)
            {
                var produto = produtosPorId[item.ProductId];
                sale.AdicionarItem(produto.Id, produto.Titulo, item.Quantidade, produto.Preco);
            }

            await _saleRepository.AdicionarAsync(sale, cancellationToken);

            var resultado = Result<SaleDetail>.Success(MapearDetalhe(sale));
            ArmazenarResultadoIdempotente(EscopoCriacao, chaveIdempotencia, fingerprint, resultado);
            return resultado;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
        {
            return Result<SaleDetail>.BusinessRule([new ResultError("regra_negocio", ex.Message)]);
        }
    }

    public async Task<Result<SaleDetail>> ObterPorIdAsync(Guid saleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sale = await _saleRepository.ObterPorIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<SaleDetail>.NotFound([new ResultError("sale_nao_encontrada", "A venda informada não foi encontrada.")]);
        }

        return Result<SaleDetail>.Success(MapearDetalhe(sale));
    }

    public async Task<Result<PagedResult<SaleSnapshot>>> ListarAsync(SaleListFilter filtro, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pagina = filtro.Page <= 0 ? 1 : filtro.Page;
        var tamanho = filtro.Size <= 0 ? 10 : Math.Min(filtro.Size, 100);

        var consulta = (await _saleRepository.ListarAsync(cancellationToken)).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filtro.Numero))
        {
            consulta = consulta.Where(sale => sale.Numero.Contains(filtro.Numero, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.ClienteNome))
        {
            consulta = consulta.Where(sale => sale.ClienteNome.Contains(filtro.ClienteNome, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.FilialNome))
        {
            consulta = consulta.Where(sale => sale.FilialNome.Contains(filtro.FilialNome, StringComparison.OrdinalIgnoreCase));
        }

        if (filtro.Cancelada.HasValue)
        {
            consulta = consulta.Where(sale => sale.Cancelada == filtro.Cancelada.Value);
        }

        if (filtro.DataMinima.HasValue)
        {
            consulta = consulta.Where(sale => sale.DataVenda >= filtro.DataMinima.Value);
        }

        if (filtro.DataMaxima.HasValue)
        {
            consulta = consulta.Where(sale => sale.DataVenda <= filtro.DataMaxima.Value);
        }

        consulta = AplicarOrdenacao(consulta, filtro.Order);

        var totalItems = consulta.Count();
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)tamanho);
        var dadosPagina = consulta
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(MapearResumo)
            .ToArray();

        var resultado = new PagedResult<SaleSnapshot>(dadosPagina, totalItems, pagina, totalPages);
        return Result<PagedResult<SaleSnapshot>>.Success(resultado);
    }

    public async Task<Result<SaleDetail>> AtualizarAsync(Guid saleId, UpdateSaleRequest requisicao, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validacao = ValidarAtualizacao(requisicao);
        if (validacao is not null)
        {
            return validacao;
        }

        var sale = await _saleRepository.ObterPorIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<SaleDetail>.NotFound([new ResultError("sale_nao_encontrada", "A venda informada não foi encontrada.")]);
        }

        var usuario = await _usersService.ObterPorIdAsync(requisicao.ClienteId, cancellationToken);
        if (usuario.IsFailure || usuario.Value is null)
        {
            return Result<SaleDetail>.NotFound([new ResultError("cliente_nao_encontrado", "O cliente informado não foi encontrado.")]);
        }

        var produtos = await _productsService.ListarPorIdsAsync(requisicao.Itens.Select(item => item.ProductId).Distinct().ToArray(), cancellationToken);
        if (produtos.IsFailure || produtos.Value is null)
        {
            return Result<SaleDetail>.NotFound([new ResultError("produto_nao_encontrado", "Um ou mais produtos informados não foram encontrados.")]);
        }

        var produtosPorId = produtos.Value.ToDictionary(produto => produto.Id);
        if (requisicao.Itens.Any(item => !produtosPorId.ContainsKey(item.ProductId)))
        {
            return Result<SaleDetail>.NotFound([new ResultError("produto_nao_encontrado", "Um ou mais produtos informados não foram encontrados.")]);
        }

        try
        {
            sale.AtualizarCabecalho(requisicao.DataVenda, requisicao.ClienteId, usuario.Value.NomeUsuario, requisicao.FilialId, requisicao.FilialNome);

            foreach (var itemAtual in sale.Items.Where(item => !item.Cancelado).ToArray())
            {
                if (!requisicao.Itens.Any(itemRequisicao => itemRequisicao.ProductId == itemAtual.ProductId))
                {
                    sale.CancelarItem(itemAtual.Id);
                }
            }

            foreach (var item in requisicao.Itens)
            {
                var produto = produtosPorId[item.ProductId];
                sale.AdicionarOuAtualizarItem(produto.Id, produto.Titulo, item.Quantidade, produto.Preco);
            }

            await _saleRepository.AtualizarAsync(sale, cancellationToken);
            return Result<SaleDetail>.Success(MapearDetalhe(sale));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
        {
            return Result<SaleDetail>.BusinessRule([new ResultError("regra_negocio", ex.Message)]);
        }
    }

    public async Task<Result<SaleDetail>> CancelarVendaAsync(Guid saleId, string? chaveIdempotencia, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fingerprint = saleId.ToString("N");
        var resultadoIdempotente = ObterResultadoIdempotente(EscopoCancelamentoVenda, chaveIdempotencia, fingerprint);
        if (resultadoIdempotente is not null)
        {
            return resultadoIdempotente;
        }

        var sale = await _saleRepository.ObterPorIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<SaleDetail>.NotFound([new ResultError("sale_nao_encontrada", "A venda informada não foi encontrada.")]);
        }

        sale.CancelarVenda();
        await _saleRepository.AtualizarAsync(sale, cancellationToken);

        var resultado = Result<SaleDetail>.Success(MapearDetalhe(sale));
        ArmazenarResultadoIdempotente(EscopoCancelamentoVenda, chaveIdempotencia, fingerprint, resultado);
        return resultado;
    }

    public async Task<Result<SaleDetail>> CancelarItemAsync(Guid saleId, Guid saleItemId, string? chaveIdempotencia, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fingerprint = $"{saleId:N}:{saleItemId:N}";
        var resultadoIdempotente = ObterResultadoIdempotente(EscopoCancelamentoItem, chaveIdempotencia, fingerprint);
        if (resultadoIdempotente is not null)
        {
            return resultadoIdempotente;
        }

        var sale = await _saleRepository.ObterPorIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Result<SaleDetail>.NotFound([new ResultError("sale_nao_encontrada", "A venda informada não foi encontrada.")]);
        }

        try
        {
            sale.CancelarItem(saleItemId);
            await _saleRepository.AtualizarAsync(sale, cancellationToken);

            var resultado = Result<SaleDetail>.Success(MapearDetalhe(sale));
            ArmazenarResultadoIdempotente(EscopoCancelamentoItem, chaveIdempotencia, fingerprint, resultado);
            return resultado;
        }
        catch (InvalidOperationException ex)
        {
            return Result<SaleDetail>.BusinessRule([new ResultError("regra_negocio", ex.Message)]);
        }
    }

    private Result<SaleDetail>? ValidarCriacao(CreateSaleRequest requisicao)
    {
        var erros = new List<ResultError>();

        if (string.IsNullOrWhiteSpace(requisicao.Numero))
        {
            erros.Add(new ResultError("numero_obrigatorio", "O número da venda é obrigatório."));
        }

        if (requisicao.ClienteId <= 0)
        {
            erros.Add(new ResultError("cliente_invalido", "O cliente deve possuir um identificador válido."));
        }

        if (requisicao.FilialId <= 0)
        {
            erros.Add(new ResultError("filial_invalida", "A filial deve possuir um identificador válido."));
        }

        if (string.IsNullOrWhiteSpace(requisicao.FilialNome))
        {
            erros.Add(new ResultError("filial_nome_obrigatorio", "O nome da filial é obrigatório."));
        }

        ValidarItens(requisicao.Itens?.Select(item => (item.ProductId, item.Quantidade)).ToArray() ?? [], erros);

        return erros.Count > 0 ? Result<SaleDetail>.Validation(erros) : null;
    }

    private Result<SaleDetail>? ValidarAtualizacao(UpdateSaleRequest requisicao)
    {
        var erros = new List<ResultError>();

        if (requisicao.ClienteId <= 0)
        {
            erros.Add(new ResultError("cliente_invalido", "O cliente deve possuir um identificador válido."));
        }

        if (requisicao.FilialId <= 0)
        {
            erros.Add(new ResultError("filial_invalida", "A filial deve possuir um identificador válido."));
        }

        if (string.IsNullOrWhiteSpace(requisicao.FilialNome))
        {
            erros.Add(new ResultError("filial_nome_obrigatorio", "O nome da filial é obrigatório."));
        }

        ValidarItens(requisicao.Itens?.Select(item => (item.ProductId, item.Quantidade)).ToArray() ?? [], erros);

        return erros.Count > 0 ? Result<SaleDetail>.Validation(erros) : null;
    }

    private static void ValidarItens(IReadOnlyCollection<(long ProductId, int Quantidade)> itens, ICollection<ResultError> erros)
    {
        if (itens.Count == 0)
        {
            erros.Add(new ResultError("itens_obrigatorios", "A venda deve possuir ao menos um item."));
            return;
        }

        if (itens.Any(item => item.ProductId <= 0))
        {
            erros.Add(new ResultError("produto_invalido", "Todos os itens devem possuir um produto válido."));
        }

        if (itens.Any(item => item.Quantidade <= 0))
        {
            erros.Add(new ResultError("quantidade_invalida", "Todos os itens devem possuir quantidade maior que zero."));
        }

        if (itens.GroupBy(item => item.ProductId).Any(grupo => grupo.Count() > 1))
        {
            erros.Add(new ResultError("produto_duplicado", "Não é permitido informar o mesmo produto mais de uma vez na venda."));
        }
    }

    private Result<SaleDetail>? ObterResultadoIdempotente(string escopo, string? chaveIdempotencia, string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(chaveIdempotencia))
        {
            return null;
        }

        if (!_idempotencyStore.TryGet(escopo, chaveIdempotencia, out var entrada) || entrada is null)
        {
            return null;
        }

        if (!string.Equals(entrada.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            return Result<SaleDetail>.Conflict([new ResultError("idempotency_key_invalida", "A chave de idempotência já foi usada para uma operação diferente.")]);
        }

        return entrada.Resultado as Result<SaleDetail>;
    }

    private void ArmazenarResultadoIdempotente(string escopo, string? chaveIdempotencia, string fingerprint, Result<SaleDetail> resultado)
    {
        if (string.IsNullOrWhiteSpace(chaveIdempotencia))
        {
            return;
        }

        _idempotencyStore.Set(escopo, chaveIdempotencia, fingerprint, resultado);
    }

    private static string GerarFingerprintCriacao(CreateSaleRequest requisicao)
    {
        var itens = string.Join(';', requisicao.Itens.OrderBy(item => item.ProductId).Select(item => $"{item.ProductId}:{item.Quantidade}"));
        return $"{requisicao.Numero}|{requisicao.ClienteId}|{requisicao.FilialId}|{requisicao.DataVenda:O}|{itens}";
    }

    private static SaleDetail MapearDetalhe(Sale sale)
    {
        return new SaleDetail(
            sale.Id,
            sale.Numero,
            sale.DataVenda,
            sale.ClienteId,
            sale.ClienteNome,
            sale.FilialId,
            sale.FilialNome,
            sale.ValorTotal,
            sale.Cancelada,
            sale.Items
                .Select(item => new SaleItemSnapshot(
                    item.Id,
                    item.ProductId,
                    item.ProductTitle,
                    item.Quantidade,
                    item.ValorUnitario,
                    item.PercentualDesconto,
                    item.ValorDesconto,
                    item.ValorTotal,
                    item.Cancelado))
                .ToArray());
    }

    private static SaleSnapshot MapearResumo(Sale sale)
    {
        return new SaleSnapshot(
            sale.Id,
            sale.Numero,
            sale.DataVenda,
            sale.ClienteId,
            sale.ClienteNome,
            sale.FilialId,
            sale.FilialNome,
            sale.ValorTotal,
            sale.Cancelada);
    }

    private static IEnumerable<Sale> AplicarOrdenacao(IEnumerable<Sale> consulta, string? order)
    {
        IOrderedEnumerable<Sale>? ordenado = null;
        var clausulas = string.IsNullOrWhiteSpace(order)
            ? ["dataVenda desc"]
            : order.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var clausula in clausulas)
        {
            var partes = clausula.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var campo = partes[0];
            var descendente = partes.Length > 1 && string.Equals(partes[1], "desc", StringComparison.OrdinalIgnoreCase);

            ordenado = AplicarOrdem(ordenado ?? consulta.OrderBy(sale => 0), campo, descendente);
        }

        return ordenado ?? consulta.OrderByDescending(sale => sale.DataVenda);
    }

    private static IOrderedEnumerable<Sale> AplicarOrdem(IOrderedEnumerable<Sale> origem, string campo, bool descendente)
    {
        return campo.ToLowerInvariant() switch
        {
            "numero" => descendente ? origem.ThenByDescending(sale => sale.Numero) : origem.ThenBy(sale => sale.Numero),
            "clientenome" => descendente ? origem.ThenByDescending(sale => sale.ClienteNome) : origem.ThenBy(sale => sale.ClienteNome),
            "filialnome" => descendente ? origem.ThenByDescending(sale => sale.FilialNome) : origem.ThenBy(sale => sale.FilialNome),
            "valortotal" => descendente ? origem.ThenByDescending(sale => sale.ValorTotal) : origem.ThenBy(sale => sale.ValorTotal),
            "cancelada" => descendente ? origem.ThenByDescending(sale => sale.Cancelada) : origem.ThenBy(sale => sale.Cancelada),
            _ => descendente ? origem.ThenByDescending(sale => sale.DataVenda) : origem.ThenBy(sale => sale.DataVenda)
        };
    }
}