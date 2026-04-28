using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Ambev.DeveloperEvaluation.Users.Application.Common;
using Ambev.DeveloperEvaluation.Users.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Persistence.Services;

public sealed class UsersServicePersistente : IUsersService
{
    private readonly DeveloperEvaluationDbContext _context;
    private readonly IPasswordSecurityService _passwordSecurityService;

    public UsersServicePersistente(DeveloperEvaluationDbContext context, IPasswordSecurityService passwordSecurityService)
    {
        _context = context;
        _passwordSecurityService = passwordSecurityService;
    }

    public async Task<Result<UserReference>> ObterPorIdAsync(long usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == usuarioId, cancellationToken);
        return usuario is null
            ? Result<UserReference>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")])
            : Result<UserReference>.Success(MapearReferencia(usuario));
    }

    public async Task<Result<IReadOnlyCollection<UserReference>>> ListarAtivosAsync(CancellationToken cancellationToken)
    {
        var usuarios = await _context.Users.AsNoTracking().Where(item => item.Status == "Active").Select(item => MapearReferencia(item)).ToArrayAsync(cancellationToken);
        return Result<IReadOnlyCollection<UserReference>>.Success(usuarios);
    }

    public async Task<Result<PagedResult<UserDetail>>> ListarAsync(UserListFilter filtro, CancellationToken cancellationToken)
    {
        var consulta = _context.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro.Username)) consulta = consulta.Where(item => item.Username.Contains(filtro.Username));
        if (!string.IsNullOrWhiteSpace(filtro.Email)) consulta = consulta.Where(item => item.Email.Contains(filtro.Email));
        if (!string.IsNullOrWhiteSpace(filtro.Role)) consulta = consulta.Where(item => item.Role == filtro.Role);
        if (!string.IsNullOrWhiteSpace(filtro.Status)) consulta = consulta.Where(item => item.Status == filtro.Status);

        consulta = (filtro.Order ?? string.Empty).ToLowerInvariant() switch
        {
            var valor when valor.Contains("email desc") => consulta.OrderByDescending(item => item.Email).ThenBy(item => item.Username),
            var valor when valor.Contains("email") => consulta.OrderBy(item => item.Email).ThenBy(item => item.Username),
            var valor when valor.Contains("username desc") => consulta.OrderByDescending(item => item.Username),
            _ => consulta.OrderBy(item => item.Username)
        };

        var page = filtro.Page <= 0 ? 1 : filtro.Page;
        var size = filtro.Size <= 0 ? 10 : Math.Min(filtro.Size, 100);
        var totalItems = await consulta.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)size);
        var data = await consulta.Skip((page - 1) * size).Take(size).Select(item => MapearDetalhe(item)).ToArrayAsync(cancellationToken);
        return Result<PagedResult<UserDetail>>.Success(new PagedResult<UserDetail>(data, totalItems, page, totalPages));
    }

    public async Task<Result<UserDetail>> ObterDetalhePorIdAsync(long usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == usuarioId, cancellationToken);
        return usuario is null
            ? Result<UserDetail>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")])
            : Result<UserDetail>.Success(MapearDetalhe(usuario));
    }

    public async Task<Result<UserDetail>> CriarAsync(UpsertUserRequest requisicao, CancellationToken cancellationToken)
    {
        var validacao = Validar(requisicao);
        if (validacao is not null) return validacao;

        var entidade = MapearEntidade(0, requisicao);
        _context.Users.Add(entidade);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<UserDetail>.Success(MapearDetalhe(entidade));
    }

    public async Task<Result<UserDetail>> AtualizarAsync(long usuarioId, UpsertUserRequest requisicao, CancellationToken cancellationToken)
    {
        var usuario = await _context.Users.SingleOrDefaultAsync(item => item.Id == usuarioId, cancellationToken);
        if (usuario is null) return Result<UserDetail>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")]);
        var validacao = Validar(requisicao);
        if (validacao is not null) return validacao;

        Aplicar(usuario, requisicao);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<UserDetail>.Success(MapearDetalhe(usuario));
    }

    public async Task<Result<UserDetail>> RemoverAsync(long usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _context.Users.SingleOrDefaultAsync(item => item.Id == usuarioId, cancellationToken);
        if (usuario is null) return Result<UserDetail>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")]);
        _context.Users.Remove(usuario);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<UserDetail>.Success(MapearDetalhe(usuario));
    }

    private static UserReference MapearReferencia(UserEntity usuario) => new(usuario.Id, usuario.Username, usuario.Email, usuario.Status, usuario.Role, usuario.Status == "Active");
    private static UserDetail MapearDetalhe(UserEntity usuario) => new(usuario.Id, usuario.Email, usuario.Username, new UserNameData(usuario.Firstname, usuario.Lastname), new UserAddressData(usuario.City, usuario.Street, usuario.Number, usuario.Zipcode, new UserGeolocationData(usuario.GeoLat, usuario.GeoLong)), usuario.Phone, usuario.Status, usuario.Role);
    private UserEntity MapearEntidade(long id, UpsertUserRequest requisicao)
    {
        var entidade = new UserEntity();
        if (id > 0) entidade.Id = id;
        Aplicar(entidade, requisicao);
        return entidade;
    }

    private void Aplicar(UserEntity entidade, UpsertUserRequest requisicao)
    {
        entidade.Email = requisicao.Email;
        entidade.Username = requisicao.Username;
        entidade.Password = _passwordSecurityService.HashPassword(requisicao.Password);
        entidade.Firstname = requisicao.Name.Firstname;
        entidade.Lastname = requisicao.Name.Lastname;
        entidade.City = requisicao.Address.City;
        entidade.Street = requisicao.Address.Street;
        entidade.Number = requisicao.Address.Number;
        entidade.Zipcode = requisicao.Address.Zipcode;
        entidade.GeoLat = requisicao.Address.Geolocation.Lat;
        entidade.GeoLong = requisicao.Address.Geolocation.Long;
        entidade.Phone = requisicao.Phone;
        entidade.Status = requisicao.Status;
        entidade.Role = requisicao.Role;
    }
    private static Result<UserDetail>? Validar(UpsertUserRequest requisicao)
    {
        var erros = new List<ResultError>();
        if (string.IsNullOrWhiteSpace(requisicao.Email)) erros.Add(new ResultError("email_obrigatorio", "O e-mail do usuário é obrigatório."));
        if (string.IsNullOrWhiteSpace(requisicao.Username)) erros.Add(new ResultError("username_obrigatorio", "O username do usuário é obrigatório."));
        if (string.IsNullOrWhiteSpace(requisicao.Password)) erros.Add(new ResultError("password_obrigatoria", "A senha do usuário é obrigatória."));
        return erros.Count > 0 ? Result<UserDetail>.Validation(erros) : null;
    }
}