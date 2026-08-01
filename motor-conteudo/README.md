# motor-conteudo

Motor de Conteúdo do Forja: extração e chunking de PDFs de edital/lei —
alimenta o pipeline de embeddings/RAG do backend em `/src` (Forja.Api).

Projeto Python independente do backend C#, gerenciado por [uv](https://docs.astral.sh/uv/),
sem nenhuma referência ao código do repositório principal além de viverem
lado a lado na mesma raiz.

## Escopo desta fase

Só o núcleo puro (`motor_conteudo/core/`): parsing de PDF e chunking de
texto, sem I/O de banco, rede ou variável de ambiente. Uma camada de I/O
(ler arquivo, falar com banco/fila) é responsabilidade de uma fase
posterior, ainda não construída.

- `core/parsing.py` — extrai texto de um PDF (bytes já lidos por quem chama).
- `core/chunking.py` — divide o texto em chunks por artigo/tópico (não por
  tamanho fixo de caractere).

## Rodando

```bash
uv sync                 # instala dependências (runtime + dev)
uv run pytest           # roda os testes
uv run mypy src tests   # verificação estática de tipos (modo strict)
```

## Decisões de design

- **pdfplumber** em vez de pypdf pra extração de texto: extração consciente
  de layout, mais confiável pra preservar a ordem de leitura em documentos
  jurídicos com cabeçalho/rodapé/coluna — o chunking depende de detectar
  marcadores de artigo/tópico em sequência correta.
- **mypy** em vez de pyright: pure-Python, sem dependência de Node,
  integra limpo com `uv add --dev` e é o padrão mais usado em CI pra
  bibliotecas Python headless como esta.
- **Chunking por artigo/tópico, não por tamanho fixo**: mesma decisão já
  usada no protótipo anterior — um corte por tamanho fixo quebraria um
  artigo no meio, ruim tanto pra apresentação quanto pra qualidade de
  embeddings/RAG.

## Fixtures de teste

`tests/fixtures/*.pdf` são gerados por `tests/fixtures/gerar_fixtures.py`
(não roda como parte da suíte — só uma vez, manualmente, se precisar
regenerar). Conteúdo:

- `lei_8112_arts_1_a_5.pdf`: texto verídico dos Art. 1º a 5º da Lei nº
  8.112/1990 (Regime Jurídico dos Servidores Públicos Civis da União).
- `edital_exemplo.pdf`: estrutura e linguagem representativas de um edital
  de concurso público real (não é cópia literal de um edital específico).
