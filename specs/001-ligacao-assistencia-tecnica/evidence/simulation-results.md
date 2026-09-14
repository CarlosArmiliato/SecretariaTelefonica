# Evidência de simulação

- Comando: `dotnet build Joana.sln` — código 0.
- Comando: `dotnet run --project tests/Joana.Acceptance -- --suite all --mode Simulation` — código 0; resultado sintético redigido com `passed: true`.
- Comando: `dotnet run --project src/Joana.Desktop -- --mode Production --diagnostic` — bloqueado com `ProductionUnavailable`.

Nenhuma ligação, gravação de áudio, integração de rede ou dado pessoal foi usado.
