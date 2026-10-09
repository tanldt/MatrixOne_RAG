CREATE DATABASE IF NOT EXISTS ticketshine_rag;
USE ticketshine_rag;

CREATE TABLE IF NOT EXISTS documents (
    id BIGINT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    source_path VARCHAR(500) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS document_chunks (
    id BIGINT PRIMARY KEY,
    document_id BIGINT NOT NULL,
    document_name VARCHAR(255) NOT NULL,
    chunk_index INT NOT NULL,
    heading VARCHAR(255),
    content TEXT NOT NULL,
    embedding VECF32(1536),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
