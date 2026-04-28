using System.Collections.Concurrent;
using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.Users.Application.Common;
using Ambev.DeveloperEvaluation.Users.Application.Contracts;

namespace Ambev.DeveloperEvaluation.IoC.Aplicacao;

public sealed class UsersServiceEmMemoria : IUsersService
{
    private readonly ConcurrentDictionary<long, UserDetail> _users = new();
    private long _currentId = 2;

    public UsersServiceEmMemoria()
    {
        _users[1] = CriarUsuarioPadrao(1, "john", "john@example.com", "Customer", "Active");
        _users[2] = CriarUsuarioPadrao(2, "mary", "mary@example.com", "Manager", "Active");
    }

    public Task<Result<UserReference>> ObterPorIdAsync(long usuarioId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_users.TryGetValue(usuarioId, out var usuario)
            ? Result<UserReference>.Success(MapearReferencia(usuario))
            : Result<UserReference>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")]));
    }

    public Task<Result<IReadOnlyCollection<UserReference>>> ListarAtivosAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<UserReference> usuarios = _users.Values
            .Where(usuario => string.Equals(usuario.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Select(MapearReferencia)
            .ToArray();

        return Task.FromResult(Result<IReadOnlyCollection<UserReference>>.Success(usuarios));
    }

    public Task<Result<PagedResult<UserDetail>>> ListarAsync(UserListFilter filtro, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pagina = filtro.Page <= 0 ? 1 : filtro.Page;
        var tamanho = filtro.Size <= 0 ? 10 : Math.Min(filtro.Size, 100);
        var consulta = _users.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filtro.Username))
        {
            consulta = consulta.Where(usuario => usuario.Username.Contains(filtro.Username, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Email))
        {
            consulta = consulta.Where(usuario => usuario.Email.Contains(filtro.Email, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Role))
        {
            consulta = consulta.Where(usuario => string.Equals(usuario.Role, filtro.Role, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Status))
        {
            consulta = consulta.Where(usuario => string.Equals(usuario.Status, filtro.Status, StringComparison.OrdinalIgnoreCase));
        }

        consulta = AplicarOrdenacao(consulta, filtro.Order);
        var totalItems = consulta.Count();
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)tamanho);
        var data = consulta.Skip((pagina - 1) * tamanho).Take(tamanho).ToArray();

        return Task.FromResult(Result<PagedResult<UserDetail>>.Success(new PagedResult<UserDetail>(data, totalItems, pagina, totalPages)));
    }

    public Task<Result<UserDetail>> ObterDetalhePorIdAsync(long usuarioId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_users.TryGetValue(usuarioId, out var usuario)
            ? Result<UserDetail>.Success(usuario)
            : Result<UserDetail>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")]));
    }

    public Task<Result<UserDetail>> CriarAsync(UpsertUserRequest requisicao, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validacao = Validar(requisicao);
        if (validacao is not null)
        {
            return Task.FromResult(validacao);
        }

        var id = Interlocked.Increment(ref _currentId);
        var usuario = MapearDetalhe(id, requisicao);
        _users[id] = usuario;
        return Task.FromResult(Result<UserDetail>.Success(usuario));
    }

    public Task<Result<UserDetail>> AtualizarAsync(long usuarioId, UpsertUserRequest requisicao, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_users.ContainsKey(usuarioId))
        {
            return Task.FromResult(Result<UserDetail>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")]));
        }

        var validacao = Validar(requisicao);
        if (validacao is not null)
        {
            return Task.FromResult(validacao);
        }

        var usuario = MapearDetalhe(usuarioId, requisicao);
        _users[usuarioId] = usuario;
        return Task.FromResult(Result<UserDetail>.Success(usuario));
    }

    public Task<Result<UserDetail>> RemoverAsync(long usuarioId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_users.TryRemove(usuarioId, out var usuario)
            ? Result<UserDetail>.Success(usuario)
            : Result<UserDetail>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")]));
    }

    private static UserReference MapearReferencia(UserDetail usuario)
    {
        return new UserReference(usuario.Id, usuario.Username, usuario.Email, usuario.Status, usuario.Role, string.Equals(usuario.Status, "Active", StringComparison.OrdinalIgnoreCase));
    }

    private static UserDetail MapearDetalhe(long id, UpsertUserRequest requisicao)
    {
        return new UserDetail(id, requisicao.Email, requisicao.Username, requisicao.Name, requisicao.Address, requisicao.Phone, requisicao.Status, requisicao.Role);
    }

    private static Result<UserDetail>? Validar(UpsertUserRequest requisicao)
    {
        var erros = new List<ResultError>();

        if (string.IsNullOrWhiteSpace(requisicao.Email))
        {
            erros.Add(new ResultError("email_obrigatorio", "O e-mail do usuário é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(requisicao.Username))
        {
            erros.Add(new ResultError("username_obrigatorio", "O username do usuário é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(requisicao.Password))
        {
            erros.Add(new ResultError("password_obrigatoria", "A senha do usuário é obrigatória."));
        }

        return erros.Count > 0 ? Result<UserDetail>.Validation(erros) : null;
    }

    private static IEnumerable<UserDetail> AplicarOrdenacao(IEnumerable<UserDetail> consulta, string? order)
    {
        var clausulas = string.IsNullOrWhiteSpace(order)
            ? ["username asc"]
            : order.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IOrderedEnumerable<UserDetail>? ordenado = null;

        foreach (var clausula in clausulas)
        {
            var partes = clausula.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var campo = partes[0].ToLowerInvariant();
            var descendente = partes.Length > 1 && string.Equals(partes[1], "desc", StringComparison.OrdinalIgnoreCase);

            ordenado = (ordenado, campo, descendente) switch
            {
                (null, "email", true) => consulta.OrderByDescending(usuario => usuario.Email),
                (null, "email", false) => consulta.OrderBy(usuario => usuario.Email),
                (null, _, true) => consulta.OrderByDescending(usuario => usuario.Username),
                (null, _, false) => consulta.OrderBy(usuario => usuario.Username),
                (_, "email", true) => ordenado.ThenByDescending(usuario => usuario.Email),
                (_, "email", false) => ordenado.ThenBy(usuario => usuario.Email),
                (_, _, true) => ordenado.ThenByDescending(usuario => usuario.Username),
                _ => ordenado.ThenBy(usuario => usuario.Username)
            };
        }

        return ordenado ?? consulta.OrderBy(usuario => usuario.Username);
    }

    private static UserDetail CriarUsuarioPadrao(long id, string username, string email, string role, string status)
    {
        return new UserDetail(
            id,
            email,
            username,
            new UserNameData(char.ToUpperInvariant(username[0]) + username[1..], "Doe"),
            new UserAddressData("São Paulo", "Rua Exemplo", (int)id * 10, "01000-000", new UserGeolocationData("-23.5505", "-46.6333")),
            "11999999999",
            status,
            role);
    }
}