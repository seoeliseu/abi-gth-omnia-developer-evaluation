# Padrão de Desenvolvimento da Aplicação

## Objetivo

Centralizar as regras de desenvolvimento da aplicação para manter consistência de código, nomenclatura e organização ao longo das fases do projeto.

## Regras

### 1. Idioma de nomenclatura

- Nomes de entidades podem permanecer em inglês, alinhados ao domínio e ao template base.
- Métodos, variáveis, propriedades locais, parâmetros, comentários e nomes auxiliares devem ser escritos em português.
- A nomenclatura deve priorizar clareza e intenção explícita, evitando abreviações desnecessárias.

### 2. Organização de DTOs por arquivo

- Nunca deixar um DTO no mesmo arquivo de outra classe não relacionada.
- Cada DTO deve possuir arquivo próprio quando fizer parte do contrato normal da aplicação.
- Se um DTO precisar de outros objetos internos de apoio, eles só podem permanecer no mesmo arquivo quando forem usados exclusivamente de forma interna naquele contexto.
- Nesses casos, os objetos auxiliares devem ser criados como classes aninhadas, evitando múltiplas classes soltas no mesmo arquivo.

### 3. Limpeza de arquivos gerados por template

- Sempre que um projeto novo for criado, remover imediatamente arquivos descartáveis gerados pelo template.
- Exemplos explícitos: `WeatherForecast`, `WeatherForecastController`, `*.http` de exemplo, `UnitTest1` e `Class1`.
- Nenhum placeholder de template deve permanecer versionado no repositório.

## Observação

Este documento deve evoluir sempre que novas convenções de desenvolvimento forem definidas durante o projeto.