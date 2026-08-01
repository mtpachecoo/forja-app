"""Camada de I/O do Motor de Conteúdo: Postgres (Neon) e Voyage AI.

Diferente de ``core/``, aqui existe efeito colateral de verdade (rede, banco,
variável de ambiente) — é exatamente por isso que fica isolado num pacote
separado, nunca importado por ``core/``.
"""
