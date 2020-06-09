-- Criação da base de dados
USE master
GO
CREATE DATABASE SocialPub
GO
-- Criação das tabelas
USE SocialPub
GO

-- ####### ENTIDADES #######

CREATE TABLE Administrador(
	codigo_administrador INTEGER NOT NULL,
	nome varchar(50) NOT NULL,
	palavra_pass varchar(50) NOT NULL,
	email varchar(50) NOT NULL UNIQUE,
	PRIMARY KEY (codigo_administrador),
	CHECK(email LIKE '%@%')
);

CREATE TABLE Utilizador(
	codigo_utilizador INTEGER IDENTITY (1,1) NOT NULL,
	palavra_pass varchar(50) NOT NULL,
	foto varchar(50),
	genero char NOT NULL,
	nome varchar(50) NOT NULL,
	data_nasc DATE NOT NULL,
	contacto VARCHAR(15),
	estado smallint NOT NULL DEFAULT 0, -- (0)Pendente | (1)Ativo | (2)Inativo
	email varchar(50) NOT NULL UNIQUE,
	endereco_pais varchar(50) NOT NULL,
	endereco_localidade varchar(50) NOT NULL,	
	PRIMARY KEY (codigo_utilizador),	
	CHECK(genero IN ('M', 'F')), -- (M)Masculino | (F)Feminino
	CHECK(email LIKE '%@%')
);

CREATE TABLE Grupo(
	codigo_grupo INTEGER IDENTITY (1,1) NOT NULL,
	codigo_utilizador INTEGER NOT NULL,
	imagem VARCHAR(50),
	nome VARCHAR(50) NOT NULL,
	descricao VARCHAR(MAX),
	estado BIT NOT NULL DEFAULT 0, -- (0)Ativo | (1)Inativo
	data_criacao DATETIME NOT NULL,
	PRIMARY KEY (codigo_grupo),
	FOREIGN KEY(codigo_utilizador) REFERENCES Utilizador(codigo_utilizador)
);

CREATE TABLE Privacidade(
	codigo_privacidade SMALLINT DEFAULT 0 NOT NULL, -- (0)Publica: guest e utilizadores veem | (1)Amigos: so amigos veem | (2)Grupo: Apenas elementos do grupo
	descricao VARCHAR(MAX) NOT NULL,
	PRIMARY KEY (codigo_privacidade)
);

CREATE TABLE Arquivo(
	codigo_arquivo INTEGER IDENTITY (1,1) NOT NULL,
	caminho VARCHAR(500) NOT NULL,
	nome varchar(100) NOT NULL,
	formato_arquivo VARCHAR(10) NOT NULL,
	PRIMARY KEY (codigo_arquivo)
);

CREATE TABLE Publicacao(
	codigo_publicacao INTEGER IDENTITY (1,1) NOT NULL,
	codigo_utilizador INTEGER NOT NULL,
	codigo_privacidade SMALLINT NOT NULL,
	conteudo VARCHAR(MAX) NOT NULL,
	estado BIT DEFAULT 0 NOT NULL, -- (0)Ativo | (1)Inativo	
	data_public DATETIME DEFAULT GETDATE() NOT NULL,
	PRIMARY KEY (codigo_publicacao),
	FOREIGN KEY(codigo_utilizador) REFERENCES Utilizador(codigo_utilizador),
	FOREIGN KEY(codigo_privacidade) REFERENCES Privacidade(codigo_privacidade)
);

-- ####### RELACIONAMENTOS #######

-- Administrador <> Utilizador
CREATE TABLE Gerir_Utilizadores(
	data_inicio DATETIME DEFAULT GETDATE() NOT NULL,
	codigo_administrador INTEGER NOT NULL,
	codigo_utilizador INTEGER NOT NULL,	
	data_fim DATETIME,
	descricao_acao varchar(MAX) NOT NULL,
	PRIMARY KEY(data_inicio),
	FOREIGN KEY(codigo_administrador) REFERENCES Administrador(codigo_administrador),
	FOREIGN KEY(codigo_utilizador) REFERENCES Utilizador(codigo_utilizador),
	CHECK(data_fim >= data_inicio)
);

-- Administrador <> Publicacao
CREATE TABLE Gerir_Publicacoes(
	data_inicio DATETIME DEFAULT GETDATE() NOT NULL,
	codigo_publicacao INTEGER NOT NULL,
	codigo_administrador INTEGER NOT NULL,
	data_fim DATETIME,
	descricao VARCHAR(MAX) NOT NULL,
	PRIMARY KEY(data_inicio),
	FOREIGN KEY(codigo_publicacao) REFERENCES Publicacao(codigo_publicacao),
	FOREIGN KEY(codigo_administrador) REFERENCES Administrador(codigo_administrador), 
	CHECK(data_fim >= data_inicio)
);

-- Utilizador <> Publicacao
CREATE TABLE Denunciar_Publicacao(
	data_denuncia DATETIME DEFAULT GETDATE() NOT NULL,
	codigo_utilizador INTEGER NOT NULL,
	codigo_publicacao INTEGER NOT NULL,	
	data_fim DATETIME,
	descricao VARCHAR(MAX) NOT NULL,
	PRIMARY KEY (data_denuncia, codigo_utilizador, codigo_publicacao),
	FOREIGN KEY(codigo_utilizador) REFERENCES Utilizador(codigo_utilizador),
	FOREIGN KEY(codigo_publicacao) REFERENCES Publicacao(codigo_publicacao),
	CHECK(data_fim >= data_denuncia)
);

-- Utilizador <> Utilizador
CREATE TABLE Denunciar_Utilizador(
	data_denuncia DATETIME DEFAULT GETDATE() NOT NULL,
	codigo_utilizador INTEGER NOT NULL,
	codigo_utilizador_denunciado INTEGER NOT NULL,	
	data_fim DATETIME,
	estado BIT DEFAULT 0 NOT NULL, -- (0)Em analise | (1)Resolvida	
	descricao VARCHAR(MAX) NOT NULL,
	PRIMARY KEY (data_denuncia, codigo_utilizador, codigo_utilizador_denunciado),
	FOREIGN KEY(codigo_utilizador) REFERENCES Utilizador(codigo_utilizador),
	FOREIGN KEY(codigo_utilizador_denunciado) REFERENCES Utilizador(codigo_utilizador),
	CHECK(data_fim >= data_denuncia)
);

-- Utilizador <> Publicacao
CREATE TABLE Comentar(
	data_comentario DATETIME DEFAULT GETDATE() NOT NULL,
	codigo_utilizador INTEGER NOT NULL,
	codigo_publicacao INTEGER NOT NULL,	
	conteudo VARCHAR(MAX) NOT NULL,
	PRIMARY KEY (data_comentario, codigo_utilizador, codigo_publicacao),
	FOREIGN KEY(codigo_utilizador) REFERENCES Utilizador(codigo_utilizador),
	FOREIGN KEY(codigo_publicacao) REFERENCES Publicacao(codigo_publicacao)
);

-- Utilizador <> Grupo
CREATE TABLE Pertencer(
	codigo_utilizador INTEGER NOT NULL,
	codigo_grupo INTEGER NOT NULL,	
	PRIMARY KEY (codigo_grupo, codigo_utilizador),
	FOREIGN KEY(codigo_utilizador) REFERENCES Utilizador(codigo_utilizador),
	FOREIGN KEY(codigo_grupo) REFERENCES Grupo(codigo_grupo)
);

-- Utilizador <> Grupo
CREATE TABLE Convidar(
	codigo_utilizador INTEGER NOT NULL,
	codigo_grupo INTEGER NOT NULL,
	data_convite DATETIME DEFAULT GETDATE() NOT NULL,
	estado SMALLINT DEFAULT 0 NOT NULL, -- -- (0) Em analise | (1) Aceite | (2) Nao aceite
	PRIMARY KEY (codigo_grupo, codigo_utilizador),
	FOREIGN KEY(codigo_utilizador) REFERENCES Utilizador(codigo_utilizador),
	FOREIGN KEY(codigo_grupo) REFERENCES Grupo(codigo_grupo)
);

-- Grupo <> Publicacao
CREATE TABLE Conter(
	codigo_publicacao INTEGER NOT NULL,
	codigo_grupo INTEGER NOT NULL,
	data_public DATETIME DEFAULT GETDATE() NOT NULL,
	PRIMARY KEY (codigo_grupo, codigo_publicacao),
	FOREIGN KEY(codigo_publicacao) REFERENCES Publicacao(codigo_publicacao),
	FOREIGN KEY(codigo_grupo) REFERENCES Grupo(codigo_grupo)
);

-- Publicacao <> Arquivo
CREATE TABLE Anexar(
	codigo_arquivo INTEGER NOT NULL,
	codigo_publicacao INTEGER NOT NULL,
	PRIMARY KEY (codigo_publicacao),
	FOREIGN KEY(codigo_arquivo) REFERENCES Arquivo(codigo_arquivo),
	FOREIGN KEY(codigo_publicacao) REFERENCES Publicacao(codigo_publicacao)
);

INSERT INTO Privacidade(codigo_privacidade, descricao) VALUES(0,'Público')
INSERT INTO Privacidade(codigo_privacidade, descricao) VALUES(1,'Amigos')
INSERT INTO Privacidade(codigo_privacidade, descricao) VALUES(2,'Grupo')
