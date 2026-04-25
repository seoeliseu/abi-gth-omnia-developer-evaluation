namespace Ambev.DeveloperEvaluation.Application.Products.Contracts;

public sealed record ProductReference(
    long Id,
    string Titulo,
    decimal Preco,
    string Categoria,
    bool Ativo);