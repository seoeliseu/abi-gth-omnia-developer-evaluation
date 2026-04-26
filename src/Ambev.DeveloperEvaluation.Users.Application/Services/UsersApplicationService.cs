using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.Users.Application.Common;
using Ambev.DeveloperEvaluation.Users.Application.Contracts;
using Ambev.DeveloperEvaluation.Users.Application.Repositories;
using Ambev.DeveloperEvaluation.Users.Domain.Entities;
using Ambev.DeveloperEvaluation.Users.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Users.Application.Services;

public sealed class UsersApplicationService : IUsersService
{
    private readonly IUserRepository _userRepository;

    public UsersApplicationService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserReference>> ObterPorIdAsync(long usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _userRepository.ObterPorIdAsync(usuarioId, cancellationToken);
        return usuario is null
            ? Result<UserReference>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")])
            : Result<UserReference>.Success(MapearReferencia(usuario));
    }

    public async Task<Result<IReadOnlyCollection<UserReference>>> ListarAtivosAsync(CancellationToken cancellationToken)
    {
        var usuarios = await _userRepository.ListarAtivosAsync(cancellationToken);
        return Result<IReadOnlyCollection<UserReference>>.Success(usuarios.Select(MapearReferencia).ToArray());
    }

    public async Task<Result<PagedResult<UserDetail>>> ListarAsync(UserListFilter filtro, CancellationToken cancellationToken)
    {
        var pagina = await _userRepository.ListarAsync(filtro, cancellationToken);
        return Result<PagedResult<UserDetail>>.Success(new PagedResult<UserDetail>(pagina.Data.Select(MapearDetalhe).ToArray(), pagina.TotalItems, pagina.CurrentPage, pagina.TotalPages));
    }

    public async Task<Result<UserDetail>> ObterDetalhePorIdAsync(long usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _userRepository.ObterPorIdAsync(usuarioId, cancellationToken);
        return usuario is null
            ? Result<UserDetail>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")])
            : Result<UserDetail>.Success(MapearDetalhe(usuario));
    }

    public async Task<Result<UserDetail>> CriarAsync(UpsertUserRequest requisicao, CancellationToken cancellationToken)
    {
        var validacao = Validar(requisicao);
        if (validacao is not null) return validacao;

        var usuario = User.Criar(
            requisicao.Email,
            requisicao.Username,
            requisicao.Password,
            new UserName(requisicao.Name.Firstname, requisicao.Name.Lastname),
            new UserAddress(
                requisicao.Address.City,
                requisicao.Address.Street,
                requisicao.Address.Number,
                requisicao.Address.Zipcode,
                new UserGeolocation(requisicao.Address.Geolocation.Lat, requisicao.Address.Geolocation.Long)),
            requisicao.Phone,
            requisicao.Status,
            requisicao.Role);

        var persistido = await _userRepository.AdicionarAsync(usuario, cancellationToken);
        return Result<UserDetail>.Success(MapearDetalhe(persistido));
    }

    public async Task<Result<UserDetail>> AtualizarAsync(long usuarioId, UpsertUserRequest requisicao, CancellationToken cancellationToken)
    {
        var usuario = await _userRepository.ObterPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null) return Result<UserDetail>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")]);

        var validacao = Validar(requisicao);
        if (validacao is not null) return validacao;

        usuario.Atualizar(
            requisicao.Email,
            requisicao.Username,
            requisicao.Password,
            new UserName(requisicao.Name.Firstname, requisicao.Name.Lastname),
            new UserAddress(
                requisicao.Address.City,
                requisicao.Address.Street,
                requisicao.Address.Number,
                requisicao.Address.Zipcode,
                new UserGeolocation(requisicao.Address.Geolocation.Lat, requisicao.Address.Geolocation.Long)),
            requisicao.Phone,
            requisicao.Status,
            requisicao.Role);

        await _userRepository.AtualizarAsync(usuario, cancellationToken);
        return Result<UserDetail>.Success(MapearDetalhe(usuario));
    }

    public async Task<Result<UserDetail>> RemoverAsync(long usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _userRepository.ObterPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null) return Result<UserDetail>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")]);

        await _userRepository.RemoverAsync(usuarioId, cancellationToken);
        return Result<UserDetail>.Success(MapearDetalhe(usuario));
    }

    private static UserReference MapearReferencia(User usuario)
        => new(usuario.Id, usuario.Username, usuario.Email, usuario.Status, usuario.Role, usuario.Status == "Active");

    private static UserDetail MapearDetalhe(User usuario)
        => new(
            usuario.Id,
            usuario.Email,
            usuario.Username,
            usuario.Password,
            new UserNameData(usuario.Name.Firstname, usuario.Name.Lastname),
            new UserAddressData(usuario.Address.City, usuario.Address.Street, usuario.Address.Number, usuario.Address.Zipcode, new UserGeolocationData(usuario.Address.Geolocation.Lat, usuario.Address.Geolocation.Long)),
            usuario.Phone,
            usuario.Status,
            usuario.Role);

    private static Result<UserDetail>? Validar(UpsertUserRequest requisicao)
    {
        var erros = new List<ResultError>();
        if (string.IsNullOrWhiteSpace(requisicao.Email)) erros.Add(new ResultError("email_obrigatorio", "O e-mail do usuário é obrigatório."));
        if (string.IsNullOrWhiteSpace(requisicao.Username)) erros.Add(new ResultError("username_obrigatorio", "O username do usuário é obrigatório."));
        if (string.IsNullOrWhiteSpace(requisicao.Password)) erros.Add(new ResultError("password_obrigatoria", "A senha do usuário é obrigatória."));
        return erros.Count > 0 ? Result<UserDetail>.Validation(erros) : null;
    }
}
