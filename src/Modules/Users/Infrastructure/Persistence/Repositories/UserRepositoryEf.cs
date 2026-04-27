using Ambev.DeveloperEvaluation.ORM.Persistence;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Ambev.DeveloperEvaluation.Users.Application.Common;
using Ambev.DeveloperEvaluation.Users.Application.Contracts;
using Ambev.DeveloperEvaluation.Users.Application.Repositories;
using Ambev.DeveloperEvaluation.Users.Domain.Entities;
using Ambev.DeveloperEvaluation.Users.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.Users.Infrastructure.Persistence.Repositories;

public sealed class UserRepositoryEf : IUserRepository
{
    private readonly DeveloperEvaluationDbContext _context;

    public UserRepositoryEf(DeveloperEvaluationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> ObterPorIdAsync(long usuarioId, CancellationToken cancellationToken)
    {
        var entidade = await _context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == usuarioId, cancellationToken);
        return entidade is null ? null : MapearDominio(entidade);
    }

    public async Task<IReadOnlyCollection<User>> ListarAtivosAsync(CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(item => item.Status == "Active")
            .Select(item => MapearDominio(item))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PagedResult<User>> ListarAsync(UserListFilter filtro, CancellationToken cancellationToken)
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
        var data = await consulta.Skip((page - 1) * size).Take(size).Select(item => MapearDominio(item)).ToArrayAsync(cancellationToken);
        return new PagedResult<User>(data, totalItems, page, totalPages);
    }

    public async Task<User> AdicionarAsync(User usuario, CancellationToken cancellationToken)
    {
        var entidade = new UserEntity();
        Aplicar(entidade, usuario);
        _context.Users.Add(entidade);
        await _context.SaveChangesAsync(cancellationToken);
        return MapearDominio(entidade);
    }

    public async Task AtualizarAsync(User usuario, CancellationToken cancellationToken)
    {
        var entidade = await _context.Users.SingleOrDefaultAsync(item => item.Id == usuario.Id, cancellationToken)
            ?? throw new InvalidOperationException("O usuário informado não foi encontrado para persistência.");

        Aplicar(entidade, usuario);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoverAsync(long usuarioId, CancellationToken cancellationToken)
    {
        var entidade = await _context.Users.SingleOrDefaultAsync(item => item.Id == usuarioId, cancellationToken)
            ?? throw new InvalidOperationException("O usuário informado não foi encontrado para persistência.");

        _context.Users.Remove(entidade);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void Aplicar(UserEntity entidade, User usuario)
    {
        entidade.Email = usuario.Email;
        entidade.Username = usuario.Username;
        entidade.Password = usuario.Password;
        entidade.Firstname = usuario.Name.Firstname;
        entidade.Lastname = usuario.Name.Lastname;
        entidade.City = usuario.Address.City;
        entidade.Street = usuario.Address.Street;
        entidade.Number = usuario.Address.Number;
        entidade.Zipcode = usuario.Address.Zipcode;
        entidade.GeoLat = usuario.Address.Geolocation.Lat;
        entidade.GeoLong = usuario.Address.Geolocation.Long;
        entidade.Phone = usuario.Phone;
        entidade.Status = usuario.Status;
        entidade.Role = usuario.Role;
    }

    private static User MapearDominio(UserEntity entidade)
    {
        return User.Reidratar(
            entidade.Id,
            entidade.Email,
            entidade.Username,
            entidade.Password,
            new UserName(entidade.Firstname, entidade.Lastname),
            new UserAddress(entidade.City, entidade.Street, entidade.Number, entidade.Zipcode, new UserGeolocation(entidade.GeoLat, entidade.GeoLong)),
            entidade.Phone,
            entidade.Status,
            entidade.Role);
    }
}
