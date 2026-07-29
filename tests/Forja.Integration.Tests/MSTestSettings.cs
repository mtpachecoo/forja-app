// Testes de integração compartilham um único container Postgres (via IntegrationTestBase) e limpam
// o banco a cada teste — rodar em paralelo causaria truncates concorrentes entre testes.
[assembly: DoNotParallelize]
