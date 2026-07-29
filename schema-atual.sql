--
-- PostgreSQL database dump
--

-- Dumped from database version 17.10 (4f20678)
-- Dumped by pg_dump version 17.10 (Debian 17.10-1.pgdg13+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: neon_auth; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA neon_auth;


--
-- Name: vector; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA public;


--
-- Name: EXTENSION vector; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION vector IS 'vector data type and ivfflat and hnsw access methods';


--
-- Name: nivel_usuario; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public.nivel_usuario AS ENUM (
    'iniciante',
    'intermediario',
    'avancado'
);


--
-- Name: origem_questao; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public.origem_questao AS ENUM (
    'gerada_ia',
    'reproduzida_prova_oficial',
    'inedita'
);


--
-- Name: status_item_plano; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public.status_item_plano AS ENUM (
    'pendente',
    'concluido'
);


--
-- Name: status_questao; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public.status_questao AS ENUM (
    'rascunho',
    'em_revisao',
    'aprovada',
    'rejeitada'
);


--
-- Name: tipo_fonte; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public.tipo_fonte AS ENUM (
    'lei',
    'edital',
    'prova'
);


--
-- Name: tipo_questao; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public.tipo_questao AS ENUM (
    'certo_errado',
    'multipla_escolha'
);


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: account; Type: TABLE; Schema: neon_auth; Owner: -
--

CREATE TABLE neon_auth.account (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    "accountId" text NOT NULL,
    "providerId" text NOT NULL,
    "userId" uuid NOT NULL,
    "accessToken" text,
    "refreshToken" text,
    "idToken" text,
    "accessTokenExpiresAt" timestamp with time zone,
    "refreshTokenExpiresAt" timestamp with time zone,
    scope text,
    password text,
    "createdAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp with time zone NOT NULL
);


--
-- Name: invitation; Type: TABLE; Schema: neon_auth; Owner: -
--

CREATE TABLE neon_auth.invitation (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    "organizationId" uuid NOT NULL,
    email text NOT NULL,
    role text,
    status text NOT NULL,
    "expiresAt" timestamp with time zone NOT NULL,
    "createdAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "inviterId" uuid NOT NULL
);


--
-- Name: jwks; Type: TABLE; Schema: neon_auth; Owner: -
--

CREATE TABLE neon_auth.jwks (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    "publicKey" text NOT NULL,
    "privateKey" text NOT NULL,
    "createdAt" timestamp with time zone NOT NULL,
    "expiresAt" timestamp with time zone
);


--
-- Name: member; Type: TABLE; Schema: neon_auth; Owner: -
--

CREATE TABLE neon_auth.member (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    "organizationId" uuid NOT NULL,
    "userId" uuid NOT NULL,
    role text NOT NULL,
    "createdAt" timestamp with time zone NOT NULL
);


--
-- Name: organization; Type: TABLE; Schema: neon_auth; Owner: -
--

CREATE TABLE neon_auth.organization (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name text NOT NULL,
    slug text NOT NULL,
    logo text,
    "createdAt" timestamp with time zone NOT NULL,
    metadata text
);


--
-- Name: project_config; Type: TABLE; Schema: neon_auth; Owner: -
--

CREATE TABLE neon_auth.project_config (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name text NOT NULL,
    endpoint_id text NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    trusted_origins jsonb NOT NULL,
    social_providers jsonb NOT NULL,
    email_provider jsonb,
    email_and_password jsonb,
    allow_localhost boolean NOT NULL,
    plugin_configs jsonb,
    webhook_config jsonb
);


--
-- Name: session; Type: TABLE; Schema: neon_auth; Owner: -
--

CREATE TABLE neon_auth.session (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    "expiresAt" timestamp with time zone NOT NULL,
    token text NOT NULL,
    "createdAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp with time zone NOT NULL,
    "ipAddress" text,
    "userAgent" text,
    "userId" uuid NOT NULL,
    "impersonatedBy" text,
    "activeOrganizationId" text
);


--
-- Name: user; Type: TABLE; Schema: neon_auth; Owner: -
--

CREATE TABLE neon_auth."user" (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name text NOT NULL,
    email text NOT NULL,
    "emailVerified" boolean NOT NULL,
    image text,
    "createdAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    role text,
    banned boolean,
    "banReason" text,
    "banExpires" timestamp with time zone
);


--
-- Name: verification; Type: TABLE; Schema: neon_auth; Owner: -
--

CREATE TABLE neon_auth.verification (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    identifier text NOT NULL,
    value text NOT NULL,
    "expiresAt" timestamp with time zone NOT NULL,
    "createdAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: bancas; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.bancas (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    nome character varying(255) NOT NULL,
    criado_em timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: carreiras; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.carreiras (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    nome character varying(255) NOT NULL,
    orgao character varying(255) NOT NULL,
    cargo character varying(255),
    ativo_no_catalogo boolean DEFAULT true NOT NULL,
    criado_em timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: chunks_conteudo; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.chunks_conteudo (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    fonte_id uuid NOT NULL,
    topico_id uuid,
    texto text NOT NULL,
    embedding public.vector(1024) NOT NULL,
    criado_em timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: disciplinas; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.disciplinas (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    nome character varying(255) NOT NULL
);


--
-- Name: editais; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.editais (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    carreira_id uuid NOT NULL,
    banca_id uuid NOT NULL,
    ano integer NOT NULL,
    url_fonte text,
    criado_em timestamp with time zone DEFAULT now() NOT NULL,
    data_prova date
);


--
-- Name: edital_peso_disciplina; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.edital_peso_disciplina (
    edital_id uuid NOT NULL,
    disciplina_id uuid NOT NULL,
    peso numeric NOT NULL,
    fonte text NOT NULL,
    edital_origem_id uuid,
    calculado_em timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: fontes_conteudo; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.fontes_conteudo (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tipo public.tipo_fonte NOT NULL,
    edital_id uuid,
    titulo character varying(500) NOT NULL,
    caminho_arquivo text,
    texto_extraido text NOT NULL,
    versao_em timestamp with time zone DEFAULT now() NOT NULL,
    criado_em timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: medalhas; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.medalhas (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    nome character varying(255) NOT NULL,
    criterio text NOT NULL
);


--
-- Name: plano_itens; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.plano_itens (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    plano_id uuid NOT NULL,
    topico_id uuid NOT NULL,
    ordem integer NOT NULL,
    tempo_alocado_min integer NOT NULL,
    data_prevista date,
    status public.status_item_plano DEFAULT 'pendente'::public.status_item_plano NOT NULL
);


--
-- Name: planos_estudo; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.planos_estudo (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    usuario_id uuid NOT NULL,
    carreira_id uuid NOT NULL,
    gerado_via_ia boolean DEFAULT true NOT NULL,
    criado_em timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: pomodoros; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pomodoros (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    sessao_id uuid NOT NULL,
    iniciado_em timestamp with time zone DEFAULT now() NOT NULL,
    finalizado_em timestamp with time zone,
    duracao_prevista_min integer DEFAULT 25 NOT NULL,
    qtd_respostas_no_ciclo integer DEFAULT 0 NOT NULL,
    pontos_concedidos integer DEFAULT 0 NOT NULL
);


--
-- Name: pontuacoes; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.pontuacoes (
    usuario_id uuid NOT NULL,
    pontos_total integer DEFAULT 0 NOT NULL,
    pontos_semana_atual integer DEFAULT 0 NOT NULL,
    semana_referencia date DEFAULT date_trunc('week'::text, (CURRENT_DATE)::timestamp with time zone) NOT NULL
);


--
-- Name: questoes; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.questoes (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    carreira_id uuid NOT NULL,
    banca_id uuid,
    disciplina_id uuid NOT NULL,
    topico_id uuid,
    tipo public.tipo_questao NOT NULL,
    enunciado text NOT NULL,
    alternativas jsonb,
    gabarito text NOT NULL,
    explicacao text NOT NULL,
    fontes_chunk_ids uuid[] NOT NULL,
    status public.status_questao DEFAULT 'rascunho'::public.status_questao NOT NULL,
    revisado_por uuid,
    revisado_em timestamp with time zone,
    criado_em timestamp with time zone DEFAULT now() NOT NULL,
    origem public.origem_questao DEFAULT 'reproduzida_prova_oficial'::public.origem_questao NOT NULL,
    fonte_prova_id uuid,
    ano integer,
    orgao character varying(255),
    CONSTRAINT chk_alternativas_multipla CHECK ((((tipo = 'multipla_escolha'::public.tipo_questao) AND (alternativas IS NOT NULL)) OR ((tipo = 'certo_errado'::public.tipo_questao) AND (alternativas IS NULL))))
);


--
-- Name: respostas_usuario; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.respostas_usuario (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    usuario_id uuid NOT NULL,
    questao_id uuid NOT NULL,
    pomodoro_id uuid,
    resposta_dada text NOT NULL,
    correta boolean NOT NULL,
    tempo_resposta_ms integer NOT NULL,
    pontuada boolean DEFAULT false NOT NULL,
    pontos_concedidos integer DEFAULT 0 NOT NULL,
    eh_revisao boolean DEFAULT false NOT NULL,
    criado_em timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT respostas_usuario_tempo_resposta_ms_check CHECK ((tempo_resposta_ms >= 0))
);


--
-- Name: revisao_espacada; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.revisao_espacada (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    usuario_id uuid NOT NULL,
    questao_id uuid NOT NULL,
    erros_consecutivos integer DEFAULT 0 NOT NULL,
    intervalo_dias_atual integer DEFAULT 1 NOT NULL,
    proxima_revisao_em date NOT NULL
);


--
-- Name: sessoes_estudo; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sessoes_estudo (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    usuario_id uuid NOT NULL,
    data_sessao date DEFAULT CURRENT_DATE NOT NULL,
    criado_em timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: streaks; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.streaks (
    usuario_id uuid NOT NULL,
    dias_consecutivos integer DEFAULT 0 NOT NULL,
    ultima_atividade_em date
);


--
-- Name: topicos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.topicos (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    edital_id uuid NOT NULL,
    disciplina_id uuid NOT NULL,
    nome character varying(500) NOT NULL,
    ordem integer DEFAULT 0 NOT NULL
);


--
-- Name: usuario_medalhas; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.usuario_medalhas (
    usuario_id uuid NOT NULL,
    medalha_id uuid NOT NULL,
    conquistada_em timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: usuarios; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.usuarios (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    email character varying(255) NOT NULL,
    nome character varying(255) NOT NULL,
    nivel public.nivel_usuario NOT NULL,
    tempo_disponivel_min_dia integer NOT NULL,
    fuso_horario character varying(64) DEFAULT 'America/Sao_Paulo'::character varying NOT NULL,
    criado_em timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT usuarios_tempo_disponivel_min_dia_check CHECK ((tempo_disponivel_min_dia > 0))
);


--
-- Name: account account_pkey; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.account
    ADD CONSTRAINT account_pkey PRIMARY KEY (id);


--
-- Name: invitation invitation_pkey; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.invitation
    ADD CONSTRAINT invitation_pkey PRIMARY KEY (id);


--
-- Name: jwks jwks_pkey; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.jwks
    ADD CONSTRAINT jwks_pkey PRIMARY KEY (id);


--
-- Name: member member_pkey; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.member
    ADD CONSTRAINT member_pkey PRIMARY KEY (id);


--
-- Name: organization organization_pkey; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.organization
    ADD CONSTRAINT organization_pkey PRIMARY KEY (id);


--
-- Name: organization organization_slug_key; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.organization
    ADD CONSTRAINT organization_slug_key UNIQUE (slug);


--
-- Name: project_config project_config_endpoint_id_key; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.project_config
    ADD CONSTRAINT project_config_endpoint_id_key UNIQUE (endpoint_id);


--
-- Name: project_config project_config_pkey; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.project_config
    ADD CONSTRAINT project_config_pkey PRIMARY KEY (id);


--
-- Name: session session_pkey; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.session
    ADD CONSTRAINT session_pkey PRIMARY KEY (id);


--
-- Name: session session_token_key; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.session
    ADD CONSTRAINT session_token_key UNIQUE (token);


--
-- Name: user user_email_key; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth."user"
    ADD CONSTRAINT user_email_key UNIQUE (email);


--
-- Name: user user_pkey; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth."user"
    ADD CONSTRAINT user_pkey PRIMARY KEY (id);


--
-- Name: verification verification_pkey; Type: CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.verification
    ADD CONSTRAINT verification_pkey PRIMARY KEY (id);


--
-- Name: bancas bancas_nome_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.bancas
    ADD CONSTRAINT bancas_nome_key UNIQUE (nome);


--
-- Name: bancas bancas_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.bancas
    ADD CONSTRAINT bancas_pkey PRIMARY KEY (id);


--
-- Name: carreiras carreiras_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.carreiras
    ADD CONSTRAINT carreiras_pkey PRIMARY KEY (id);


--
-- Name: chunks_conteudo chunks_conteudo_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.chunks_conteudo
    ADD CONSTRAINT chunks_conteudo_pkey PRIMARY KEY (id);


--
-- Name: disciplinas disciplinas_nome_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.disciplinas
    ADD CONSTRAINT disciplinas_nome_key UNIQUE (nome);


--
-- Name: disciplinas disciplinas_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.disciplinas
    ADD CONSTRAINT disciplinas_pkey PRIMARY KEY (id);


--
-- Name: editais editais_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.editais
    ADD CONSTRAINT editais_pkey PRIMARY KEY (id);


--
-- Name: edital_peso_disciplina edital_peso_disciplina_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.edital_peso_disciplina
    ADD CONSTRAINT edital_peso_disciplina_pkey PRIMARY KEY (edital_id, disciplina_id);


--
-- Name: fontes_conteudo fontes_conteudo_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.fontes_conteudo
    ADD CONSTRAINT fontes_conteudo_pkey PRIMARY KEY (id);


--
-- Name: medalhas medalhas_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medalhas
    ADD CONSTRAINT medalhas_pkey PRIMARY KEY (id);


--
-- Name: plano_itens plano_itens_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.plano_itens
    ADD CONSTRAINT plano_itens_pkey PRIMARY KEY (id);


--
-- Name: planos_estudo planos_estudo_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.planos_estudo
    ADD CONSTRAINT planos_estudo_pkey PRIMARY KEY (id);


--
-- Name: pomodoros pomodoros_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pomodoros
    ADD CONSTRAINT pomodoros_pkey PRIMARY KEY (id);


--
-- Name: pontuacoes pontuacoes_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pontuacoes
    ADD CONSTRAINT pontuacoes_pkey PRIMARY KEY (usuario_id);


--
-- Name: questoes questoes_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.questoes
    ADD CONSTRAINT questoes_pkey PRIMARY KEY (id);


--
-- Name: respostas_usuario respostas_usuario_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.respostas_usuario
    ADD CONSTRAINT respostas_usuario_pkey PRIMARY KEY (id);


--
-- Name: revisao_espacada revisao_espacada_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.revisao_espacada
    ADD CONSTRAINT revisao_espacada_pkey PRIMARY KEY (id);


--
-- Name: revisao_espacada revisao_espacada_usuario_id_questao_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.revisao_espacada
    ADD CONSTRAINT revisao_espacada_usuario_id_questao_id_key UNIQUE (usuario_id, questao_id);


--
-- Name: sessoes_estudo sessoes_estudo_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sessoes_estudo
    ADD CONSTRAINT sessoes_estudo_pkey PRIMARY KEY (id);


--
-- Name: streaks streaks_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.streaks
    ADD CONSTRAINT streaks_pkey PRIMARY KEY (usuario_id);


--
-- Name: topicos topicos_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.topicos
    ADD CONSTRAINT topicos_pkey PRIMARY KEY (id);


--
-- Name: usuario_medalhas usuario_medalhas_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuario_medalhas
    ADD CONSTRAINT usuario_medalhas_pkey PRIMARY KEY (usuario_id, medalha_id);


--
-- Name: usuarios usuarios_email_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_email_key UNIQUE (email);


--
-- Name: usuarios usuarios_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_pkey PRIMARY KEY (id);


--
-- Name: account_userId_idx; Type: INDEX; Schema: neon_auth; Owner: -
--

CREATE INDEX "account_userId_idx" ON neon_auth.account USING btree ("userId");


--
-- Name: invitation_email_idx; Type: INDEX; Schema: neon_auth; Owner: -
--

CREATE INDEX invitation_email_idx ON neon_auth.invitation USING btree (email);


--
-- Name: invitation_organizationId_idx; Type: INDEX; Schema: neon_auth; Owner: -
--

CREATE INDEX "invitation_organizationId_idx" ON neon_auth.invitation USING btree ("organizationId");


--
-- Name: member_organizationId_idx; Type: INDEX; Schema: neon_auth; Owner: -
--

CREATE INDEX "member_organizationId_idx" ON neon_auth.member USING btree ("organizationId");


--
-- Name: member_userId_idx; Type: INDEX; Schema: neon_auth; Owner: -
--

CREATE INDEX "member_userId_idx" ON neon_auth.member USING btree ("userId");


--
-- Name: organization_slug_uidx; Type: INDEX; Schema: neon_auth; Owner: -
--

CREATE UNIQUE INDEX organization_slug_uidx ON neon_auth.organization USING btree (slug);


--
-- Name: session_userId_idx; Type: INDEX; Schema: neon_auth; Owner: -
--

CREATE INDEX "session_userId_idx" ON neon_auth.session USING btree ("userId");


--
-- Name: verification_identifier_idx; Type: INDEX; Schema: neon_auth; Owner: -
--

CREATE INDEX verification_identifier_idx ON neon_auth.verification USING btree (identifier);


--
-- Name: idx_chunks_embedding; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_chunks_embedding ON public.chunks_conteudo USING hnsw (embedding public.vector_cosine_ops);


--
-- Name: idx_chunks_topico; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_chunks_topico ON public.chunks_conteudo USING btree (topico_id);


--
-- Name: idx_editais_banca; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_editais_banca ON public.editais USING btree (banca_id);


--
-- Name: idx_editais_carreira; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_editais_carreira ON public.editais USING btree (carreira_id);


--
-- Name: idx_plano_itens_plano; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_plano_itens_plano ON public.plano_itens USING btree (plano_id);


--
-- Name: idx_pomodoros_sessao; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_pomodoros_sessao ON public.pomodoros USING btree (sessao_id);


--
-- Name: idx_questoes_filtro; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_questoes_filtro ON public.questoes USING btree (carreira_id, banca_id, disciplina_id, status);


--
-- Name: idx_respostas_usuario_questao; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_respostas_usuario_questao ON public.respostas_usuario USING btree (usuario_id, questao_id);


--
-- Name: idx_revisao_pendente; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_revisao_pendente ON public.revisao_espacada USING btree (usuario_id, proxima_revisao_em);


--
-- Name: idx_sessoes_usuario_data; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_sessoes_usuario_data ON public.sessoes_estudo USING btree (usuario_id, data_sessao);


--
-- Name: idx_topicos_disciplina; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_topicos_disciplina ON public.topicos USING btree (disciplina_id);


--
-- Name: idx_topicos_edital; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_topicos_edital ON public.topicos USING btree (edital_id);


--
-- Name: account account_userId_fkey; Type: FK CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.account
    ADD CONSTRAINT "account_userId_fkey" FOREIGN KEY ("userId") REFERENCES neon_auth."user"(id) ON DELETE CASCADE;


--
-- Name: invitation invitation_inviterId_fkey; Type: FK CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.invitation
    ADD CONSTRAINT "invitation_inviterId_fkey" FOREIGN KEY ("inviterId") REFERENCES neon_auth."user"(id) ON DELETE CASCADE;


--
-- Name: invitation invitation_organizationId_fkey; Type: FK CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.invitation
    ADD CONSTRAINT "invitation_organizationId_fkey" FOREIGN KEY ("organizationId") REFERENCES neon_auth.organization(id) ON DELETE CASCADE;


--
-- Name: member member_organizationId_fkey; Type: FK CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.member
    ADD CONSTRAINT "member_organizationId_fkey" FOREIGN KEY ("organizationId") REFERENCES neon_auth.organization(id) ON DELETE CASCADE;


--
-- Name: member member_userId_fkey; Type: FK CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.member
    ADD CONSTRAINT "member_userId_fkey" FOREIGN KEY ("userId") REFERENCES neon_auth."user"(id) ON DELETE CASCADE;


--
-- Name: session session_userId_fkey; Type: FK CONSTRAINT; Schema: neon_auth; Owner: -
--

ALTER TABLE ONLY neon_auth.session
    ADD CONSTRAINT "session_userId_fkey" FOREIGN KEY ("userId") REFERENCES neon_auth."user"(id) ON DELETE CASCADE;


--
-- Name: chunks_conteudo chunks_conteudo_fonte_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.chunks_conteudo
    ADD CONSTRAINT chunks_conteudo_fonte_id_fkey FOREIGN KEY (fonte_id) REFERENCES public.fontes_conteudo(id) ON DELETE CASCADE;


--
-- Name: chunks_conteudo chunks_conteudo_topico_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.chunks_conteudo
    ADD CONSTRAINT chunks_conteudo_topico_id_fkey FOREIGN KEY (topico_id) REFERENCES public.topicos(id) ON DELETE SET NULL;


--
-- Name: editais editais_banca_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.editais
    ADD CONSTRAINT editais_banca_id_fkey FOREIGN KEY (banca_id) REFERENCES public.bancas(id) ON DELETE RESTRICT;


--
-- Name: editais editais_carreira_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.editais
    ADD CONSTRAINT editais_carreira_id_fkey FOREIGN KEY (carreira_id) REFERENCES public.carreiras(id) ON DELETE RESTRICT;


--
-- Name: edital_peso_disciplina edital_peso_disciplina_disciplina_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.edital_peso_disciplina
    ADD CONSTRAINT edital_peso_disciplina_disciplina_id_fkey FOREIGN KEY (disciplina_id) REFERENCES public.disciplinas(id);


--
-- Name: edital_peso_disciplina edital_peso_disciplina_edital_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.edital_peso_disciplina
    ADD CONSTRAINT edital_peso_disciplina_edital_id_fkey FOREIGN KEY (edital_id) REFERENCES public.editais(id);


--
-- Name: edital_peso_disciplina edital_peso_disciplina_edital_origem_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.edital_peso_disciplina
    ADD CONSTRAINT edital_peso_disciplina_edital_origem_id_fkey FOREIGN KEY (edital_origem_id) REFERENCES public.editais(id);


--
-- Name: usuarios fk_usuarios_neon_auth; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT fk_usuarios_neon_auth FOREIGN KEY (id) REFERENCES neon_auth."user"(id) ON DELETE CASCADE;


--
-- Name: fontes_conteudo fontes_conteudo_edital_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.fontes_conteudo
    ADD CONSTRAINT fontes_conteudo_edital_id_fkey FOREIGN KEY (edital_id) REFERENCES public.editais(id) ON DELETE SET NULL;


--
-- Name: plano_itens plano_itens_plano_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.plano_itens
    ADD CONSTRAINT plano_itens_plano_id_fkey FOREIGN KEY (plano_id) REFERENCES public.planos_estudo(id) ON DELETE CASCADE;


--
-- Name: plano_itens plano_itens_topico_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.plano_itens
    ADD CONSTRAINT plano_itens_topico_id_fkey FOREIGN KEY (topico_id) REFERENCES public.topicos(id) ON DELETE RESTRICT;


--
-- Name: planos_estudo planos_estudo_carreira_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.planos_estudo
    ADD CONSTRAINT planos_estudo_carreira_id_fkey FOREIGN KEY (carreira_id) REFERENCES public.carreiras(id) ON DELETE RESTRICT;


--
-- Name: planos_estudo planos_estudo_usuario_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.planos_estudo
    ADD CONSTRAINT planos_estudo_usuario_id_fkey FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE CASCADE;


--
-- Name: pomodoros pomodoros_sessao_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pomodoros
    ADD CONSTRAINT pomodoros_sessao_id_fkey FOREIGN KEY (sessao_id) REFERENCES public.sessoes_estudo(id) ON DELETE CASCADE;


--
-- Name: pontuacoes pontuacoes_usuario_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.pontuacoes
    ADD CONSTRAINT pontuacoes_usuario_id_fkey FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE CASCADE;


--
-- Name: questoes questoes_banca_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.questoes
    ADD CONSTRAINT questoes_banca_id_fkey FOREIGN KEY (banca_id) REFERENCES public.bancas(id) ON DELETE SET NULL;


--
-- Name: questoes questoes_carreira_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.questoes
    ADD CONSTRAINT questoes_carreira_id_fkey FOREIGN KEY (carreira_id) REFERENCES public.carreiras(id) ON DELETE RESTRICT;


--
-- Name: questoes questoes_disciplina_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.questoes
    ADD CONSTRAINT questoes_disciplina_id_fkey FOREIGN KEY (disciplina_id) REFERENCES public.disciplinas(id) ON DELETE RESTRICT;


--
-- Name: questoes questoes_fonte_prova_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.questoes
    ADD CONSTRAINT questoes_fonte_prova_id_fkey FOREIGN KEY (fonte_prova_id) REFERENCES public.fontes_conteudo(id) ON DELETE SET NULL;


--
-- Name: questoes questoes_revisado_por_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.questoes
    ADD CONSTRAINT questoes_revisado_por_fkey FOREIGN KEY (revisado_por) REFERENCES public.usuarios(id);


--
-- Name: questoes questoes_topico_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.questoes
    ADD CONSTRAINT questoes_topico_id_fkey FOREIGN KEY (topico_id) REFERENCES public.topicos(id) ON DELETE SET NULL;


--
-- Name: respostas_usuario respostas_usuario_pomodoro_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.respostas_usuario
    ADD CONSTRAINT respostas_usuario_pomodoro_id_fkey FOREIGN KEY (pomodoro_id) REFERENCES public.pomodoros(id) ON DELETE SET NULL;


--
-- Name: respostas_usuario respostas_usuario_questao_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.respostas_usuario
    ADD CONSTRAINT respostas_usuario_questao_id_fkey FOREIGN KEY (questao_id) REFERENCES public.questoes(id) ON DELETE CASCADE;


--
-- Name: respostas_usuario respostas_usuario_usuario_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.respostas_usuario
    ADD CONSTRAINT respostas_usuario_usuario_id_fkey FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE CASCADE;


--
-- Name: revisao_espacada revisao_espacada_questao_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.revisao_espacada
    ADD CONSTRAINT revisao_espacada_questao_id_fkey FOREIGN KEY (questao_id) REFERENCES public.questoes(id) ON DELETE CASCADE;


--
-- Name: revisao_espacada revisao_espacada_usuario_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.revisao_espacada
    ADD CONSTRAINT revisao_espacada_usuario_id_fkey FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE CASCADE;


--
-- Name: sessoes_estudo sessoes_estudo_usuario_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sessoes_estudo
    ADD CONSTRAINT sessoes_estudo_usuario_id_fkey FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE CASCADE;


--
-- Name: streaks streaks_usuario_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.streaks
    ADD CONSTRAINT streaks_usuario_id_fkey FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE CASCADE;


--
-- Name: topicos topicos_disciplina_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.topicos
    ADD CONSTRAINT topicos_disciplina_id_fkey FOREIGN KEY (disciplina_id) REFERENCES public.disciplinas(id) ON DELETE RESTRICT;


--
-- Name: topicos topicos_edital_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.topicos
    ADD CONSTRAINT topicos_edital_id_fkey FOREIGN KEY (edital_id) REFERENCES public.editais(id) ON DELETE CASCADE;


--
-- Name: usuario_medalhas usuario_medalhas_medalha_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuario_medalhas
    ADD CONSTRAINT usuario_medalhas_medalha_id_fkey FOREIGN KEY (medalha_id) REFERENCES public.medalhas(id) ON DELETE CASCADE;


--
-- Name: usuario_medalhas usuario_medalhas_usuario_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuario_medalhas
    ADD CONSTRAINT usuario_medalhas_usuario_id_fkey FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

