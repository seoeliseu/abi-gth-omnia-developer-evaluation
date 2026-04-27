namespace Ambev.DeveloperEvaluation.Products.Application.Contracts;

public sealed record ProductReference(
    long Id,
    string Titulo,
    decimal Preco,
    string Categoria,
    bool Ativo);
